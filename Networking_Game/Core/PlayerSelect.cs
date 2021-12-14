using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Networking_Game
{
    public class PlayerSelect : GameCore
    {
        MouseState previousMouseState;
        Player[] selectedPlayers;
        

        public PlayerSelect(int playerTot)
        {
            selectedPlayers = new Player[playerTot];
            players = Player.GetAllCombinations().ToList(); // TODO: don´t convert to list only to use it as array again
            grid = Grid.GeneratePlayerSelectGrid(players.ToArray());
        }

        protected override void Initialize()
        {
            // Initialize mouse state
            previousMouseState = Mouse.GetState(); 
            base.Initialize();
            ConfigureCamera();
        }

        protected override void Update(GameTime gameTime)
        {
            // Update Mouse
            MouseState mouseState = Mouse.GetState();

            // Select Player on mouse left click
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                if (previousMouseState.LeftButton == ButtonState.Released)
                {
                    // Get the position of the mouse in the world
                    Vector2 mouseWorldPos = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
                    // Get the square that contains that position
                    Point? sq = grid.SquareContains(mouseWorldPos, gridLayout);
                    // If that square is valid, and it has a owner,
                    if (sq != null)
                    {
                        if (grid.Squares[sq.Value.X, sq.Value.Y].Owner != null)
                        {
                            // Set player and empty square
                            selectedPlayers[activePlayerIndex++] = grid.Squares[sq.Value.X, sq.Value.Y].Owner;
                            if (activePlayerIndex >= selectedPlayers.Length) Exit(); // exit if players have been selected
                            grid.Squares[sq.Value.X, sq.Value.Y].Owner = null;
                        }
                    }
                }
            }
        
            previousMouseState = mouseState;

            base.Update(gameTime);
        }

        public static Player[] GetSelectedPlayers(int PlayerTot)
        {
            PlayerSelect playerselect = new PlayerSelect(PlayerTot);
            playerselect.Run();

            return playerselect.selectedPlayers;

        }
    }
}