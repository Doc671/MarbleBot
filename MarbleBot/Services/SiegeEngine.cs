using MarbleBot.Data.Repositories.Games;
using MarbleBot.Entities.Games;
using MarbleBot.Entities.Games.Siege;
using MarbleBot.Games.Siege;
using MarbleBot.Services.Games;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace MarbleBot.Services;

public class SiegeEngine
{
    private readonly IServiceScopeFactory _scopeFactory;

    private const int _maxXSpots = 7;
    private const int _minX = -_maxXSpots / 2;

    public SiegeEngine(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async IAsyncEnumerable<SiegeMessage> RunSiegeAsync(IReadOnlyList<SiegeMarble> marbles, Boss boss, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (marbles.Any(marble => marble.Health != 0) && boss.Health != 0)
        {
            const int attackDelay = 5000;
            await Task.Delay(attackDelay, cancellationToken);

            SiegeMarble[] aliveMarbles = marbles.Where(marble => marble.Health != 0).ToArray();
            ShuffleMarblePositions(aliveMarbles);

            foreach (SiegeMarble marble in aliveMarbles)
            {
                marble.Dodging = false;
            }

            int attackIndex = Random.Shared.Next(boss.Attacks.Count);
            Attack attack = boss.Attacks[attackIndex];

            int attackX = _minX + Random.Shared.Next(_maxXSpots);
            int attackWidth = (int)MathF.Round(attack.Accuracy / 100f * _maxXSpots);
            int attackMinX = Math.Max(attackX - attackWidth / 2, _minX);
            int attackMaxX = Math.Min(attackX + attackWidth / 2, _minX + _maxXSpots - 1);

            SiegeMarble[] threatenedMarbles = aliveMarbles.Where(marble => marble.X >= attackMinX && marble.X <= attackMaxX).ToArray();
            string threatenedMarbleString = threatenedMarbles.Length == 0 ? "None" : string.Join(' ', threatenedMarbles.Select(marble => marble.Name));

            yield return new BossChargingSiegeMessage(
                $"{boss.Name} is charging {attack.Name}! Threatened marbles: {threatenedMarbleString}.", 
                SiegeSessionState.BossCharging, 
                threatenedMarbles.Select(marble => marble.Id).ToList());

            const int dodgeDelay = 5000;
            await Task.Delay(dodgeDelay, cancellationToken);

            if (boss.Health == 0)
            {
                break;
            }

            foreach (SiegeMarble marble in threatenedMarbles)
            {
                if (marble.Dodging)
                {
                    yield return new SiegeMessage($"{marble.Name} dodged {boss.Name}'s {attack.Name}!", SiegeSessionState.BossIdle);
                }
                else
                {
                    marble.Damage(attack.Damage, attack.Name);
                }
            }
        }

        if (boss.Health == 0)
        {
            yield return new SiegeMessage($"The marbles have defeated {boss.Name}!", SiegeSessionState.Complete);
        }
        else
        {
            yield return new SiegeMessage($"{boss.Name} has defeated the marbles!", SiegeSessionState.Complete);
        }
    }

    private static void ShuffleMarblePositions(IReadOnlyList<SiegeMarble> marbles)
    {
        var xSpotCount = new int[_maxXSpots];
        foreach (var marble in marbles) 
        {
            int index = Random.Shared.Next(_maxXSpots);
            marble.X = _minX + index;
            marble.Y = xSpotCount[index]++;
        }
    }

    public async Task<Boss> ChooseBossAsync(IReadOnlyList<SignupEntry> signupEntries)
    {
        int stageTotal = signupEntries.Sum(entry => entry.User.Stage);
        float averageStage = stageTotal / (float)signupEntries.Count;
        int flooredStage = (int)MathF.Floor(averageStage);
        float remainder = averageStage - flooredStage;
        int stage;
        if (remainder > float.Epsilon || Random.Shared.NextSingle() > remainder) 
        {
            stage = flooredStage;
        }
        else
        {
            stage = flooredStage + 1;
        }

        return await ChooseBossInStageAsync(stage, signupEntries.Count);
    }

    private async Task<Boss> ChooseBossInStageAsync(int stage, int marbleCount)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var bossDao = scope.ServiceProvider.GetRequiredService<BossDao>();

        IReadOnlyList<Boss> bosses = await bossDao.GetAllInStageAsync(stage);

        float bossWeight = marbleCount * (Random.Shared.NextSingle() * 5 + 1);
        const int finalBossWeightThreshold = 50;
        int bossIndex = Math.Min((int)MathF.Round(bossWeight * bosses.Count / finalBossWeightThreshold), 1);
        return bosses[bossIndex];
    }
    
    public static async Task<Dictionary<ulong, SiegeMarble>> ConvertToMarblesAsync(IReadOnlyList<SignupEntry> signupEntries, Boss boss)
    {
        int marbleHealth = ((int)boss.Difficulty + 2) * 5;
        return signupEntries
            .ToDictionary(entry => entry.UserId, entry => new SiegeMarble(entry.User, entry.MarbleName, marbleHealth));
    }
}
