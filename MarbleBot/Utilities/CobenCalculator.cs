namespace MarbleBot.Utilities;

public static class CobenCalculator
{
    public static Dictionary<string, CobenResult> Calculate(string[] names, long[] currentPoints, long[] pointsToDistribute, int eventEliminationCount = 1, int simulationCount = 1_000_000, bool showIntervals = false)
    {
        int athleteCount = currentPoints.Length;
        if (pointsToDistribute.Length != athleteCount)
        {
            Array.Resize(ref pointsToDistribute, athleteCount);
        }

        long minPointsToDistribute = 0;
        long maxPointsToDistribute = 0;
        for (int i = 0; i < athleteCount; i++)
        {
            if (pointsToDistribute[i] < minPointsToDistribute)
            {
                minPointsToDistribute = pointsToDistribute[i];
            }
            if (pointsToDistribute[i] > maxPointsToDistribute)
            {
                maxPointsToDistribute = pointsToDistribute[i];
            }
        }

        long[] orderedCurrentPoints = (long[])currentPoints.Clone();
        Array.Sort(orderedCurrentPoints);

        // The threshold is the contestant with the nth worst current score + the maximum points for the next event
        // where n is the number of eliminations
        // If a contestant's score + the minimum points for the next event is above the threshold, they are immune from elimination
        // Therefore, they should be removed from consideration
        var returnDictionary = new Dictionary<string, CobenResult>();
        long immunityThreshold = orderedCurrentPoints[eventEliminationCount - 1] + maxPointsToDistribute;
        bool[] indicesToRemove = new bool[athleteCount];
        int numIndicesToRemove = 0;
        for (int i = 0; i < athleteCount; i++)
        {
            if (currentPoints[i] + minPointsToDistribute > immunityThreshold)
            {
                returnDictionary[names[i]] = new CobenResult(0f);
                indicesToRemove[i] = true;
                numIndicesToRemove++;
            }
        }

        if (numIndicesToRemove != 0)
        {
            int newAthleteCount = athleteCount - numIndicesToRemove;
            long[] newCurrentPoints = new long[newAthleteCount];
            string[] newNames = new string[newAthleteCount];
            int newIndex = 0;
            for (int i = 0; i < athleteCount; i++)
            {
                if (!indicesToRemove[i])
                {
                    newCurrentPoints[newIndex] = currentPoints[i];
                    newNames[newIndex] = names[i];
                    newIndex++;
                }
            }

            athleteCount = newAthleteCount;
            currentPoints = newCurrentPoints;
            names = newNames;
        }

        double[] eliminationCounts = new double[athleteCount];
        Parallel.For(0, simulationCount,
            () => {
                var threadLocalState = new
                {
                    EventEliminationCount = eventEliminationCount,
                    Random = new Random(Random.Shared.Next()),
                    PointsToDistribute = (long[])pointsToDistribute.Clone(),
                    NewPoints = new long[athleteCount],
                    LowestPoints = new long[eventEliminationCount],
                    EliminationCounts = new double[athleteCount],
                    EliminationPossible = new bool[athleteCount]
                };

                Array.Fill(threadLocalState.LowestPoints, long.MaxValue);

                return threadLocalState;
            },
            (i, loopState, threadLocalState) =>
            {
                Shuffle(threadLocalState.PointsToDistribute, threadLocalState.Random);

                for (int j = 0; j < athleteCount; j++)
                {
                    long newPoints = currentPoints[j] + threadLocalState.PointsToDistribute[j];
                    threadLocalState.NewPoints[j] = newPoints;

                    // Check if the new points are among the lowest for this simulation
                    int k = 0;
                    bool inserted = false;
                    while (k < threadLocalState.EventEliminationCount && !inserted)
                    {
                        if (newPoints < threadLocalState.LowestPoints[k])
                        {
                            // Move everything greater than newPoints to the right before inserting newPoints
                            for (int l = threadLocalState.EventEliminationCount - 1; l > k; l--)
                            {
                                threadLocalState.LowestPoints[l] = threadLocalState.LowestPoints[l - 1];
                            }
                            threadLocalState.LowestPoints[k] = newPoints;
                            inserted = true;
                        }

                        k++;
                    }
                }

                // Find who got the nth lowest score and increment their elimination count
                long nthLowestPoints = threadLocalState.LowestPoints[threadLocalState.EventEliminationCount - 1];
                int endangeredAthleteCount = 0;
                for (int j = 0; j < athleteCount; j++)
                {
                    if (threadLocalState.NewPoints[j] <= nthLowestPoints)
                    {
                        endangeredAthleteCount++;
                        threadLocalState.EliminationPossible[j] = true;
                    }
                }

                // If multiple contestants are tied for elimination, assume that a tiebreaker will occur
                // and that the original elimination count remains the same
                double eliminationsToAdd = (double)threadLocalState.EventEliminationCount / endangeredAthleteCount;
                for (int j = 0; j < athleteCount; j++)
                {
                    if (threadLocalState.EliminationPossible[j])
                    {
                        threadLocalState.EliminationCounts[j] += eliminationsToAdd;
                    }
                }

                Array.Fill(threadLocalState.LowestPoints, long.MaxValue);
                Array.Fill(threadLocalState.EliminationPossible, false);

                return threadLocalState;
            },
            (threadLocalState) => 
            {
                for (int i = 0; i < athleteCount; i++)
                {
                    double localEliminationCount = threadLocalState.EliminationCounts[i];
                    if (localEliminationCount != 0)
                    {
                        InterlockedAddDouble(ref eliminationCounts[i], threadLocalState.EliminationCounts[i]);
                    }
                }
            }
        );

        float[] chances = eliminationCounts
            .Select(count => (float)count / simulationCount)
            .ToArray();

        foreach (var pair in names.Zip(chances, (name, chance) => new { name, chance }))
        {
            string name = pair.name.ToString();

            CobenResult result;
            if (showIntervals)
            {
                const float zScore = 1.96f; // 95% confidence
                result = new CobenResult(pair.chance, zScore, simulationCount); 
            }
            else
            {
                result = new CobenResult(pair.chance);
            }

            returnDictionary[name] = result;
        }

        return returnDictionary;
    }

    private static void Shuffle(long[] array, Random random)
    {
        int n = array.Length;
        while (n > 1)
        {
            int k = random.Next(n--);
            (array[n], array[k]) = (array[k], array[n]);
        }
    }

    private static double InterlockedAddDouble(ref double location1, double value)
    {
        double newCurrentValue = Volatile.Read(ref location1);

        while (true)
        {
            double currentValue = newCurrentValue;
            double newValue = currentValue + value;
            newCurrentValue = Interlocked.CompareExchange(ref location1, newValue, newCurrentValue);

            if (newCurrentValue.Equals(currentValue))
            {
                return newValue;
            }
        }
    }
}
