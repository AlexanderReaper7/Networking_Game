using System;
using System.CodeDom.Compiler;
using Tools_XNA_dotNET_Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Networking_Game
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private GridLayout gridLayout;
        private Grid grid;
        public Camera2D camera;
        Rectangle playArea;

        public Game1()
        {
            Console.WriteLine("Creating " + nameof(Game1));
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            // Set to borderless window
            Window.IsBorderless = true;
            // Show mouse
            IsMouseVisible = true;
            // Enable multisampling
            graphics.PreferMultiSampling = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Console.WriteLine("Starting "+ nameof(Game1));

            // Get screen size
            int width = graphics.GraphicsDevice.DisplayMode.Width;
            int height = graphics.GraphicsDevice.DisplayMode.Height;

            // Set initial window size
            int windowSize = width > height ? height : width;
            graphics.PreferredBackBufferWidth = windowSize;
            graphics.PreferredBackBufferHeight = windowSize;
            graphics.ApplyChanges();

            // Move window to the center
            //Window.Position = new Point((width /2) - (windowSize /2), (height / 2) - (windowSize / 2));
            // Move window to the upper right corner
            Window.Position = new Point((width) - (windowSize), height - (windowSize )); 

            gridLayout = new GridLayout(20, new Vector2(1,0), 1f, Color.White, new Color(Color.White, 55));

            // Start game
            NewGame(new Point(9,9));

            base.Initialize();
        }

        private void InitializeCommands()
        {
        }

        private void NewGame(Point gridSize)
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
        }

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
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, camera.GetViewMatrix());
            grid.Draw(spriteBatch, gridLayout, camera);

            // Draw a red rectangle over playArea
            //spriteBatch.DrawFilledRectangle(playArea, new Color(Color.Red,55));
            
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
