using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Tools_XNA_dotNET_Framework;
using Color = System.Drawing.Color;
using Console = Colorful.Console;

namespace Networking_Game.Local
{
    class GameLocal : GameCore
    {
        private MouseState previousMouseState;
        private bool newTurn;


        public GameLocal() : base()
        {

            
        }

        protected override void Initialize()
        {
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
            camera = new Camera2D(this) { Origin = Vector2.Zero };
            playArea = gridLayout.CalculatePlayArea(gridSize.X, gridSize.Y);

            // Get screen size
            Point screenSize = new Point(graphics.GraphicsDevice.DisplayMode.Width, graphics.GraphicsDevice.DisplayMode.Height);

            // Set initial window size
            Point maxWindowSize = screenSize.X > screenSize.Y ? new Point(screenSize.Y) : new Point(screenSize.X);
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
            Window.Position = new Point(screenSize.X - graphics.PreferredBackBufferWidth, screenSize.Y - graphics.PreferredBackBufferHeight);

            // Create players
            players = new Player[maxPlayers];
            for (int i = 0; i < maxPlayers; i++)
            {
                // Prompt for player settings
                Console.WriteLine("Creating player " + (i + 1), Color.White);
                GetPlayerSettingsInput(out Player newPlayer);
                players[i] = newPlayer;
            }

            newTurn = true;
            turnNumber = 1;
            Console.WriteLine("Starting game", Color.White);
        }


        protected override void Update(GameTime gameTime)
        {
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
                            if (grid.ClaimSquare((Point)sq, ActivePlayer))
                            {
                                // Change active player
                                NextPlayer();
                            }
                        }
                    }
                }
            }

            previousMouseState = mouseState;


            base.Update(gameTime);
        }
    }
}
