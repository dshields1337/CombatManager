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
              <Monster><Name>Zombie</Name><CR>1/2</CR><HP>12</HP><Type>undead</Type><Init>-1</Init><id>2</id></Monster>
              <Monster><Name>Aboleth</Name><CR>7</CR><HP>84</HP><Type>aberration</Type><Init>5</Init><id>1</id></Monster>
            </ArrayOfMonster>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        List<CreatureSummary> creatures = CreatureSummary.Load(stream);

        Assert.HasCount(2, creatures);
        Assert.AreEqual("Aboleth", creatures[0].Name);
        Assert.AreEqual("Aboleth  •  CR 7", creatures[0].ListText);
        Assert.AreEqual(84, creatures[0].HP);
        Assert.AreEqual(5, creatures[0].InitiativeModifier);
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

    [TestMethod]
    public void MagicItemsLoadFilterAndFindFullDetails()
    {
        const string shortXml = "<ArrayOfMagicItem><MagicItem><Name>Dagger of Venom</Name><CL>5</CL><Group>Weapon</Group><Source>Core</Source><BaseMagicItem>+1 dagger</BaseMagicItem><id>13</id></MagicItem></ArrayOfMagicItem>";
        const string fullXml = "<ArrayOfMagicItemDetails><MagicItemDetails><ID>13</ID><Aura>faint necromancy</Aura><Price>8,302 gp</Price><Description>Poisonous dagger.</Description><Mythic>0</Mythic><LegendaryWeapon>0</LegendaryWeapon></MagicItemDetails></ArrayOfMagicItemDetails>";
        using var shortStream = new MemoryStream(Encoding.UTF8.GetBytes(shortXml));
        using var fullStream = new MemoryStream(Encoding.UTF8.GetBytes(fullXml));

        List<MagicItemSummary> items = MagicItemSummary.Load(shortStream);
        MagicItemDetails details = MagicItemDetails.Find(fullStream, 13);

        Assert.HasCount(1, MagicItemSummary.Filter(items, "dagger", "Weapon"));
        Assert.AreEqual("+1 dagger", items[0].BaseMagicItem);
        Assert.AreEqual("8,302 gp", details.Price);
        Assert.AreEqual("Poisonous dagger.", details.Description);
    }

    [TestMethod]
    public void CombatRosterAddsNamesRemovesAndClearsCreatures()
    {
        var roster = new CombatRoster();
        var goblin = new CreatureSummary { Id = 7, Name = "Goblin", CR = "1/3", HP = 6 };

        CombatParticipant first = roster.Add(goblin);
        CombatParticipant second = roster.Add(goblin);

        Assert.AreEqual("Goblin", first.DisplayName);
        Assert.AreEqual("Goblin 2", second.DisplayName);
        Assert.AreEqual(6, second.CurrentHP);
        Assert.IsTrue(roster.Remove(first.Sequence));
        Assert.HasCount(1, roster.Participants);
        roster.Clear();
        Assert.IsEmpty(roster.Participants);
        Assert.AreEqual("Goblin", roster.Add(goblin).DisplayName);
    }

    [TestMethod]
    public void CombatRosterOrdersInitiativeAndTracksRounds()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin" });
        CombatParticipant dragon = roster.Add(new CreatureSummary { Id = 8, Name = "Dragon" });
        CombatParticipant ogre = roster.Add(new CreatureSummary { Id = 9, Name = "Ogre" });

        Assert.IsTrue(roster.SetInitiative(goblin.Sequence, 12));
        Assert.IsTrue(roster.SetInitiative(dragon.Sequence, 20));
        Assert.IsTrue(roster.SetInitiative(ogre.Sequence, 12));
        Assert.AreEqual("Dragon", roster.Participants[0].DisplayName);
        Assert.AreEqual("Goblin", roster.Participants[1].DisplayName);
        Assert.AreEqual("Ogre", roster.Participants[2].DisplayName);
        Assert.AreEqual("Dragon", roster.NextTurn().DisplayName);
        Assert.AreEqual(1, roster.Round);
        Assert.AreEqual("Goblin", roster.NextTurn().DisplayName);
        Assert.AreEqual("Ogre", roster.NextTurn().DisplayName);
        Assert.AreEqual("Dragon", roster.NextTurn().DisplayName);
        Assert.AreEqual(2, roster.Round);
        Assert.AreEqual("Ogre", roster.PreviousTurn().DisplayName);
        Assert.AreEqual(1, roster.Round);
    }

    [TestMethod]
    public void CombatRosterSetsMultipleInitiativesAtomically()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin" });
        CombatParticipant dragon = roster.Add(new CreatureSummary { Id = 8, Name = "Dragon" });
        Assert.IsFalse(roster.SetInitiatives(new Dictionary<int, int> { [999] = 30 }));
        Assert.IsNull(goblin.Initiative);
        Assert.IsNull(dragon.Initiative);
        Assert.IsTrue(roster.SetInitiatives(new Dictionary<int, int> { [goblin.Sequence] = 12, [dragon.Sequence] = 21 }));
        Assert.AreEqual("Dragon", roster.Participants[0].DisplayName);
        Assert.AreEqual(12, goblin.Initiative);
        Assert.AreEqual(21, dragon.Initiative);
    }

    [TestMethod]
    public void CombatRosterRollsAllInitiativeWithModifiers()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin", InitiativeModifier = 6 });
        CombatParticipant zombie = roster.Add(new CreatureSummary { Id = 8, Name = "Zombie", InitiativeModifier = -1 });
        CombatParticipant player = roster.AddManual("Valeros", 30, 4);
        int[] rolls = [12, 8, 15];
        int rollIndex = 0;
        Assert.AreEqual(3, roster.RollInitiatives(() => rolls[rollIndex++]));
        Assert.AreEqual(18, goblin.Initiative);
        Assert.AreEqual(7, zombie.Initiative);
        Assert.AreEqual(19, player.Initiative);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => roster.RollInitiatives(() => 21));
        using var stream = new MemoryStream();
        roster.Save(stream);
        stream.Position = 0;
        Assert.IsTrue(CombatRoster.TryLoad(stream, out CombatRoster restored));
        Assert.AreEqual(6, restored.Participants.Single(item => item.Name == "Goblin").InitiativeModifier);
        Assert.AreEqual(-1, restored.Participants.Single(item => item.Name == "Zombie").InitiativeModifier);
        Assert.AreEqual(4, restored.Participants.Single(item => item.Name == "Valeros").InitiativeModifier);
    }

    [TestMethod]
    public void CombatRosterAppliesDamageAndCapsHealingAtMaximumHP()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin", HP = 6 });

        Assert.IsTrue(roster.ApplyDamage(goblin.Sequence, 8));
        Assert.AreEqual(-2, goblin.CurrentHP);
        Assert.IsTrue(goblin.IsDefeated);
        Assert.IsTrue(roster.ApplyHealing(goblin.Sequence, 3));
        Assert.AreEqual(1, goblin.CurrentHP);
        Assert.IsFalse(goblin.IsDefeated);
        Assert.IsTrue(roster.ApplyHealing(goblin.Sequence, 20));
        Assert.AreEqual(6, goblin.CurrentHP);
        Assert.IsFalse(roster.ApplyDamage(goblin.Sequence, -1));
    }

    [TestMethod]
    public void CombatRosterConsumesAndPersistsTemporaryHpBeforeCurrentHp()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin", HP = 10 });
        Assert.IsTrue(roster.SetTemporaryHp(goblin.Sequence, 5));
        Assert.IsTrue(roster.ApplyDamage(goblin.Sequence, 3));
        Assert.AreEqual(2, goblin.TemporaryHP);
        Assert.AreEqual(10, goblin.CurrentHP);
        Assert.IsTrue(roster.ApplyDamage(goblin.Sequence, 6));
        Assert.AreEqual(0, goblin.TemporaryHP);
        Assert.AreEqual(6, goblin.CurrentHP);
        Assert.IsTrue(roster.ApplyHealing(goblin.Sequence, 2));
        Assert.AreEqual(8, goblin.CurrentHP);
        Assert.AreEqual(0, goblin.TemporaryHP);
        Assert.IsFalse(roster.SetTemporaryHp(goblin.Sequence, -1));
        roster.SetTemporaryHp(goblin.Sequence, 4);
        using var stream = new MemoryStream();
        roster.Save(stream);
        stream.Position = 0;
        Assert.IsTrue(CombatRoster.TryLoad(stream, out CombatRoster restored));
        Assert.AreEqual(4, restored.Participants[0].TemporaryHP);
    }

    [TestMethod]
    public void CombatRosterNamesPersistAppearInSummariesAndClearWithEncounter()
    {
        var roster = new CombatRoster();
        roster.SetEncounterName("  Vault ambush  ");
        roster.AddManual("Valeros", 30);
        StringAssert.StartsWith(roster.ToSummaryText(), "Vault ambush");
        using var stream = new MemoryStream();
        roster.Save(stream);
        stream.Position = 0;
        Assert.IsTrue(CombatRoster.TryLoad(stream, out CombatRoster restored));
        Assert.AreEqual("Vault ambush", restored.EncounterName);
        restored.Clear();
        Assert.AreEqual(string.Empty, restored.EncounterName);
        StringAssert.StartsWith(restored.ToSummaryText(), "Combat Manager Encounter");
    }

    [TestMethod]
    public void SavedCharacterLibraryCreatesEditsDeletesAndPersistsTemplates()
    {
        var library = new SavedCharacterLibrary();
        SavedCharacter valeros = library.Add("  Valeros  ", 30, 5, "  Human fighter  ");
        SavedCharacter kyra = library.Add("Kyra", 24, 2, string.Empty);
        Assert.AreEqual("Kyra", library.Characters[0].Name);
        Assert.AreEqual("Valeros", library.Characters[1].Name);
        Assert.IsTrue(library.Update(valeros.Id, "Valeros the Brave", 34, 6, "Sword and shield"));
        Assert.IsFalse(library.Update(999, "Missing", 1, 0, string.Empty));
        Assert.IsTrue(library.Remove(kyra.Id));
        Assert.IsFalse(library.Remove(kyra.Id));
        using var stream = new MemoryStream();
        library.Save(stream);
        stream.Position = 0;
        Assert.IsTrue(SavedCharacterLibrary.TryLoad(stream, out SavedCharacterLibrary restored));
        Assert.HasCount(1, restored.Characters);
        SavedCharacter saved = restored.Characters[0];
        Assert.AreEqual("Valeros the Brave", saved.Name);
        Assert.AreEqual(34, saved.MaximumHP);
        Assert.AreEqual(6, saved.InitiativeModifier);
        Assert.AreEqual("Sword and shield", saved.Notes);
        Assert.IsGreaterThan(saved.Id, restored.Add("Merisiel", 26, 7, null).Id);
    }

    [TestMethod]
    public void CombatRosterAddsAndPersistsIndependentSavedCharacterCopies()
    {
        var template = new SavedCharacter { Id = 12, Name = "Valeros", MaximumHP = 30, InitiativeModifier = 5, Notes = "Human fighter" };
        var roster = new CombatRoster();
        CombatParticipant participant = roster.AddSavedCharacter(template);
        Assert.AreEqual(12, participant.SavedCharacterId);
        Assert.IsTrue(participant.IsSavedCharacter);
        Assert.AreEqual(5, participant.InitiativeModifier);
        Assert.AreEqual("Human fighter", participant.Notes);
        template.Name = "Changed template";
        Assert.AreEqual("Valeros", participant.Name);
        using var stream = new MemoryStream();
        roster.Save(stream);
        stream.Position = 0;
        Assert.IsTrue(CombatRoster.TryLoad(stream, out CombatRoster restored));
        Assert.AreEqual(12, restored.Participants[0].SavedCharacterId);
        Assert.AreEqual("Valeros", restored.Participants[0].Name);
    }

    [TestMethod]
    public void SavedEncounterLibraryCreatesUpdatesRenamesDeletesAndPersistsSnapshots()
    {
        var library = new SavedEncounterLibrary();
        SavedEncounter vault = library.Add("  Vault ambush  ", "<CombatRoster version=\"1\" round=\"0\" />");
        SavedEncounter bridge = library.Add("Bridge", "snapshot two");
        Assert.AreEqual("Bridge", library.Encounters[0].Name);
        Assert.IsTrue(library.Update(vault.Id, "Vault finale", "updated snapshot"));
        Assert.IsTrue(library.Rename(bridge.Id, "Old Bridge"));
        Assert.IsFalse(library.Rename(999, "Missing"));
        Assert.IsTrue(library.Remove(bridge.Id));
        using var stream = new MemoryStream();
        library.Save(stream);
        stream.Position = 0;
        Assert.IsTrue(SavedEncounterLibrary.TryLoad(stream, out SavedEncounterLibrary restored));
        Assert.HasCount(1, restored.Encounters);
        Assert.AreEqual("Vault finale", restored.Encounters[0].Name);
        Assert.AreEqual("updated snapshot", restored.Encounters[0].Snapshot);
        Assert.IsGreaterThan(restored.Encounters[0].Id, restored.Add("Next", "next snapshot").Id);
    }

    [TestMethod]
    public void SavedEncounterLibraryRejectsCorruptData()
    {
        using var corrupt = new MemoryStream(Encoding.UTF8.GetBytes("not encounters"));
        Assert.IsFalse(SavedEncounterLibrary.TryLoad(corrupt, out SavedEncounterLibrary fallback));
        Assert.IsEmpty(fallback.Encounters);
    }

    [TestMethod]
    public void CombatRosterPersistsFullEncounterAndRejectsCorruptData()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin", CR = "1/3", HP = 6 });
        roster.SetInitiative(goblin.Sequence, 12);
        roster.ApplyDamage(goblin.Sequence, 8);
        roster.NextTurn();
        using var stream = new MemoryStream();
        roster.Save(stream);
        stream.Position = 0;

        Assert.IsTrue(CombatRoster.TryLoad(stream, out CombatRoster restored));
        Assert.HasCount(1, restored.Participants);
        Assert.AreEqual(-2, restored.Participants[0].CurrentHP);
        Assert.AreEqual(12, restored.Participants[0].Initiative);
        Assert.AreEqual("Goblin", restored.ActiveParticipant.DisplayName);
        Assert.AreEqual(1, restored.Round);

        using var corrupt = new MemoryStream(Encoding.UTF8.GetBytes("not an encounter"));
        Assert.IsFalse(CombatRoster.TryLoad(corrupt, out CombatRoster fallback));
        Assert.IsEmpty(fallback.Participants);
    }

    [TestMethod]
    public void CombatRosterAddsAndPersistsManualCombatants()
    {
        var roster = new CombatRoster();
        CombatParticipant first = roster.AddManual("  Valeros  ", 24, 3);
        CombatParticipant second = roster.AddManual("valeros", 30);
        Assert.AreEqual("Valeros", first.DisplayName);
        Assert.AreEqual("valeros 2", second.DisplayName);
        Assert.AreEqual(24, first.CurrentHP);
        Assert.IsTrue(first.IsManual);
        Assert.AreEqual(3, first.InitiativeModifier);

        using var stream = new MemoryStream();
        roster.Save(stream);
        stream.Position = 0;
        Assert.IsTrue(CombatRoster.TryLoad(stream, out CombatRoster restored));
        Assert.HasCount(2, restored.Participants);
        Assert.IsTrue(restored.Participants[0].IsManual);
        Assert.AreEqual(3, restored.Participants[0].InitiativeModifier);
    }

    [TestMethod]
    public void CombatRosterEditsManualParticipantsAndResetsHp()
    {
        var roster = new CombatRoster();
        CombatParticipant valeros = roster.AddManual("Valeros", 24);
        Assert.IsTrue(roster.UpdateManual(valeros.Sequence, "Valeros the Brave", 30, -4, 7));
        Assert.AreEqual("Valeros the Brave", valeros.DisplayName);
        Assert.AreEqual(30, valeros.MaximumHP);
        Assert.AreEqual(-4, valeros.CurrentHP);
        Assert.AreEqual(7, valeros.InitiativeModifier);
        Assert.IsTrue(valeros.IsDefeated);
        Assert.IsTrue(roster.ResetHp(valeros.Sequence));
        Assert.AreEqual(30, valeros.CurrentHP);
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin", HP = 6 });
        Assert.IsFalse(roster.UpdateManual(goblin.Sequence, "Edited Goblin", 20, 20));
    }

    [TestMethod]
    public void CombatRosterPersistsParticipantNotes()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin", HP = 6 });
        Assert.IsTrue(roster.SetNotes(goblin.Sequence, "  Prone; poisoned  "));
        Assert.AreEqual("Prone; poisoned", goblin.Notes);
        using var stream = new MemoryStream();
        roster.Save(stream);
        stream.Position = 0;
        Assert.IsTrue(CombatRoster.TryLoad(stream, out CombatRoster restored));
        Assert.AreEqual("Prone; poisoned", restored.Participants[0].Notes);
        Assert.IsTrue(restored.SetNotes(goblin.Sequence, string.Empty));
        Assert.AreEqual(string.Empty, restored.Participants[0].Notes);
    }

    [TestMethod]
    public void CombatRosterDuplicatesParticipantsWithIndependentEncounterState()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin", CR = "1/3", HP = 6 });
        roster.ApplyDamage(goblin.Sequence, 4);
        roster.SetInitiative(goblin.Sequence, 18);
        roster.SetNotes(goblin.Sequence, "Prone");
        CombatParticipant copy = roster.Duplicate(goblin.Sequence);
        Assert.AreEqual("Goblin 2", copy.DisplayName);
        Assert.AreEqual(6, copy.CurrentHP);
        Assert.IsNull(copy.Initiative);
        Assert.AreEqual("Prone", copy.Notes);
        roster.ApplyDamage(copy.Sequence, 2);
        Assert.AreEqual(2, goblin.CurrentHP);
        Assert.AreEqual(4, copy.CurrentHP);
    }

    [TestMethod]
    public void CombatRosterTicksExpiresAndPersistsTimedConditions()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin", HP = 6 });
        CombatParticipant ogre = roster.Add(new CreatureSummary { Id = 8, Name = "Ogre", HP = 30 });
        roster.SetInitiative(goblin.Sequence, 20);
        roster.SetInitiative(ogre.Sequence, 10);
        Assert.IsTrue(roster.AddCondition(goblin.Sequence, "  Stunned  ", 2));
        Assert.IsFalse(roster.AddCondition(goblin.Sequence, "Invalid", -1));
        roster.NextTurn();
        roster.NextTurn();
        Assert.AreEqual(1, goblin.Conditions[0].RemainingTurns);
        roster.NextTurn();
        Assert.HasCount(1, goblin.Conditions);
        roster.NextTurn();
        Assert.IsEmpty(goblin.Conditions);

        roster.AddCondition(ogre.Sequence, "Slowed", 3);
        using var stream = new MemoryStream();
        roster.Save(stream);
        stream.Position = 0;
        Assert.IsTrue(CombatRoster.TryLoad(stream, out CombatRoster restored));
        Assert.AreEqual("Slowed", restored.Participants.Single(item => item.Name == "Ogre").Conditions[0].Name);
        Assert.AreEqual(3, restored.Participants.Single(item => item.Name == "Ogre").Conditions[0].RemainingTurns);
        Assert.IsTrue(restored.RemoveCondition(ogre.Sequence, 0));
        Assert.IsEmpty(restored.Participants.Single(item => item.Name == "Ogre").Conditions);
        Assert.IsFalse(restored.RemoveCondition(ogre.Sequence, 0));
    }

    [TestMethod]
    public void ConditionReferencesLoadSortAndFindByName()
    {
        const string xml = "<ArrayOfCondition><Condition><Name>Prone</Name><Text>On the ground.</Text></Condition><Condition><Name>Blinded</Name><Text>Cannot see.</Text></Condition></ArrayOfCondition>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        List<ConditionReference> conditions = ConditionReference.Load(stream);
        Assert.HasCount(2, conditions);
        Assert.AreEqual("Blinded", conditions[0].Name);
        Assert.AreEqual("On the ground.", ConditionReference.Find(conditions, "prone").Description);
        Assert.IsNull(ConditionReference.Find(conditions, "missing"));
    }

    [TestMethod]
    public void CombatRosterEditsTimedConditionsAndValidatesInput()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin" });
        roster.AddCondition(goblin.Sequence, "Prone", 2);
        Assert.IsFalse(roster.UpdateCondition(goblin.Sequence, 1, "Stunned", 3));
        Assert.IsFalse(roster.UpdateCondition(goblin.Sequence, 0, string.Empty, 3));
        Assert.IsFalse(roster.UpdateCondition(goblin.Sequence, 0, "Stunned", -1));
        Assert.IsTrue(roster.UpdateCondition(goblin.Sequence, 0, "  Stunned  ", 4));
        Assert.AreEqual("Stunned (4)", goblin.Conditions[0].DisplayText);
        using var stream = new MemoryStream();
        roster.Save(stream);
        stream.Position = 0;
        Assert.IsTrue(CombatRoster.TryLoad(stream, out CombatRoster restored));
        Assert.AreEqual("Stunned (4)", restored.Participants[0].Conditions[0].DisplayText);
    }

    [TestMethod]
    public void CombatRosterKeepsUntimedConditionsUntilRemoved()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin" });
        roster.SetInitiative(goblin.Sequence, 12);
        Assert.IsTrue(roster.AddCondition(goblin.Sequence, "Prone", 0));
        Assert.AreEqual("Prone", goblin.Conditions[0].DisplayText);
        Assert.IsFalse(goblin.Conditions[0].IsTimed);
        roster.NextTurn();
        roster.NextTurn();
        Assert.HasCount(1, goblin.Conditions);
        using var stream = new MemoryStream();
        roster.Save(stream);
        stream.Position = 0;
        Assert.IsTrue(CombatRoster.TryLoad(stream, out CombatRoster restored));
        Assert.AreEqual("Prone", restored.Participants[0].Conditions[0].DisplayText);
    }

    [TestMethod]
    public void CombatRosterClearsOnlySelectedParticipantsConditions()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin" });
        CombatParticipant ogre = roster.Add(new CreatureSummary { Id = 8, Name = "Ogre" });
        roster.AddCondition(goblin.Sequence, "Prone", 0);
        roster.AddCondition(goblin.Sequence, "Stunned", 2);
        roster.AddCondition(ogre.Sequence, "Shaken", 3);
        Assert.IsTrue(roster.ClearConditions(goblin.Sequence));
        Assert.IsEmpty(goblin.Conditions);
        Assert.HasCount(1, ogre.Conditions);
        Assert.IsFalse(roster.ClearConditions(goblin.Sequence));
        Assert.IsFalse(roster.ClearConditions(999));
    }

    [TestMethod]
    public void CombatRosterBuildsReadableEncounterSummary()
    {
        var roster = new CombatRoster();
        CombatParticipant valeros = roster.AddManual("Valeros", 30);
        roster.SetInitiative(valeros.Sequence, 18);
        roster.ApplyDamage(valeros.Sequence, 5);
        roster.SetTemporaryHp(valeros.Sequence, 3);
        roster.SetNotes(valeros.Sequence, "Blessed");
        roster.AddCondition(valeros.Sequence, "Hasted", 3);
        roster.NextTurn();
        string summary = roster.ToSummaryText();
        StringAssert.Contains(summary, "Combat Manager Encounter — Round 1");
        StringAssert.Contains(summary, "▶ Valeros — HP 25/30 + 3 temporary — Initiative 18 — Blessed — Hasted (3)");
    }

    [TestMethod]
    public void CombatRosterResetsTurnsWithoutClearingEncounterState()
    {
        var roster = new CombatRoster();
        CombatParticipant goblin = roster.Add(new CreatureSummary { Id = 7, Name = "Goblin", HP = 6 });
        roster.SetInitiative(goblin.Sequence, 20);
        roster.ApplyDamage(goblin.Sequence, 2);
        roster.SetNotes(goblin.Sequence, "Marked");
        roster.AddCondition(goblin.Sequence, "Slowed", 2);
        roster.NextTurn();
        roster.ResetTurns();
        Assert.IsNull(goblin.Initiative);
        Assert.IsNull(roster.ActiveParticipant);
        Assert.AreEqual(0, roster.Round);
        Assert.AreEqual(4, goblin.CurrentHP);
        Assert.AreEqual("Marked", goblin.Notes);
        Assert.HasCount(1, goblin.Conditions);
    }
}
