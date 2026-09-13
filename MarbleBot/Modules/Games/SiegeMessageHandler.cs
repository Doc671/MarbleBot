using MarbleBot.Entities.Games.Siege;
using MarbleBot.Games;
using MarbleBot.Games.Siege;
using MarbleBot.Services.Games;
using System.Collections.Concurrent;
using System.Text;

namespace MarbleBot.Modules.Games;

public class SiegeMessageHandler
{
    private readonly IUserMessage _message;
    private readonly EmbedBuilder _embedBuilder;
    private EmbedFieldBuilder? _eventFieldBuilder;
    private EmbedFieldBuilder? _marbleFieldBuilder;
    private readonly ComponentBuilder _componentBuilder;

    private readonly BlockingCollection<string> _siegeEvents = new(10);

    private readonly IReadOnlyList<SiegeMarble> _marbles;

    public SiegeMessageHandler(SiegeSession siegeSession, IUserMessage message, EmbedBuilder embedBuilder, ComponentBuilder componentBuilder, Boss boss, IReadOnlyList<SiegeMarble> marbles)
    {
        _message = message;
        _embedBuilder = embedBuilder;
        _componentBuilder = componentBuilder;
        _marbles = marbles;

        siegeSession.SiegeMessageSent += OnSiegeMessageSent;

        boss.HealthChanged += OnBossHealthChanged;

        foreach (SiegeMarble marble in marbles)
        {
            marble.HealthChanged += OnMarbleHealthChanged;
        }

        Task.Run(() => InitialiseMessage(boss, marbles));
    }

    private async Task InitialiseMessage(Boss boss, IReadOnlyList<SiegeMarble> marbles)
    {
        var marbleOutput = new StringBuilder();
        foreach (SiegeMarble marble in marbles)
        {
            marbleOutput.AppendLine($"**{marble.Name}** [{marble.User.Username}]");
        }

        _embedBuilder
            .WithTitle($"Siege v {boss.Name}")
            .WithDescription($"Health: **{boss.Health}**/{boss.MaxHealth}\nAttacks: **{boss.Attacks.Count}**\nDifficulty: **{boss.Difficulty} {(int)boss.Difficulty}**/10")
            .WithThumbnailUrl(boss.ImageUrl)
            .AddField($"Marbles: **{marbles.Count}**", marbleOutput.ToString());

        await _message.ModifyAsync(message => message.Embed = _embedBuilder.Build());
        await ToggleButtonAsync("siege_bash", false);
    }
    
    private async void OnSiegeMessageSent(object? sender, SiegeMessage e)
    {
        switch (e.SessionState)
        {
            case SiegeSessionState.BossCharging:
                await ToggleButtonAsync("siege_dodge", false);
                var fullMessage = (e as BossChargingSiegeMessage)!;
                await UpdateImageAsync(fullMessage.ImageStream);
                break;
            case SiegeSessionState.BossIdle:
                await ToggleButtonAsync("siege_dodge", true);
                break;
            case SiegeSessionState.Complete:
                await ToggleButtonAsync("siege_bash", true);
                await ToggleButtonAsync("siege_dodge", true);
                break;
            default:
                break;
        }
    }

    public async Task AddSiegeMessage(SiegeMessage message)
    {
        await AddEventAsync(message.Message);
    }

    private async void OnBossHealthChanged(object? sender, HealthChangedEventArgs e)
    {
        if (e.OldHealth == e.NewHealth)
        {
            return;
        }

        if (e.OldHealth < e.NewHealth)
        {
            await AddEventAsync($"{e.AttackName} healed {e.CombatantName}! {e.OldHealth} -> {e.NewHealth} HP.");
        }
        else
        {
            int damage = e.OldHealth - e.NewHealth;
            string damageMessage = damage switch
            {
                <8 => "slow",
                <15 => "fast",
                <21 => "brutal",
                _ => "CRITICAL"
            };

            await AddEventAsync($"{e.AttackName} hit {e.CombatantName} for a {damageMessage} attack! {e.OldHealth} -> {e.NewHealth} HP.");
        }

        await UpdateDescriptionAsync(e.NewHealth, e.MaxHealth);
    }

    private async void OnMarbleHealthChanged(object? sender, HealthChangedEventArgs e)
    {
        if (e.OldHealth == e.NewHealth)
        {
            return;
        }

        if (e.OldHealth < e.NewHealth)
        {
            await AddEventAsync($"{e.CombatantName} was healed by {e.AttackName}! {e.OldHealth} -> {e.NewHealth} HP.");
        }
        else
        {
            await AddEventAsync($"{e.CombatantName} was hit by {e.AttackName}! {e.OldHealth} -> {e.NewHealth} HP.");
        }

        await UpdateMarbleFieldAsync();
    }

    private async Task AddEventAsync(string message)
    {
        if (_siegeEvents.Count == _siegeEvents.BoundedCapacity)
        {
            _siegeEvents.Take();
        }

        _siegeEvents.Add(message);

        await UpdateEventFieldAsync();
    }

    private async Task UpdateEventFieldAsync()
    {
        var stringBuilder = new StringBuilder();
        foreach (string message in _siegeEvents.Take(_siegeEvents.Count - 1))
        {
            stringBuilder.AppendLine(message);
        }

        stringBuilder.AppendLine($"**{_siegeEvents.Last()}**");

        _eventFieldBuilder ??= _embedBuilder.Fields.FirstOrDefault(fieldBuilder => fieldBuilder.Name == "Events");
        if (_eventFieldBuilder == null)
        {
            _embedBuilder.AddField("Events", stringBuilder.ToString());
        }
        else
        {
            _eventFieldBuilder.WithValue(stringBuilder.ToString());
        }

        await _message.ModifyAsync(message => message.Embed = _embedBuilder.Build());
    }

    private async Task UpdateMarbleFieldAsync()
    {
        var stringBuilder = new StringBuilder();
        foreach (SiegeMarble marble in _marbles)
        {
            stringBuilder.AppendLine($"{marble.Name} [{marble.User.Username}]: **{marble.Health}**/{marble.MaxHealth} HP");
        }

        _marbleFieldBuilder ??= _embedBuilder.Fields.FirstOrDefault(fieldBuilder => fieldBuilder.Name.StartsWith("Marble"));
        if (_marbleFieldBuilder == null)
        {
            _embedBuilder.AddField("Events", stringBuilder.ToString());
        }
        else
        {
            _marbleFieldBuilder.WithValue(stringBuilder.ToString());
        }

        await _message.ModifyAsync(message => message.Embed = _embedBuilder.Build());
    }

    private async Task UpdateDescriptionAsync(int newHealth, int maxHealth)
    {
        _embedBuilder.WithDescription($"Health: **{newHealth}**/{maxHealth}");
        await _message.ModifyAsync(message => message.Embed = _embedBuilder.Build());
    }

    private async Task UpdateImageAsync(MemoryStream imageStream)
    {
        _embedBuilder.WithThumbnailUrl("attachment://Siege.png");

        await _message.ModifyAsync(message =>
        {
            message.Attachments = new[] { new FileAttachment(imageStream, "Siege.png") };
            message.Embed = _embedBuilder.Build();
        });
    }

    private async Task ToggleButtonAsync(string customId, bool disabled)
    {
        for (int i = 0; i < _componentBuilder.ActionRows.Count; i++)
        {
            ActionRowBuilder? row = _componentBuilder.ActionRows[i];
            if (row != null)
            {
                for (int j = 0; j < row.Components.Count; j++)
                {
                    if (row.Components[j] is ButtonBuilder button && button.CustomId == customId)
                    {
                        button.WithDisabled(disabled);
                        await _message.ModifyAsync(message => message.Components = _componentBuilder.Build());
                        return;
                    }
                }
            }
        }
    }
}
