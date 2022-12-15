using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace WintBot;

public class CoinCollectionModule: InteractionModuleBase<SocketInteractionContext>
{

    private readonly UserDbContext _db;

    public CoinCollectionModule(UserDbContext db) => _db = db;
    

    [SlashCommand("ping", "Pings the bot")]
    public async Task HandlePingCommand()
    {
        await RespondAsync("Pong `InteractionModuleBase` working...");
    }

    [SlashCommand("daily", "Redeem your daily christmas gif coins...")]
    public async Task HandleDaily()
    {
        User? user = _db.UserList.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
        DateTime currTime = DateTime.Now;
        if (user == null)
        {
            DateTime changedDate = currTime.AddMinutes(1.0);
            _db.Add(new User() {UserId = Context.User.Id, coins = 200, NextClaimTime = changedDate});
            await _db.SaveChangesAsync();
            var embed = new EmbedBuilder()
                            .WithTitle("Woohoo...!")
                            .WithDescription("200 Snow Coins was deposited into your account")
                            .WithColor(Color.Green);
            await RespondAsync(embed: embed.Build());
        } else
        {
            int timeStatus = DateTime.Compare(user.NextClaimTime, currTime);
            if (timeStatus == 1 || timeStatus == 0)
            {
                TimeSpan timeDiff = user.NextClaimTime.Subtract(DateTime.Now);
                var eb = new EmbedBuilder()
                             .WithDescription(
                                String.Format(
                                    "❄️ Your next Snow Coins claim will be availabe in {0:00}:{1:00}:{2:00} !",
                                    timeDiff.Hours,
                                    timeDiff.Minutes,
                                    timeDiff.Seconds
                                )
                             )
                             .WithColor(Color.Red)
                             .WithTitle("☃️ Umm, Wait...!");
                await RespondAsync(embed: eb.Build());
            } else if (timeStatus == -1)
            {
                DateTime changedDate = currTime.AddHours(3.0);
                user.coins += 200;
                user.NextClaimTime = changedDate;
                await _db.SaveChangesAsync();
                var eb = new EmbedBuilder()
                             .WithTitle("❄️ Success")
                             .WithDescription("Added 200 Snow Coins to your wallet.")
                             .WithColor(Color.DarkBlue);   
                await RespondAsync(embed: eb.Build());
            }
        }
    }

    [SlashCommand("mywallet", "Check your availabe Snow Coins")]
    public async Task HandleMyWalletCommand()
    {
        User? user = _db.UserList.Where(
            x => x.UserId == Context.User.Id &&
                 x.coins != null
        ).FirstOrDefault();
        if (user == null)
        {
            var eb = new EmbedBuilder()
                         .WithTitle("☃️ Oops...!")
                         .WithColor(Color.DarkOrange)
                         .WithDescription("Claim your daily Snow Coins to open a wallet");
            await RespondAsync(embed: eb.Build());
        } else 
        {
            var eb = new EmbedBuilder()
                         .WithColor(Color.DarkOrange)
                         .WithDescription($"❄️ Your balance is **{user.coins.ToString()}** Snow Coins.");
            await RespondAsync(embed: eb.Build());
        }

    }

    [SlashCommand("guess", "Generate a random number to guess")]
    public async Task HandleGuessCommand()
    {
        var button = new ComponentBuilder()
                            .WithButton("Yeah", "game_start")
                            .WithButton("Nah", "nah_button");
        await RespondAsync("Are you sure to start a Game ?", components: button.Build());
    }

    [ComponentInteraction("game_start")]
    public async Task HandleButton()
    {
        await RespondWithModalAsync<GetUserNumberInputModal>("demo_modal");
    }

    [ComponentInteraction("nah_button")]
    public async Task HandleNah()
    {
        var message = (SocketMessageComponent)Context.Interaction;
        await message.Message.DeleteAsync();
    }


    [ModalInteraction("demo_modal")]
    public async Task HandleModal(GetUserNumberInputModal modal)
    {
        _db.UserList.Add(new User() {Id = Context.User.Id, selectedNumber = modal.number});
        await RespondAsync($"The Selected Number is {modal.number}");
    }
}

public class GetUserNumberInputModal : IModal
{
    public string Title => "Guess a number!";

    [InputLabel("Hmm, A single digit number ?")]
    [ModalTextInput("num_input", TextInputStyle.Short, "Enter a random number...", maxLength: 1)]
    public int? number {get; set;}
}