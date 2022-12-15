using Microsoft.EntityFrameworkCore;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using WeCantSpell.Hunspell;

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
            // if (Context.User.Id == Convert.ToUInt64(user))
            // {
            //     await RespondAsync("Wait for the opponent to accept the challenge.", ephemeral: true);
            // } else 
            // {
            //     var userObject = await Context.Channel.GetUserAsync(Convert.ToUInt64(user));
            //     var btn = new ComponentBuilder()
            //             .WithButton("Click Here", $"cmd_open_num_modal_{user}_{opponent}", ButtonStyle.Primary);
            //     await RespondAsync($"{userObject.Mention}, click here to enter your number...", components: btn.Build(), ephemeral: true);
            // }
            var userObject = await Context.Channel.GetUserAsync(Convert.ToUInt64(user));
            var btn = new ComponentBuilder()
                        .WithButton("Click Here", $"cmd_open_num_modal_{user}_{opponent}", ButtonStyle.Primary);
            await RespondAsync($"{userObject.Mention}, click here to enter your number...", components: btn.Build(), ephemeral: true);
        }
    }

    [ComponentInteraction("cmd_open_num_modal_*_*")]
    public async Task HandleModalOpenCommand(string user, string opponent)
    {
        await RespondWithModalAsync<NumberModal>($"num_modal_{user}_{opponent}");
    }

    [ModalInteraction("num_modal_*_*")]
    public async Task HandleModalInteraction(NumberModal modal, string user, string opponent)
    {
        await RespondAsync($"The entered number is {modal.enteredNumber}", ephemeral: true);
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

public class NumberModal : IModal
{
    public string Title => "Challenge";

    [InputLabel("Enter a number for an opponenet to guess...")]
    [ModalTextInput("num", TextInputStyle.Short, maxLength: 1)]
    public int enteredNumber { get; set; }
}