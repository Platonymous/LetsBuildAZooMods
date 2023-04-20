using Microsoft.Xna.Framework;
using System;

namespace ModLoader.Events
{
    public class UpdateTickEventArgs : EventArgs
    {
        public Game Game { get; private set; }

        public UpdateTickEventArgs(Game game)
        {
            Game = game;
        }
    }
}
