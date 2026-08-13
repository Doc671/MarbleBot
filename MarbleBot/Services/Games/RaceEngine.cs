using MarbleBot.Entities.Games;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MarbleBot.Services.Games;

public class RaceEngine
{
    private readonly string[]? _deathMessages;
    private readonly Dictionary<string, string>? _specialDeathMessages;

    private const int _eliminationDelayMilliseconds = 1500;

    public RaceEngine()
    {
        string deathMessageFilePath = Path.Combine(AppContext.BaseDirectory, "Resources", "RaceDeathMessages.txt");
        if (File.Exists(deathMessageFilePath))
        {
            _deathMessages = File.ReadAllLines(deathMessageFilePath);
        }

        string specialDeathMessageFilePath = Path.Combine(AppContext.BaseDirectory, "Resources", "RaceSpecialMessages.json");
        if (File.Exists(specialDeathMessageFilePath))
        {
            using var streamReader = new StreamReader(specialDeathMessageFilePath);
            _specialDeathMessages = JsonSerializer.Deserialize<Dictionary<string, string>>(streamReader.BaseStream);
        }
    }

    public async IAsyncEnumerable<RaceResult> RunRaceAsync(IReadOnlyList<SignupEntry> signupEntries, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<SignupEntry> entries = signupEntries.ToList();
        for (int remainingMarbleCount = entries.Count; remainingMarbleCount > 1; remainingMarbleCount--)
        {
            await Task.Delay(_eliminationDelayMilliseconds, cancellationToken);

            int eliminatedIndex = Random.Shared.Next(remainingMarbleCount);

            SignupEntry currentEntry = entries[eliminatedIndex];

            string deathMessage;
            if (_specialDeathMessages != null && _specialDeathMessages.TryGetValue(currentEntry.MarbleName, out string? value))
            {
                deathMessage = value;
            }
            else if (_deathMessages == null)
            {
                deathMessage = "was eliminated";
            }
            else
            {
                int messageIndex = Random.Shared.Next(_deathMessages.Length);
                deathMessage = _deathMessages[messageIndex];
            }

            entries.RemoveAt(eliminatedIndex);

            yield return new RaceResult(currentEntry.MarbleName, deathMessage, remainingMarbleCount);
        }

        await Task.Delay(_eliminationDelayMilliseconds, cancellationToken);

        SignupEntry winnerEntry = entries[0];
        yield return new RaceResult(winnerEntry.MarbleName, "is the winner", 1);
    }
}
