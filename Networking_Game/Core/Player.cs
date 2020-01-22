using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tools_XNA_dotNET_Framework;
using Color = Microsoft.Xna.Framework.Color;
using Console = Colorful.Console;
using Point = Microsoft.Xna.Framework.Point;

namespace Networking_Game
{
    public enum PlayerShape
    {
        Circle,
        Cross,
        Rectangle,
        Triangle,
        Diamond,
    }

    /// <summary>
    /// Contains drawing methods for the different player shapes
    /// </summary>
    public class Player
    {
        public string Name { get; private set; }
        public PlayerShape Shape { get; private set; }
        public KnownColor Color // TODO: change valid colors so you can always see the color and there´s no similar ones.
        {
            get =>  knownColor;
            private set { 
                color = System.Drawing.Color.FromKnownColor(value).ToXNAColor();
                knownColor = value;
            }
        }

        public int Score { get; set; }

        private Color color;
        private KnownColor knownColor;

        public Player(string name, PlayerShape shape, KnownColor color)
        {
            Name = name;
            Shape = shape; 
            Color = color;
        }

        

        public void Draw(SpriteBatch spriteBatch, Vector2 position, GridLayout gridLayout)
        {
            float halfSquare = gridLayout.SquareSize / 2f;
            switch (Shape)
            {
                case PlayerShape.Circle:
                    spriteBatch.DrawCircle(position + new Vector2(halfSquare - gridLayout.LineThickness * 0.5f, halfSquare + gridLayout.LineThickness * 0.5f), halfSquare - (gridLayout.LineThickness * 1.5f), 32, color);
                    break;

                case PlayerShape.Cross:
                    float x1, x2, y1, y2;
                    x1 = position.X + gridLayout.LineThickness * 2;
                    x2 = position.X + gridLayout.SquareSize - gridLayout.LineThickness *2;
                    y1 = position.Y + gridLayout.LineThickness * 2;
                    y2 = position.Y + gridLayout.SquareSize - gridLayout.LineThickness *2;
                    spriteBatch.DrawLine(x1, y1, x2, y2, color);
                    spriteBatch.DrawLine(x2, y1 + gridLayout.LineThickness/2, x1, y2 + gridLayout.LineThickness/2, color);
                    break;

                case PlayerShape.Rectangle:
                    spriteBatch.DrawRectangle(position + new Vector2(gridLayout.LineThickness, gridLayout.LineThickness * 2), new Vector2(gridLayout.SquareSize - (gridLayout.LineThickness * 4)), color);
                    break;

                case PlayerShape.Triangle:
                    spriteBatch.DrawCircle(position + new Vector2(halfSquare - gridLayout.LineThickness * 0.5f, halfSquare + gridLayout.LineThickness * 0.5f), halfSquare - (gridLayout.LineThickness * 1.5f), 3, color);
                    break;

                case PlayerShape.Diamond:
                    spriteBatch.DrawCircle(position + new Vector2(halfSquare - gridLayout.LineThickness * 0.5f, halfSquare + gridLayout.LineThickness * 0.5f), halfSquare - (gridLayout.LineThickness * 1.5f), 4, color);
                    break;

                default:
                    throw new NotImplementedException(nameof(Shape) + " is not implemented yet.");
            }
        }

        /// <summary>
        /// Gets input for a player
        /// </summary>
        /// <param name="player"></param>
        public static Player GetPlayerSettingsInput()
        {
            // Get name TODO: refactor into while loop BUG: name should not be empty
            N_INPUT:
            Colorful.Console.Write("Input name:  ", System.Drawing.Color.White);
            string name = ConsoleManager.GetPriorityInput();
            const int maxNameLength = 12;
            if (name.Length > maxNameLength)
            {
                Colorful.Console.WriteLine($"That name is too long, max length is {maxNameLength}.", System.Drawing.Color.Red);
                goto N_INPUT;
            }

            // Get shape TODO: refactor into while loop
            S_INPUT:
            // Write available shapes
            Colorful.Console.WriteLine("Available shapes: ", System.Drawing.Color.White);
            foreach (var s in Enum.GetNames(typeof(PlayerShape)))
            {
                Colorful.Console.WriteLine(s, System.Drawing.Color.White);
            }
            Colorful.Console.Write("Input shape: ", System.Drawing.Color.White);
            string str = ConsoleManager.GetPriorityInput();
            PlayerShape shape;
            if (int.TryParse(str, out int shapeInt))
            {
                if (shapeInt < 1 || shapeInt > Enum.GetNames(typeof(PlayerShape)).Length)
                {
                    Colorful.Console.WriteLine("incorrect input, try again.", System.Drawing.Color.Red);
                    goto S_INPUT;
                }
                shape = (PlayerShape)shapeInt - 1;
            }
            else
            {
                if (!Enum.TryParse(str, true, out shape))
                {
                    Colorful.Console.WriteLine("incorrect input, try again.", System.Drawing.Color.Red);
                    goto S_INPUT;
                }
            }

            // Get color TODO: make example colors random TODO: refactor into while loop
            C_INPUT:
            System.Drawing.Color[] exampleColors = new System.Drawing.Color[] { System.Drawing.Color.Red, System.Drawing.Color.Blue, System.Drawing.Color.Yellow, System.Drawing.Color.Cyan, System.Drawing.Color.PeachPuff, System.Drawing.Color.White, };
            Colorful.Console.WriteLine("Example colors: ", System.Drawing.Color.White);
            foreach (System.Drawing.Color exampleColor in exampleColors)
            {
                Colorful.Console.Write(exampleColor.ToKnownColor() + " ", exampleColor);
            }
            Colorful.Console.WriteLine();
            Colorful.Console.Write("Input color: ", System.Drawing.Color.White);
            if (!Enum.TryParse(ConsoleManager.GetPriorityInput(), true, out KnownColor color))
            {
                Colorful.Console.WriteLine("incorrect input, try again.", System.Drawing.Color.Red);
                goto C_INPUT;
            }
            // TODO: check if the combination of color and shape is already in use 
            Console.WriteLine($"Created player {name} using {color} {shape}.", System.Drawing.Color.FromKnownColor(color));
            return new Player(name, shape, color);
        }
    }
}
