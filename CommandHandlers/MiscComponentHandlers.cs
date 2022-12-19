using Discord;
using Discord.WebSocket;
using Discord.Interactions;

namespace WintBot;

public class MiscComponentModule: InteractionModuleBase<SocketInteractionContext>
{
    private readonly UserDbContext _db;

    public MiscComponentModule(UserDbContext db) => _db = db;

    [ComponentInteraction("confirm_*_*")]
    public async Task HandleBuyCommand(string confirmation, string penalties)
    {
        Console.WriteLine(confirmation);
        if (confirmation == "yes")
        {
            var wordGameobj = _db.WordGameList.Where(
                x => x.GuildId == Context.Guild.Id
            ).FirstOrDefault();
            var user = _db.UserList.Where(
                        x => x.UserId == Context.User.Id
                ).FirstOrDefault();
            if (wordGameobj == null)
            {
                await RespondAsync(
                    "Santa Word Guess has not been enabled in your server. See /word-game register.", ephemeral: true
                );
            }
            else
            {
                user!.coins -= Convert.ToInt32(penalties) * 50;
                wordGameobj.penalties += Convert.ToInt32(penalties);
                await _db.SaveChangesAsync();
                var emb = new EmbedBuilder()
                              .WithTitle("Success")
                              .WithDescription($"{Context.User.Mention} has bought **{penalties} penalties**...!")
                              .WithColor(Color.Purple)
                              .WithImageUrl("https://i.imgur.com/ZS6LQps.png")
                              .WithThumbnailUrl("https://i.imgur.com/YYf7xDD.gif");
                await RespondAsync(embed: emb.Build());
            }
        } else if (confirmation == "no")
        {
            await Context.Interaction.DeleteOriginalResponseAsync();
        }
    }
}