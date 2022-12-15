using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace WintBot;

public class NumberGameModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly UserDbContext _db;

    public NumberGameModule(UserDbContext db) => _db = db;

    [SlashCommand("challenge", "Challenge an user to play the Snow Guess game")]
    public async Task HandleChallengeCommand(SocketGuildUser opponent)
    {
        NumberGame newGame = new NumberGame()
        {
            GuildId = Context.Guild.Id,
            SelfUserId = Context.User.Id,
            OpponentUserId = opponent.Id
        };
        _db.NumberGameList.Add(newGame);
        await _db.SaveChangesAsync();
        var eb = new EmbedBuilder()
                     .WithTitle("New Challenge")
                     .WithDescription($"{Context.User.Mention} challenged you to the Number game.");
        var buttons = new ComponentBuilder()
                          .WithButton("Agree", $"cmd_agree_{opponent.Id}_{Context.User.Id}", ButtonStyle.Success)
                          .WithButton("Decline", "cmd_decline", ButtonStyle.Danger);
        await RespondAsync($"||{opponent.Mention}||", embed: eb.Build(), components: buttons.Build());
    }

    [ComponentInteraction("cmd_decline")]
    public async Task HandleDeclineCommand()
    {
        var message = (SocketMessageComponent)Context.Interaction;
        await message.Message.DeleteAsync();
    }

    [ComponentInteraction("cmd_agree_*_*")]
    public async Task HandleAgreeCommand(string opponent, string user)
    {
        NumberGame? game = _db.NumberGameList.Where(
            x => (double)x.SelfUserId! == Convert.ToDouble(user) && 
                    (double)x.OpponentUserId! == Convert.ToDouble(opponent)
        ).FirstOrDefault();
        if (game == null)
        {
            await RespondAsync("This game is not for you. Try creating a game yourself...", ephemeral: true);
        } else if (game != null)
        {
            if (Context.User.Id == Convert.ToUInt64(user))
            {
                await RespondAsync("Wait for the opponent to accept the challenge.", ephemeral: true);
            } else 
            {
                var userObject = await Context.Channel.GetUserAsync(Convert.ToUInt64(user));
                var btn = new ComponentBuilder()
                        .WithButton("Click Here", $"cmd_open_num_modal_{user}_{opponent}", ButtonStyle.Primary);
                await RespondAsync($"{userObject.Mention}, click here to enter your number...", components: btn.Build(), ephemeral: true);
            }
        }
    }

    [ComponentInteraction("cmd_open_num_modal_*_*")]
    public async Task HandleModalOpenCommand(string user, string opponent)
    {
        await RespondWithModalAsync<NumberModal>($"num_modal_{user}_{opponent}");
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
            repeat = a.Next(0, 10);
            if (!randomList.Contains(repeat) && !randomList.Contains(MyNumber))
                    randomList.Add(repeat);
        }

        for (int i = 0; randomList.Count <=4 ; i++)
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
            if (selected == answer)
            {
                var useObj = await Context.Channel.GetUserAsync(Convert.ToUInt64(user));
                var opponenet = await Context.Channel.GetUserAsync(Convert.ToUInt64(opponent));
                List<EmbedFieldBuilder> fields = new ()
                {
                    new EmbedFieldBuilder()
                        .WithName("Challenger")
                        .WithValue($"{useObj.Mention}"),
                    new EmbedFieldBuilder()
                        .WithName("Winner")
                        .WithValue($"{opponenet.Mention}")
                };
                var eb = new EmbedBuilder()
                            .WithTitle("🥇 Hooray!, You Guessed..!")
                            .WithDescription("**200** Snow Coins has been deposited to your wallet")
                            .WithFields(fields)
                            .WithColor(Color.Green);

                await RespondAsync(embed: eb.Build());
            } else 
            {
                var ebn = new EmbedBuilder()
                             .WithTitle("Ouch...")
                             .WithDescription("You made it wrong..")
                             .WithColor(Color.Red);
                await RespondAsync(embed: ebn.Build());
            }
        } else 
        {
            await RespondAsync("This game is not for you, try creating a new one...", ephemeral: true);
        }
    }
}

public class NumberModal : IModal
{
    public string Title => "Challenge";

    [InputLabel("Enter a number for an opponenet to guess...")]
    [ModalTextInput("num", TextInputStyle.Short, maxLength: 1)]
    public int enteredNumber { get; set; }
}