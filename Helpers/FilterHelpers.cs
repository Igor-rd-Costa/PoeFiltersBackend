

using System.Numerics;

public static class FilterHelpers
{
    public static bool ParseGameString(string gameString, out Game game)
    {
        if (string.IsNullOrEmpty(gameString) || !(gameString.Equals("poe1") || gameString.Equals("poe2")))
        {
            game = (Game)(-1);
            return false;
        }
        if (gameString.Equals("poe1"))
        {
            game = Game.POE1;
            return true;
        }

        game = Game.POE2;
        return true;
    }

    public static FilterRuleStyle DefaultRuleStyle()
    {
        return new()
        {
            FontSize = 32,
            BorderColor = null,
            DropSound = null,
            DropIcon = null,
            DropPlayEffect = null,
            TextColor = new() { R = 170, G = 158, B = 129, A = 1 },
            BackgroundColor = new() { R = 0, G = 0, B = 0, A = 0.7f }
        };
    }
}