using MarbleBot.Utilities;

namespace MarbleBot.Tests.Utilities;

public class CobenTests
{
    [Theory]
    // Note that the lower and upper intervals here are rough goalposts rather than Wilson intervals
    [InlineData(new string[] { "Black", "White" }, new long[] { 50, 50 }, new long[] { 100, 50 }, new float[] { 0.49f, 0.49f }, new float[] { 0.51f, 0.51f })]
    [InlineData(new string[] { "Red", "Orange", "Yellow" }, new long[] { 50, 25, 10 }, new long[] { 100, 75, 50 }, new float[] { 0.07333333f, 0.4066666f, 0.49f }, new float[] { 0.09333333f, 0.4266666f, 0.51f })]
    public void CalculateCoben_OneElimination_ReturnsValuesInCorrectRange(string[] names, long[] currentPoints, long[] pointsToDistribute, float[] lowerIntervals, float[] upperIntervals)
    {
        Dictionary<string, CobenResult> results = CobenCalculator.Calculate(names, currentPoints, pointsToDistribute);

        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            float lowerInterval = lowerIntervals[i];
            float upperInterval = upperIntervals[i];
            CobenResult result = results[name];
            Assert.InRange(result.Chance, lowerInterval, upperInterval);
        }
    }
}
