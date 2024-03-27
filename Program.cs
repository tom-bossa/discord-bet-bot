using System.Text;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace BetBot;

internal static class Program {
    private static DiscordSocketClient _client = new();

    private static List<Bet> _bets = new();

    public static async Task Main(string[] args) {
        var config = new DiscordSocketConfig {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        };
        if (File.Exists("bets.json")) {
            _bets = JsonConvert.DeserializeObject<List<Bet>>(await File.ReadAllTextAsync("bets.json")) ?? new List<Bet>();
        }

        // It is recommended to Dispose of a client when you are finished
        // using it, at the end of your app's lifetime.
        _client = new DiscordSocketClient(config);

        // Subscribing to client events, so that we may receive them whenever they're invoked.
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;
        _client.InteractionCreated += InteractionCreatedAsync;
        var token = await File.ReadAllTextAsync("token.txt");
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        // Block the program until it is closed.
        await Task.Delay(Timeout.Infinite);
    }

    private static Task LogAsync(LogMessage log) {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private static Task ReadyAsync() {
        Console.WriteLine($"{_client.CurrentUser} is connected!");
        return Task.CompletedTask;
    }

    private static async Task MessageReceivedAsync(SocketMessage message) {
        // The bot should never respond to itself.
        if (message.Author.Id == _client.CurrentUser.Id) {
            return;
        }

        var parts = message.Content.Split(' ');

        if (parts[0] == "!bet") {
            if (parts.Length <= 2) {
                await message.Channel.SendMessageAsync("Usage: `!bet <amount (defaults to " + MoneyString(20) +
                                                       " if skipped)> <@user> <description>`");
                return;
            }
            await CommandBet(message, parts);
        }

        if (parts[0] == "!stats") {
            await CommandBetStats(message);
        }

        if (parts[0] == "!unresolved") {
            await CommandBetsUnresolved(message);
        }

        if (parts[0] == "!help") {
            await CommandBetHelp(message);
        }

        if (parts[0] == "!leaderboard") {
            await CommandBetLeaderboard(message);
        }
    }

    private static async Task CommandBetLeaderboard(SocketMessage message) {
        var userBalances = new Dictionary<ulong, int>();
        foreach (var bet in _bets) {
            var moneyDirection = bet.IsWin ? 1 : -1;
            if (userBalances.ContainsKey(bet.From)) {
                userBalances[bet.From] += bet.Amount * moneyDirection;
            } else {
                userBalances.Add(bet.From, bet.Amount * moneyDirection);
            }

            if (userBalances.ContainsKey(bet.To)) {
                userBalances[bet.To] -= bet.Amount * moneyDirection;
            } else {
                userBalances.Add(bet.To, -bet.Amount * moneyDirection);
            }
        }

        var sortedBalances = userBalances.OrderByDescending(kvp => kvp.Value).ToList();
        var sb = new StringBuilder();
        sb.Append("**Leaderboard\n```cpp\n");
        if (sortedBalances.Count <= 0) {
            sb.Append("No bets have been placed yet!\n");
        } else {
            for (var i = 0; i < sortedBalances.Count; i++) {
                var user = await _client.GetUserAsync(sortedBalances[i].Key);
                string moneyString = MoneyString(sortedBalances[i].Value);
                sb.Append((i + 1 + ". " + GetDisplayName(user) + " ").PadRight(38 - moneyString.Length) + moneyString + "\n");
            }
        }

        sb.Append("```**");

        await message.Channel.SendMessageAsync(sb.ToString());
    }

    private static async Task CommandBetHelp(SocketMessage message) {
        var helpMessage = @"
**Commands:**
- `!bet amount @user description`: Place a bet with a specified user, amount (optional : defaults to 20), and description. Example: `!bet @JohnDoe Pizza bet`
- `!unresolved`: Re-display any bets that are still unresolved.
- `!stats @user`: Show statistics for a user (bets won, who they owe money to, who owes them etc) (defaults to the author if no user specified).
- `!leaderboard`: Display a leaderboard of users based on their overall balances.
- `!help`: Display this help message.
";
        await message.Channel.SendMessageAsync(helpMessage);
    }

    private static async Task CommandBetsUnresolved(SocketMessage message) {
        var unresolvedBets = _bets.Where(b => !b.HasResolved).ToList();
        if (unresolvedBets.Count == 0) {
            await message.Channel.SendMessageAsync("No unresolved bets found");
            return;
        }

        foreach (var bet in unresolvedBets) {
            var betTo = await _client.GetUserAsync(bet.To);
            var betFrom = await _client.GetUserAsync(bet.From);
            await message.Channel.SendMessageAsync(GetDisplayName(betFrom) + " has bet " + GetDisplayName(betTo) + " " + MoneyString(bet.Amount) +
                                                   " on : _" + bet.Description + "_\n" +
                                                   "**Waiting for outcome:**", components: GetComponentBuilder(betFrom, betTo, bet).Build());
        }
    }

    private static async Task CommandBetStats(SocketMessage message) {
        var user = message.Author;
        if (message.MentionedUsers.Count == 1) {
            user = message.MentionedUsers.First();
        }

        var userBets = _bets.Where(b => b.From == user.Id || b.To == user.Id).ToList();
        if (userBets.Count == 0) {
            await message.Channel.SendMessageAsync("**" + GetDisplayName(user) + " has no bets**");
            return;
        }
        // bets placed, won, total money bet, actual end balance, and breakdowns of individuals you owe money to or are owed money from
        var betsInvolvedIn = userBets.Count;
        var betsWon = 0;
        //var totalMoneyBet = 0;
        var actualBalance = 0;
        Dictionary<ulong, int> userTotals = new();

        foreach (var bet in userBets) {
            if (bet.From == user.Id) {
                if (bet.IsWin) {
                    betsWon++;
                }

                var dir = bet.IsWin ? 1 : -1;
                //totalMoneyBet += bet.Amount;
                actualBalance += bet.Amount * dir;
                if (userTotals.ContainsKey(bet.To)) {
                    userTotals[bet.To] += bet.Amount * dir;
                } else {
                    userTotals.Add(bet.To, bet.Amount * dir);
                }
            } else if (bet.To == user.Id) {
                if (!bet.IsWin) {
                    betsWon++;
                }

                var dir = !bet.IsWin ? 1 : -1;
                //totalMoneyBet += bet.Amount;
                actualBalance += bet.Amount * dir;
                if (userTotals.ContainsKey(bet.From)) {
                    userTotals[bet.From] += bet.Amount * dir;
                } else {
                    userTotals.Add(bet.From, bet.Amount * dir);
                }
            }
        }

        var userBreakdown = "```diff\n";
        foreach (var kvp in userTotals) {
            var otherUser = await _client.GetUserAsync(kvp.Key);
            userBreakdown += (kvp.Value > 0 ? "+  " : "-  ") + GetDisplayName(otherUser) + " : " + MoneyString(kvp.Value) + "\n";
        }

        userBreakdown += "```";

        await message.Channel.SendMessageAsync("\n\n**" + GetDisplayName(user) + "\n" +
                                               "CURRENT BALANCE: " + MoneyString(actualBalance) + "**\n" +
                                               "**```cpp\n" + betsWon + " / " + betsInvolvedIn + " Bets Won```**\n" +
                                               "**_User Breakdown_** : \n" +
                                               userBreakdown);
    }

    private static string MoneyString(int amount) {
        return amount < 0 ? "-£" + Math.Abs(amount) : "£" + amount;
    }

    private static async Task CommandBet(SocketMessage message, string[] parts) {
        var amount = 20;
        var skipCount = 2;
        if (int.TryParse(parts[1], out var overrideAmount)) {
            amount = overrideAmount;
            skipCount = 3;
        }

        if (amount <= 0) {
            await message.Channel.SendMessageAsync("Amount must be greater than 0!");
            return;
        }

        if (message.MentionedUsers.Count != 1) {
            await message.Channel.SendMessageAsync("You need to mention a user to bet with!");
            return;
        }

        var bet = new Bet {
            Id = message.Id,
            TimeStamp = (ulong)DateTimeOffset.Now.ToUnixTimeSeconds(),
            From = message.Author.Id,
            To = message.MentionedUsers.First().Id,
            Amount = amount,
            Description = string.Join(' ', parts.Skip(skipCount)),
            HasResolved = false
        };
        _bets.Add(bet);
        await File.WriteAllTextAsync("bets.json", JsonConvert.SerializeObject(_bets));
        
        await message.Channel.SendMessageAsync(message.Author.Mention + " has bet " + message.MentionedUsers.First().Mention + " " +
                                               MoneyString(amount) +
                                               " on : _" + bet.Description + "_\n" +
                                               "**Waiting for outcome:**", components: GetComponentBuilder(message.Author, message.MentionedUsers.First(), bet).Build());
    }


    private static ComponentBuilder GetComponentBuilder(IUser from, IUser to, Bet bet) {
        var cb = new ComponentBuilder();
        cb = cb.WithButton(GetDisplayName(from) + " Wins it", bet.Id + "-win");
        cb = cb.WithButton(GetDisplayName(to) + " Denies them", bet.Id + "-lose");
        cb = cb.WithButton("Cancel Bet", bet.Id + "-cancel");
        return cb;
    } 

    private static string GetDisplayName(IUser user) {
        if (string.IsNullOrEmpty(user.GlobalName)) {
            return user.Username;
        }
        return user.GlobalName;
    }

    private static async Task InteractionCreatedAsync(SocketInteraction interaction) {
        if (interaction is SocketMessageComponent component) {
            var idData = component.Data.CustomId.Split('-');
            if (!ulong.TryParse(idData[0], out var id)) {
                await interaction.RespondAsync("Invalid ID");
                return;
            }

            var bet = _bets.FirstOrDefault(b => b.Id == id);
            if (bet == null) {
                await interaction.RespondAsync("Couldn't find this bet in the list of active bets");
                return;
            }

            if (bet.HasResolved) {
                await interaction.RespondAsync("This bet has already been resolved");
                return;
            }

            // Delete the previous message
            await component.Message.DeleteAsync();

            bet.HasResolved = true;

            var betTo = await _client.GetUserAsync(bet.To);
            var betFrom = await _client.GetUserAsync(bet.From);

            var replyString = "";
            if (idData[1] == "cancel") {
                _bets.Remove(bet);
                replyString = "The bet has been cancelled";
            }

            if (idData[1] == "win") {
                bet.IsWin = true;
                replyString = WordLookups.WinBetResponses[new Random().Next(WordLookups.WinBetResponses.Length)];
                replyString = replyString.Replace("%WINNER%", betFrom.Mention);
                replyString = replyString.Replace("%LOSER%", betTo.Mention);
            }

            if (idData[1] == "lose") {
                bet.IsWin = false;
                replyString = WordLookups.LostBetResponses[new Random().Next(WordLookups.LostBetResponses.Length)];
                replyString = replyString.Replace("%WINNER%", betTo.Mention);
                replyString = replyString.Replace("%LOSER%", betFrom.Mention);
            }

            replyString = replyString.Replace("%AMOUNT%", MoneyString(bet.Amount));
            replyString = replyString.Replace("%DESCRIPTION%", bet.Description);

            // Report back the bet result
            await interaction.Channel.SendMessageAsync(replyString);

            await File.WriteAllTextAsync("bets.json", JsonConvert.SerializeObject(_bets));
        }
    }
}
