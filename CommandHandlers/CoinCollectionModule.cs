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
        int ping = Context.Client.Latency;
        await RespondAsync($"Pong, `{ping.ToString()}ms`");
    }

    [SlashCommand("daily", "Redeem your daily christmas gif coins...")]
    public async Task HandleDaily()
    {
        User? user = _db.UserList.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
        DateTime currTime = DateTime.Now;
        if (user == null)
        {
            DateTime changedDate = currTime.AddHours(3.0);
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
}