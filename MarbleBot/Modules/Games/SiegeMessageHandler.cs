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
    private EmbedFieldBuilder? _attackFieldBuilder;
    private readonly ComponentBuilder _componentBuilder;
    private readonly Dictionary<string, ButtonBuilder> _buttonBuilders;

    private readonly BlockingCollection<string> _siegeEvents = new(10);

    private readonly IReadOnlyList<SiegeMarble> _marbles;

    private FileAttachment _thumbnailAttachment;

    private bool _attackFieldEnabled;

    public SiegeMessageHandler(SiegeSession siegeSession, IUserMessage message, EmbedBuilder embedBuilder, ComponentBuilder componentBuilder, Boss boss, IReadOnlyList<SiegeMarble> marbles)
    {
        _message = message;
        _embedBuilder = embedBuilder;
        _componentBuilder = componentBuilder;
        _buttonBuilders = _componentBuilder.ActionRows.SelectMany(rowBuilder => rowBuilder.Components)
            .OfType<ButtonBuilder>()
            .ToDictionary(builder => builder.CustomId, builder => builder);

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

        ToggleButton("siege_bash", false);
        await UpdateMessageAsync();
    }
    
    private async void OnSiegeMessageSent(object? sender, SiegeMessage e)
    {
        switch (e.SessionState)
        {
            case SiegeSessionState.BossCharging:
                ToggleButton("siege_dodge_veryleft", false);
                ToggleButton("siege_dodge_left", false);
                ToggleButton("siege_dodge_right", false);
                ToggleButton("siege_dodge_veryright", false);
                var fullMessage = (e as BossChargingSiegeMessage)!;
                UpdateImage(fullMessage.ImageStream);
                await UpdateMessageAsync();
                break;
            case SiegeSessionState.BossIdle:
                ToggleButton("siege_dodge_veryleft", true);
                ToggleButton("siege_dodge_left", true);
                ToggleButton("siege_dodge_right", true);
                ToggleButton("siege_dodge_veryright", true);

                if (e is AttackCompleteSiegeMessage attackCompleteMessage)
                {
                    UpdateImage(attackCompleteMessage.ImageStream);
                }

                await UpdateMessageAsync();
                break;
            case SiegeSessionState.Complete:
                ToggleButton("siege_bash", true);
                ToggleButton("siege_dodge_veryleft", true);
                ToggleButton("siege_dodge_left", true);
                ToggleButton("siege_dodge_right", true);
                ToggleButton("siege_dodge_veryright", true);
                await UpdateMessageAsync();
                break;
            default:
                break;
        }
    }

    public async Task AddSiegeMessage(SiegeMessage message)
    {
        if (message is BossChargingSiegeMessage bossChargingMessage)
        {
            UpdateAttackField(bossChargingMessage);
            await UpdateMessageAsync();
        }
        else
        {
            DisableAttackField();
            await AddEventAsync(message.Message);
        }
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

        UpdateDescription(e.NewHealth, e.MaxHealth);
        await UpdateMessageAsync();
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

        UpdateMarbleField();
        await UpdateMessageAsync();
    }

    private async Task AddEventAsync(string message)
    {
        if (_siegeEvents.Count == _siegeEvents.BoundedCapacity)
        {
            _siegeEvents.Take();
        }

        _siegeEvents.Add(message);

        UpdateEventField();
        await UpdateMessageAsync();
    }

    private void UpdateEventField()
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
    }

    private void UpdateMarbleField()
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
    }

    private void UpdateAttackField(BossChargingSiegeMessage message)
    {
        string fieldValue;
        if (message.TargetedMarbles.Count == 0)
        {
            fieldValue = "Threatened marbles: None.";
        }
        else
        {
            var stringBuilder = new StringBuilder("Threatened marbles: ");
            foreach (SiegeMarble marble in message.TargetedMarbles)
            {
                stringBuilder.Append($"{marble.Name}, ");
            }

            stringBuilder.Remove(stringBuilder.Length - 2, 2);
            stringBuilder.AppendLine($".\nUse the direction buttons to move out of the way!");
            fieldValue = stringBuilder.ToString();
        }

        _attackFieldBuilder ??= _embedBuilder.Fields.FirstOrDefault(fieldBuilder => fieldBuilder.Name.EndsWith("inbound!"));
        if (_attackFieldBuilder == null)
        {
            _embedBuilder.AddField($"{message.AttackName} inbound!", fieldValue);
        }
        else
        {
            _attackFieldBuilder.WithValue(fieldValue);
            if (!_embedBuilder.Fields.Contains(_attackFieldBuilder))
            {
                _embedBuilder.Fields.Add(_attackFieldBuilder);
            }
        }

        _attackFieldEnabled = true;
    }

    private void UpdateDescription(int newHealth, int maxHealth)
    {
        _embedBuilder.WithDescription($"Health: **{newHealth}**/{maxHealth}");
    }

    private void UpdateImage(MemoryStream imageStream)
    {
        _embedBuilder.WithThumbnailUrl("attachment://Siege.png");

        _thumbnailAttachment = new FileAttachment(imageStream, "Siege.png");
    }

    private void DisableAttackField()
    {
        if (_attackFieldBuilder != null && _attackFieldEnabled)
        {
            _embedBuilder.Fields.Remove(_attackFieldBuilder);
            _attackFieldEnabled = false;
        }
    }

    private void ToggleButton(string customId, bool disabled)
    {
        if (!_buttonBuilders.TryGetValue(customId, out ButtonBuilder? buttonBuilder))
        {
            return;
        }

        buttonBuilder.WithDisabled(disabled);
    }

    private async Task UpdateMessageAsync()
    {
        await _message.ModifyAsync(message =>
        {
            if (_thumbnailAttachment.Stream != null)
            {
                message.Attachments = new[] { _thumbnailAttachment };
            }

            message.Components = _componentBuilder.Build();
            message.Embed = _embedBuilder.Build();
        });
    }
}
