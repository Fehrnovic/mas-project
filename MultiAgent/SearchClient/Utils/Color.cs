using System;

namespace MultiAgent.SearchClient.Utils
{
    public static class ColorExtension
    {
        public static Color FromString(string s)
        {
            return s.ToLower() switch
            {
                "red" => Color.Red,
                "blue" => Color.Blue,
                "cyan" => Color.Cyan,
                "pink" => Color.Pink,
                "grey" => Color.Grey,
                "green" => Color.Green,
                "brown" => Color.Brown,
                "purple" => Color.Purple,
                "orange" => Color.Orange,
                "lightblue" => Color.LightBlue,
                _ => throw new ArgumentOutOfRangeException(s),
            };
        }
    }

    public enum Color
    {
        Red,
        Blue,
        Cyan,
        Pink,
        Grey,
        Green,
        Brown,
        Purple,
        Orange,
        LightBlue,
    }
}
