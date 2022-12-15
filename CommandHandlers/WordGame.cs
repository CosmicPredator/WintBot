using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using WeCantSpell.Hunspell;

public class WordGame : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("wordgame", "Start a word game...")]
    public async Task HandleGameStart()
    {
        await RespondAsync(
            "The game will start from now on... Every word from now will be considered as game."
        );
    }

    [SlashCommand("check", "Check is the word is valid english")]
    public async Task Handle(string word)
    {
        var dictionary = WordList.CreateFromFiles(@"en_US.dic");
        bool Ok = dictionary.Check(word);
        if (Ok)
        {
            await RespondAsync("Valid", ephemeral: true);
        } else 
        {
            await RespondAsync("not valid", ephemeral: true);
        }
    }
}