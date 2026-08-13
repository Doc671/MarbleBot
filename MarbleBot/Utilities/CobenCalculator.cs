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

        int[] eliminationCounts = new int[athleteCount];
        Parallel.For(0, simulationCount,
            () => {
                var threadLocalState = new
                {
                    EventEliminationCount = eventEliminationCount,
                    Random = new Random(Random.Shared.Next()),
                    PointsToDistribute = (long[])pointsToDistribute.Clone(),
                    NewPoints = new long[athleteCount],
                    LowestPoints = new long[eventEliminationCount],
                    EliminationCounts = new int[athleteCount]
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
                for (int j = 0; j < athleteCount; j++)
                {
                    if (threadLocalState.NewPoints[j] <= nthLowestPoints)
                    {
                        threadLocalState.EliminationCounts[j]++;
                    }
                }

                Array.Fill(threadLocalState.LowestPoints, long.MaxValue);

                return threadLocalState;
            },
            (threadLocalState) => 
            {
                for (int i = 0; i < athleteCount; i++)
                {
                    int localEliminationCount = threadLocalState.EliminationCounts[i];
                    if (localEliminationCount != 0)
                    {
                        Interlocked.Add(ref eliminationCounts[i], threadLocalState.EliminationCounts[i]);
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
}
