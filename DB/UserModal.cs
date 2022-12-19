namespace WintBot;


public class User
{
    public ulong? Id {get; set;}
    public ulong? UserId {get; set;}
    public int? selectedNumber {get; set;}
    public ulong? GuildId {get; set;}
    public int? coins {get; set; }
    public DateTime NextClaimTime {get; set; }
}