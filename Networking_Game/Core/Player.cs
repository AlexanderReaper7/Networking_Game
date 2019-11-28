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

    }
}
