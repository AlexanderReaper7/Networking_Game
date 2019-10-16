using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;








using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tools_XNA_dotNET_Framework;

namespace Networking_Game
{
    /// <summary>
    /// The data structure for a space in the game grid.
    /// </summary>
    struct GridSquare
    {
        private Point position;
        private Player owner;
        private bool isRevealed;
    }

    /// <summary>
    /// Contains data for the visual components for grid
    /// </summary>
    public struct GridLayout
    {
        public float SquareSize { get; set; }
        public float Margin { get; set; }
        public bool Bordered { get; set; }
        public float LineThickness { get; set; }
        public Color Color { get; set; }

        public GridLayout(float squareSize, float margin, bool bordered, float lineThickness, Color color)
        {
            SquareSize = squareSize;
            Margin = margin;
            Bordered = bordered;
            LineThickness = lineThickness;
            Color = color;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Grid
    {
        private GridSquare[] gridSquares;

        private int SpotSize;
        private int sizeX;
        private int sizeY;

        public Grid(Point gridSize) : this(gridSize.X, gridSize.Y) { }

        public Grid(int sizeX, int sizeY)
        {
            if (sizeX < 3) throw new ArgumentOutOfRangeException(nameof(sizeX), "Must be greater than 2");
            if (sizeY < 3) throw new ArgumentOutOfRangeException(nameof(sizeY), "Must be greater than 2");

            this.sizeX = sizeX;
            this.sizeY = sizeY;

            int size = sizeX * sizeY;
            gridSquares = new GridSquare[size];
        }


        public void DrawGrid(SpriteBatch spriteBatch, GridLayout gridLayout)
        {
            spriteBatch.DrawGrid(sizeX, sizeY, gridLayout.SquareSize, new Vector2(gridLayout.Margin), gridLayout.Color, gridLayout.LineThickness);
            
        }

        public void DrawSquares(SpriteBatch spriteBatch)
        {

        }

        public void Draw(SpriteBatch spriteBatch, GridLayout gridLayout)
        {
            DrawGrid(spriteBatch, gridLayout);
            DrawSquares(spriteBatch);
        }
    }
}
