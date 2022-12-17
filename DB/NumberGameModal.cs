namespace WintBot;

public class NumberGame
{
    public ulong Id { get; set; }
    public double? SubmittedNumber { get; set; }
    public ulong SelfUserId { get; set; }
    public ulong OpponentUserId { get; set; }
    public ulong GuildId { get; set; }
    public int betAmount { get; set; }
}