using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace WintBot;

public class HelpCommandsClass: InteractionModuleBase<SocketInteractionContext>
{
    public enum Choices
    {
        general,
        daily,
        mywallet,
        challenge
    }

    [SlashCommand("help", "Get to know about the bot.")]
    public async Task HandleHelpCommand(Choices choice = Choices.general)
    {
        switch (choice)
        {
            case Choices.daily:
                await RespondAsync(embed: dailyEmbed.Build(), ephemeral: true);
                break;
            case Choices.mywallet:
                await RespondAsync(embed: myWalletEmbed.Build(), ephemeral: true);
                break;
            case Choices.challenge:
                await RespondAsync(embed: challengeEmbed.Build(), ephemeral: true);
                break;
            case Choices.general:
                await RespondAsync(embed: generalEmbed.Build(), ephemeral: true);
                break;
        }
    }

    private EmbedBuilder generalEmbed = new EmbedBuilder()
                    .WithTitle("Hello, I'm Winter Chan")
                    .WithImageUrl("https://i.imgur.com/ZS6LQps.png")
                    .WithThumbnailUrl("https://i.imgur.com/0FGBMbm.png")
                    .WithColor(Color.Blue)
                    .WithDescription(
                        @"
**A Fun bot to make this Christmas Enjoyable...!**


I'm a simple fun bot, which has a currency system and a word game called `Santa Word Guess` to play with.

I also has a number guessing game, to challenge your server members a.k.a friends.

Use `/help` and select a command from the choice to know about it.

U can find my Source Code in [GitHub](https://discord.gg/ggZn8PaQed).

**❄️  Merry Christmas  ❄️**
");

    private EmbedBuilder dailyEmbed = new EmbedBuilder()
                    .WithTitle("The /daily Command.")
                    .WithImageUrl("https://i.imgur.com/ZS6LQps.png")
                    .WithThumbnailUrl("https://i.imgur.com/Wu42LVx.gif")
                    .WithDescription(
                        @"This Command is used by Server Members to collect thier daily coins.

On successful execution, **200 Snow Coins** will be credited to the user's wallet.

When executed by the user for the first time, a unique wallet will be created and 200 coins will be deposited as usual.

A Claim cooldown of 1 hour will be imposed after each claim.


**Commands**
`/daily`");

    private EmbedBuilder myWalletEmbed = new EmbedBuilder()
                    .WithTitle("The /mywallet Command.")
                    .WithImageUrl("https://i.imgur.com/ZS6LQps.png")
                    .WithThumbnailUrl("https://i.imgur.com/Wu42LVx.gif")
                    .WithDescription(
                        @"This Command is used by Server Members to check their Snow Coins amount.

On successful execution, the bot will reply with their Snow Coins balance.

When executed without creating a wallet, the bot will prompt to use `/daily` command.


**Commands**
`/mywallet`");

    private EmbedBuilder challengeEmbed = new EmbedBuilder()
                    .WithTitle("The /challenge Command.")
                    .WithImageUrl("https://i.imgur.com/ZS6LQps.png")
                    .WithThumbnailUrl("https://i.imgur.com/Wu42LVx.gif")
                    .WithDescription(
                        @"This Command is used by Server Members to challenge another Server member for a Number Fight.

On successful execution, the game will start. The user, who started the game, should enter a number for the opponent to guess.

Then that number will shuffled among set of other numbers. the opponent have to guess the number entered by the user.

If successfully guessed, the bet Snow Coins will be credited to the opponent. If failed, the Coins will be credited to the user.

One cannot start a game with insufficient Snow Coins.

**Commands**
`/mywallet <opponent> <bet>`");
}