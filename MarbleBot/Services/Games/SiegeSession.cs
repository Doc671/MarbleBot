using MarbleBot.Entities;
using MarbleBot.Entities.Games;
using MarbleBot.Entities.Games.Siege;
using MarbleBot.Games.Siege;
using MarbleBot.Modules.Games;

namespace MarbleBot.Services.Games;

public class SiegeSession
{
    public int MarbleLimit { get; }
    public SiegeSessionState State { get; private set; }

    private readonly Lock _lock = new();
    private readonly List<SignupEntry> _entries = [];

    private readonly SiegeEngine _siegeEngine;

    private SiegeMessageHandler? _messageHandler;

    private Boss? _boss;
    private Dictionary<ulong, SiegeMarble>? _marbles;

    public event EventHandler<SiegeMessage>? SiegeMessageSent;

    public SiegeSession(SiegeEngine siegeEngine, IReadOnlyList<SignupEntry> entries, int marbleLimit)
    {
        _siegeEngine = siegeEngine;
        _entries.AddRange(entries);
        MarbleLimit = marbleLimit;
    }

    public bool ContainsId(ulong userId)
    {
        lock (_lock)
        {
            return _entries.Any(entry => entry.UserId == userId);
        }
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

    public bool TryAddEntry(User user, string marbleName)
    {
        lock (_lock)
        {
            if (State != SiegeSessionState.Signup)
            {
                return false;
            }

            if (_entries.Count >= MarbleLimit)
            {
                return false;
            }

            _entries.Add(new SignupEntry() { UserId = user.Id, User = user, MarbleName = marbleName });

            return true;
        }
    }

    public bool TryRemoveEntry(ulong userId, string marbleName)
    {
        lock (_lock)
        {
            if (State != SiegeSessionState.Signup)
            {
                return false;
            }

            int elementsRemoved = _entries.RemoveAll(entry => entry.UserId == userId && string.Equals(entry.MarbleName, marbleName, StringComparison.OrdinalIgnoreCase));
            return elementsRemoved != 0;
        }
    }

    public async Task StartAsync(IUserMessage message, EmbedBuilder embedBuilder, ComponentBuilder componentBuilder)
    {
        SignupEntry[] entriesCopy;
        lock (_lock)
        {
            if (State != SiegeSessionState.Signup)
            {
                throw new InvalidOperationException("Game session is not in a valid state.");
            }

            State = SiegeSessionState.BossIdle;
            entriesCopy = _entries.ToArray();
        }

        _boss = await _siegeEngine.ChooseBossAsync(entriesCopy);
        _marbles = await SiegeEngine.ConvertToMarblesAsync(entriesCopy, _boss);

        List<SiegeMarble> marbles = _marbles.Values.ToList();
        _messageHandler = new SiegeMessageHandler(this, message, embedBuilder, componentBuilder, _boss, marbles);

        try
        {
            await foreach (SiegeMessage siegeMessage in _siegeEngine.RunSiegeAsync(marbles, _boss))
            {
                State = siegeMessage.SessionState;
                await _messageHandler.AddSiegeMessage(siegeMessage);

                SiegeMessageSent?.Invoke(this, siegeMessage);
            }
        }
        finally
        {
            lock (_lock)
            {
                State = SiegeSessionState.Complete;
            }
        }
    }

    public async Task<string?> TryBashAsync(ulong userId)
    {
        lock (_lock)
        {
            if (State is SiegeSessionState.Signup or SiegeSessionState.Complete)
            {
                return "You cannot attack in this siege!";
            }

            if (_marbles == null || !_marbles.TryGetValue(userId, out SiegeMarble? marble))
            {
                return "You are not participating in this siege!";
            }

            if (marble.Health == 0)
            {
                return "Your marble has died, so you cannot attack!";
            }

            double totalSeconds = (DateTime.UtcNow - marble.LastAttack).TotalSeconds;
            const double cooldownSeconds = 5.0;
            if (totalSeconds > cooldownSeconds)
            {
                int marbleDamage = Random.Shared.Next(1, 25);
                if (marbleDamage > 20)
                {
                    marbleDamage = (marbleDamage * 3) / 2;
                }

                marble.LastAttack = DateTime.UtcNow;
                _boss!.Damage(marbleDamage, marble.Name);
                return null;
            }
        }

        return null;
    }

    public async Task<string?> TryDodgeAsync(ulong userId)
    {
        lock (_lock)
        {
            if (State is SiegeSessionState.Signup or SiegeSessionState.Complete)
            {
                return "You cannot dodge in this siege!";
            }

            if (_marbles == null || !_marbles.TryGetValue(userId, out SiegeMarble? marble))
            {
                return "You are not participating in this siege!";
            }

            if (marble.Health == 0)
            {
                return "Your marble has died, so you cannot dodge!";
            }

            marble.Dodging = true;
        }

        return null;
    }
}
