using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
    public enum GameType
    {
        FillBoard
    }

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
        protected Player[] players;

        protected GameType gameType;
        protected int turnNumber;
        protected bool newTurn;
        protected int activePlayerIndex;
        protected Player ActivePlayer => players[activePlayerIndex];

        public GameCore()
        {
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
            //gridLayout = new GridLayout(20, new Vector2(1,0), 1f, Microsoft.Xna.Framework.Color.White, new Microsoft.Xna.Framework.Color(Microsoft.Xna.Framework.Color.White, 55));

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
            activePlayerIndex = (activePlayerIndex + 1) % players.Length;
            newTurn = true;
            turnNumber++;
        }

        /// <summary>
        /// Check if the condition to end the game is fulfilled 
        /// </summary>
        /// <returns></returns>
        protected bool CheckGameEndCondition()
        {
            // Check if all GridSquares are filled (number of turns equals number of GridSquares)
            int maxTurns = grid.Squares.Length;
            return turnNumber <= maxTurns;
        }

        protected void EndGame()
        {
            // Write game end
            Console.WriteLine("\n\n Game End \n", Color.White); // TODO: use fancy gradient

            // Sort players by descending score
            List<Tuple<int, string, Color>> entries = new List<Tuple<int, string, Color>>(players.Length);
            foreach (Player player in players)
            {
                entries.Add(new Tuple<int, string, Color>(player.Score, player.Name, Color.FromKnownColor(player.Color)));
            }
            entries = entries.OrderByDescending(t => t.Item1).ToList();

            // Check for winners (multiple in case of tie)
            int winners = 1;
            while (entries[winners - 1].Item1 == entries[winners]?.Item1)
            {
                winners++;
                if (winners == entries.Count) break;
            }

            // Write winner TODO: write other end states, no winner (everyone got 0), tie
            for (int i = 0; i < winners; i++)
            {
                Console.WriteLine($"    Winner : {entries[i].Item2} with {entries[i].Item1} points \n", entries[i].Item3);
            }
            // Write Player scores
            for (int i = winners; i < entries.Count; i++)
            {
                Console.WriteLine($"{entries[i].Item2} : {entries[i].Item1}", entries[i].Item3);
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

            // Draw grid
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, camera.GetViewMatrix());
            grid?.Draw(spriteBatch, gridLayout, camera);
            spriteBatch.End();
            

            base.Draw(gameTime);
        }
    }
}
