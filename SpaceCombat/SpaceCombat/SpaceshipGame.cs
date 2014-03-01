using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace SpaceCombat
{
    /// <summary>
    /// Hold an ennumeration of the possible states the game can be in.
    /// </summary>
    public enum GameState { SplashPage, Running, Won, Lost }

    /// <summary>
    /// This is the main game class which inherits from the XNA Game class.
    /// </summary>
    public class SpaceshipGame : Microsoft.Xna.Framework.Game
    {
        //Graphics and spritebatch variables
        GraphicsDeviceManager graphics;
        GraphicsDevice device;
        SpriteBatch spriteBatch;
        SpriteFont statsFont;

        //Screen buffer sizes
        int screenWidth = 853;
        int screenHeight = 480;
        
        //Holds the current game state
        GameState currentGameState = GameState.SplashPage;

        //Holds current and previous keyboard states, so changes in user input can be detected
        KeyboardState lastKeyboardState = new KeyboardState();
        KeyboardState currentKeyboardState = new KeyboardState();

        //Variables holding the game objects
        Planet planet;
        Spaceship spaceship;
        FuelCell[] fuelCells;
        Turret[] turrets;
        List<Missile> readyMissiles;
        List<Missile> firedMissiles;
        GameObject boundingSphere;

        //Holds the splash screen image
        Texture2D splashScreenTexture;

        //Holds the music and sound effect files
        Song music;
        SoundEffect fuelCellPickupSound;
        SoundEffect explosionSound;

        //Partical emitter variables
        ParticleEmitter exhaustParticleEmitter;
        ParticleSystem fireParticles;
        ParticleSystem explosionParticles;
        List<Explosion> explosions; //Stores all currently occuring explosions

        //Variables required by the skybox
        Model skyboxModel;
        Texture2D[] skyboxTextures;
        Effect skyboxEffect;

        //Holds the camera
        Camera camera;

        //Random number generator
        Random random = new Random();

        int stepCounter = 0; //Incriments at each game step
        int menuScreenCounter = 0; //Counts the amount of game steps a menu screen has been displayed for

        int retrievedFuelCells = 0; //The numer of fuelcells retrieved by the player
        int playerHealth = GameConstants.PlayerStartHealth; //Health of the player, is decreased by 1 when spaceship is hit
        int level = 1; //Current level
        bool prevSpaceshipCrashState = false; //Stores whether the spaceship was in a state of crashing in the previous game step

        //Values to update to change difficulty for each level
        int missileVelocity = 4;
        int numTurrets = 20;
        int timeBetweenMissiles = 50;
        int missileAccuracy = 50;
        int numFuelCells = 1;

        /// <summary>
        /// Constructor, simply initialises graphics and the content root directory.
        /// </summary>
        public SpaceshipGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = screenWidth;
            graphics.PreferredBackBufferHeight = screenHeight;
            graphics.ApplyChanges();
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            //Initialise objects
            camera = new Camera();
            spaceship = new Spaceship();
            planet = new Planet();
            fuelCells = new FuelCell[numFuelCells];
            turrets = new Turret[numTurrets];
            readyMissiles = new List<Missile>();
            firedMissiles = new List<Missile>();
            boundingSphere = new GameObject();
            explosions = new List<Explosion>();

            //Initialise particle effect variables
            fireParticles = new FireParticleSystem(this, Content);
            fireParticles.DrawOrder = 500;
            Components.Add(fireParticles);

            explosionParticles = new ExplosionParticleSystem(this, Content);
            explosionParticles.DrawOrder = 500;
            Components.Add(explosionParticles);

            base.Initialize();
        }

        /// <summary>
        /// Load all game objects with their models, as well ad initialising sounds and other 
        /// entities used in the game.  Sets up spaceship starting location.
        /// </summary>
        protected override void LoadContent()
        {
            //Create a new SpriteBatch, which can be used to draw 2D graphics.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            //Create a graphics device variable
            device = graphics.GraphicsDevice;

            //Load content of misc game entities
            statsFont = Content.Load<SpriteFont>("Fonts/stats_font");
            skyboxEffect = Content.Load<Effect>("Effects/skybox_effect");
            splashScreenTexture = Content.Load<Texture2D> ("Images/splash_screen");

            //Load object models
            spaceship.LoadContent(Content, "Models/spaceship");
            planet.LoadContent(Content, "Models/planet");
            boundingSphere.Model = Content.Load<Model>("Models/sphere");
            skyboxModel = LoadSkyboxModel("Models/skybox", out skyboxTextures);

            //Iteratively load content for fuelcells, also generate the random seeds which will be used in calculating positions and rotation
            for (int i = 0; i < numFuelCells; i++)
            {
                Vector3 axis = new Vector3((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5));
                float angle = (float)random.NextDouble() * 6.242f;
                fuelCells[i] = new FuelCell();
                fuelCells[i].LoadContent(Content, "Models/fuelcell", axis, angle, planet.BoundingSphere);
            }

            //turrets loaded in the same was as fuelcells
            for (int i = 0; i < numTurrets; i++)
            {
                Vector3 axis = new Vector3((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5));
                float angle = (float)random.NextDouble() * 6.242f;
                turrets[i] = new Turret();
                turrets[i].LoadContent(Content, "Models/turret", axis, angle, planet.BoundingSphere);
            }

            //Populate the ready missile array with all the missiles
            for (int i = 0; i < GameConstants.NumMissiles; i++)
            {
                readyMissiles.Add(new Missile());
                readyMissiles[i].LoadContent(Content, "Models/missile", spaceship);
            }

            //Load sounds, and start playing music
            explosionSound = Content.Load<SoundEffect>("Sound/explosion");
            fuelCellPickupSound = Content.Load<SoundEffect>("Sound/fuelcell_pickup");
            music = Content.Load<Song>("Sound/music");
            MediaPlayer.Play(music);

            //Initial spaceship starting location and rotation
            spaceship.Position = new Vector3(0, 300, 1000);
            spaceship.Rotation = Quaternion.Identity;
            exhaustParticleEmitter = new ParticleEmitter(fireParticles, 1000, spaceship.Position);
        }

        /*
         * Helper method used to load the skybox with the texture files.
         * Code from: http://www.riemers.net/eng/Tutorials/XNA/Csharp/Series2/Skybox.php.
         */
        private Model LoadSkyboxModel(string assetName, out Texture2D[] textures)
        {
            Model newModel = Content.Load<Model>(assetName);
            textures = new Texture2D[newModel.Meshes.Count];
            int i = 0;

            foreach (ModelMesh mesh in newModel.Meshes)
            {
                foreach (BasicEffect currentEffect in mesh.Effects)
                {
                    textures[i++] = currentEffect.Texture;
                }
            }
            foreach (ModelMesh mesh in newModel.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = skyboxEffect.Clone();
                }
            }

            return newModel;
        }

        /// <summary>
        /// No content is unloaded in this project.
        /// </summary>
        protected override void UnloadContent()
        {
        }

        /// <summary>
        /// Game logic is updated here.  Much of the logic is run through calls to 
        /// update methods in the various game objects, but a number of checks for
        /// events that may have occured at each game step are made here.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            //Update keyboard state
            lastKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();

            //Allows the game to exit when esc key is pressed
            if (currentKeyboardState.IsKeyDown(Keys.Escape))
            {
                this.Exit();
            }

            //If the current state is SplashPage, display the splash page image until the enter key is pressed
            if (currentGameState == GameState.SplashPage)
            {
                //Check for enter key press, move to running state if it is
                if ((lastKeyboardState.IsKeyDown(Keys.Enter) && (currentKeyboardState.IsKeyUp(Keys.Enter))))
                {
                    currentGameState = GameState.Running;
                }
            }

            //This code is executed when the game state is Running, the main game logic is here
            else if (currentGameState == GameState.Running)
            {
                stepCounter++;
                float aspectRatio = graphics.GraphicsDevice.Viewport.AspectRatio;

                //Update the spaceship
                spaceship.Update(currentKeyboardState, planet.BoundingSphere, turrets);

                //If the step counter is a multiple of the timeBetween missiles variable, fire a new missile.
                if ((stepCounter % timeBetweenMissiles) == 0)
                {
                    if (readyMissiles.Count > 0)
                    {
                        readyMissiles[0].Fire(closestTurretToShip().Position, predictedSpaceshipFuturePos());
                        //Add it to the fired list, and remove it from the ready list
                        firedMissiles.Add(readyMissiles[0]);
                        readyMissiles.RemoveAt(0);
                    }
                }
                //Check conditions of each missile
                for (int i = 0; i < firedMissiles.Count; i++)
                {
                    //Check fired missiles list to see if any should stop firing
                    if (firedMissiles[i].IsFiring == false)
                    {
                        //If it has stopped firing, reset age to 0, and move from firing to ready list
                        firedMissiles[i].Age = 0;
                        readyMissiles.Add(firedMissiles[i]);
                        firedMissiles.RemoveAt(i);
                    }
                    //Check to see if missile has exploded (has collided with the player's ship)
                    else if (firedMissiles[i].IsExploded == true)
                    {
                        //If it has collided, reset age to 0, set it to not exploding, and move from firing to ready list
                        firedMissiles[i].Age = 0;
                        firedMissiles[i].IsExploded = false;
                        readyMissiles.Add(firedMissiles[i]);
                        firedMissiles.RemoveAt(i);

                        //Reduce player's health
                        playerHealth--;

                        //Create a new explosion and play the explosion sound
                        explosions.Add(new Explosion(explosionParticles, spaceship.Position));
                        explosionSound.Play();
                    }
                    else
                    {
                        //Else, update the missile
                        firedMissiles[i].Update(missileVelocity);
                    }
                }

                //Check's the condition of the fuelcells
                retrievedFuelCells = 0;
                //loop through and update the fuelcells
                //If any are retrieved, increment the retrieved fuelcell counter
                foreach (FuelCell fuelCell in fuelCells)
                {
                    fuelCell.Update(spaceship.BoundingSphere, fuelCellPickupSound);
                    if (fuelCell.Retrieved)
                    {
                        retrievedFuelCells++;
                    }
                }
                //If the retrieved fuelcells equal the number of fuelcell in the game, the level is complete.
                //Update the level and enter the Won game state.
                if (retrievedFuelCells == numFuelCells)
                {
                    level++;
                    currentGameState = GameState.Won;
                }

                //If the player's health is 0, the player is dead; enter the Lost game state
                if (playerHealth <= 0)
                {
                    currentGameState = GameState.Lost;
                }

                try
                {
                    //Check each explosion to see if it is time to stop existing, because it has reached it's maximum age
                    foreach (Explosion explosion in explosions)
                    {
                        explosion.Update(gameTime);
                        if (explosion.Age >= GameConstants.ExplosionDuration)
                        {
                            explosions.Remove(explosion);
                        }
                    }
                }
                //If array is empy, stop looping.
                catch (InvalidOperationException)
                {
                }

                //Check to see if the spaceship is crashing - if it's colliding with the planet, or one of the turrets
                //If it has, play explosion sound, and reduce player's health.
                if (spaceship.IsCrashing)
                {
                    //Only count as crashed if it wasn't crashed in the previous state, this stops the player from
                    //losing health for every step they spend intersecting the bounding sphere they are crashing with
                    if (!prevSpaceshipCrashState)
                    {
                        explosionSound.Play();
                        playerHealth--;
                        prevSpaceshipCrashState = true;
                    }
                }
                else
                {
                    prevSpaceshipCrashState = false;
                }

                //Finally, update camera and particle emitter for the ship
                camera.Update(spaceship.Position, spaceship.Rotation, aspectRatio);
                exhaustParticleEmitter.Update(gameTime, spaceship.Position);
            }

            //If the current gamestate is Won, stay in this state until the menuScreenCounter is less than 150
            else if (currentGameState == GameState.Won)
            {
                menuScreenCounter++;
                //On the last step, enter the new level by updating the difficulty, resseting the game and entering the running state
                if (menuScreenCounter >= 150)
                {
                    updateForLevel();
                    ResetGame();
                    currentGameState = GameState.Running;
                }
            }
            //Much the same as above, although the level is simply restarted
            else if (currentGameState == GameState.Lost)
            {
                menuScreenCounter++;
                if (menuScreenCounter >= 150)
                {
                    ResetGame();
                    currentGameState = GameState.Running;
                }
            }
            base.Update(gameTime);
        }

        /*
         * Helper method for finding a random turret out of the turrets that are closest to the ship.
         * Used when calculating which turret to fire a missile from
         */
        private Turret closestTurretToShip()
        {
            //Work out how closest turrets to use to pick a random turret from
            //This is worked out to be 20% of the total turrets, and at least 1
            int numFiringTurrets = numTurrets / 5;
            if (numFiringTurrets > 10)
            {
                numFiringTurrets = 1;
            }
            //Create an array for the current closest turrets, and a corrosponding array to
            //hold their distances from the spaceship, for example, the distance from the spaceship
            //of a turret at index 3 in the first array will be held in index 3 of the second array
            Turret[] closestTurrets = new Turret[numFiringTurrets];
            float[] closestTurretDistances = new float[numFiringTurrets];
            //Initialise the arrays with dummy initial values
            for (int i = 0; i < numFiringTurrets; i++)
            {
                closestTurrets[i] = new Turret();
                closestTurretDistances[i] = float.MaxValue; //High number, so turrets will always replace these
            }
            //Iterate through each turret, checking if it is closer to the spaceship
            //than any turrets in the current array of closest turrets
            for (int i = 1; i < turrets.Length; i++)
            {
                float distance = Vector3.Distance(spaceship.Position, turrets[i].Position);
                bool finished = false;
                int j = 0;
                while (!finished && j < numFiringTurrets)
                {
                    //If is is closer
                    if (distance < closestTurretDistances[j])
                    {
                        //swap it in at the same index of the turret it was closer than
                        closestTurrets[j] = turrets[i];
                        closestTurretDistances[j] = distance;
                        finished = true; //Stop iterating
                    }
                    j++;
                }
                //Sort and reverse the array, so the furthest turrets will always be considered first
                Array.Sort(closestTurretDistances, closestTurrets);
                Array.Reverse(closestTurretDistances);
                Array.Reverse(closestTurrets);
            }
            //Randomly pick a turret from the array of closests turrets, and return it
            int turretToPickIndex = random.Next(0, numFiringTurrets);
            return closestTurrets[turretToPickIndex];
        }

        /*
         * This method is used to predict the future position of the spaceship, so that the missiles know where to aim.
         * Otherwise they would aim themselves behind where the spaceship is when they arrive at it's location.
         * Some innacuracy is also added here for more realism.
         */
        private Vector3 predictedSpaceshipFuturePos()
        {
            //The units ahead in direction the spacechip is facing to aim at
            float distanceAhead = 50;

            //Use the same code to predict the future position as is used to move the spaceship normally
            Vector3 addVector = Vector3.Transform(new Vector3(0, 0, -1), spaceship.Rotation);
            Vector3 futurePosition = spaceship.Position + addVector * distanceAhead;

            //Add or subtract a random value to all the vector coordinates to reduce accuracy
            Vector3 inaccurateFuturePos = Vector3.Zero;
            inaccurateFuturePos.X = futurePosition.X + random.Next(0, missileAccuracy) - random.Next(0, missileAccuracy);
            inaccurateFuturePos.Y = futurePosition.Y + random.Next(0, missileAccuracy) - random.Next(0, missileAccuracy);
            inaccurateFuturePos.Z = futurePosition.Z + random.Next(0, missileAccuracy) - random.Next(0, missileAccuracy);

            return inaccurateFuturePos;
        }

        //Method to update the variables which affect the difficulty when a new level is entered.
        private void updateForLevel()
        {
            missileVelocity += GameConstants.MissileVelocityIncrease;
            numTurrets += GameConstants.NumTurretsIncrease;
            timeBetweenMissiles += GameConstants.TimeBetweenMissilesDecrease;
            missileAccuracy += GameConstants.MissileAccuracyDecrease;
            numFuelCells += GameConstants.NumFuelCellsIncrease;
        }

        /*
         * Resests the game objects and some variables to the starting configurations.
         * Much of this is very similar to the LoadContent method.
         */
        private void ResetGame()
        {
            //Settings to reset to start a new game
            retrievedFuelCells = 0;
            menuScreenCounter = 0;
            stepCounter = 0;
            playerHealth = GameConstants.PlayerStartHealth;
            
            //Create new fuelcells, turrets and missiles
            fuelCells = new FuelCell[numFuelCells];
            turrets = new Turret[numTurrets];
            readyMissiles = new List<Missile>();
            firedMissiles = new List<Missile>();

            //Load the fuelcells, turrets and missiles in the same way they were loaded in LoadContent
            for (int i = 0; i < numFuelCells; i++)
            {
                Vector3 axis = new Vector3((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5));
                float angle = (float)random.NextDouble() * 6.242f;
                fuelCells[i] = new FuelCell();
                fuelCells[i].LoadContent(Content, "Models/fuelcell", axis, angle, planet.BoundingSphere);
            }

            for (int i = 0; i < numTurrets; i++)
            {
                Vector3 axis = new Vector3((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5));
                float angle = (float)random.NextDouble() * 6.242f;
                turrets[i] = new Turret();
                turrets[i].LoadContent(Content, "Models/turret", axis, angle, planet.BoundingSphere);
            }

            for (int i = 0; i < GameConstants.NumMissiles; i++)
            {
                readyMissiles.Add(new Missile());
                readyMissiles[i].LoadContent(Content, "Models/missile", spaceship);
            }

            explosions.Clear(); //Remove all occuring explosions

            //Initial spaceship starting location and rotation
            spaceship.Position = new Vector3(0, 300, 1000);
            spaceship.Rotation = Quaternion.Identity;
            exhaustParticleEmitter = new ParticleEmitter(fireParticles, 1000, spaceship.Position);
        }

        /// <summary>
        /// The main draw method, this calls a different draw method depending on the current state of the game.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            switch (currentGameState)
            {
                case GameState.SplashPage:
                    DrawSplashScreen();
                    break;
                case GameState.Running:
                    DrawGameplay();
                    break;
                case GameState.Won:
                    DrawWinOrLoseScreen(GameConstants.StrGameWon, true);
                    break;
                case GameState.Lost:
                    DrawWinOrLoseScreen(GameConstants.StrGameLost, false);
                    break;
            }

            base.Draw(gameTime);
        }

        //Method to draw the splash screen when the game is started
        //Code from: http://www.riemers.net/eng/Tutorials/XNA/Csharp/Series2D/Drawing_fullscreen_images.php
        private void DrawSplashScreen()
        {
            spriteBatch.Begin();
            Rectangle screenRectangle = new Rectangle(0, 0, screenWidth, screenHeight);
            spriteBatch.Draw(splashScreenTexture, screenRectangle, Color.White);
            spriteBatch.End();
        }

        /*
         * Main draw method which is called when the actual game is running.
         * This mainly consist of calls to the draw methods in the various game objects.
         */
        private void DrawGameplay()
        {
            //Draw particles
            fireParticles.SetCamera(camera.ViewMatrix, camera.ProjectionMatrix);
            explosionParticles.SetCamera(camera.ViewMatrix, camera.ProjectionMatrix);
            //Reset the graphics device settings for drawing 3d graphics, 
            //because the particle emitters involve drawing 2D grapghics
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            //Draw skybox, planet and spaceship
            DrawSkybox();
            planet.Draw(camera.ViewMatrix, camera.ProjectionMatrix);
            spaceship.Draw(camera.ViewMatrix, camera.ProjectionMatrix);

            //Loop through the turrets, fuelcells and fired missiles, and draw them
            for (int i = 0; i < numTurrets; i++)
            {
                turrets[i].Draw(camera.ViewMatrix, camera.ProjectionMatrix);
            }
            for (int i = 0; i < numFuelCells; i++)
            {
                if (!fuelCells[i].Retrieved)
                {
                    fuelCells[i].Draw(camera.ViewMatrix, camera.ProjectionMatrix);
                }
            }
            for (int i = 0; i < firedMissiles.Count; i++)
            {
                firedMissiles[i].Draw(camera.ViewMatrix, camera.ProjectionMatrix);
            }
            //Draw the stats over the display
            DrawStats();
        }

        //Helper method for drawing the stats, mainly 3rd party code from FuelCell, although modified to draw
        //slightly different strings
        //Code from: http://msdn.microsoft.com/en-us/library/dd940288.aspx
        private void DrawStats()
        {
            float xOffsetText, yOffsetText;
            //Build strings to show the health, and to show the number of fuelcells collected out of the total fuelcells
            string str1 = GameConstants.StrPlayerHealth;
            string str2 = GameConstants.StrCellsFound + retrievedFuelCells.ToString() + " of " + numFuelCells.ToString();
            Rectangle rectSafeArea;

            str1 += playerHealth.ToString();

            //Calculate str1 position
            rectSafeArea = GraphicsDevice.Viewport.TitleSafeArea;

            xOffsetText = rectSafeArea.X;
            yOffsetText = rectSafeArea.Y;

            Vector2 strSize = statsFont.MeasureString(str1);
            Vector2 strPosition = new Vector2((int)xOffsetText + 10, (int)yOffsetText);

            //Draw the strings
            spriteBatch.Begin();
            spriteBatch.DrawString(statsFont, str1, strPosition, Color.White);
            strPosition.Y += strSize.Y;
            spriteBatch.DrawString(statsFont, str2, strPosition, Color.White);
            spriteBatch.End();

            DepthStencilState dss = new DepthStencilState();
            dss.DepthBufferEnable = true;
            GraphicsDevice.DepthStencilState = dss;
        }
        /*
         * This is called to draw the winning or losing screen when the game is in the
         * winning or losing state.  Again, most of the code is from fuelcell, with updated
         * message strings.
         * Code from: http://msdn.microsoft.com/en-us/library/dd940288.aspx
         */
        private void DrawWinOrLoseScreen(string message, bool won)
        {
            float xOffsetText, yOffsetText;
            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            xOffsetText = yOffsetText = 0;
            Vector2 strResult;
            //Check if the game was won or lost, and build a different message string depending on this
            if (won)
            {
                strResult = statsFont.MeasureString(message + level);
            }
            else
            {
                strResult = statsFont.MeasureString(message);
            }
            
            Vector2 strCenter = new Vector2(strResult.X / 2, strResult.Y / 2);

            yOffsetText = (viewportSize.Y / 2 - strCenter.Y);
            xOffsetText = (viewportSize.X / 2 - strCenter.X);
            Vector2 strPosition = new Vector2((int)xOffsetText, (int)yOffsetText);

            spriteBatch.Begin();
            if (won)
            {
                spriteBatch.DrawString(statsFont, message + level, strPosition, Color.Red);
            }
            else
            {
                spriteBatch.DrawString(statsFont, message, strPosition, Color.Red);
            }
            spriteBatch.End();

            DepthStencilState dss = new DepthStencilState();
            dss.DepthBufferEnable = true;
            GraphicsDevice.DepthStencilState = dss;
        }

        /*
         * This 3rd party code is used to draw the skybox.
         * Code from: http://www.riemers.net/eng/Tutorials/XNA/Csharp/Series2/Skybox.php
         */
        private void DrawSkybox()
        {
            SamplerState ss = new SamplerState();
            ss.AddressU = TextureAddressMode.Clamp;
            ss.AddressV = TextureAddressMode.Clamp;
            device.SamplerStates[0] = ss;

            DepthStencilState dss = new DepthStencilState();
            dss.DepthBufferEnable = false;
            device.DepthStencilState = dss;

            Matrix[] skyboxTransforms = new Matrix[skyboxModel.Bones.Count];
            skyboxModel.CopyAbsoluteBoneTransformsTo(skyboxTransforms);
            int i = 0;
            foreach (ModelMesh mesh in skyboxModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    Matrix worldMatrix = skyboxTransforms[mesh.ParentBone.Index] * Matrix.CreateTranslation(spaceship.Position);
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Textured"];
                    currentEffect.Parameters["xWorld"].SetValue(worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(camera.ViewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(camera.ProjectionMatrix);
                    currentEffect.Parameters["xTexture"].SetValue(skyboxTextures[i++]);
                }
                mesh.Draw();
            }

            dss = new DepthStencilState();
            dss.DepthBufferEnable = true;
            device.DepthStencilState = dss;
        }
    }
}
