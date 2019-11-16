using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tools_XNA_dotNET_Framework;
using Color = Microsoft.Xna.Framework.Color;
using Console = Colorful.Console;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;


namespace Networking_Game
{
    /// <summary>
    /// The data structure for a space in the game grid.
    /// </summary>
    public struct GridSquare
    {
        public Player Owner;

        public void Draw(SpriteBatch spriteBatch, Vector2 position, GridLayout gridLayout)
        {
            Owner?.Draw(spriteBatch, position, gridLayout);
        }

        public void ClaimSquare(Player player)
        {
            Owner = player;
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
        public readonly int MaxPlayers = Enum.GetNames(typeof(PlayerShape)).Length * Enum.GetNames(typeof(KnownColor)).Length;

        private GridSquare[] Squares;

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
            for (int i = 0; i < size; i++)
            {
                Squares[i] = new GridSquare();
            }
        }

        /// <summary>
        /// Claims a square on the grid for the player
        /// </summary>
        /// <param name="position"></param>
        /// <param name="player"></param>
        /// <returns>successfully claimed square</returns>
        public bool ClaimSquare(Point position, Player player)
        {
            if (Squares[position.Y * sizeY + position.X].Owner == null)
            {
                Squares[position.Y * sizeY + position.X].ClaimSquare(player);
                Console.WriteLine($"{player.Name} Claimed square {position}", System.Drawing.Color.FromKnownColor(player.Color));
                return true;
            }

            return false;
        }

        private GridSquare GetGridSquare(Point position)
        {
            return Squares[position.Y * sizeY + position.X]; // TODO Check for null (index out of range)
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

        /// <summary>
        /// Checks if the square has three or more squares in a line with the same owner
        /// </summary>
        /// <param name="checkPoint">The point to check</param>
        /// <param name="owner">The owner</param>
        /// <returns>array of matching other points in the line</returns>
        public List<List<Point>> IsInARow(Point checkPoint, Player owner)
        {
            var lines = new List<Tuple<Point, Point>>();

            // check neighboring squares
            for (int y = -1; y < 2; y++)
            {
                for (int x = -1; x < 2; x++)
                {
                    Point point = new Point(x, y); // this point is i=0
                    // If this square has the same owner...
                    if (GetGridSquare(point + checkPoint).Owner == owner)
                    {
                        // Keep going in the same direction...
                        int i = 1;
                        while (GetGridSquare(new Point(point.X * (i +1), point.Y * (i + 1)) + checkPoint).Owner == owner)
                        {
                            i++;
                        }
                        // Save point if it kept going past checkpoint
                        Point p1 = new Point(point.X * i, point.Y * i) + checkPoint;
                        // And opposite direction
                        i = -1;
                        while (GetGridSquare(new Point(point.X * (i - 1), point.Y * (i - 1)) + checkPoint).Owner == owner)
                        {
                            i--;
                        }
                        // Save opposite point
                        Point p2 = new Point(point.X * i, point.Y * i) + checkPoint;

                        // If the line is longer than 2 squares...
                        if (Vector2.Distance(p1.ToVector2(), p2.ToVector2()) >= 3)
                        {
                            // Add points to list
                            lines.Add(new Tuple<Point, Point>(p1,p2));
                        }

                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks the adjacent squares for the same owner
        /// </summary>
        /// <param name="checkPoint"></param>
        /// <param name="owner"></param>
        /// <returns>The point relative to checkPoint that has the same owner, or null if no points were found</returns>
        private List<Point> CheckNeighboringSquares(Point checkPoint, Player owner)
        {
            List<Point> branchingPoints = new List<Point>();

            // check neighboring squares
            for (int y = -1; y < 2; y++)
            {
                for (int x = -1; x < 2; x++)
                {
                    Point point = new Point(x, y);
                    // If this square has the same owner...
                    if (GetGridSquare(point + checkPoint).Owner == owner)
                    {
                        // add this point to output
                        branchingPoints.Add(point);
                    }
                }
            }

            return null;
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
            
            // Iterate through Y
            for (int i = 0; i < sizeY; i++)
            {
                y = gridLayout.SquareSize * i + gridLayout.Position.Y;
                // Iterate through X
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
