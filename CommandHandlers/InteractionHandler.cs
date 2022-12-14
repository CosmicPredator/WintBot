using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System.Reflection;

namespace WintBot;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _commands;
    private readonly IServiceProvider _services;

    public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services)
    {
        _client = client;
        _commands = commands;
        _services = services;
    }

    public async Task InitializeAsync()
    {
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        _client.InteractionCreated += HandleInteraction;
    }

    private async Task HandleInteraction(SocketInteraction args)
    {
        try
        {
            var ctx = new SocketInteractionContext(_client, args);
            await _commands.ExecuteCommandAsync(ctx, _services);
        } catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}