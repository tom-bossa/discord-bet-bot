namespace BetBot;

public static class WordLookups {
    public static string[] LostBetResponses = {
        "Unfortunately, it seems %LOSER% has fallen short of the mark and lost the _\"%DESCRIPTION%\"_ bet. %WINNER% Takes the win and **%AMOUNT%!**",
        "In an unexpected turn of events, %LOSER% has ended up on the losing end of the _\"%DESCRIPTION%\"_ wager. %WINNER% takes the win and **%AMOUNT%!**",
        "Against the odds, %LOSER% has unfortunately found themselves in the position of having lost the _\"%DESCRIPTION%\"_ bet. %WINNER% takes the win and **%AMOUNT%!**",
        "Despite their best efforts, %LOSER% has found themselves at the losing end of the _\"%DESCRIPTION%\"_ bet and **%AMOUNT%** goes to %WINNER%!",
        "It appears luck wasn't on their side this time around, as %LOSER% now owes %WINNER% **%AMOUNT%** after losing the _\"%DESCRIPTION%\"_ bet.",
        "%LOSER% gave it their all, but unfortunately, they've ended up losing **%AMOUNT%** to %WINNER%...  for the _\"%DESCRIPTION%\"_ bet.",
        "It's a bitter pill to swallow, but %LOSER% come up short for the _\"%DESCRIPTION%\"_ bet... **%AMOUNT%!** goes to %WINNER%!",
        "Despite their confidence going in, %LOSER% ultimately fell short and lost **%AMOUNT%** to %WINNER% for the _\"%DESCRIPTION%\"_ bet."
    };

    public static string[] WinBetResponses = {
        "Unbelievable news! %WINNER% emerged as the undisputed champion of the _\"%DESCRIPTION%\"_ bet and **%AMOUNT%** is now owed by %LOSER%!",
        "Thunderous applause! %WINNER% crushed the _\"%DESCRIPTION%\"_ bet and claimed victory against %LOSER% for **%AMOUNT%!**",
        "Incredible scenes! %WINNER% has soared to unparalleled heights and won the _\"%DESCRIPTION%\"_ bet and %LOSER% now owes them **%AMOUNT%!**",
        "A victory for the ages! %WINNER% is now **%AMOUNT%** richer courtesy of %LOSER%! The _\"%DESCRIPTION%\"_ bet has been won!",
        "Roars of triumph! %WINNER% vanquished the naysayers (And %LOSER%) **%AMOUNT%** in the bag for the _\"%DESCRIPTION%\"_ bet!",
        "A glorious moment! %WINNER% has ascended to the pinnacle of success and won the _\"%DESCRIPTION%\"_ bet against %LOSER% and netted **%AMOUNT%!**",
        "The stuff of legends! %WINNER% etched their name in history with an astounding win in the _\"%DESCRIPTION%\"_ bet! **%AMOUNT%** paid out by %LOSER%!",
        "%WINNER% has shaken the very foundations of the _\"%DESCRIPTION%\"_ bet with their awe-inspiring victory! %LOSER% has been vanquished and pays **%AMOUNT%!**"
    };
}
