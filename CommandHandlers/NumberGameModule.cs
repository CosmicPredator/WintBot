using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace WintBot;

/// <summary>
/// The Class which constitutes the Commands required for Number Game
/// like /challenge etc.,
/// This class is scoped to the assembly.
/// The DB servcies are injected to this class.
/// </summary>


public class NumberGameModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly UserDbContext _db;

    // The DB Context is injected
    public NumberGameModule(UserDbContext db) => _db = db;

    // Handler for /challenge command
    [SlashCommand("challenge", "Challenge an user to play the Snow Guess game")]
    public async Task HandleChallengeCommand(SocketGuildUser opponent, int bet)
    {
        var userCoins = _db.UserList.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
        var opponentCoins = _db.UserList.Where(x => x.UserId == opponent.Id).FirstOrDefault();
        if (userCoins == null || userCoins.coins == null)
        {
            await RespondAsync("It seems you didn't created a wallet yet. To create a wallet, use /daily", ephemeral: true);
        } else if (opponentCoins == null || opponentCoins == null)
        {
            await RespondAsync($"It seems your opponent **{opponent.Username}** didn't created a wallet yet. To create a wallet, use /daily", ephemeral: true);
        }
        else
        {
            if (userCoins.coins < bet)
            {
                await RespondAsync("You don't have that much Snow Coins to play with..!", ephemeral: true);
            } else if (opponentCoins.coins < bet)
            {
                await RespondAsync("Your opponent didn't have that much Snow Coins to play with..!", ephemeral: true);
            }
            else
            {
                NumberGame newGame = new NumberGame()
                {
                    GuildId = Context.Guild.Id,
                    SelfUserId = Context.User.Id,
                    OpponentUserId = opponent.Id,
                    betAmount = bet
                };
                _db.NumberGameList.Add(newGame);
                await _db.SaveChangesAsync();
                var eb = new EmbedBuilder()
                             .WithTitle("?????? New Challenge")
                             .WithDescription($"{Context.User.Mention} challenged you to the Number game for **{bet.ToString()}** Snow Coins")
                             .WithThumbnailUrl("https://i.imgur.com/oHNGdW1.gif")
                             .WithColor(Color.Blue)
                             .WithImageUrl("https://i.imgur.com/ZS6LQps.png");
                var buttons = new ComponentBuilder()
                                  .WithButton("Agree", $"cmd_agree_{Context.User.Id}_{opponent.Id}", ButtonStyle.Success)
                                  .WithButton("Decline", $"cmd_decline_{Context.User.Id}_{opponent.Id}", ButtonStyle.Danger);
                await RespondAsync($"||{opponent.Mention}||", embed: eb.Build(), components: buttons.Build());
            }
        }
    }

    [ComponentInteraction("cmd_decline_*_*")]
    public async Task HandleDeclineCommand(string user, string opponent)
    {
        if (Context.User.Id == Convert.ToUInt64(user))
        {
            await RespondAsync("That should be done by the opponent...", ephemeral: true);
        }
        else if (Context.User.Id == Convert.ToUInt64(opponent))
        {
            var message = (SocketMessageComponent)Context.Interaction;
            await message.Message.DeleteAsync();
        }
        else
        {
            await RespondAsync("This game is not for you, try creating a new game...", ephemeral: true);
        }
    }

    [ComponentInteraction("cmd_agree_*_*")]
    public async Task HandleAgreeCommand(string user, string opponent)
    {
        if (Context.User.Id != Convert.ToUInt64(user) && Context.User.Id != Convert.ToUInt64(opponent))
        {
            await RespondAsync("This game is not for you. Try creating a game yourself...", ephemeral: true);
        }
        else
        {
            if (Context.User.Id == Convert.ToUInt64(user))
            {
                await RespondAsync("Wait for the opponent to accept the challenge.", ephemeral: true);
            }
            else if (Context.User.Id == Convert.ToUInt64(opponent)) 
            {
                var opponentObj = await Context.Channel.GetUserAsync(Convert.ToUInt64(opponent));
                var opponentDailyObj = _db.UserList.Where(x => x.Id == Convert.ToUInt64(opponent)).FirstOrDefault();
                if (opponentDailyObj == null || opponentDailyObj!.coins == null)
                {
                    var userObject = await Context.Channel.GetUserAsync(Convert.ToUInt64(user));
                    var btn = new ComponentBuilder()
                            .WithButton("Click Here", $"cmd_open_num_modal_{user}_{opponent}", ButtonStyle.Primary);
                    await RespondAsync($"{userObject.Mention}, click here to enter your number... \n Be aware that you have **only one chance** to submit your number.", 
                        components: btn.Build()
                    );
                    var msg = (SocketMessageComponent)Context.Interaction;
                    await msg.Message.DeleteAsync();
                } else 
                {
                    await RespondAsync($"{opponentObj.Mention}, it seems you didn't have enough coins to bet with...");
                }
            }
        }
    }

    [ComponentInteraction("cmd_open_num_modal_*_*")]
    public async Task HandleModalOpenCommand(string user, string opponent)
    {
        if (Context.User.Id == Convert.ToUInt64(user))
        {
            await RespondWithModalAsync<NumberModal>($"num_modal_{user}_{opponent}");
            if (Context.Interaction.HasResponded)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(
                    x => 
                    {
                        x.Components = new ComponentBuilder().Build();
                        x.Content = "You have used your chance.";
                    }
                );
            }
        }
        else
        {
            await RespondAsync("This is not for you...", ephemeral: true);
        }
    }

    [ModalInteraction("num_modal_*_*")]
    public async Task HandleModalInteraction(string user, string opponent, NumberModal modal)
    {
        Random a = new Random();
        List<int> randomList = new List<int>();
        int MyNumber = modal.enteredNumber;
        int repeat = 0;

        void NewNumber()
        {
            repeat = a.Next(0, 99);
            if (!randomList.Contains(repeat) && !randomList.Contains(MyNumber))
                randomList.Add(repeat);
        }

        for (int i = 0; randomList.Count <= 4; i++)
        {
            NewNumber();
        }

        int index = a.Next(0, 4);

        randomList[index] = MyNumber;

        var btns = new ComponentBuilder();

        foreach (int i in randomList)
        {
            btns.WithButton(i.ToString(), $"selection_{i}_{MyNumber}_{user}_{opponent}");
        }

        var opponentObject = await Context.Channel.GetUserAsync(Convert.ToUInt64(opponent));

        var eb = new EmbedBuilder()
                     .WithDescription($"{opponentObject.Mention}, Guess the number now.,");

        await RespondAsync(embed: eb.Build(), components: btns.Build());
    }

    [ComponentInteraction("selection_*_*_*_*")]
    public async Task HandleSelectionCommand(string selected, string answer, string user, string opponent)
    {
        if (Context.User.Id == Convert.ToUInt64(opponent))
        {
            var userObj = await Context.Channel.GetUserAsync(Convert.ToUInt64(user));
            var opponentObj = await Context.Channel.GetUserAsync(Convert.ToUInt64(opponent));
            var userDailyObj = _db.UserList.Where(x => x.UserId == Convert.ToUInt64(user)).FirstOrDefault();
            var opponentDailyObj = _db.UserList.Where(x => x.UserId == Convert.ToUInt64(opponent)).FirstOrDefault();
            var game = _db.NumberGameList.Where(
                    x => x.SelfUserId == Convert.ToUInt64(user) ||
                            x.OpponentUserId == Convert.ToUInt64(opponent)
                ).FirstOrDefault();
            if (selected == answer)
            {
                if (game != null)
                {
                    userDailyObj!.coins -= game.betAmount;
                    opponentDailyObj!.coins += game.betAmount;
                    await _db.SaveChangesAsync();
                }
                List<EmbedFieldBuilder> fields = new()
                {
                    new EmbedFieldBuilder()
                        .WithName("Challenger")
                        .WithValue($"{userObj.Mention}"),
                    new EmbedFieldBuilder()
                        .WithName("Winner")
                        .WithValue($"{opponentObj.Mention}")
                };
                var eb = new EmbedBuilder()
                            .WithTitle("???? Hooray!, You Guessed..!")
                            .WithDescription($"**{game!.betAmount}** Snow Coins has been deposited to your wallet")
                            .WithFields(fields)
                            .WithColor(Color.Green)
                            .WithThumbnailUrl("https://i.imgur.com/t7J6krZ.gif")
                            .WithImageUrl("https://i.imgur.com/ZS6LQps.png");

                await RespondAsync(embed: eb.Build());
                var msg = (SocketMessageComponent)Context.Interaction;
                await msg.Message.DeleteAsync();
            }
            else
            {
                if (game != null)
                {
                    userDailyObj!.coins += game.betAmount;
                    opponentDailyObj!.coins -= game.betAmount;
                    await _db.SaveChangesAsync();
                }
                var ebn = new EmbedBuilder()
                             .WithTitle("Ouch...")
                             .WithDescription($"You got it wrong. {userObj.Mention} chose **{answer}**.")
                             .WithColor(Color.Red)
                             .WithImageUrl("https://i.imgur.com/ZS6LQps.png")
                             .WithThumbnailUrl("https://i.imgur.com/wADiNP5.gif");
                await RespondAsync(embed: ebn.Build());
                var msg = (SocketMessageComponent)Context.Interaction;
                await msg.Message.DeleteAsync();
            }
            if (game != null)
            {
                _db.Remove(game);
                await _db.SaveChangesAsync();
            }
        }
        else
        {
            await RespondAsync("This game is not for you, try creating a new one...", ephemeral: true);
        }
    }
}

// The modal class for getting user input.
public class NumberModal : IModal
{
    public string Title => "Challenge";

    [InputLabel("Enter a number for an opponenet to guess...")]
    [ModalTextInput("num", TextInputStyle.Short, maxLength: 2)]
    public int enteredNumber { get; set; }
}