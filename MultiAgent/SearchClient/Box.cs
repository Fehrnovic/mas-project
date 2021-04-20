namespace MultiAgent.SearchClient
{
    public class Box
    {
        public readonly char Letter;
        private Position _initialPosition;
        public readonly Color Color;

        public Box(char letter, Color color, Position initialPosition)
        {
            Letter = letter;
            Color = color;
            _initialPosition = initialPosition;
        }

        public Position GetInitialLocation()
        {
            return _initialPosition;
        }

        // DO NOT OVERWRITE EQUALS && HASHCODE!!
        // TWO BOXES SHOULD ONLY BE EQUAL ON SAME REFERENCE
    }
}
