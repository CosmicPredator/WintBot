namespace WintBot;

public class WordGameModel
{
    public ulong Id {get; set; }
    public ulong GuildId {get; set; }
    public ulong Channel {get; set; }
    public int score {get; set; } = 0;
    public int HighScore {get; set; } = 0;
    public int penalties {get; set; } = 3;
}
