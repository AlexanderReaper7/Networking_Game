using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Xml;
using Tools_XNA_dotNET_Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = System.Drawing.Color;
using Console = Colorful.Console;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Networking_Game
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class LocalGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private GridLayout gridLayout;
        private Grid grid;
        private Camera2D camera;
        private Rectangle playArea;
        private Player[] players;
        private MouseState PreviousMouseState;

        public LocalGame()
        {
            Console.WriteLine("Creating " + nameof(LocalGame), Color.Gray);
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
            // Get screen size
            int width = graphics.GraphicsDevice.DisplayMode.Width;
            int height = graphics.GraphicsDevice.DisplayMode.Height;

            // Set initial window size TODO fix bug here
            int windowSize = width > height ? height : width;
            graphics.PreferredBackBufferWidth = windowSize;
            graphics.PreferredBackBufferHeight = windowSize;
            graphics.ApplyChanges();

            // Move window to the center
            //Window.Position = new Point((width /2) - (windowSize /2), (height / 2) - (windowSize / 2));
            // Move window to the upper right corner
            Window.Position = new Point((width) - (windowSize), height - (windowSize)); 

            // Create grid layout settings
            gridLayout = new GridLayout(20, new Vector2(1,0), 1f, Microsoft.Xna.Framework.Color.White, new Microsoft.Xna.Framework.Color(Microsoft.Xna.Framework.Color.White, 55));

            // Initialize mouse state
            PreviousMouseState = Mouse.GetState();

            // Start game
            GetGameSettingsInput(out Point gridSize, out int maxPlayers);
            NewGame(gridSize, maxPlayers);

            base.Initialize();
        }

        private void GetGameSettingsInput(out Point gridSize, out int maxPlayers)
        {
            X_INPUT:
            Console.Write("Input grid size for the X axis: ");
            if (!int.TryParse(ConsoleManager.GetPriorityInput(), out int result))
            {
                Console.WriteLine("Input is not an integer, try again.", Color.Red);
                goto X_INPUT;
            }
            if (result < 3)
            {
                Console.WriteLine("Input cannot be less than 3", Color.Red);
                goto X_INPUT;
            }
            gridSize.X = result;

            Y_INPUT:
            Console.Write("Input grid size for the Y axis: ");
            if (!int.TryParse(ConsoleManager.GetPriorityInput(), out result))
            {
                Console.WriteLine("Input is not an integer, try again.", Color.Red);
                goto Y_INPUT;
            }
            if (result < 3)
            {
                Console.WriteLine("Input cannot be less than 3", Color.Red);
                goto Y_INPUT;
            }
            gridSize.Y = result;

            // Get max players
            M_INPUT:
            Console.Write("Input maximum amount of players: ", Color.ForestGreen);
            if (!int.TryParse(ConsoleManager.GetPriorityInput(), out result))
            {
                Console.WriteLine("Input is not an integer, try again.", Color.Red);
                goto M_INPUT;
            }
            if (result < 1)
            {
                Console.WriteLine("Input cannot be less than 1", Color.Red);
                goto M_INPUT;
            }
            maxPlayers = result;

        }

        private void NewGame(Point gridSize, int maxPlayers)
        {
            grid = new Grid(gridSize); 
            camera = new Camera2D(this) {Origin = Vector2.Zero};
            playArea = gridLayout.CalculatePlayArea(gridSize.X, gridSize.Y);

            // Zoom 
            if (playArea.Width > playArea.Height) camera.ZoomToMatchWidth(playArea);
            else camera.ZoomToMatchHeight(playArea);

            // crop window to be size of grid
            Vector2 transform = Vector2.Transform(new Vector2(playArea.Right, playArea.Bottom), camera.GetViewMatrix());
            graphics.PreferredBackBufferWidth = (int)Math.Round(transform.X);
            graphics.PreferredBackBufferHeight = (int)Math.Round(transform.Y);
            graphics.ApplyChanges();

            // Create players
            players = new Player[maxPlayers];
            for (int i = 0; i < maxPlayers; i++)
            {
                // Prompt for player settings
                Console.WriteLine("Creating player " + (i+1));
                GetPlayerSettingsInput(out Player newPlayer);
                players[i] = newPlayer;
            }

            Console.WriteLine("Starting game");
        }

        private void GetPlayerSettingsInput(out Player player)
        {
            // Get name
            N_INPUT:
            Console.Write("Input name:  ");
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

            // Get shape
            S_INPUT:
            // Write available shapes
            Console.WriteLine("Available shapes: ");
            foreach (var s in Enum.GetNames(typeof(PlayerShape)))
            {
                Console.WriteLine(s);
            }
            Console.Write("Input shape: ");
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

            
            // Get color
            C_INPUT:
            Color[] exampleColors = new Color[] { Color.Red, Color.Blue, Color.Yellow, Color.Cyan, Color.PeachPuff, Color.White, };
            Console.WriteLine("Example colors: ");
            foreach (Color exampleColor in exampleColors)
            {
                Console.Write(exampleColor.ToKnownColor() + " ", exampleColor);
            }
            Console.WriteLine();
            Console.Write("Input color: ");
            if (!Enum.TryParse(ConsoleManager.GetPriorityInput(), true, out KnownColor color))
            {
                Console.WriteLine("incorrect input, try again.", Color.Red);
                goto C_INPUT;
            }

            Console.WriteLine($"Created player {name} using {color} {shape}.", Color.FromKnownColor(color));
            player = new Player(name, shape, color);
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

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Update Mouse
            MouseState mouseState = Mouse.GetState();

            // Place marker on mouse left click
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                if (PreviousMouseState.LeftButton == ButtonState.Released)
                {
                    // Get the position of the mouse in the world
                    Vector2 mouseWorldPos = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
                    // Get the square that contains that position
                    Point? sq = grid.SquareContains(mouseWorldPos, gridLayout);
                    // If that square is valid, then claim it
                    if (sq != null)
                    {
                        grid.ClaimSquare((Point)sq, players[0]);
                    }
                }
            }

            PreviousMouseState = mouseState;
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, camera.GetViewMatrix());
            grid.Draw(spriteBatch, gridLayout, camera);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
