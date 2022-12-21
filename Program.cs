using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WeCantSpell.Hunspell;

namespace WintBot;

/// <summary>
/// The Main Entry point of the bot.
/// Initializes the IServicesCollection for injecting
/// throughout the Assembly.
/// This bot is hosted on Railway.app as a Docker Conatiner.
/// Uses Dependency Injection to porcess all InteractionModuleBase Objects.
/// </summary>

public class Program
{    
    public IServiceProvider _services;
    private List<WordPlayer> wordHistory = new();
    public Program() => _services = CreateProvider();
    public static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

    public static IServiceProvider CreateProvider()
    {
        
        // Given Message Content Previlage to read the Guild's messages 
        // for Santa Word Guess game.

        // Create the config for the bot.
        DiscordSocketConfig config = new()
        {
            GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.AllUnprivileged,
            LogGatewayIntentWarnings = false,
            AlwaysDownloadUsers = true,
            LogLevel = LogSeverity.Info
        };

        // Adding all services to the Container
        // The InMemoryDatabase will be alive as long as the bot is online.
        var collection = new ServiceCollection()
                                .AddDbContext<UserDbContext>(
                                    x => x.UseInMemoryDatabase("user_db")
                                )
                                .AddSingleton(config)
                                .AddSingleton<DiscordSocketClient>()
                                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                                .AddSingleton<InteractionHandler>();

        // Finally building the countainer
        return collection.BuildServiceProvider();
    }

    // The "Asynced" Main Function
    public async Task MainAsync()
    {
        var client = _services.GetRequiredService<DiscordSocketClient>();
        var sCommands = _services.GetRequiredService<InteractionService>();
        await _services.GetRequiredService<InteractionHandler>().InitializeAsync();


        // A Simple handler to handle logging, from the base DiscordSocketClient
        client.Log += async (LogMessage message) =>
        {
            await Task.CompletedTask;
            Console.WriteLine(message.ToString());
        };

        // A Simple handler to log from CommandHandlers
        sCommands.Log += async (LogMessage message) =>
        {
            Console.WriteLine(message.ToString());
            await Task.CompletedTask;
        };

        // When client is ready, sync all commands globally
        client.Ready += async () =>
        {
            await sCommands.RegisterCommandsGloballyAsync();
        };

        // Not a recommended way, but the best optimized wat to handle message.
        // So the Santa Word Game is implemented Straight away.
        client.MessageReceived += HandleWordAsync;
 
        // In dev environment
        // await client.LoginAsync(TokenType.Bot, "<TOKEN_HERE>");

        // In prod environment
        await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TOKEN"));

        // Stating the client to poll
        await client.StartAsync();

        // Setting the Rich Presence of the bot.
        await client.SetGameAsync("People enjoying Christmas...!", null, ActivityType.Watching);

        // Make the bot run forever
        await Task.Delay(Timeout.Infinite);
    }


    /// <summary>
    /// The one and only function to handle the message recieved and evaluate according
    /// to the rules of Santa Word Guess game.
    /// </summary>
    public async Task HandleWordAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;
        SocketGuildUser user = (SocketGuildUser)message.Author;
        SocketUserMessage msg = (SocketUserMessage)message;
        var _db = _services.GetRequiredService<UserDbContext>();
        var guildExists = _db.WordGameList.Where(
            x => x.GuildId == user.Guild.Id
        ).FirstOrDefault();

        // I used the standard en_US deictionary for evaluating the words.
        var dictionary = WordList.CreateFromFiles(@"en_US.dic", @"en_US.aff");
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
                            char lastLetter = message.Content[^1];
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
                                char lastLetter = message.Content[^1];
                                char firstLetter = message.Content[0];
                                if (char.ToLower(firstLetter) == char.ToLower((char)prevMessage.LastWord!))
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
                                                    .WithDescription($"**'{message.Content}'** didn't start with the letter **'{prevMessage.LastWord!}'**. The game starts again...!")
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
                                                .WithDescription("Same user should not play again. Restart the game from start.")
                                                .WithThumbnailUrl("https://i.imgur.com/wADiNP5.gif");
                                await message.Channel.SendMessageAsync(embed: embed.Build());
                            }
                        }
                    }
                    else
                    {
                        // if message is wrong, reply with "X" symbol.
                        await message.Channel.SendMessageAsync($"**'{message.Content}'** is not a valid english word...!");
                        await message.AddReactionAsync(Emoji.Parse("❌"));
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
                                 .WithDescription($"**'{message.Content}'** was said previously, try typing a new word. \n\n**{guildExists.penalties+1}/3** penalties left.")
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

// An object required in the wordgame,
// to keep track of things
public class WordPlayer
{
    public ulong? UserId { get; set; }
    public string? Message { get; set; }
    public char? LastWord { get; set; }
}