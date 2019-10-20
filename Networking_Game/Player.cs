using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tools_XNA_dotNET_Framework;

namespace Networking_Game
{
    public enum PlayerShape
    {
        Circle,
        Cross
    }
    /// <summary>
    /// Contains drawing methods for the different player shapes
    /// </summary>
    class Player
    {
        public PlayerShape Shape { get; private set; }
        public Color Color { get; private set; }

        public Player(PlayerShape playerShape)
        {
            this.Shape = playerShape;
        }

        void ClaimSquare(GridSquare square)
        {
            square.PlayerShape = Shape;
            square.PlayerColor = Color;
        }

        public static void Draw(SpriteBatch spriteBatch, Vector2 position, PlayerShape shape, Color color, GridLayout gridLayout)
        {
            switch (shape)
            {
                case PlayerShape.Circle:
                    float halfSquare = gridLayout.SquareSize / 2f;
                    spriteBatch.DrawCircle(position + new Vector2(halfSquare), halfSquare,12, color);
                    break;

                case PlayerShape.Cross: // TODO: write comments
                    float x1, x2, y1, y2;
                    x1 = position.X;
                    x2 = position.X + gridLayout.SquareSize;
                    y1 = position.Y;
                    y2 = position.Y + gridLayout.SquareSize;
                    spriteBatch.DrawLine(x1, y1, x2, y2, color);
                    spriteBatch.DrawLine(x1, y2, x2, y1, color);
                    break;

                default:
                    throw new NotImplementedException(nameof(shape) + " is not implemented yet.");
            }
        }
    }
}
