using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tools_XNA_dotNET_Framework;
using Color = System.Drawing.Color;
using Console = Colorful.Console;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Networking_Game
{

    /// <summary>
    /// The core of the game
    /// </summary>
    public class GameCore : Game
    {
        protected GraphicsDeviceManager graphics;
        protected SpriteBatch spriteBatch;

        public GridLayout gridLayout;
        public Grid grid;
        public Camera2D camera;
        protected Rectangle playArea;
        protected List<Player> players = new List<Player>();

        protected int turnNumber;
        protected int activePlayerIndex;
        protected Player ActivePlayer => players[activePlayerIndex];

        public GameCore()
        {
            ConsoleManager.Start();
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            // Set to borderless window
            Window.IsBorderless = true;
            // Show mouse
            IsMouseVisible = true;
            // Enable multisampling
            graphics.PreferMultiSampling = true;
            // Disable v-sync
            graphics.SynchronizeWithVerticalRetrace = false;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // Create grid layout settings
            gridLayout = new GridLayout(20, new Vector2(1,0), 1f, Microsoft.Xna.Framework.Color.White, new Microsoft.Xna.Framework.Color(Microsoft.Xna.Framework.Color.White, 55));

            base.Initialize();
        }

        #region Content

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
        }

        #endregion

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        /// <summary>
        /// Starts the next round and increments active player
        /// </summary>
        protected void NextPlayer()
        {
            activePlayerIndex = (activePlayerIndex + 1) % players.Count;
            turnNumber++;
        }

        protected void ConfigureCamera()
        {
            camera = new Camera2D(this) { Origin = Vector2.Zero };
            playArea = gridLayout.CalculatePlayArea(grid.sizeX, grid.sizeY);

            // Get screen size
            Point screenSize = new Point(graphics.GraphicsDevice.DisplayMode.Width, graphics.GraphicsDevice.DisplayMode.Height);

            // Set initial window size
            Point maxWindowSize = screenSize.X > screenSize.Y ? new Point(screenSize.Y) : new Point(screenSize.X); //TODO: what is this?
            graphics.PreferredBackBufferWidth = screenSize.X - ConsoleManager.ConsoleWindow.Right - 8;
            graphics.PreferredBackBufferHeight = screenSize.Y;
            graphics.ApplyChanges();

            // Zoom 
            if (playArea.Width > playArea.Height) camera.ZoomToMatchWidth(playArea);
            else camera.ZoomToMatchHeight(playArea);
            // Crop window to be size of grid
            Vector2 transform = Vector2.Transform(new Vector2(playArea.Right, playArea.Bottom), camera.GetViewMatrix());
            graphics.PreferredBackBufferWidth = (int)Math.Round(transform.X);
            graphics.PreferredBackBufferHeight = (int)Math.Round(transform.Y);
            graphics.ApplyChanges();

            // Move window next to console
            //Window.Position = new Point(ConsoleManager.ConsoleWindow.Right -8, 0);
            // Move window to the center
            //Window.Position = new Point((width /2) - (windowSize /2), (height / 2) - (windowSize / 2));
            // Move window to the upper right corner
            // BUG: window moves to bottom and not top
            Window.Position = new Point(screenSize.X - graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight - screenSize.Y);
        }


        /// <summary>
        /// Check if the condition to end the game is fulfilled 
        /// </summary>
        /// <returns></returns>
        protected bool CheckGameEndCondition()
        {
            // Check if all GridSquares are filled (number of turns equals number of GridSquares)
            return turnNumber <= grid.Squares.Length;
        }

        protected void EndGame()
        {
            // Write game end
            Console.WriteLine("\n\n Game End \n", Color.White); // TODO: use fancy gradient

            // Sort players by descending score

            var sortedPlayers = (from player in players orderby player.Score descending select player).ToArray();
            // Check for winners (multiple in case of tie)
            var winners = (from winner in sortedPlayers 
                where winner.Score == sortedPlayers[0].Score 
                where winner.Score > 0 
                select winner).ToArray();

            // No winner
            if (winners.Length < 1)
            {
                // TODO: write other end states like, no winner (everyone got 0), tie
            }
            else // Write winner 
                foreach (Player winner in winners)
                {
                    Console.WriteLine($" Winner : {winner.Name} with {winner.Score} points", winner.Color);
                }

            // Write empty line for spacing between winners and everyone
            Console.WriteLine();

            // Write all Player scores
            foreach (Player player in sortedPlayers)
            {
                Console.WriteLine($"{player.Name} : {player.Score}", player.Color);
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

            if (grid != null)
            {
                // Draw grid
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, camera?.GetViewMatrix());
                grid.Draw(spriteBatch, gridLayout, camera);
                spriteBatch.End();
            }
            

            base.Draw(gameTime);
        }
    }
}
