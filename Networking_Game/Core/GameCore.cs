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
    /// <summary>
    /// The local version of the game
    /// </summary>
    public class GameCore : Game
    {
        enum GameType
        {
            FillBoard
        }

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public GridLayout gridLayout;
        public Grid grid;
        public Camera2D camera;
        private Rectangle playArea;
        private Player[] players;
        private MouseState previousMouseState;
        private GameType gameType;
        private bool newTurn;
        private int turnNumber;
        public int activePlayerIndex;
        public Player ActivePlayer => players[activePlayerIndex];

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

            // Initialize mouse state
            previousMouseState = Mouse.GetState();

            // Start Default game
            StartFillBoard();

            base.Initialize();
        }


        protected void StartFillBoard()
        {
            GetGameSettingsInput(out Point gridSize, out int maxPlayers);
            NewGame(gridSize, maxPlayers);
            gameType = GameType.FillBoard;
        }

        private void NewGame(Point gridSize, int maxPlayers)
        {
            grid = new Grid(gridSize); 
            camera = new Camera2D(this) {Origin = Vector2.Zero};
            playArea = gridLayout.CalculatePlayArea(gridSize.X, gridSize.Y);

            // Get screen size
            Point screenSize = new Point(graphics.GraphicsDevice.DisplayMode.Width, graphics.GraphicsDevice.DisplayMode.Height);

            // Set initial window size
            Point maxWindowSize = screenSize.X > screenSize.Y ? new Point(screenSize.Y): new Point(screenSize.X);
            graphics.PreferredBackBufferWidth = screenSize.X - ConsoleManager.ConsoleWindow.Right-8;
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
            Window.Position = new Point(screenSize.X - graphics.PreferredBackBufferWidth, screenSize.Y - graphics.PreferredBackBufferHeight); 

            // Create players
            players = new Player[maxPlayers];
            for (int i = 0; i < maxPlayers; i++)
            {
                // Prompt for player settings
                Console.WriteLine("Creating player " + (i+1), Color.White);
                GetPlayerSettingsInput(out Player newPlayer);
                players[i] = newPlayer;
            }

            newTurn = true;
            turnNumber = 1;
            Console.WriteLine("Starting game", Color.White);
        }

        private void GetPlayerSettingsInput(out Player player) // TODO: Move to Core/ConsoleManager
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
            if (int.TryParse(str, out int sint)) //TODO better name for sint
            {
                if (sint < 1 || sint > Enum.GetNames(typeof(PlayerShape)).Length)
                {
                    Console.WriteLine("incorrect input, try again.", Color.Red);
                    goto S_INPUT;
                }
                shape = (PlayerShape)sint-1;
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

        private void GetGameSettingsInput(out Point gridSize, out int maxPlayers) // TODO: Move to Core/ConsoleManager
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

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        #endregion

        protected virtual void PreUpdate() { }
        protected virtual void PostUpdate() { }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            PreUpdate();

            // Update Mouse
            MouseState mouseState = Mouse.GetState();

            // Check if game end condition has been fulfilled
            if (!CheckGameEndCondition())
            {
                // Write whose turn it is
                if (newTurn)
                {
                    Console.Write($"{ActivePlayer.Name}´s turn, ", Color.FromKnownColor(ActivePlayer.Color));
                    newTurn = false;
                }

                // Place marker on mouse left click
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    if (previousMouseState.LeftButton == ButtonState.Released)
                    {
                        // Get the position of the mouse in the world
                        Vector2 mouseWorldPos = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
                        // Get the square that contains that position
                        Point? sq = grid.SquareContains(mouseWorldPos, gridLayout);
                        // If that square is valid, then claim it
                        if (sq != null)
                        {
                            // If the claim was successful
                            if (grid.ClaimSquare((Point) sq, ActivePlayer))
                            {
                                // Change active player
                                NextPlayer();
                            }
                        }
                    }
                }
            }

            previousMouseState = mouseState;
            PostUpdate();
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
        private bool CheckGameEndCondition()
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
            int maxTurns = grid.Squares.Length;
            // Check if all GridSquares are filled
            if (turnNumber <= maxTurns) return false;
            // Sort players by descending score
            List<Tuple<int, string, Color>> entries = new List<Tuple<int, string, Color>>(players.Length);
            foreach (Player player in players)
            {
                entries.Add(new Tuple<int, string, Color>(player.Score, player.Name, Color.FromKnownColor(player.Color)));
            }
            entries = entries.OrderByDescending(t => t.Item1).ToList();

            // Write game end
            Console.WriteLine("\n\n ~~~~~~ Game End ~~~~~~ \n\n", Color.White); // TODO: use fancy gradient
            // Write winner
            Console.WriteLine($" ~~~~~~ Winner : {entries[0].Item2} with {entries[0].Item1} points ~~~~~~ \n", entries[0].Item3);
            // Write Player scores
            for (int i = 1; i < entries.Count; i++)
            {
                Console.WriteLine($"{entries[i].Item2} : {entries[i].Item1}", entries[i].Item3);
            }

            // Exit game
            Exit();
            return true;
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
