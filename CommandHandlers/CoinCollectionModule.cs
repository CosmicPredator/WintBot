using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace WintBot;

/// <summary>
/// The Class which constitutes the Snow Coin Collecting events and commands
/// like /daily, /mywallet etc.,
/// This class is scoped to the assembly.
/// The DB servcies are injected to this class.
/// This class constitutes the follwowing commands,
/// > /ping (dev only)
/// > /daily
/// > /mywallet
/// </summary>


public class CoinCollectionModule: InteractionModuleBase<SocketInteractionContext>
{
    private readonly UserDbContext _db;

    // DB Context is injected
    public CoinCollectionModule(UserDbContext db) => _db = db;
    

    // Handling of /ping command.
    [SlashCommand("ping", "Pings the bot")]
    public async Task HandlePingCommand()
    {
        if (Context.User.Id == 793688107077468171)
        {
            int ping = Context.Client.Latency;
            await RespondAsync($"Pong, `{ping.ToString()}ms`");
        } else
        {
            await RespondAsync("It's a developer-only command...", ephemeral: true);
        }
    }

    // Handling of /daily command
    [SlashCommand("daily", "Redeem your daily Christmas Snow Coins...")]
    public async Task HandleDaily()
    {
        User? user = _db.UserList.Where(
            x => x.UserId == Context.User.Id &&
                x.GuildId == Context.Guild.Id
        ).FirstOrDefault();
        DateTime currTime = DateTime.Now;
        if (user == null)
        {
            DateTime changedDate = currTime.AddHours(1.0);
            _db.Add(new User() {
                UserId = Context.User.Id, coins = 200, 
                NextClaimTime = changedDate,
                GuildId = Context.Guild.Id
            });
            await _db.SaveChangesAsync();
            var embed = new EmbedBuilder()
                            .WithTitle("Woohoo...!")
                            .WithDescription("**200** Snow Coins was deposited into your account")
                            .WithColor(Color.Green)
                            .WithImageUrl("https://i.imgur.com/ZS6LQps.png")
                            .WithThumbnailUrl("https://i.imgur.com/YYf7xDD.gif");
            await RespondAsync(embed: embed.Build());
        } else
        {
            int timeStatus = DateTime.Compare(user.NextClaimTime, currTime);
            if (timeStatus == 1 || timeStatus == 0)
            {
                TimeSpan timeDiff = user.NextClaimTime.Subtract(DateTime.Now);
                long unix = ((DateTimeOffset)user.NextClaimTime).ToUnixTimeSeconds();
                var eb = new EmbedBuilder()
                             .WithDescription(
                                $"Your next Snow Coins claim will be availabe in <t:{unix}:R>!"
                             )
                             .WithColor(Color.Red)
                             .WithTitle("?????? Umm, Wait...!")
                             .WithImageUrl("https://i.imgur.com/ZS6LQps.png");
                await RespondAsync(embed: eb.Build());
            } else if (timeStatus == -1)
            {
                DateTime changedDate = currTime.AddHours(3.0);
                user.coins += 200;
                user.NextClaimTime = changedDate;
                await _db.SaveChangesAsync();
                var eb = new EmbedBuilder()
                             .WithTitle("?????? Success")
                             .WithDescription("Added **200** Snow Coins to your wallet.")
                             .WithColor(Color.DarkBlue)
                             .WithImageUrl("https://i.imgur.com/ZS6LQps.png")
                             .WithThumbnailUrl("https://i.imgur.com/YYf7xDD.gif");   
                await RespondAsync(embed: eb.Build());
            }
        }
    }

    // handling ot /mywallet command
    [SlashCommand("mywallet", "Check your availabe Snow Coins")]
    public async Task HandleMyWalletCommand()
    {
        User? user = _db.UserList.Where(
            x => x.UserId == Context.User.Id &&
                 x.coins != null &&
                 x.GuildId == Context.Guild.Id
        ).FirstOrDefault();
        if (user == null)
        {
            var eb = new EmbedBuilder()
                         .WithTitle("?????? Oops...!")
                         .WithColor(Color.DarkOrange)
                         .WithDescription("Claim your daily Snow Coins to open a wallet")
                         .WithImageUrl("https://i.imgur.com/ZS6LQps.png");
            await RespondAsync(embed: eb.Build());
        } else 
        {
            var eb = new EmbedBuilder()
                         .WithColor(Color.DarkOrange)
                         .WithDescription($"?????? Your balance is **{user.coins.ToString()}** Snow Coins.")
                         .WithImageUrl("https://i.imgur.com/ZS6LQps.png");
            await RespondAsync(embed: eb.Build());
        }

    }
}