using CombatManager;
using ScottsUtils;

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
}
