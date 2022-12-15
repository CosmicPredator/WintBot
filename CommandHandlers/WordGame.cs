using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

public class WordGame : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("wordgame", "Start a word game...")]
    public async Task HandleGameStart()
    {
        await RespondAsync(
            "The game will start from now on... Every word from now will be considered as game."
        );
    }
}