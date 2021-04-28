using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient
{
    public class Box
    {
        public readonly char Letter;
        public readonly Color Color;
        private readonly Position _initialPosition;

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

        public override string ToString()
        {
            return $"{Letter}. Initial pos: ({_initialPosition.Row},{_initialPosition.Column})";
        }

        // DO NOT OVERWRITE EQUALS && HASHCODE!!
        // TWO BOXES SHOULD ONLY BE EQUAL ON SAME REFERENCE
    }
}
