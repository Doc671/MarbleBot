namespace MarbleBot.Utilities;

public readonly struct CobenResult
{
    public float Chance { get; }
    public float LowerInterval { get; }
    public float UpperInterval { get; }

    public CobenResult(float chance)
    {
        Chance = chance;
    }

    public CobenResult(float chance, float lowerInterval, float upperInterval)
    {
        Chance = chance;
        LowerInterval = lowerInterval;
        UpperInterval = upperInterval;
    }

    public CobenResult(float chance, float zScore, int simulationCount)
    {
        Chance = chance;

        // Calculate Wilson intervals
        float a = chance + zScore * zScore / (2 * simulationCount);
        float b = zScore * MathF.Sqrt(chance * (1 - chance) / simulationCount + zScore * zScore / (4 * simulationCount * simulationCount));
        float c = 1 + zScore * zScore / simulationCount;
        LowerInterval = MathF.Max((a - b) / c, 0f);
        UpperInterval = MathF.Min((a + b) / c, 1f);
    }
}
