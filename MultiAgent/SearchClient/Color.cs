using System;

namespace MultiAgent.searchClient
{
    public static class ColorExtension
    {
        public static Color FromString(String s)
        {
            return (s.ToLower()) switch
            {
                "blue" => Color.Blue,
                "red" => Color.Red,
                "cyan" => Color.Cyan,
                "purple" => Color.Purple,
                "green" => Color.Green,
                "orange" => Color.Orange,
                "pink" => Color.Pink,
                "grey" => Color.Grey,
                "lightblue" => Color.LightBlue,
                "brown" => Color.Brown,
                _ => throw new ArgumentOutOfRangeException(s),
            };
        }
    }

    public enum Color
    {
        Blue,
        Red,
        Cyan,
        Purple,
        Green,
        Orange,
        Pink,
        Grey,
        LightBlue,
        Brown
    }
}
