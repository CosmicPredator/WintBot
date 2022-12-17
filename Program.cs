using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace WintBot;

public class Program
{
    public IServiceProvider _services;

    public Program() => _services = CreateProvider();
    public static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

    public static IServiceProvider CreateProvider()
    {

        DiscordSocketConfig config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged,
            LogGatewayIntentWarnings = false,
            AlwaysDownloadUsers = true,
            LogLevel = LogSeverity.Info
        };

        var collection = new ServiceCollection()
                                .AddDbContext<UserDbContext> (
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
        
        await client.LoginAsync(TokenType.Bot, Credentials.Token);
        await client.StartAsync();

        await client.SetGameAsync("People enjoying Christmas...!", null, ActivityType.Watching);

        await Task.Delay(Timeout.Infinite);
    }
}