using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tools_XNA_dotNET_Framework;

namespace Networking_Game
{
    /// <summary>
    /// The data structure for a space in the game grid.
    /// </summary>
    public struct GridSquare
    {
        public PlayerShape? PlayerShape { get; set; }
        public Color? PlayerColor { get; set; }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, GridLayout gridLayout)
        {
            if (PlayerShape != null && PlayerColor != null)
            {
                Player.Draw(spriteBatch, position, (PlayerShape)PlayerShape, (Color)PlayerColor, gridLayout);
            }
        }
    }

    /// <summary>
    /// Contains data for the visual components for grid
    /// </summary>
    public struct GridLayout
    {
        public float SquareSize { get; }
        public Vector2 Position { get; }
        public float LineThickness { get; }
        public Color Color { get; }
        public Color MouseOverlayColor { get; }

        public GridLayout(float squareSize, Vector2 position, float lineThickness, Color color, Color mouseOverlayColor)
        {
            SquareSize = squareSize;
            Position = position;
            LineThickness = lineThickness;
            Color = color;
            MouseOverlayColor = mouseOverlayColor;
        }

        public Rectangle CalculatePlayArea(int squaresX, int squaresY)
        {
            return new Rectangle(
                (int)Math.Ceiling(Position.X),
                (int)Math.Ceiling(Position.Y),
                (int)Math.Ceiling(SquareSize * squaresX + LineThickness),
                (int)Math.Ceiling(SquareSize * squaresY + LineThickness));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Grid
    {
        public GridSquare[] Squares;

        private readonly int sizeX;
        private readonly int sizeY;

        public Grid(Point gridSize) : this(gridSize.X, gridSize.Y) { }

        public Grid(int sizeX, int sizeY)
        {
            if (sizeX < 3) throw new ArgumentOutOfRangeException(nameof(sizeX), "Must be greater than 2");
            if (sizeY < 3) throw new ArgumentOutOfRangeException(nameof(sizeY), "Must be greater than 2");

            this.sizeX = sizeX;
            this.sizeY = sizeY;

            int size = sizeX * sizeY;
            Squares = new GridSquare[size];
        }

        /// <summary>
        /// Calculates what square contains the position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="gridLayout"></param>
        /// <returns>Square containing the point</returns>
        public Point? SquareContains(Vector2 position, GridLayout gridLayout)
        {
            // Return null if position is outside of play area
            if (!gridLayout.CalculatePlayArea(sizeX,sizeY).Contains(position)) return null;
            // Remove position offset
            position -= gridLayout.Position;
            // Calculate which square
            int squareX = (int)(position.X / (gridLayout.SquareSize));
            int squareY = (int)(position.Y / (gridLayout.SquareSize));

            return new Point(squareX, squareY);
        }

        public void Draw(SpriteBatch spriteBatch, GridLayout gridLayout, Camera2D camera)
        {
            DrawGrid(spriteBatch, gridLayout);
            DrawSquares(spriteBatch, gridLayout, camera);
        }


        public void DrawGrid(SpriteBatch spriteBatch, GridLayout gridLayout)
        {
            spriteBatch.DrawGrid(sizeX, sizeY, gridLayout.SquareSize, gridLayout.Position, gridLayout.Color, gridLayout.LineThickness);
        }

        public void DrawSquares(SpriteBatch spriteBatch, GridLayout gridLayout, Camera2D camera)
        {
            // Determine what square the mouse is in
            Vector2 mousePosition = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            mousePosition = camera.ScreenToWorld(mousePosition);
            Point? mouseSquare = SquareContains(mousePosition, gridLayout);

            // Upper left corner of current square
            float x, y;
            
            for (int i = 0; i < sizeY; i++)
            {
                y = gridLayout.SquareSize * i + gridLayout.Position.Y;
                for (int j = 0; j < sizeX; j++)
                {
                    x = gridLayout.SquareSize * j + gridLayout.Position.X;
                    // Draw owner image
                    Squares[i * sizeY + j].Draw(spriteBatch, new Vector2(x,y), gridLayout);
                    // Draw image on the square the mouse is in
                    if (mouseSquare != null)
                        if (mouseSquare.Value.X == j && mouseSquare.Value.Y == i)
                        {
                            spriteBatch.DrawFilledRectangle(
                                j * gridLayout.SquareSize + gridLayout.Position.X,
                                i * gridLayout.SquareSize + gridLayout.Position.Y,
                                gridLayout.SquareSize, gridLayout.SquareSize,
                                gridLayout.MouseOverlayColor);
                        }
                }
            }
        }
    }
}
