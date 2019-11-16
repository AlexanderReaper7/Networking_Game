using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tools_XNA_dotNET_Framework;
using Color = Microsoft.Xna.Framework.Color;

namespace Networking_Game
{
    public enum PlayerShape
    {
        Circle,
        Cross,
        Rectangle,
    }

    /// <summary>
    /// Contains drawing methods for the different player shapes
    /// </summary>
    public class Player
    {
        public string Name { get; private set; }
        public PlayerShape Shape { get; private set; }
        public KnownColor Color
        {
            get =>  knownColor;
            private set { 
                color = XNAColor(System.Drawing.Color.FromKnownColor(value));
                knownColor = value;
            }
        }

        private Color color;
        private KnownColor knownColor;

        public Player(string name, PlayerShape shape, KnownColor color)
        {
            Name = name;
            Shape = shape;
            Color = color;
        }

        #region ColorConverters

        private Color XNAColor(System.Drawing.Color color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }

        private System.Drawing.Color SystemColor(Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        #endregion

        public static void Draw(SpriteBatch spriteBatch, Vector2 position, PlayerShape shape, Color color, GridLayout gridLayout)
        {
            switch (shape)
            {
                case PlayerShape.Circle:
                    float halfSquare = gridLayout.SquareSize / 2f;
                    spriteBatch.DrawCircle(position + new Vector2(halfSquare - gridLayout.LineThickness * 0.5f, halfSquare + gridLayout.LineThickness *0.5f) , halfSquare - (gridLayout.LineThickness * 1.5f),32, color);
                    break;

                case PlayerShape.Cross:
                    float x1, x2, y1, y2;
                    x1 = position.X;
                    x2 = position.X + gridLayout.SquareSize;
                    y1 = position.Y;
                    y2 = position.Y + gridLayout.SquareSize;
                    spriteBatch.DrawLine(x1, y1, x2, y2, color);
                    spriteBatch.DrawLine(x1, y2, x2, y1, color);
                    break;

                case PlayerShape.Rectangle:
                    spriteBatch.DrawRectangle(position + new Vector2(gridLayout.LineThickness, gridLayout.LineThickness *2) , new Vector2(gridLayout.SquareSize - (gridLayout.LineThickness *4)), color);
                    break;

                default:
                    throw new NotImplementedException(nameof(shape) + " is not implemented yet.");
            }
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, GridLayout gridLayout)
        {
            switch (Shape)
            {
                case PlayerShape.Circle:
                    float halfSquare = gridLayout.SquareSize / 2f;
                    spriteBatch.DrawCircle(position + new Vector2(halfSquare - gridLayout.LineThickness * 0.5f, halfSquare + gridLayout.LineThickness * 0.5f), halfSquare - (gridLayout.LineThickness * 1.5f), 32, color);
                    break;

                case PlayerShape.Cross:
                    float x1, x2, y1, y2;
                    x1 = position.X;
                    x2 = position.X + gridLayout.SquareSize;
                    y1 = position.Y;
                    y2 = position.Y + gridLayout.SquareSize;
                    spriteBatch.DrawLine(x1, y1, x2, y2, color);
                    spriteBatch.DrawLine(x1, y2, x2, y1, color);
                    break;

                case PlayerShape.Rectangle:
                    spriteBatch.DrawRectangle(position + new Vector2(gridLayout.LineThickness, gridLayout.LineThickness * 2), new Vector2(gridLayout.SquareSize - (gridLayout.LineThickness * 4)), color);
                    break;

                default:
                    throw new NotImplementedException(nameof(Shape) + " is not implemented yet.");
            }
        }

    }
}
