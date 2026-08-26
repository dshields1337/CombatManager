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
}
