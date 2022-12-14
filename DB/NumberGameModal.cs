namespace WintBot;

public class NumberGame
{
    public ulong Id { get; set; }
    public int? SubmittedNumber { get; set; }
    public int? SelfUserId { get; set; }
    public int? OpponentUserId { get; set; }
    public int? GuildId { get; set; }
}