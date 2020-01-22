using System;
using System.Collections.Generic;
using System.Drawing;
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
                spriteBatch.DrawFilledRectangle(position.X - (gridLayout.LineThickness / 2), position.Y + (gridLayout.LineThickness / 2), gridLayout.SquareSize, gridLayout.SquareSize, System.Drawing.Color.FromKnownColor(Owner.Color).ToXNAColor());
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

        private GridSquare[,] squares;

        private readonly int sizeX;
        private readonly int sizeY;
        private const int minLineLength = 3;

        public GridSquare[,] Squares => squares;

        public Grid(Point gridSize) : this(gridSize.X, gridSize.Y) { }

        public Grid(int sizeX, int sizeY)
        {
            if (sizeX < minLineLength) throw new ArgumentOutOfRangeException(nameof(sizeX), "Must be greater than 2");// TODO: fix error message
            if (sizeY < minLineLength) throw new ArgumentOutOfRangeException(nameof(sizeY), "Must be greater than 2");

            this.sizeX = sizeX;
            this.sizeY = sizeY;

            squares = new GridSquare[sizeX,sizeY];
            for (int x = 0; x < squares.GetLength(0); x++)
            {
                for (int y = 0; y < squares.GetLength(1); y++)
                {
                    squares[x, y] = new GridSquare();
                }
            }
        }

        /// <summary>
        /// Claims a square on the grid for the player
        /// </summary>
        /// <param name="position"></param>
        /// <param name="player"></param>
        /// <returns>Successfully claimed square</returns>
        public bool ClaimSquare(Point position, Player player)
        {
            try
            {
                // If the square does not already have an owner
                if (squares[position.X, position.Y].Owner == null)
                {
                    // Claim the square
                    squares[position.X, position.Y].Claim(player);
                    Console.WriteLine($"{player.Name} Claimed square {position}", System.Drawing.Color.FromKnownColor(player.Color));
                    // Check for line
                    List<Point> newlyLinedPoints = CheckForLine(position, player);
                    // Add score squared to total amount of points
                    player.Score += newlyLinedPoints.Count * newlyLinedPoints.Count;
                    return true;
                }

                return false;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
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
            int squareX = (int)(position.X / gridLayout.SquareSize);
            int squareY = (int)(position.Y / gridLayout.SquareSize);

            return new Point(squareX, squareY);
        }

        /// <summary>
        /// Checks if the square has three or more squares in a line with the same owner and is not already 
        /// </summary>
        /// <param name="checkPoint">The point to check</param>
        /// <param name="owner">The owner</param>
        /// <returns>the new line points</returns>
        public List<Point> CheckForLine(Point checkPoint, Player owner)
        {
            List<Point> output = new List<Point>();

            // Return if the square is not valid
            if ((IsValidBranchPoint(checkPoint, owner) ?? false) == false) return new List<Point>();

            List<Point> directions = new List<Point>{new Point(-1), new Point(0,-1), new Point(1, -1), new Point(-1, 0)};
            // Check neighboring squares that are not already part of a line
            foreach (Point point in directions)
            {
                List<Point> points = new List<Point> {checkPoint};
                int j = 1, i = j;
                while (true)
                {
                    Point branchPoint = new Point(point.X * i, point.Y * i) + checkPoint;
                    // Keep branching if the point has the same owner and is not already in a line
                    while (IsValidBranchPoint(branchPoint, owner) ?? false)
                    {
                        // Add point to list
                        points.Add(branchPoint);
                        i += j;
                        branchPoint = new Point(point.X * i, point.Y * i) + checkPoint;
                    }

                    // If i is positive flip restart loop to go other direction
                    if (j > 0)
                    {
                        j = -1;
                        i = j;
                    }
                    else break;
                }

                if (points.Count >= minLineLength)
                {
                    foreach (Point p in points)
                    {
                        squares[p.X, p.Y].SetIsInLine(true);
                    }
                    // add to output
                    output.AddRange(points);
                }
            }

            return output;
        }

        private bool? IsValidBranchPoint(Point branchPoint, Player owner)
        {
            try
            {
                return squares[branchPoint.X, branchPoint.Y].Owner == owner &&
                       squares[branchPoint.X, branchPoint.Y].IsInLine == false;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Draw(SpriteBatch spriteBatch, GridLayout gridLayout, Camera2D camera)
        {
            spriteBatch.DrawGrid(sizeX, sizeY, gridLayout.SquareSize, gridLayout.Position, gridLayout.Color, gridLayout.LineThickness);
            DrawSquares(spriteBatch, gridLayout, camera);
        }

        private void DrawSquares(SpriteBatch spriteBatch, GridLayout gridLayout, Camera2D camera)
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
                    squares[j,i].Draw(spriteBatch, new Vector2(x,y), gridLayout);

                    // Draw image on the square the mouse is in
                    if (mouseSquare != null)
                        if (mouseSquare.Value.X == j && mouseSquare.Value.Y == i)
                        {
                            spriteBatch.DrawFilledRectangle(
                                j * gridLayout.SquareSize + gridLayout.Position.X - gridLayout.LineThickness / 2,
                                i * gridLayout.SquareSize + gridLayout.Position.Y + gridLayout.LineThickness / 2,
                                gridLayout.SquareSize, gridLayout.SquareSize,
                                gridLayout.MouseOverlayColor);
                        }
                }
            }
        }

        public static Grid GetGameSettingsInput() // TODO: Move to Core/ConsoleManager
        {
            Point gridSize = new Point();
            int maxPlayers;

            X_INPUT:
            Console.Write("Input grid size for the X axis: ", System.Drawing.Color.White);
            if (!int.TryParse(ConsoleManager.GetPriorityInput(), out int result))
            {
                Console.WriteLine("Input is not an integer, try again.", System.Drawing.Color.Red);
                goto X_INPUT;
            }
            if (result < 3 || result > 2048) // TODO: make max size based on screen resolution
            {
                Console.WriteLine("Input must be in range 3 to 2048", System.Drawing.Color.Red);
                goto X_INPUT;
            }
            gridSize.X = result;

            Y_INPUT:
            Console.Write("Input grid size for the Y axis: ", System.Drawing.Color.White);
            if (!int.TryParse(ConsoleManager.GetPriorityInput(), out result))
            {
                Console.WriteLine("Input is not an integer, try again.", System.Drawing.Color.Red);
                goto Y_INPUT;
            }
            if (result < 3 || result > 2048)
            {
                Console.WriteLine("Input cannot be less than 3", System.Drawing.Color.Red);
                goto Y_INPUT;
            }
            gridSize.Y = result;

            // Get max players
            M_INPUT:
            Console.Write("Input maximum amount of players: ", System.Drawing.Color.White);
            if (!int.TryParse(ConsoleManager.GetPriorityInput(), out result))
            {
                Console.WriteLine("Input is not an integer, try again.", System.Drawing.Color.Red);
                goto M_INPUT;
            }
            if (result < 1 || result > Grid.MaxPlayers)
            {
                Console.WriteLine($"Input must be in range 1 to {Grid.MaxPlayers}", System.Drawing.Color.Red);
                goto M_INPUT;
            }
            maxPlayers = result;

            return new Grid(gridSize); // TODO: embed players into Grid
        }
    }
}
