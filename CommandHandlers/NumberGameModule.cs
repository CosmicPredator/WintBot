using Microsoft.EntityFrameworkCore;
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
            GuildId = (int)Context.Guild.Id,
            SelfUserId = (int)Context.User.Id,
            OpponentUserId = (int)opponent.Id
        };
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

    [ComponentInteraction("cmd_agree_*")]
    public async Task HandleAgreeCommand(string opponent, string user)
    {
        NumberGame? game = _db.NumberGameList.Where(
            x => x.SelfUserId == Convert.ToInt32(user) && 
                    x.OpponentUserId == Convert.ToInt32(opponent)
        ).FirstOrDefault();

        if (game == null)
        {
            await RespondAsync("This game is not for you. Try creating a game yourself...");
        } else if (game != null)
        {
            
        }

        var btn = new ComponentBuilder()
                      .WithButton("Click Here", "cmd_open_num_modal", ButtonStyle.Primary);
        await RespondAsync($"Click here to enter your number...", components: btn.Build(), ephemeral: true);
    }

    [ComponentInteraction("cmd_open_num_modal")]
    public async Task HandleModalOpenCommand()
    {
        await RespondWithModalAsync<NumberModal>("num_modal");
    }

    [ModalInteraction("num_modal")]
    public async Task HandleModalInteraction(NumberModal modal)
    {
        await RespondAsync($"The entered number is {modal.enteredNumber}", ephemeral: true);
    }
}

public class NumberModal : IModal
{
    public string Title => "Challenge";

    [InputLabel("Enter a number for an opponenet to guess...")]
    [ModalTextInput("num", TextInputStyle.Short, maxLength: 1)]
    public int enteredNumber { get; set; }
}