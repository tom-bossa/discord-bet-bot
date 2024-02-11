namespace BetBot;

[Serializable]
public class Bet {
    public ulong Id;
    public ulong TimeStamp;
    public ulong From;
    public ulong To;
    public int Amount;
    public string Description;
    public bool IsWin;
    public bool HasResolved;
}
