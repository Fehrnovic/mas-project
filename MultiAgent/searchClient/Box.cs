﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiAgent.searchClient
{
    public class Box
    {
        public char Letter;
        public Position Position;
        public Color Color;

        public Box(char letter,  Color color)
        {
            Letter = letter;
            Color = color;
        }
    }
}
