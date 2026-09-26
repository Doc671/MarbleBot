using MarbleBot.Data.Repositories.Games;
using MarbleBot.Entities.Games;
using MarbleBot.Entities.Games.Siege;
using MarbleBot.Games.Siege;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace MarbleBot.Services.Games;

public class SiegeEngine
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SiegeRendererService _siegeRendererService;

    private const int _maxXSpots = 7;
    private const int _minX = -_maxXSpots / 2;

    public SiegeEngine(IServiceScopeFactory scopeFactory, SiegeRendererService siegeRendererService)
    {
        _scopeFactory = scopeFactory;
        _siegeRendererService = siegeRendererService;
    }

    public async IAsyncEnumerable<SiegeMessage> RunSiegeAsync(IReadOnlyList<SiegeMarble> marbles, Boss boss, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ShuffleMarblePositions(marbles);
        while (marbles.Any(marble => marble.Health != 0) && boss.Health != 0)
        {
            const int attackDelay = 5000;
            await Task.Delay(attackDelay, cancellationToken);

            SiegeMarble[] aliveMarbles = marbles.Where(marble => marble.Health != 0).ToArray();

            foreach (SiegeMarble marble in aliveMarbles)
            {
                marble.MoveDelta = 0;
            }

            int attackIndex = Random.Shared.Next(boss.Attacks.Count);
            Attack attack = boss.Attacks[attackIndex];

            int attackX = _minX + Random.Shared.Next(_maxXSpots);
            int attackWidth = (int)MathF.Round(attack.Accuracy / 100f * _maxXSpots);
            int attackMinX = Math.Max(attackX - attackWidth / 2, _minX);
            int attackMaxX = Math.Min(attackX + attackWidth / 2, _minX + _maxXSpots - 1);

            SiegeMarble[] threatenedMarbles = aliveMarbles.Where(marble => marble.X >= attackMinX && marble.X <= attackMaxX).ToArray();
            string threatenedMarbleString = threatenedMarbles.Length == 0 ? "None" : string.Join(' ', threatenedMarbles.Select(marble => marble.Name));

            MemoryStream imageStream = await _siegeRendererService.RenderMarblesWithAttackWarningAsync(marbles, 0xFF728CFF, _maxXSpots, _minX, attackMinX, attackMaxX);

            yield return new BossChargingSiegeMessage(
                $"{boss.Name} is charging {attack.Name}! Threatened marbles: {threatenedMarbleString}.", 
                SiegeSessionState.BossCharging, 
                threatenedMarbles.Select(marble => marble.Id).ToList(),
                imageStream);

            const int dodgeDelay = 5000;
            await Task.Delay(dodgeDelay, cancellationToken);

            if (boss.Health == 0)
            {
                break;
            }

            foreach (SiegeMarble marble in aliveMarbles)
            {
                marble.X += marble.MoveDelta;
            }

            foreach (SiegeMarble marble in threatenedMarbles)
            {
                if (marble.X < attackMinX || marble.X > attackMaxX)
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
