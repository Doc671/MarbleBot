using Discord;
using Discord.Commands;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using static MarbleBot.Modules.MarbleBotModule;

namespace MarbleBot.Common.Games.War
{
    public class War
    {
        public WarTeam LeftTeam { get; }
        public WarTeam RightTeam { get; }

        private readonly WarMarble? _aiMarble;
        private readonly int _aiMarbleReachDistance;
        private readonly ImmutableArray<WarMarble> _allMarbles;

        private readonly Emoji[] _arrowEmojis =
        {
            new Emoji("\u2B05"),
            new Emoji("\u2B06"),
            new Emoji("\u27A1"),
            new Emoji("\u2B07")
        };

        private readonly SocketCommandContext _context;
        private readonly EmbedBuilder _embedBuilder;
        private readonly List<IEmote> _emojisToReactWith = new();
        private readonly GamesService _gamesService;
        private readonly Grid _grid;
        private readonly ulong _id;
        private readonly Emoji _negativeSquaredCrossMarkEmoji = new("\u274E");
        private readonly RandomService _randomService;
        private readonly Emoji _star2Emoji = new("\uD83C\uDF1F");
        private readonly Timer _timeoutTimer = new(20000);

        private bool _endCalled;
        private bool _finished;
        private IUserMessage? _originalMessage;
        private bool _userMoved;
        private int _turnIndex;

        private enum FieldIndex
        {
            Grid,
            Teams,
            Log,
            Options
        }

        public War(SocketCommandContext context, GamesService gamesService, RandomService randomService, ulong id,
            IEnumerable<WarMarble> team1Marbles, IEnumerable<WarMarble> team2Marbles, WarMarble? aiMarble)
        {
            _context = context;
            _gamesService = gamesService;
            _randomService = randomService;

            _id = id;

            if (aiMarble != null)
            {
                _aiMarble = aiMarble;
                _aiMarbleReachDistance = _aiMarble.Weapon.WeaponClass == WeaponClass.Ranged ? 3 : 1;
            }

            (string team1Name, string team2Name) = GetTeamNames();

            var team1Boost = (WarBoost)_randomService.Rand.Next(1, 4);
            var team2Boost = (WarBoost)_randomService.Rand.Next(1, 4);

            LeftTeam = new WarTeam(team1Name, team1Marbles, true, team1Boost);
            RightTeam = new WarTeam(team2Name, team2Marbles, false, team2Boost);

            var allMarbles = new WarMarble[team1Marbles.Count() + team2Marbles.Count()];
            for (int i = 0; i < allMarbles.Length; i += 2)
            {
                WarMarble team1Marble = team1Marbles.ElementAt(i);
                allMarbles[i] = team1Marble;
                team1Marble.Team = LeftTeam;

                WarMarble team2Marble = team2Marbles.ElementAt(i);
                allMarbles[i + 1] = team2Marble;
                team2Marble.Team = RightTeam;
            }

            _allMarbles = allMarbles.ToImmutableArray();

            _grid = new Grid(LeftTeam, RightTeam, _randomService);

            _embedBuilder = new EmbedBuilder()
            {
                Color = GetColor(context),
                Description = "Loading...",
                Fields = new List<EmbedFieldBuilder>()
                {
                    new EmbedFieldBuilder()
                    {
                        IsInline = true,
                        Name = "Grid",
                        Value = _grid.Display()
                    }
                },
                Title = "Marble War :crossed_swords:"
            };

            _timeoutTimer.Elapsed += TimeoutTimer_Elapsed;
        }

        public async Task Attack(WarMarble userMarble, WarMarble targetMarble, bool usingWeapon, int baseDamage)
        {
            bool aiMarbleTurn = IsAiMarbleTurn();
            if (usingWeapon

                // Melee weapons have a range of 1
                && (userMarble.Weapon.WeaponClass == WeaponClass.Melee && !Grid.IsWithinDistance(userMarble, targetMarble, 1)

                    // Ranged weapons have a range of 3 and can't attack if the user's line of sight is blocked
                    || userMarble.Weapon.WeaponClass == WeaponClass.Ranged
                    && (!Grid.IsWithinDistance(userMarble, targetMarble, 3) || !_grid.IsPathClear(userMarble.Position, targetMarble.Position))))
            {
                if (aiMarbleTurn)
                {
                    _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"**{userMarble.Name}** tried to attack **{targetMarble.Name}**, but cannot reach them!\n";
                }
                else
                {
                    _embedBuilder.Fields[(int)FieldIndex.Log].WithValue($"**{userMarble.Name}** tried to attack **{targetMarble.Name}**, but cannot reach them!\n");
                }

                await UpdateDisplay(false, false, false, false);
                await MoveToNextTurn();
                return;
            }

            bool checkTeamDeaths = false;
            string weaponString = usingWeapon ? $" with **{userMarble.Weapon.Name}**" : "";
            string fieldDescription;
            if (!usingWeapon || userMarble.Weapon.Hits == 1)
            {
                if (usingWeapon && _randomService.Rand.Next(0, 100) > userMarble.Weapon.Accuracy)
                {
                    if (aiMarbleTurn)
                    {
                        _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"**{userMarble.Name}** tried to attack **{targetMarble.Name}**, but missed!\n";
                    }
                    else
                    {
                        _embedBuilder.Fields[(int)FieldIndex.Log].WithValue($"**{userMarble.Name}** tried to attack **{targetMarble.Name}**, but missed!\n");
                    }

                    await UpdateDisplay(false, false, false, false);
                    await MoveToNextTurn();
                    return;
                }

                int damage = CalculateDamage(userMarble, targetMarble, baseDamage);
                targetMarble.DealDamage(damage);
                if (targetMarble.Health == 0)
                {
                    fieldDescription = $"**{userMarble.Name}** dealt **{damage}** damage to **{targetMarble.Name}**{weaponString}, killing them!\n";
                    checkTeamDeaths = true;
                }
                else
                {
                    fieldDescription = $"**{userMarble.Name}** dealt **{damage}** damage to **{targetMarble.Name}**{weaponString}!\n{targetMarble.Name}'s remaining health: **{targetMarble.Health}**/{targetMarble.MaxHealth}\n";
                }
            }
            else
            {
                int hitsPerformed = 0;
                int totalDamage = 0;
                var fieldDescriptionBuilder = new StringBuilder($"**{userMarble.Name}** attacked **{targetMarble}{weaponString}**!\n");
                do
                {
                    hitsPerformed++;
                    if (_randomService.Rand.Next(0, 100) > userMarble.Weapon.Accuracy)
                    {
                        int damage = CalculateDamage(userMarble, targetMarble, baseDamage);
                        totalDamage += damage;
                        targetMarble.DealDamage(damage);
                        fieldDescriptionBuilder.AppendLine($"**{damage}** damage dealt!");
                    }
                    else
                    {
                        fieldDescriptionBuilder.AppendLine("Missed!");
                    }
                }
                while (hitsPerformed < userMarble.Weapon.Hits && targetMarble.Health == 0);

                if (targetMarble.Health == 0)
                {
                    fieldDescriptionBuilder.Append($"**{targetMarble.Name}** was killed!");
                    checkTeamDeaths = true;
                }
                else if (totalDamage == 0)
                {
                    fieldDescriptionBuilder.Append($"All attacks missed!");
                }
                else
                {
                    fieldDescriptionBuilder.AppendLine($"{targetMarble.Name} took a total of **{totalDamage}** damage!\n{targetMarble.Name}'s remaining health: **{targetMarble.Health}**/{targetMarble.MaxHealth}");
                }

                fieldDescription = fieldDescriptionBuilder.ToString();
            }

            if (aiMarbleTurn)
            {
                _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"\n{fieldDescription}";
            }
            else
            {
                _embedBuilder.Fields[(int)FieldIndex.Log].WithValue(fieldDescription);
            }

            _embedBuilder.Fields[(int)FieldIndex.Options].WithValue("None");
            await UpdateDisplay(true, true, false, true);

            await Task.Delay(2000);

            if (checkTeamDeaths && IsATeamDead())
            {
                await OnGameEnd();
                return;
            }

            await MoveToNextTurn();
        }

        private bool IsAiMarbleTurn()
        {
            return _aiMarble != null && IsMarbleTurn(_aiMarble.Id);
        }

        private async Task Boost()
        {
            if (!CanBoost())
            {
                return;
            }

            WarMarble currentMarble = GetCurrentMarble();
            currentMarble.Boosted = true;
            WarTeam enemyTeam = currentMarble.Team!.IsLeftTeam ? RightTeam : LeftTeam;

            _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"\n**{currentMarble.Name}** has attempted to use **Team {currentMarble.Team.Name}**'s boost!";
            await UpdateDisplay(false, false, false, false);

            // Activate boost if enough team members (half rounded up) have chosen to boost
            int boosters = currentMarble.Team.Marbles.Aggregate(0, (total, marble) => marble.Boosted ? total + 1 : total);
            int boostsRequired = (int)MathF.Ceiling(currentMarble.Team.Marbles.Count / 2f);
            if (boosters >= boostsRequired)
            {
                currentMarble.Team.BoostUsed = true;
                var output = new StringBuilder();
                switch (currentMarble.Team.Boost)
                {
                    case WarBoost.HealKit:
                        {
                            IEnumerable<WarMarble> teammatesToHeal = currentMarble.Team.Marbles.OrderBy(m => Guid.NewGuid()).Take(boostsRequired);
                            foreach (WarMarble teammate in teammatesToHeal)
                            {
                                if (teammate.Health > 0)
                                {
                                    teammate.Health += 8;
                                    output.AppendLine($"**{teammate.Name}** recovered **8** health! (**{teammate.Health}**/{teammate.MaxHealth})");
                                }
                            }

                            break;
                        }
                    case WarBoost.MissileStrike:
                        {
                            foreach (WarMarble enemy in enemyTeam.Marbles)
                            {
                                if (enemy.Health > 0)
                                {
                                    enemy.Health -= 5;
                                }
                            }

                            output.Append($"All of **Team {enemyTeam.Name}** took **5** damage!");
                            break;
                        }
                    case WarBoost.Rage:
                        {
                            foreach (WarMarble teammate in currentMarble.Team.Marbles)
                            {
                                teammate.DamageMultiplier *= 2;
                                teammate.LastRage = DateTime.UtcNow;
                                teammate.Rage = true;
                            }

                            output.Append($"**Team {currentMarble.Team.Name}** can deal x2 damage for the next 10 seconds!");
                            break;
                        }
                    case WarBoost.SpikeTrap:
                        {
                            IEnumerable<WarMarble> enemiesToDamage = enemyTeam.Marbles.OrderBy(m => Guid.NewGuid()).Take(boostsRequired);
                            foreach (WarMarble enemy in enemiesToDamage)
                            {
                                if (enemy.Health > 0)
                                {
                                    enemy.Health -= 8;
                                    output.AppendLine($"**{enemy.Name}** took **8** damage! Remaining health: **{enemy.Health}**/{enemy.MaxHealth}");
                                }
                            }

                            break;
                        }
                }

                _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"\nBoost successful! **{currentMarble.Name}** used **{currentMarble.Team.Boost.ToString().CamelToTitleCase()}**!\n{output}";
            }
            else
            {
                _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"\nBoost failed! **{boosters}** out of the required **{boostsRequired}** team members have chosen to use Team {currentMarble.Team.Name}'s **{currentMarble.Team.Boost.ToString().CamelToTitleCase()}**.";
            }

            await UpdateDisplay(false, true, true, true);
        }

        private int CalculateDamage(WarMarble userMarble, WarMarble targetMarble, int baseDamage)
        {
            float spikesMultiplier = userMarble.Spikes?.OutgoingDamageMultiplier ?? 1;
            float shieldMultiplier = targetMarble.Shield?.IncomingDamageMultiplier ?? 1;
            float randomMultiplier = 1 + 0.5f * (float)_randomService.Rand.NextDouble();

            int damage = (int)MathF.Round(baseDamage * spikesMultiplier * shieldMultiplier * randomMultiplier);

            userMarble.DamageDealt += damage;

            return damage;
        }

        private bool CanBoost()
        {
            WarMarble currentMarble = GetCurrentMarble();
            return !(currentMarble.Boosted || currentMarble.Team!.BoostUsed);
        }

        private async Task EndMarbleTurn()
        {
            _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"\n**{_allMarbles[_turnIndex].Name}**'{GetPlural(_allMarbles[_turnIndex].Name)} turn ended.\n";
            await _originalMessage!.RemoveAllReactionsAsync();
            await UpdateDisplay(false, false, true, false);
            await MoveToNextTurn();
        }

        public void Finalise()
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            _gamesService.Wars.TryRemove(_id, out _);
            using var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{_id}.war");
            marbleList.Write("");
            _timeoutTimer.Dispose();
        }

        public WarMarble GetCurrentMarble()
        {
            return _allMarbles[_turnIndex];
        }

        private string GetMoveMessage()
        {
            string[] directionStrings = { "left", "up", "right", "down" };
            (int x, int y)[] directions = { (-1, 0), (0, -1), (1, 0), (0, 1) };
            var validDirectionStrings = new List<string>();
            for (int i = 0; i < directions.Length; i++)
            {
                (int x, int y) = directions[i];
                WarMarble currentMarble = GetCurrentMarble();
                if (_grid.IsValidCoords(currentMarble.Position.X + x, currentMarble.Position.Y + y))
                {
                    validDirectionStrings.Add(directionStrings[i]);
                    _emojisToReactWith.Add(_arrowEmojis[i]);
                }
            }

            var output = new StringBuilder("- React with ");
            for (int i = 0; i < validDirectionStrings.Count - 2; i++)
            {
                output.Append($":arrow_{validDirectionStrings[i]}:, ");
            }

            output.AppendLine($":arrow_{validDirectionStrings[^2]}: or :arrow_{validDirectionStrings[^1]}: to move.");
            return output.ToString();
        }

        private string GetOptionsMessage()
        {
            WarMarble currentMarble = GetCurrentMarble();
            var output = new StringBuilder();
            _emojisToReactWith.Clear();
            if (!_userMoved)
            {
                output.Append(GetMoveMessage());

                if (CanBoost())
                {
                    _emojisToReactWith.Add(_star2Emoji);
                }

                _emojisToReactWith.Add(_negativeSquaredCrossMarkEmoji);
            }

            output.Append("- Use `mb/war attack <marble ID>` to attack an adjacent enemy marble.\n");

            if (!currentMarble.Boosted || !currentMarble.Team!.BoostUsed)
            {
                output.Append("- React with :star2: to use your team's boost.\n");
            }

            output.Append("- React with :negative_squared_cross_mark: to end your turn.\n");

            return output.ToString();
        }

        private string GetTeamInfo()
        {
            var output = new StringBuilder();
            output.AppendLine($"**Team {LeftTeam.Name}**");
            if (!LeftTeam.BoostUsed)
            {
                output.AppendLine($"Boost: **{LeftTeam.Boost.ToString().CamelToTitleCase()}**");
            }

            for (int i = 0; i < LeftTeam.Marbles.Count; i++)
            {
                WarMarble marble = LeftTeam.Marbles.ElementAt(i);
                output.AppendLine($"`[{i + 1}]` {marble.DisplayEmoji} **{marble.Name}** (HP: **{marble.Health}**/{marble.MaxHealth}) [{marble.Username}#{marble.Discriminator}]");
            }

            output.AppendLine($"**Team {RightTeam.Name}**");
            if (!RightTeam.BoostUsed)
            {
                output.AppendLine($"Boost: **{RightTeam.Boost.ToString().CamelToTitleCase()}**");
            }

            for (int i = 0; i < RightTeam.Marbles.Count; i++)
            {
                WarMarble marble = RightTeam.Marbles.ElementAt(i);
                output.AppendLine($"`[{i + 1}]` {marble.DisplayEmoji} **{marble.Name}** (HP: **{marble.Health}**/{marble.MaxHealth}) [{marble.Username}#{marble.Discriminator}]");
            }

            return output.ToString();
        }

        private (string, string) GetTeamNames()
        {
            var nameList = new List<string>();
            using (var teamNames = new StreamReader($"Resources{Path.DirectorySeparatorChar}WarTeamNames.txt"))
            {
                while (!teamNames.EndOfStream)
                {
                    nameList.Add(teamNames.ReadLine()!);
                }
            }

            string team2Name;
            string team1Name = nameList[_randomService.Rand.Next(0, nameList.Count)];
            do
            {
                team2Name = nameList[_randomService.Rand.Next(0, nameList.Count)];
            }
            while (string.CompareOrdinal(team1Name, team2Name) == 0);

            return (team1Name, team2Name);
        }

        private string GetTurnTitle()
        {
            WarMarble currentMarble = GetCurrentMarble();
            return $"**{currentMarble.Name}**'{GetPlural(currentMarble.Name)} turn";
        }

        private static string GetPlural(string word)
        {
            return word[^1] == 's' ? "" : "s";
        }

        public bool IsMarbleTurn(ulong marbleId)
        {
            return _allMarbles[_turnIndex].Id == marbleId;
        }

        private bool IsATeamDead()
        {
            return LeftTeam.Marbles.Sum(m => m.Health) == 0 || RightTeam.Marbles.Sum(m => m.Health) == 0;
        }

        public async Task OnReactionAdded(IEmote emote, IUser user)
        {
            const char negativeSquaredCrossMark = '\u274E';
            const string star2 = "\uD83C\uDF1F";
            if (emote.Name[0] == negativeSquaredCrossMark)
            {
                await EndMarbleTurn();
                return;
            }
            else if (emote.Name == star2)
            {
                await Boost();
                return;
            }

            if (_userMoved || user.Id != _allMarbles[_turnIndex].Id)
            {
                await _originalMessage!.RemoveReactionAsync(emote, user);
                return;
            }

            const char leftArrow = '\u2B05';
            const char upArrow = '\u2B06';
            const char downArrow = '\u2B07';
            const char rightArrow = '\u27A1';
            int changeX = 0, changeY = 0;
            string direction;
            WarMarble currentMarble = GetCurrentMarble();
            switch (emote.Name[0])
            {
                case leftArrow:
                    changeX = -1;
                    direction = "left";
                    break;
                case upArrow:
                    changeY = -1;
                    direction = "up";
                    break;
                case downArrow:
                    changeY = 1;
                    direction = "down";
                    break;
                case rightArrow:
                    changeX = 1;
                    direction = "right";
                    break;
                default:
                    await _originalMessage!.RemoveReactionAsync(emote, user);
                    return;
            }

            if (!_grid.IsValidCoords(currentMarble.Position.X + changeX, currentMarble.Position.Y + changeY))
            {
                return;
            }

            _userMoved = true;
            _grid.MoveMarble(_allMarbles[_turnIndex], changeX, changeY);

            _embedBuilder.Fields[(int)FieldIndex.Log].Value = $"{GetTurnTitle()}\n**{currentMarble.Name}** moved **{direction}**!";
            await UpdateDisplay(true, false, true, false);
        }

        private async Task MoveToNextTurn()
        {
            if (_finished)
            {
                return;
            }

            _userMoved = false;
            _timeoutTimer.Stop();

            int originalTurnIndex = _turnIndex;
            do
            {
                if (++_turnIndex == _allMarbles.Length)
                {
                    _turnIndex = 0;
                }
            }
            while (_allMarbles[_turnIndex].Health == 0 && originalTurnIndex != _turnIndex);

            if (_turnIndex == originalTurnIndex)
            {
                await OnGameEnd();
            }
            else if (IsAiMarbleTurn())
            {
                _embedBuilder.Fields[(int)FieldIndex.Options].WithValue("None");
                _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"\n{GetTurnTitle()}";
                await UpdateDisplay(false, false, false, false);
                await PerformAiMarbleTurn();
            }
            else
            {
                _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"\n{GetTurnTitle()}";
                await UpdateDisplay(false, false, true, true);
                _timeoutTimer.Start();
            }
        }

        private async Task OnGameEnd()
        {
            if (_endCalled)
            {
                return;
            }

            _endCalled = true;
            int team1Total = LeftTeam.Marbles.Sum(m => m.Health);
            int team2Total = RightTeam.Marbles.Sum(m => m.Health);
            WarTeam winningTeam = team1Total > team2Total ? LeftTeam : RightTeam;
            EmbedBuilder? builder = new EmbedBuilder()
                .WithColor(GetColor(_context))
                .WithTitle($"Team {winningTeam.Name} has defeated Team {(team2Total == 0 ? RightTeam : LeftTeam).Name}! :trophy:");
            var team1Output = new StringBuilder();
            var team2Output = new StringBuilder();

            foreach (WarMarble marble in LeftTeam.Marbles)
            {
                team1Output.AppendLine($"**{marble.Name}** (HP: **{marble.Health}**/{marble.MaxHealth}, Wpn: {marble.Weapon}) [{marble.Username}#{marble.Discriminator}]");
            }

            foreach (WarMarble marble in RightTeam.Marbles)
            {
                team2Output.AppendLine($"**{marble.Name}** (HP: **{marble.Health}**/{marble.MaxHealth}, Wpn: {marble.Weapon}) [{marble.Username}#{marble.Discriminator}]");
            }

            builder.AddField($"Team {LeftTeam.Name} Final Stats", team1Output.ToString())
                .AddField($"Team {RightTeam.Name} Final Stats", team2Output.ToString());

            IDictionary<ulong, MarbleBotUser> usersDict = MarbleBotUser.GetUsers();
            foreach (WarMarble marble in winningTeam.Marbles)
            {
                if (_aiMarble != null && marble.Id == _aiMarble.Id)
                {
                    continue;
                }

                MarbleBotUser user = await MarbleBotUser.FindAsync(_context, usersDict, marble.Id);
                if ((DateTime.UtcNow - user.LastWarWin).TotalHours > 6 && marble.DamageDealt > 0)
                {
                    var output = new StringBuilder();
                    int earnings = marble.DamageDealt * 5;
                    output.AppendLine($"Damage dealt (x5): {UnitOfMoney}**{earnings:n2}**");
                    user.WarWins++;

                    if (marble.Health > 0)
                    {
                        earnings += 200;
                        output.AppendLine($"Alive bonus: {UnitOfMoney}**{200:n2}**");
                    }

                    if (user.Items.ContainsKey(83))
                    {
                        earnings *= 3;
                        output.AppendLine("Pendant bonus: x**3**");
                    }

                    if (output.Length > 0)
                    {
                        user.LastWarWin = DateTime.UtcNow;
                        user.Balance += earnings;
                        user.NetWorth += earnings;
                        output.AppendLine($"__**Total: {UnitOfMoney}{earnings:n2}**__");
                        builder.AddField($"**{marble.Username}**'s earnings", output.ToString());
                        usersDict.Remove(marble.Id);
                        usersDict.Add(marble.Id, user);
                    }
                }
            }

            await _context.Channel.SendMessageAsync(embed: builder.Build());
            MarbleBotUser.UpdateUsers(usersDict);
            Finalise();
        }

        private async Task PerformAiMarbleTurn()
        {
            await Task.Delay(2000);
            WarMarble[] reachableEnemies = LeftTeam.Marbles.Where(marble => Grid.IsWithinDistance(_aiMarble!, marble, _aiMarbleReachDistance)).ToArray();
            WarMarble randomEnemyMarble;

            if (!reachableEnemies.Any())
            {
                do
                {
                    randomEnemyMarble = LeftTeam.Marbles.ElementAt(_randomService.Rand.Next(0, reachableEnemies.Length));
                }
                while (randomEnemyMarble.Health == 0);

                int xDifference = randomEnemyMarble.Position.X - _aiMarble!.Position.X;
                int yDifference = randomEnemyMarble.Position.Y - _aiMarble!.Position.Y;
                if (Math.Abs(yDifference) > Math.Abs(xDifference))
                {
                    int yChange = Math.Sign(yDifference);
                    if (_grid.IsValidCoords(_aiMarble.Position.X, _aiMarble.Position.Y + yChange))
                    {
                        _grid.MoveMarble(_aiMarble, 0, yChange);
                        _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"\n**{_aiMarble!.Name}** moved!";
                    }
                }
                else
                {
                    int xChange = Math.Sign(xDifference);
                    if (_grid.IsValidCoords(_aiMarble.Position.X + xChange, _aiMarble.Position.Y))
                    {
                        _grid.MoveMarble(_aiMarble, xChange, 0);
                        _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"\n**{_aiMarble!.Name}** moved!";
                    }
                }

                await UpdateDisplay(true, false, false, false);
            }

            reachableEnemies = LeftTeam.Marbles.Where(marble => Grid.IsWithinDistance(_aiMarble!, marble, _aiMarbleReachDistance)).ToArray();

            if (!reachableEnemies.Any())
            {
                _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"\n**{_aiMarble!.Name}**'{GetPlural(_aiMarble.Name)} turn ended.\n";
                await MoveToNextTurn();
                await UpdateDisplay(false, false, false, false);
                return;
            }

            do
            {
                randomEnemyMarble = reachableEnemies[_randomService.Rand.Next(0, reachableEnemies.Length)];
            }
            while (randomEnemyMarble.Health == 0);

            await Attack(_aiMarble!, randomEnemyMarble, true, _aiMarble!.Weapon.Damage);
        }

        public async Task Start()
        {
            _originalMessage = await _context.Channel.SendMessageAsync(embed: _embedBuilder.Build());
            _embedBuilder.AddField("Team info", GetTeamInfo(), true);
            _embedBuilder.AddField("Log", GetTurnTitle());
            _embedBuilder.AddField($"**{_allMarbles[_turnIndex].Name}**'s options", ".");
            _embedBuilder.Description = null;
            await UpdateDisplay(false, false, true, true);
        }

        private async void TimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _embedBuilder.Fields[(int)FieldIndex.Log].Value += $"\n**{GetCurrentMarble().Name}** timed out!";
            await EndMarbleTurn();
        }

        private async Task UpdateDisplay(bool updateGrid, bool updateTeams, bool updateOptions, bool updateOptionEmojis)
        {
            if (updateGrid)
            {
                _embedBuilder.Fields[(int)FieldIndex.Grid].Value = _grid.Display();
            }

            if (updateTeams)
            {
                _embedBuilder.Fields[(int)FieldIndex.Teams].WithValue(GetTeamInfo());
            }

            if (updateOptions)
            {
                _embedBuilder.Fields[(int)FieldIndex.Options].Name = $"**{_allMarbles[_turnIndex].Name}**'s options";
                _embedBuilder.Fields[(int)FieldIndex.Options].Value = GetOptionsMessage();
            }

            if (await _context.Channel.GetMessageAsync(_originalMessage!.Id) == null)
            {
                Finalise();
                return;
            }

            await _originalMessage!.ModifyAsync(message => message.Embed = _embedBuilder.Build());

            if (updateOptionEmojis)
            {
                if (!IsAiMarbleTurn())
                {
                    await _originalMessage!.RemoveAllReactionsAsync();
                    await _originalMessage.AddReactionsAsync(_emojisToReactWith.ToArray());
                }
            }
        }
    }
}
