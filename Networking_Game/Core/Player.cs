using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tools_XNA_dotNET_Framework;
using Console = Colorful.Console;

namespace Networking_Game
{
    public enum PlayerShape
    {
        Circle,
        Cross,
        Rectangle,
        Triangle,
        Diamond
    }

    public enum PlayerColor
    {
        Red,
        Orange,
        Yellow,
        Blue,
        Cyan,
        Lime,
        Green,
        Teal,
        Purple,
        Pink,
        HotPink
    }

    public static class PlayerColors
    {
        public static Color[] Colors = {Color.Red, Color.Orange, Color.Yellow, Color.Blue, Color.Cyan, Color.Lime, Color.Green, Color.Teal, Color.Purple, Color.Pink, Color.HotPink};
    }

    /// <summary>
    ///     Contains drawing methods for the different player shapes
    /// </summary>
    [Serializable]
    public class Player
    {
        public Player(string name, PlayerShape shape, PlayerColor color)
        {
            Name = name;
            Shape = shape;
            Color = color;
        }

        public string Name { get; private set; }
        public PlayerShape Shape { get; private set; }
        public PlayerColor Color { get; private set; }
        public int Score { get; set; }

        public Color drawingColor
        {
            get { return PlayerColors.Colors[(int) Color]; }
        }

        public static Player GetRandomisedPlayer()
        {
            Random rand = new Random();

            PlayerShape shape;
            PlayerColor color;

            shape = (PlayerShape) rand.Next(0, Enum.GetValues(typeof(PlayerShape)).Length - 1);
            color = (PlayerColor) rand.Next(0, Enum.GetValues(typeof(PlayerColor)).Length - 1);

            return new Player($"{Enum.GetName(typeof(PlayerColor), color)} {Enum.GetName(typeof(PlayerShape), shape)}", shape, color);
        }

        public static Player[] GetAllCombinations()
        {
            var colors = Enum.GetNames(typeof(PlayerColor));
            var shapes = Enum.GetNames(typeof(PlayerShape));
            var output = new Player[colors.Length * shapes.Length];
            int i = 0;
            for (int y = 0; y < shapes.Length; y++)
            {
                for (int x = 0; x < colors.Length; x++)
                {
                    output[i] = new Player($"{colors[x]} {shapes[y]}", (PlayerShape)y, (PlayerColor)x);
                    i++;
                }
            }

            return output;
        }

        /// <summary>
        ///     Gets input for a player
        /// </summary>
        /// <param name="player"></param>
        public static Player GetPlayerSettingsInput()
        {
            // Get name
            string name = null;
            const int maxNameLength = 12;
            while (true)
            {
                Console.Write("Input name:  ", System.Drawing.Color.White);
                name = ConsoleManager.GetPriorityInput();
                if (name.Length > maxNameLength)
                {
                    Console.WriteLine($"That name is too long, max length is {maxNameLength}", System.Drawing.Color.Red);
                    continue;
                }

                if (name.Length < 1)
                {
                    Console.WriteLine("Name can not be empty", System.Drawing.Color.Red);
                    continue;
                }

                if (name == "empty") System.Console.WriteLine("i guess it can be empty...");
                break;
            }

            // Get shape 
            PlayerShape shape;
            while (true)
            {
                // Write available shapes
                Console.WriteLine("Available shapes: ", System.Drawing.Color.White);
                foreach (string s in Enum.GetNames(typeof(PlayerShape))) Console.WriteLine(s, System.Drawing.Color.White);
                Console.Write("Input shape: ", System.Drawing.Color.White);
                string str = ConsoleManager.GetPriorityInput();
                if (int.TryParse(str, out int shapeInt))
                {
                    if (shapeInt < 1 || shapeInt > Enum.GetNames(typeof(PlayerShape)).Length) // TODO: Separate 
                    {
                        Console.WriteLine("incorrect input, try again.", System.Drawing.Color.Red);
                        continue;
                    }

                    shape = (PlayerShape) shapeInt - 1;
                }
                else
                {
                    if (!Enum.TryParse(str, true, out shape))
                    {
                        Console.WriteLine("incorrect input, try again.", System.Drawing.Color.Red);
                        continue;
                    }
                }

                break;
            }

            // Get drawingColor
            PlayerColor color;
            while (true)
            {
                Console.Write("Colors: ", System.Drawing.Color.White);
                string[] cs = Enum.GetNames(typeof(PlayerColor));
                for (int i = 0; i < cs.Length; i++) Console.Write($"{cs[i]} ", PlayerColors.Colors[i].ToSystemColor());
                if (!Enum.TryParse(ConsoleManager.WaitGetPriorityInput("\nInput color: ", false), true, out color))
                {
                    Console.WriteLine("incorrect input, try again.", System.Drawing.Color.Red);
                    continue;
                }

                break;
            }

            Console.WriteLine($"Created player {name} using {color} {shape}.", PlayerColors.Colors[(int) color].ToSystemColor());
            return new Player(name, shape, color);
        }


        public void Draw(SpriteBatch spriteBatch, Vector2 position, GridLayout gridLayout)
        {
            float halfSquare = gridLayout.SquareSize / 2f;
            switch (Shape)
            {
                case PlayerShape.Circle:
                    spriteBatch.DrawCircle(position + new Vector2(halfSquare - gridLayout.LineThickness * 0.5f, halfSquare + gridLayout.LineThickness * 0.5f), halfSquare - gridLayout.LineThickness * 1.5f, 32, drawingColor);
                    break;

                case PlayerShape.Cross:
                    float x1, x2, y1, y2;
                    x1 = position.X + gridLayout.LineThickness * 2;
                    x2 = position.X + gridLayout.SquareSize - gridLayout.LineThickness * 2;
                    y1 = position.Y + gridLayout.LineThickness * 2.5f;
                    y2 = position.Y + gridLayout.SquareSize - gridLayout.LineThickness * 2;
                    spriteBatch.DrawLine(x1, y1, x2, y2, drawingColor);
                    spriteBatch.DrawLine(x2, y1 + gridLayout.LineThickness / 2, x1, y2 + gridLayout.LineThickness / 2, drawingColor);
                    break;

                case PlayerShape.Rectangle:
                    spriteBatch.DrawRectangle(position + new Vector2(gridLayout.LineThickness, gridLayout.LineThickness * 2), new Vector2(gridLayout.SquareSize - gridLayout.LineThickness * 4), drawingColor);
                    break;

                case PlayerShape.Triangle:
                    spriteBatch.DrawCircle(position + new Vector2(halfSquare - gridLayout.LineThickness * 0.5f, halfSquare + gridLayout.LineThickness * 0.5f), halfSquare - gridLayout.LineThickness * 1.5f, 3, drawingColor);
                    break;

                case PlayerShape.Diamond:
                    spriteBatch.DrawCircle(position + new Vector2(halfSquare - gridLayout.LineThickness * 0.5f, halfSquare + gridLayout.LineThickness * 0.5f), halfSquare - gridLayout.LineThickness * 1.5f, 4, drawingColor);
                    break;

                default: throw new NotImplementedException(nameof(Shape) + " is not implemented yet.");
            }
        }
    }
}