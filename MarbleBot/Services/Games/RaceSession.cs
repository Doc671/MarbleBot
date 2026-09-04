using MarbleBot.Entities.Games;

namespace MarbleBot.Services.Games;

public class RaceSession
{
    public int MarbleLimit { get; }
    public RaceSessionState State { get; private set; }

    private readonly Lock _lock = new();
    private readonly List<SignupEntry> _entries = [];

    private readonly RaceEngine _engine;

    public RaceSession(RaceEngine engine, IReadOnlyList<SignupEntry> entries, int marbleLimit)
    {
        _engine = engine;
        _entries.AddRange(entries);
        MarbleLimit = marbleLimit;
    }

    public IReadOnlyList<SignupEntry> GetEntries()
    {
        lock (_lock)
        {
            return _entries.AsReadOnly();
        }
    }

    public int GetEntryCount()
    {
        lock (_lock)
        {
            return _entries.Count;
        }
    }

    public bool TryAddEntry(ulong userId, string marbleName)
    {
        lock (_lock)
        {
            if (State != RaceSessionState.Signup)
            {
                return false;
            }

            if (_entries.Count >= MarbleLimit)
            {
                return false;
            }

            _entries.Add(new SignupEntry() { UserId = userId, MarbleName = marbleName } );

            return true;
        }
    }

    public bool TryRemoveEntry(ulong userId, string marbleName)
    {
        lock (_lock)
        {
            if (State != RaceSessionState.Signup)
            {
                return false;
            }

            int elementsRemoved = _entries.RemoveAll(entry => entry.UserId == userId && string.Equals(entry.MarbleName, marbleName, StringComparison.OrdinalIgnoreCase));
            return elementsRemoved != 0;
        }
    }

    public async IAsyncEnumerable<RaceResult> RunAsync()
    {
        List<SignupEntry> entriesCopy;
        lock (_lock)
        {
            if (State != RaceSessionState.Signup)
            {
                throw new InvalidOperationException("Game session is not in a valid state.");
            }

            State = RaceSessionState.InProgress;
            entriesCopy = _entries.ToList();
        }

        try
        {
            await foreach (RaceResult result in _engine.RunRaceAsync(entriesCopy))
            {
                yield return result;
            }
        }
        finally
        {
            lock (_lock)
            {
                State = RaceSessionState.Complete;
            }
        }
    }
}
