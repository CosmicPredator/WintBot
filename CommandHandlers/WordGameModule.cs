using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using WeCantSpell.Hunspell;

namespace WintBot;

/// <summary>
/// The Class which constitutes the Commands required for Sants Word Guess game.
/// like /word-guess register etc.,
/// This class is scoped to the assembly.
/// The DB servcies are injected to this class.
/// </summary>


[Group("word-guess", "Set of commands used for Santa Word Guess")]
public class WordGameModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly UserDbContext _db;
    public WordGameModule(UserDbContext db) => _db = db;
    private string ins = @"
**Instructions:**
```
🎿 Firstly, a person will say a single word.

🎿 Then, another person will say a word, starting with the last letter of the pervious word.

🎿 For example, a user said `rat` first. Then the second user should say a word starting with letter 't', for example, `tail`.

🎿 The same thing must be repeated until the users find no words.

🎿 Using the internet for word will make the game boring.

🎿 All words will be evaluated with Santa's pocket dictionary.

🎿 A cooldown of 5 senconds will be implemented on each correct word.

🎿 If a word is repeated, it'll not considered for evaluation.

🎿 Of Course, penalties will be present. Play with consciousness.

🎿 U can see the Server's score by using /word-guess score command.
```
";

    // register a specific channel to play word game.
    [SlashCommand("register", "Register a channel for Santa Word Guess")]
    public async Task HandleRegisterCommand(SocketGuildChannel channel)
    {
        var channelExists = _db.WordGameList.Where(
            x => x.GuildId == Context.Guild.Id
        ).FirstOrDefault();

        if (channelExists == null)
        {
            WordGameModel newChannel = new WordGameModel()
            {
                GuildId = Context.Guild.Id,
                Channel = channel.Id,
            };
            _db.WordGameList.Add(newChannel);
            await _db.SaveChangesAsync();
            var eb = new EmbedBuilder()
                        .WithColor(Color.Gold)
                        .WithTitle("Success")
                        .WithAuthor(new EmbedAuthorBuilder()
                        {
                            IconUrl = Context.User.GetAvatarUrl(),
                            Name = Context.User.Username
                        })
                        .WithCurrentTimestamp()
                        .WithDescription($"#{channel.Name} will be used for Santa Word Guess..!")
                        .WithThumbnailUrl("https://i.imgur.com/XzkCEnv.gif");
            await RespondAsync(embed: eb.Build());
            var guildChannel = Context.Guild.GetTextChannel(channel.Id);
            var embed = new EmbedBuilder()
                         .WithTitle("Welcome to Santa Word Guess...!")
                         .WithColor(Color.Purple)
                         .WithThumbnailUrl("https://i.imgur.com/G52TLum.gif");
            await guildChannel.SendMessageAsync(
                embed: embed.Build()
            );
            await guildChannel.SendMessageAsync(
                ins
            );
        }
        else if (channelExists.Channel != channel.Id)
        {
            channelExists.Channel = channel.Id;
            await _db.SaveChangesAsync();
            await RespondAsync("Default channel for Santa Word Guess had been updated...!", ephemeral: true);
        }
        else
        {
            await RespondAsync("Channel already registerd for Santa Word Guess...", ephemeral: true);
        }
    }

    // Handle score command
    [SlashCommand("score", "Gets the current high score of the Santa Word Guess")]
    public async Task HandleScore()
    {
        var obj = _db.WordGameList.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();
        if (obj != null)
        {
            var fields = new List<EmbedFieldBuilder>()
            {
                new EmbedFieldBuilder()
                {
                    Name = "**All time best score**",
                    Value = obj.HighScore.ToString()
                },
                new EmbedFieldBuilder()
                {
                    Name = "**Current score**",
                    Value = obj.score.ToString()
                },
                new EmbedFieldBuilder()
                {
                    Name = "**Penalties**",
                    Value = obj.penalties.ToString()
                }
            };
            var eb = new EmbedBuilder()
                         .WithColor(Color.Magenta)
                         .WithTitle("Santa Word Guess Scores")
                         .WithFields(fields)
                         .WithThumbnailUrl("https://i.imgur.com/vE2mxys.gif")
                         .WithImageUrl("https://i.imgur.com/ZS6LQps.png");
            await RespondAsync(embed: eb.Build());
        }
        else
        {
            await RespondAsync("Santa Word Guess has not been enabled in your server. See /word-game register.", ephemeral: true);
        }
    }

    // Handle reset-score command
    [SlashCommand("reset-score", "Resets the previous scores and High Scores")]
    public async Task HandleResetCommand()
    {
        var obj = _db.WordGameList.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();
        if (obj != null)
        {
            obj.score = 0;
            obj.HighScore = 0;
            await _db.SaveChangesAsync();
            await RespondAsync("Successfully resetted the scores...!", ephemeral: true);
        }
    }

    // Handle instructions connamd
    [SlashCommand("instructions", "Instructions on how to play the Santa Word Guess")]
    public async Task HandleInstructionCommand()
    {
        await RespondAsync(ins, ephemeral: true);
    }

    // Hande buy-penalties command
    [SlashCommand("buy-penalties", "Buy penalties for Santa Word Guess using your Snow Coins.")]
    public async Task HandleAddPenalties(int penalties)
    {
        User? user = _db.UserList.Where(
            x => x.UserId == Context.User.Id &&
                 x.coins != null
        ).FirstOrDefault();
        if (user == null)
        {
            var eb = new EmbedBuilder()
                         .WithTitle("☃️ Oops...!")
                         .WithColor(Color.DarkOrange)
                         .WithDescription("It seems you don't have a wallet. Claim your daily Snow Coins to create one.")
                         .WithImageUrl("https://i.imgur.com/ZS6LQps.png");
            await RespondAsync(embed: eb.Build());
        }
        else
        {
            if (user.coins >= penalties * 20)
            {
                var btns = new ComponentBuilder()
                               .WithButton("Yeah, Buy it", $"confirm_yes_{penalties}", ButtonStyle.Success)
                               .WithButton("Nah, Never mind", $"confirm_no_{penalties}", ButtonStyle.Danger);
                var eb = new EmbedBuilder()
                             .WithTitle("Are you sure?")
                             .WithDescription(
                                $"{penalties} penalties x 50 Coins = **{penalties * 50}** Snow Coins will be debited from your wallet."
                             )
                             .WithColor(Color.DarkTeal)
                             .WithThumbnailUrl("https://i.imgur.com/TW9fT2t.gif")
                             .WithImageUrl("https://i.imgur.com/ZS6LQps.png");
                await RespondAsync(
                    embed: eb.Build(),
                    components: btns.Build(),
                    ephemeral: true
                );
            }
            else
            {
                await RespondAsync($"You didn't have enough Snow Coins to buy **{penalties} penalties**.", ephemeral: true);
            }
        }
    }
}