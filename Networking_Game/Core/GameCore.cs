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
            gridLayout = new GridLayout(20, new Vector2(1,0), 1f, Microsoft.Xna.Framework.Color.White, new Microsoft.Xna.Framework.Color(Microsoft.Xna.Framework.Color.White, 55));

            base.Initialize();
        }


        /// <summary>
        /// Gets input for a player
        /// </summary>
        /// <param name="player"></param>
        protected void GetPlayerSettingsInput(out Player player)
        {
            // Get name TODO: refactor into while loop BUG: name should not be empty
            N_INPUT:
            Console.Write("Input name:  ", Color.White);
            string name = ConsoleManager.GetPriorityInput();
            const int maxNameLength = 12;
            if (name.Length > maxNameLength)
            {
                Console.WriteLine($"That name is too long, max length is {maxNameLength}.", Color.Red);
                goto N_INPUT;
            }

            // if there are other players
            foreach (Player p in players)
            {
                // check their names so to not have duplicates
                if (p?.Name == name)
                {
                    Console.WriteLine("That name is already in use, please pick another.", Color.Red);
                    goto N_INPUT;
                }
            }

            // Get shape TODO: refactor into while loop
            S_INPUT:
            // Write available shapes
            Console.WriteLine("Available shapes: ", Color.White);
            foreach (var s in Enum.GetNames(typeof(PlayerShape)))
            {
                Console.WriteLine(s, Color.White);
            }
            Console.Write("Input shape: ", Color.White);
            string str = ConsoleManager.GetPriorityInput();
            PlayerShape shape;
            if (int.TryParse(str, out int shapeInt))
            {
                if (shapeInt < 1 || shapeInt > Enum.GetNames(typeof(PlayerShape)).Length)
                {
                    Console.WriteLine("incorrect input, try again.", Color.Red);
                    goto S_INPUT;
                }
                shape = (PlayerShape)shapeInt-1;
            }
            else
            {
                if (!Enum.TryParse(str, true, out shape))
                {
                    Console.WriteLine("incorrect input, try again.", Color.Red);
                    goto S_INPUT;
                }
            }

            // Get color TODO: make example colors random TODO: refactor into while loop
            C_INPUT:
            Color[] exampleColors = new Color[] { Color.Red, Color.Blue, Color.Yellow, Color.Cyan, Color.PeachPuff, Color.White, };
            Console.WriteLine("Example colors: ", Color.White);
            foreach (Color exampleColor in exampleColors)
            {
                Console.Write(exampleColor.ToKnownColor() + " ", exampleColor);
            }
            Console.WriteLine();
            Console.Write("Input color: ", Color.White);
            if (!Enum.TryParse(ConsoleManager.GetPriorityInput(), true, out KnownColor color))
            {
                Console.WriteLine("incorrect input, try again.", Color.Red);
                goto C_INPUT;
            }
            // TODO: check if the combination of color and shape is already in use 
            Console.WriteLine($"Created player {name} using {color} {shape}.", Color.FromKnownColor(color));
            player = new Player(name, shape, color);
        }

        protected void GetGameSettingsInput(out Point gridSize, out int maxPlayers) // TODO: Move to Core/ConsoleManager
        {
            X_INPUT:
            Console.Write("Input grid size for the X axis: ", Color.White);
            if (!int.TryParse(ConsoleManager.GetPriorityInput(), out int result))
            {
                Console.WriteLine("Input is not an integer, try again.", Color.Red);
                goto X_INPUT;
            }
            if (result < 3 || result > 2048) // TODO: make max size based on screen resolution
            {
                Console.WriteLine("Input must be in range 3 to 2048", Color.Red);
                goto X_INPUT;
            }
            gridSize.X = result;

            Y_INPUT:
            Console.Write("Input grid size for the Y axis: ", Color.White);
            if (!int.TryParse(ConsoleManager.GetPriorityInput(), out result))
            {
                Console.WriteLine("Input is not an integer, try again.", Color.Red);
                goto Y_INPUT;
            }
            if (result < 3 || result > 2048)
            {
                Console.WriteLine("Input cannot be less than 3", Color.Red);
                goto Y_INPUT;
            }
            gridSize.Y = result;

            // Get max players
            M_INPUT:
            Console.Write("Input maximum amount of players: ", Color.White);
            if (!int.TryParse(ConsoleManager.GetPriorityInput(), out result))
            {
                Console.WriteLine("Input is not an integer, try again.", Color.Red);
                goto M_INPUT;
            }
            if (result < 1 || result > Grid.MaxPlayers)
            {
                Console.WriteLine($"Input must be in range 1 to {Grid.MaxPlayers}", Color.Red);
                goto M_INPUT;
            }
            maxPlayers = result;

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
            switch (gameType)
            {
                case GameType.FillBoard:
                    return CheckFillBoardCondition();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Check if the condition for fill board condition is met and end game if true.
        /// </summary>
        /// <returns></returns>
        private bool CheckFillBoardCondition()
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
            grid.Draw(spriteBatch, gridLayout, camera);
            spriteBatch.End();
            

            base.Draw(gameTime);
        }
    }
}
