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
    public class GameLocal : GameCore
    {
        MouseState previousMouseState;
        bool newTurn;

        protected override void Initialize()
        {
            // Initialize mouse state
            previousMouseState = Mouse.GetState();


            base.Initialize();     
            
            // Start Default game
            NewGame();
        }

        private void NewGame()
        {
            grid = Grid.GetGameSettingsInput();


            // Create players TODO: handle minPlayers
            players = PlayerSelect.GetSelectedPlayers(grid.maxPlayers).ToList();

            //for (int i = 0; i < grid.maxPlayers; i++)
            //{
            //    // Prompt for player settings
            //    Console.WriteLine("Creating player " + (i + 1), Color.White);
            //    var v = Player.GetPlayerSettingsInput();
            //    players[i] = v; // BUG: crashes here because of unknown reason
            //}

            ConfigureCamera();
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
                    Console.Write($"{ActivePlayer.Name}´s turn, ", new Microsoft.Xna.Framework.Color((uint)ActivePlayer.Color).ToSystemColor());
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
                                NextTurn();
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
