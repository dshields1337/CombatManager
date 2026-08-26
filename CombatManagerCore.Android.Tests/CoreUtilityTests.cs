using CombatManager;
using ScottsUtils;
using System.Text.RegularExpressions;
using System.Text;

namespace CombatManagerCore.Android.Tests;

[TestClass]
public sealed class CoreUtilityTests
{
    [TestMethod]
    public void ClampRestrictsValuesToInclusiveBounds()
    {
        Assert.AreEqual(1, 0.Clamp(1, 10));
        Assert.AreEqual(5, 5.Clamp(1, 10));
        Assert.AreEqual(10, 11.Clamp(1, 10));
    }

    [TestMethod]
    public void DecommaMovesTextAfterCommaToFront()
    {
        Assert.AreEqual("Fire Goblin", CMStringUtilities.DecommaText("Goblin, Fire"));
        Assert.AreEqual("Goblin", CMStringUtilities.DecommaText("Goblin"));
    }

    [TestMethod]
    public void RomanNumeralsRoundTripRepresentativeValues()
    {
        int[] values = [0, 1, 4, 9, 14, 49, 3999];

        foreach (int value in values)
        {
            string roman = RomanNumbers.NumberToRoman(value);
            Assert.AreEqual(value, RomanNumbers.RomanToNumber(roman), $"Failed for {roman}");
        }
    }

    [TestMethod]
    public void CoinParsesAndCalculatesGoldValue()
    {
        var coin = new Coin("2 pp 3 gp 4 sp 5 cp");

        Assert.AreEqual(2, coin.PP);
        Assert.AreEqual(3, coin.GP);
        Assert.AreEqual(4, coin.SP);
        Assert.AreEqual(5, coin.CP);
        Assert.AreEqual(23.45m, coin.GPValue);
        Assert.AreEqual("2 pp 3 gp 4 sp 5 cp", coin.ToString());
    }

    [TestMethod]
    public void SizeChangesClampToKnownRange()
    {
        Assert.AreEqual(MonsterSize.Fine, SizeMods.ChangeSize(MonsterSize.Tiny, -20));
        Assert.AreEqual(MonsterSize.Colossal, SizeMods.ChangeSize(MonsterSize.Large, 20));
        Assert.AreEqual(0, SizeMods.StepsFromMedium(MonsterSize.Medium));
    }

    [TestMethod]
    public void DieRollParsesCompoundExpression()
    {
        DieRoll roll = DieRoll.FromString("2d6+1d4+3");

        Assert.IsNotNull(roll);
        Assert.AreEqual("2d6+1d4+3", roll.Text);
        Assert.AreEqual(3, roll.TotalCount);
        Assert.AreEqual(19, roll.Max);
    }

    [TestMethod]
    public void DieRollProducesResultsInsideExpectedRange()
    {
        var roll = new DieRoll(2, 6, 3);

        RollResult result = roll.Roll();

        Assert.HasCount(2, result.Rolls);
        Assert.AreEqual(3, result.Mod);
        Assert.IsTrue(result.Total >= 5 && result.Total <= 15);
        Assert.IsTrue(result.Rolls.All(item => item.Result >= 1 && item.Result <= item.Die));
    }

    [TestMethod]
    public void ConditionBonusCloneIsIndependent()
    {
        var original = new ConditionBonus { Str = 2, AC = -1, LoseDex = true };
        var clone = (ConditionBonus)original.Clone();

        clone.Str = 5;

        Assert.AreEqual(2, original.Str);
        Assert.AreEqual(5, clone.Str);
        Assert.AreEqual(-1, clone.AC);
        Assert.IsTrue(clone.LoseDex);
    }

    [TestMethod]
    public void SkillValueParsesSubtypeAndFormatsModifier()
    {
        var skill = new SkillValue("Knowledge (Arcana)") { Mod = 7 };

        Assert.AreEqual("Knowledge", skill.Name);
        Assert.AreEqual("Arcana", skill.Subtype);
        Assert.AreEqual("Knowledge (Arcana)", skill.FullName);
        Assert.AreEqual("Knowledge (Arcana) +7", skill.Text);
    }

    [TestMethod]
    public void SpecialAbilityMapsTypeIndexAndClones()
    {
        var ability = new SpecialAbility
        {
            Name = "Frightful Presence",
            AbilityTypeIndex = 2,
            Text = "Nearby creatures may become frightened.",
            ConstructionPoints = 3
        };

        var clone = (SpecialAbility)ability.Clone();

        Assert.AreEqual("Su", ability.Type);
        Assert.AreEqual(2, ability.AbilityTypeIndex);
        Assert.AreEqual(ability.Name, clone.Name);
        Assert.AreEqual(ability.Text, clone.Text);
        Assert.AreEqual(ability.ConstructionPoints, clone.ConstructionPoints);
    }

    [TestMethod]
    public void CharacterClassNamesRoundTripCaseInsensitively()
    {
        Assert.AreEqual("Arcane Archer", CharacterClass.GetName(CharacterClassEnum.ArcaneArcher));
        Assert.AreEqual(CharacterClassEnum.ArcaneArcher, CharacterClass.GetEnum("arcane archer"));
    }

    [TestMethod]
    public void CreatureTypeCalculatesBaseAttackAndSaves()
    {
        CreatureTypeInfo outsider = CreatureTypeInfo.GetInfo("outsider");

        Assert.AreEqual(10, outsider.GetBAB(10));
        Assert.AreEqual(7, CreatureTypeInfo.GetSave(good: true, hd: 10));
        Assert.AreEqual(3, CreatureTypeInfo.GetSave(good: false, hd: 10));
        Assert.IsTrue(outsider.IsClassSkill("Any Skill"));
    }

    [TestMethod]
    public void SourceAliasesResolveToCanonicalSource()
    {
        Assert.AreEqual("Pathfinder Core Rulebook", SourceInfo.GetSource("PF Core"));
        Assert.AreEqual(SourceType.Core, SourceInfo.GetSourceType("PFRPG CORE"));
        Assert.AreEqual(SourceType.Other, SourceInfo.GetSourceType("Unknown Test Source"));
    }

    [TestMethod]
    public void AttackParsesFormatsAndResolvesSeededWeapon()
    {
        var longsword = new Weapon
        {
            Name = "longsword",
            Plural = "longswords",
            Hands = "One-Handed",
            Class = "Martial"
        };
        Weapon.SetWeapons([longsword]);
        WeaponSpecialAbility.SetSpecialAbilities([]);

        const string text = "longsword +7/+2 (1d8+4/19-20)";
        Match match = Regex.Match(text, Attack.RegexString(null), RegexOptions.IgnoreCase);

        Assert.IsTrue(match.Success);
        Attack attack = Attack.ParseAttack(match);
        Assert.AreSame(longsword, attack.Weapon);
        Assert.AreEqual("1d8+4", attack.Damage.Text);
        Assert.AreEqual(19, attack.CritRange);
        CollectionAssert.AreEqual(new[] { 7, 2 }, attack.Bonus);
        Assert.AreEqual(text, attack.Text);
    }

    [TestMethod]
    public void AttackSetCountsHandsAndClonesAttacks()
    {
        var greatsword = new Weapon { Name = "greatsword", Hands = "Two-Handed", Class = "Martial" };
        var attack = new Attack(1, "greatsword", 5, new DieRoll(2, 6, 3), null) { Weapon = greatsword };
        var set = new AttackSet { WeaponAttacks = [attack] };

        var clone = (AttackSet)set.Clone();

        Assert.AreEqual(2, set.Hands);
        Assert.AreNotSame(set.WeaponAttacks[0], clone.WeaponAttacks[0]);
        Assert.AreEqual(set.ToString(), clone.ToString());
    }

    [TestMethod]
    public void AfflictionParsesWithoutMonsterDependency()
    {
        var ability = new SpecialAbility
        {
            Name = "Poison",
            Text = "injury; save Fort DC 12; frequency 1/day for 6 days; effect 1d2 Strength damage; cure 1 save"
        };

        Affliction affliction = Affliction.FromSpecialAbility("Giant Wasp", ability);

        Assert.IsNotNull(affliction);
        Assert.AreEqual("Giant Wasp Poison", affliction.Name);
        Assert.AreEqual(12, affliction.Save);
        Assert.IsFalse(affliction.Once);
        Assert.AreEqual(1, affliction.Frequency);
        Assert.AreEqual("day", affliction.FrequencyUnit);
        Assert.AreEqual(6, affliction.Limit);
        Assert.AreEqual("day", affliction.LimitUnit);
        Assert.AreEqual("1d2", affliction.DamageDie.Text);
        Assert.AreEqual("Strength", affliction.DamageType);
    }

    [TestMethod]
    public void AfflictionFormatsSecondaryDamageAndClonesDice()
    {
        var affliction = new Affliction
        {
            Type = "Disease",
            Cause = "contact",
            SaveType = "Fort",
            Save = 14,
            Once = true,
            DamageDie = new DieRoll(1, 3, 0),
            DamageType = "Strength",
            SecondaryDamageDie = new DieRoll(1, 4, 0),
            SecondaryDamageType = "Constitution",
            Cure = "2 saves"
        };

        var clone = (Affliction)affliction.Clone();

        StringAssert.Contains(affliction.Text, "1d3 Strength and 1d4 Constitution");
        Assert.AreNotSame(affliction.DamageDie, clone.DamageDie);
        Assert.AreNotSame(affliction.SecondaryDamageDie, clone.SecondaryDamageDie);
    }

    [TestMethod]
    public void InitiativeCountSortsByBaseDexThenTiebreaker()
    {
        var lowerDex = new InitiativeCount(15, 2, 10);
        var higherDex = new InitiativeCount(15, 4, 1);
        var higherBase = new InitiativeCount(16, 0, 0);

        Assert.IsTrue(lowerDex < higherDex);
        Assert.IsTrue(higherDex < higherBase);
        Assert.AreEqual("15-4-1", higherDex.Text);
        Assert.AreEqual(higherDex, (InitiativeCount)higherDex.Clone());
    }

    [TestMethod]
    public void CreatureSummariesLoadAndSortFromBestiaryXml()
    {
        const string xml = """
            <ArrayOfMonster>
              <Monster><Name>Zombie</Name><CR>1/2</CR><HP>12</HP><Type>undead</Type><id>2</id></Monster>
              <Monster><Name>Aboleth</Name><CR>7</CR><HP>84</HP><Type>aberration</Type><id>1</id></Monster>
            </ArrayOfMonster>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        List<CreatureSummary> creatures = CreatureSummary.Load(stream);

        Assert.HasCount(2, creatures);
        Assert.AreEqual("Aboleth", creatures[0].Name);
        Assert.AreEqual("Aboleth  •  CR 7", creatures[0].ListText);
        Assert.AreEqual(84, creatures[0].HP);
        Assert.AreEqual("undead", creatures[1].Type);
    }

    [TestMethod]
    public void CreatureSummarySetsCombineSortAndRemoveDuplicateIds()
    {
        CreatureSummary[] first =
        [
            new() { Id = 1, Name = "Zombie" },
            new() { Id = 2, Name = "Aboleth" }
        ];
        CreatureSummary[] second =
        [
            new() { Id = 1, Name = "Duplicate Zombie" },
            new() { Id = 3, Name = "Goblin" }
        ];

        List<CreatureSummary> creatures = CreatureSummary.Combine(first, second);

        Assert.HasCount(3, creatures);
        Assert.AreEqual("Aboleth", creatures[0].Name);
        Assert.AreEqual("Goblin", creatures[1].Name);
        Assert.AreEqual("Zombie", creatures[2].Name);
    }

    [TestMethod]
    public void CreatureFiltersCombineSearchTypeAndChallengeRating()
    {
        CreatureSummary[] creatures =
        [
            new() { Name = "Goblin", Type = "humanoid", CR = "1/3" },
            new() { Name = "Goblin Dog", Type = "animal", CR = "1" },
            new() { Name = "Orc", Type = "humanoid", CR = "1/3" }
        ];

        List<CreatureSummary> filtered = CreatureSummary.Filter(creatures, "goblin", "humanoid", "1/3");

        Assert.HasCount(1, filtered);
        Assert.AreEqual("Goblin", filtered[0].Name);
        Assert.IsLessThan(CreatureSummary.ChallengeRatingValue("1"), CreatureSummary.ChallengeRatingValue("1/2"));
    }

    [TestMethod]
    public void CreatureDetailsFindsOneFullRecordById()
    {
        const string xml = """
            <ArrayOfMonster>
              <Monster><Name>Goblin</Name><Description>Wrong record</Description><id>1</id></Monster>
              <Monster><Name>Aboleth</Name><AbilityScores>Str 20, Dex 12</AbilityScores><SpecialAbilities>Mucus Cloud</SpecialAbilities><Description>Ancient aquatic creature.</Description><id>21</id></Monster>
            </ArrayOfMonster>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        CreatureDetails details = CreatureDetails.Find(stream, 21);

        Assert.IsNotNull(details);
        Assert.AreEqual("Aboleth", details.Name);
        Assert.AreEqual("Str 20, Dex 12", details.AbilityScores);
        Assert.AreEqual("Mucus Cloud", details.SpecialAbilities);
    }

    [TestMethod]
    public void FeatsLoadSortAndFilterByCommaSeparatedType()
    {
        const string xml = """
            <ArrayOfFeat>
              <Feat><Id>2</Id><Name>Power Attack</Name><Type>Combat</Type><Summary>Trade accuracy for damage.</Summary><Prerequistites>Str 13</Prerequistites></Feat>
              <Feat><Id>1</Id><Name>Acrobatic</Name><Type>General, Teamwork</Type><Summary>Improve movement skills.</Summary></Feat>
            </ArrayOfFeat>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        List<FeatSummary> feats = FeatSummary.Load(stream);
        List<FeatSummary> filtered = FeatSummary.Filter(feats, "movement", "Teamwork");

        Assert.AreEqual("Acrobatic", feats[0].Name);
        Assert.HasCount(1, filtered);
        Assert.AreEqual(1, filtered[0].Id);
        Assert.AreEqual("Str 13", feats[1].Prerequisites);
    }

    [TestMethod]
    public void SpellsLoadFilterAndFindFullDetails()
    {
        const string shortXml = "<ArrayOfSpell><Spell><name>Acid Arrow</name><school>conjuration</school><spell_level>wizard 2</spell_level><short_description>Acid damage.</short_description><id>1</id></Spell></ArrayOfSpell>";
        const string fullXml = "<ArrayOfSpell><Spell><name>Acid Arrow</name><casting_time>1 action</casting_time><range>long</range><description>An arrow of acid.</description><id>1</id></Spell></ArrayOfSpell>";
        using var shortStream = new MemoryStream(Encoding.UTF8.GetBytes(shortXml));
        using var fullStream = new MemoryStream(Encoding.UTF8.GetBytes(fullXml));

        List<SpellSummary> spells = SpellSummary.Load(shortStream);
        SpellDetails details = SpellDetails.Find(fullStream, 1);

        Assert.HasCount(1, SpellSummary.Filter(spells, "acid", "conjuration"));
        Assert.AreEqual("wizard 2", spells[0].Levels);
        Assert.AreEqual("An arrow of acid.", details.Description);
    }

    [TestMethod]
    public void RulesLoadFilterAndFindFullDetails()
    {
        const string shortXml = "<ArrayOfRule><Rule><ID>5</ID><Name>Grapple</Name><Source>Core</Source><Type>Combat Maneuvers</Type><Format>grapple</Format></Rule></ArrayOfRule>";
        const string fullXml = "<ArrayOfRuleDetails><RuleDetails><ID>5</ID><Details>Make a combat maneuver check. &lt;b&gt;Move&lt;/b&gt;</Details></RuleDetails></ArrayOfRuleDetails>";
        using var shortStream = new MemoryStream(Encoding.UTF8.GetBytes(shortXml));
        using var fullStream = new MemoryStream(Encoding.UTF8.GetBytes(fullXml));

        List<RuleSummary> rules = RuleSummary.Load(shortStream);
        RuleDetails details = RuleDetails.Find(fullStream, 5);

        Assert.HasCount(1, RuleSummary.Filter(rules, "grapple", "Combat Maneuvers"));
        Assert.AreEqual("Core", rules[0].Source);
        Assert.AreEqual("Make a combat maneuver check. Move", details.Details);
    }
}
