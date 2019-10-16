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
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Window.IsBorderless = true;
            Window.AllowUserResizing = true;
            IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            gridLayout = new GridLayout(25,50,true,1.4f,Color.White);

            NewGame(new Point(25));

            base.Initialize();
        }

        private void NewGame(Point gridSize)
        {
            grid = new Grid(gridSize);
            camera = new Camera2D(this) {Origin = Vector2.Zero};
            playArea = new Rectangle(0, 0,
                (int)(gridLayout.SquareSize * gridSize.X + gridLayout.Margin * 2),
                (int)(gridLayout.SquareSize * gridSize.Y + gridLayout.Margin * 2));
            //camera.ZoomToMatchHeight(playArea);
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
            // Move and zoom camera to show entire grid
            //camera.LookAt(camera.ScreenToWorld(new Vector2(Mouse.GetState().X, Mouse.GetState().Y)));
            camera.LookAt(Mouse.GetState().Position);

            //look at center of grid
            //camera.ZoomToMatchHeight(playArea);
            //camera.ZoomToMatchWidth(playArea);
            //camera.Position = new Vector2(Mouse.GetState().Position.X, Mouse.GetState().Position.Y);
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            //spriteBatch.Begin(SpriteSortMode.Deferred,null,null,null,null,null,camera.GetViewMatrix());
            spriteBatch.Begin();
            grid.Draw(spriteBatch, gridLayout);
            spriteBatch.DrawFilledRectangle(playArea, new Color(Color.Red,55));
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
