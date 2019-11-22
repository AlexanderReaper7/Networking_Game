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
        public bool IsInLine;

        public GridSquare(Player owner = null, bool isInLine = false)
        {
            Owner = owner;
            IsInLine = isInLine;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, GridLayout gridLayout)
        {
            if (IsInLine)
            {
                spriteBatch.DrawFilledRectangle(new Rectangle((int)position.X, (int)position.Y, (int)gridLayout.SquareSize, (int)gridLayout.SquareSize), System.Drawing.Color.FromKnownColor(Owner.Color).ToXNAColor());
            }
            else
            {
                Owner?.Draw(spriteBatch, position, gridLayout);
            }
        }

        public void Claim(Player player)
        {
            Owner = player;
        }

        public void SetIsInLine(bool value)
        {
            IsInLine = value;
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
        public static readonly int MaxPlayers = Enum.GetNames(typeof(PlayerShape)).Length * Enum.GetNames(typeof(KnownColor)).Length;

        private GridSquare[,] Squares;

        private readonly int sizeX;
        private readonly int sizeY;
        private const int minLineLength = 2;

        public Grid(Point gridSize) : this(gridSize.X, gridSize.Y) { }

        public Grid(int sizeX, int sizeY)
        {
            if (sizeX < 3) throw new ArgumentOutOfRangeException(nameof(sizeX), "Must be greater than 2");
            if (sizeY < 3) throw new ArgumentOutOfRangeException(nameof(sizeY), "Must be greater than 2");

            this.sizeX = sizeX;
            this.sizeY = sizeY;

            Squares = new GridSquare[sizeX,sizeY];
            for (int x = 0; x < Squares.GetLength(0); x++)
            {
                for (int y = 0; y < Squares.GetLength(1); y++)
                {
                    Squares[x, y] = new GridSquare();
                }
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
            try
            {
                // If the square does not already have an owner
                if (Squares[position.X, position.Y].Owner == null)
                {
                    // Claim the square
                    Squares[position.X, position.Y].Claim(player);
                    Console.WriteLine($"{player.Name} Claimed square {position}", System.Drawing.Color.FromKnownColor(player.Color));
                    // Check for line
                    List<Point> newlyLinedPoints = CheckForLine(position, player);

                    return true;
                }

                return false;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        //private ref GridSquare GetGridSquare(Point position)
        //{
        //    return ref Squares[position.X, position.Y];
        //}

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
            int squareX = (int)(position.X / gridLayout.SquareSize);
            int squareY = (int)(position.Y / gridLayout.SquareSize);

            return new Point(squareX, squareY);
        }

        /// <summary>
        /// Checks if the square has three or more squares in a line with the same owner
        /// </summary>
        /// <param name="checkPoint">The point to check</param>
        /// <param name="owner">The owner</param>
        /// <returns>the new line points</returns>
        public List<Point> CheckForLine(Point checkPoint, Player owner)
        {
            List<Point> output = new List<Point>();
            try
            {
                // Return if the square is already in a line
                if (Squares[checkPoint.X, checkPoint.Y].IsInLine) return output;

                // check neighboring squares that are not already part of a line
                for (int y = -1; y < 2; y++)
                {
                    for (int x = -1; x < 2; x++)
                    {
                        // Skip 0,0 
                        if (x == 0 && y == 0) continue;
                        // This point is i=0
                        Point point = new Point(x, y); 
                        // Skip if this square does not exist or is already in a line
                        try
                        {
                            if (Squares[checkPoint.X + point.X, checkPoint.Y + point.Y].IsInLine) continue;
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        // If this square has the same owner...
                        if (Squares[point.X + checkPoint.X, point.Y + checkPoint.Y].Owner == owner)
                        {
                            List<Point> points = new List<Point>();
                            // Add to points
                            points.Add(point + checkPoint);
                            // Keep going in the same direction... TODO: remove duplicate code
                            bool branching;
                            int i = 1;
                            Point branchPoint = new Point(point.X * (i + 1), point.Y * (i + 1)) + checkPoint;
                            try
                            {
                                // Keep branching if the point has the same owner and is not already in a line
                                branching = Squares[branchPoint.X, branchPoint.Y].Owner == owner && Squares[branchPoint.X, branchPoint.Y].IsInLine == false;
                            }
                            catch (IndexOutOfRangeException)
                            {
                                branching = false;
                            }
                            while (branching)
                            {
                                // Add point to list
                                points.Add(branchPoint);
                                i++;
                                branchPoint = new Point(point.X * (i + 1), point.Y * (i + 1)) + checkPoint;
                                try
                                {
                                    // Keep branching if the point has the same owner and is not already in a line
                                    branching = Squares[branchPoint.X, branchPoint.Y].Owner == owner && Squares[branchPoint.X, branchPoint.Y].IsInLine == false;
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    branching = false;
                                }
                            }

                            // And opposite direction
                            i = -1;
                            branchPoint = new Point(point.X * (i - 1), point.Y * (i - 1)) + checkPoint;

                            try
                            {
                                // Keep branching if the point has the same owner and is not already in a line
                                branching = Squares[branchPoint.X, branchPoint.Y].Owner == owner && Squares[branchPoint.X, branchPoint.Y].IsInLine == false;
                            }
                            catch (IndexOutOfRangeException)
                            {
                                branching = false;
                            }
                            while (branching)
                            {
                                // Add point to list
                                points.Add(branchPoint);
                                i--;
                                branchPoint = new Point(point.X * (i - 1), point.Y * (i - 1)) + checkPoint;
                                try
                                {
                                    // Keep branching if the point has the same owner and is not already in a line
                                    branching = Squares[branchPoint.X, branchPoint.Y].Owner == owner && Squares[branchPoint.X, branchPoint.Y].IsInLine == false;
                                }
                                catch (IndexOutOfRangeException )
                                {
                                    branching = false;
                                }
                            }

                            if (points.Count >= minLineLength)
                            {
                                foreach (Point p in points)
                                {
                                    Squares[p.X,p.Y].SetIsInLine(true);
                                }

                                return output;
                            }

                            return new List<Point>();
                            //TODO: remove duplicate points? are there any?
                        }



                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                return new List<Point>();
            }

            return new List<Point>();
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
                    Squares[j,i].Draw(spriteBatch, new Vector2(x,y), gridLayout);

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
