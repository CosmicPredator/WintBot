using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WeCantSpell.Hunspell;

namespace WintBot;

public class Program
{
    public IServiceProvider _services;
    private List<WordPlayer> wordHistory = new();
    public Program() => _services = CreateProvider();
    public static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

    public static IServiceProvider CreateProvider()
    {

        DiscordSocketConfig config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.AllUnprivileged,
            LogGatewayIntentWarnings = false,
            AlwaysDownloadUsers = true,
            LogLevel = LogSeverity.Info
        };

        var collection = new ServiceCollection()
                                .AddDbContext<UserDbContext>(
                                    x => x.UseSqlite("DataSource=DBSource/users.db")
                                )
                                .AddSingleton(config)
                                .AddSingleton<DiscordSocketClient>()
                                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                                .AddSingleton<InteractionHandler>();

        return collection.BuildServiceProvider();
    }

    public async Task MainAsync()
    {
        var client = _services.GetRequiredService<DiscordSocketClient>();
        var sCommands = _services.GetRequiredService<InteractionService>();
        await _services.GetRequiredService<InteractionHandler>().InitializeAsync();


        client.Log += async (LogMessage message) =>
        {
            await Task.CompletedTask;
            Console.WriteLine(message.ToString());
        };

        sCommands.Log += async (LogMessage message) =>
        {
            Console.WriteLine(message.ToString());
            await Task.CompletedTask;
        };

        client.Ready += async () =>
        {
            await sCommands.RegisterCommandsToGuildAsync(1041363391790465075);
        };

        client.MessageReceived += HandleWordAsync;

        await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TOKEN"));
        await client.StartAsync();

        await client.SetGameAsync("People enjoying Christmas...!", null, ActivityType.Watching);

        await Task.Delay(Timeout.Infinite);
    }


    public async Task HandleWordAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;
        SocketGuildUser user = (SocketGuildUser)message.Author;
        SocketUserMessage msg = (SocketUserMessage)message;
        var _db = _services.GetRequiredService<UserDbContext>();
        var guildExists = _db.WordGameList.Where(
            x => x.GuildId == user.Guild.Id
        ).FirstOrDefault();
        var dictionary = WordList.CreateFromFiles(@"en_US.dic");
        if (guildExists != null)
        {
            if (message.Channel.Id == guildExists.Channel)
            {
                var duplicate = wordHistory.Where(x => x.Message == message.Content).FirstOrDefault();
                if (duplicate == null)
                {
                    bool ok = dictionary.Check(message.Content.ToString().ToLower());
                    if (ok)
                    {
                        if (wordHistory.Count == 0)
                        {
                            char lastLetter = message.Content[message.Content.Length - 1];
                            wordHistory.Add(
                                        new WordPlayer()
                                        {
                                            UserId = message.Author.Id,
                                            Message = message.Content,
                                            LastWord = lastLetter
                                        }
                                    );
                            guildExists.score += 1;
                            await _db.SaveChangesAsync();
                            await message.AddReactionAsync(Emoji.Parse("✅"));
                        }
                        else
                        {
                            WordPlayer? prevMessage = wordHistory.Last();
                            if (message.Author.Id != prevMessage.UserId)
                            {
                                char lastLetter = message.Content[message.Content.Length - 1];
                                char firstLetter = message.Content[0];
                                if (Char.ToLower(firstLetter) == Char.ToLower((char)prevMessage.LastWord!))
                                {
                                    wordHistory.Add(
                                        new WordPlayer()
                                        {
                                            UserId = message.Author.Id,
                                            Message = message.Content,
                                            LastWord = lastLetter
                                        }
                                    );
                                    guildExists.score += 1;
                                    if (guildExists.score == guildExists.HighScore)
                                    {
                                        var emb = new EmbedBuilder()
                                                    .WithTitle("New High Score...!")
                                                    .WithDescription("You guys beat your pervious high score.")
                                                    .WithColor(Color.LightOrange)
                                                    .WithThumbnailUrl("https://i.imgur.com/t7J6krZ.gif");
                                        await message.Channel.SendMessageAsync(embed: emb.Build());
                                    }
                                    await _db.SaveChangesAsync();
                                    await message.AddReactionAsync(Emoji.Parse("✅"));
                                }
                                else
                                {
                                    guildExists.HighScore = guildExists.score;
                                    guildExists.score = 0;
                                    await _db.SaveChangesAsync();
                                    wordHistory.Clear();
                                    var embed = new EmbedBuilder()
                                                    .WithColor(Color.Orange)
                                                    .WithTitle("Oops...!")
                                                    .WithDescription("That was a wrong word. The game starts again...!")
                                                    .WithThumbnailUrl("https://i.imgur.com/wADiNP5.gif");
                                    await message.Channel.SendMessageAsync(embed: embed.Build());
                                }
                            }
                            else
                            {
                                guildExists.HighScore = guildExists.score;
                                guildExists.score = 0;
                                await _db.SaveChangesAsync();
                                wordHistory.Clear();
                                var embed = new EmbedBuilder()
                                                .WithColor(Color.Orange)
                                                .WithTitle("Alas...!")
                                                .WithDescription("Same user message again. Start the game from start")
                                                .WithThumbnailUrl("https://i.imgur.com/wADiNP5.gif");
                                await message.Channel.SendMessageAsync(embed: embed.Build());
                            }
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("That's not a valid english word...!");
                        await message.DeleteAsync();
                    }
                }
                else
                {
                    if (guildExists.penalties != 0)
                    {
                        guildExists.penalties -= 1;
                        await _db.SaveChangesAsync();
                        var eb = new EmbedBuilder()
                                 .WithColor(Color.DarkRed)
                                 .WithDescription($"This word was said previously, try typing a new word. \n\n**{guildExists.penalties+1}/3** penalties left.")
                                 .WithTitle("Penalty...!");
                        await message.Channel.SendMessageAsync(embed: eb.Build());
                    }
                    else
                    {
                        guildExists.penalties = 3;
                        await _db.SaveChangesAsync();
                        guildExists.HighScore = guildExists.score;
                        guildExists.score = 0;
                        await _db.SaveChangesAsync();
                        wordHistory.Clear();
                        var embed = new EmbedBuilder()
                                        .WithColor(Color.Orange)
                                        .WithTitle("Oops...!")
                                        .WithDescription("The penalties are over, Start from first.")
                                        .WithThumbnailUrl("https://i.imgur.com/wADiNP5.gif");
                        await message.Channel.SendMessageAsync(embed: embed.Build());
                    }
                }
            }
        }
    }
}

public class WordPlayer
{
    public ulong? UserId { get; set; }
    public string? Message { get; set; }
    public char? LastWord { get; set; }
}