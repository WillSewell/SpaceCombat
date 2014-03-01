using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SpaceCombat
{
    /// <summary>
    /// This is a class which represents generic game objects.  Almost all objects in
    /// the game inherit from this.
    /// </summary>
    class GameObject
    {
        //Stores the object's 3D model
        public Model Model { get; set; }
        //Stores the object's position in 3D space
        public Vector3 Position { get; set; }
        //Stores the object's bounding sphere, used for collision detection with other objects
        public BoundingSphere BoundingSphere { get; set; }

        /// <summary>
        /// Constructor, simply initialises the properties.
        /// </summary>
        public GameObject()
        {
            Model = null;
            Position = Vector3.Zero;
            BoundingSphere = new BoundingSphere();
        }

        /*
         * Third party code for a calculating bounding sphere for the game object.
         * It works by getting the bounding spheres of the model meshes, and then
         * iteratively merges these to create larger bounding spheres that contain
         * all bounding spheres looked at until that point.
         * Code from: http://msdn.microsoft.com/en-us/library/dd940288.aspx
         */
        protected BoundingSphere CalculateBoundingSphere()
        {
            BoundingSphere mergedSphere = new BoundingSphere();
            BoundingSphere[] boundingSpheres;
            int index = 0;
            int meshCount = Model.Meshes.Count;

            boundingSpheres = new BoundingSphere[meshCount];
            foreach (ModelMesh mesh in Model.Meshes)
            {
                boundingSpheres[index++] = mesh.BoundingSphere;
            }

            mergedSphere = boundingSpheres[0];
            if ((Model.Meshes.Count) > 1)
            {
                index = 1;
                do
                {
                    mergedSphere = BoundingSphere.CreateMerged(mergedSphere, boundingSpheres[index]);
                    index++;
                } while (index < Model.Meshes.Count);
            }
            return mergedSphere;
        }
    }

    /// <summary>
    /// This is a class which represents the player's spacehip avatar.
    /// </summary>
    class Spaceship : GameObject
    {
        //Rotation of spaceship
        public Quaternion Rotation { get; set; }
        //Whether the spaceship is crashing, e.i. intersecting the bounding sphere of the planet or a gun turret
        public bool IsCrashing { get; private set; }

        /// <summary>
        /// Constructor which initialises the rotation.
        /// </summary>
        public Spaceship() : base()
        {
            Rotation = Quaternion.Identity;
        }

        /// <summary>
        /// Loads the objects 3D model and calculates a bounding sphere for it.
        /// </summary>
        /// <param name="content">The game's content manager</param>
        /// <param name="modelName">The path of the model file</param>
        public void LoadContent(ContentManager content, string modelName)
        {
            Model = content.Load<Model>(modelName);

            //Calculate a correctly scaled sphere by calculating a new bounding sphere,
            //And multiply the this by the scaling factor
            BoundingSphere = CalculateBoundingSphere();
            BoundingSphere scaledSphere = BoundingSphere;
            scaledSphere.Radius *= GameConstants.SpaceshipBoundingSphereFactor;
            BoundingSphere = new BoundingSphere(scaledSphere.Center, scaledSphere.Radius);
        }

        /// <summary>
        /// The update method where the logic for the movement of the spaceship is calculated.
        /// </summary>
        /// <param name="keyboardState">The current state of the keyboard</param>
        /// <param name="planetBoundingSphere">The bounding sphere of the planet</param>
        /// <param name="turrets">The array of turrets in the game</param>
        public void Update(KeyboardState keyboardState, BoundingSphere planetBoundingSphere, Turret[] turrets)
        {
            Vector3 futurePosition; //The position the spaceship will move into at this gamestep, unless a collision will stop it
            float turnAmountSide = 0;
            float turnAmountUp = 0;

            //Check if any keys are pressed down, if any are, add or subtract from that turn variable
            if (keyboardState.IsKeyDown(Keys.A))
            {
                turnAmountSide = 0.03f;
            }
            else if (keyboardState.IsKeyDown(Keys.D))
            {
                turnAmountSide = -0.03f;
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                turnAmountUp = 0.03f;
            }
            else if (keyboardState.IsKeyDown(Keys.W))
            {
                turnAmountUp = -0.03f;
            }

            //Calculate the rotation quaternion by creating angles from the turn amount values.
            //Code from: http://www.riemers.net/eng/Tutorials/XNA/Csharp/Series2/Flight_kinematics.php
            Quaternion additionalRot = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), turnAmountSide) * Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), turnAmountUp);
            Rotation *= additionalRot;

            //Calculate the position from the rotation
            Vector3 addVector = Vector3.Transform(new Vector3(0, 0, -1), Rotation);
            futurePosition = Position + addVector * GameConstants.ShipVelocity;

            //Validate the spaceship's movement (only move it if the future position won't intersect the bounding sphere of the plabet or a turret)
            //If it can move to the future position, update the current position, and update the bounding sphere to the new position
            if (ValidateMovement(futurePosition, planetBoundingSphere, turrets))
            {
                Position = futurePosition;
                IsCrashing = false;

                BoundingSphere updatedSphere;
                updatedSphere = BoundingSphere;
                updatedSphere.Center = Position;
                BoundingSphere = new BoundingSphere(updatedSphere.Center, updatedSphere.Radius);
            }
            else
            {
                IsCrashing = true;
            }
        }

        //Helper method to validate the spaceships movement, returns true if movement is valid, false if not
        private bool ValidateMovement(Vector3 futurePosition, BoundingSphere planetBoundingSphere, Turret[] turrets)
        {
            BoundingSphere futureBoundingSphere = BoundingSphere;
            futureBoundingSphere.Center = futurePosition;

            //Check for collision with planet
            if (futureBoundingSphere.Intersects(planetBoundingSphere))
            {
                return false;
            }

            //Loop through turrets and check for collisions
            for (int i = 0; i < turrets.Length; i++)
            {
                if (futureBoundingSphere.Intersects(turrets[i].BoundingSphere))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// The draw method for this class which draws the object to the screen.
        /// This code is inspired by the FuelCell tutorial, but it is a really
        /// quite generic block of code that is used frequently often in XNA programs.
        /// </summary>
        /// <param name="view">The camera view</param>
        /// <param name="projection">The camera projection</param>
        public void Draw(Matrix view, Matrix projection)
        {
            //Stores all the bone transforms, future proofing code from FuelCell game used in building the world matrix
            Matrix[] transforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(transforms);

            //World matrix is made from the the position (created into a translation matrix), 
            //and the rotation which is flipped 180 degrees by a rotation matrix to make it point forward rather than backwards
            Matrix translation = Matrix.CreateTranslation(Position);
            Matrix rotation = Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateFromQuaternion(Rotation);

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = rotation * transforms[mesh.ParentBone.Index] * translation;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                }
                mesh.Draw();
            }
        }
    }

    /// <summary>
    /// Represents the missile turrets that are generated on the surface of the planet
    /// </summary>
    class Turret : GameObject
    {
        //This class is simimlar to the spaceship class, although it doesn't update, 
        //and the way it's initial location is calculated is different
        public Matrix Orientation { get; set; }

        /// <summary>
        /// Loads the objects 3D model and calculates a bounding sphere for it.
        /// </summary>
        /// <param name="content">The game's content manager</param>
        /// <param name="modelName">The path of the model file</param>
        /// <param name="axis">A vector giving the location on the planet's surface</param>
        /// <param name="angle">The angle to rotate the turret by</param>
        /// <param name="planetBoundingSphere">The bounding sphere of the planet</param>
        public void LoadContent(ContentManager content, string modelName, Vector3 axis, float angle, BoundingSphere planetBoundingSphere)
        {
            Model = content.Load<Model>(modelName);

            //3rd party code to work out the inital position on the planet surface.
            //Code from: http://forums.create.msdn.com/forums/t/81774.aspx
            axis.Normalize();
            Matrix gunOrientation = Matrix.CreateFromAxisAngle(axis, angle);
            gunOrientation.Translation = planetBoundingSphere.Center + (gunOrientation.Up * planetBoundingSphere.Radius);
            Orientation = gunOrientation;
            Position = gunOrientation.Translation;

            BoundingSphere = CalculateBoundingSphere();
            BoundingSphere scaledSphere = BoundingSphere;
            scaledSphere.Radius *= GameConstants.TurretBoundingSphereFactor;
            BoundingSphere = new BoundingSphere(Position, scaledSphere.Radius);
            }

        /// <summary>
        /// Draws the turret.
        /// </summary>
        /// <param name="view">The camera view</param>
        /// <param name="projection">The camera projection</param>
        public void Draw(Matrix view, Matrix projection)
        {
            Matrix[] transforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(transforms);

            //The orientation is rotated 270 degrees to make them face upwards out from the planet
            Matrix orientation = Matrix.CreateRotationX(MathHelper.Pi + MathHelper.Pi / 2) * Orientation;

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = orientation;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                }
                mesh.Draw();
            }
        }
    }

    /// <summary>
    /// Represents a missile.
    /// </summary>
    class Missile : GameObject
    {
        Spaceship spaceship; //The spaceship to detect for a collision
        Vector3 direction; //The direction (rotation) of the missile
        public int Age { set; get; } //The time since the missile was fired
        public bool IsFiring { private set; get; } //Whether the missile is firing
        public bool IsExploded { set; get; } // Whether the missile has exploded

        /// <summary>
        /// Loads the objects 3D model and calculates a bounding sphere for it.
        /// </summary>
        /// <param name="content">The game's content manager</param>
        /// <param name="modelName">The path of the model file</param>
        /// <param name="spaceship">The spaceship to fire at</param>
        public void LoadContent(ContentManager content, string modelName, Spaceship spaceship)
        {
            Model = content.Load<Model>(modelName);
            this.spaceship = spaceship;

            IsFiring = false;
            IsExploded = false;
        }

        /// <summary>
        /// Called when the missile should be fired
        /// </summary>
        /// <param name="startPosition">The start location (a turret)</param>
        /// <param name="target">The predicted future position of the spaceship</param>
        public void Fire(Vector3 startPosition, Vector3 target)
        {
            Position = startPosition;

            //Code calculating direction from it's start location and target location.
            //Code from: http://forums.create.msdn.com/forums/t/81901.aspx
            direction = Vector3.Normalize(target - Position);

            BoundingSphere = CalculateBoundingSphere();
            BoundingSphere scaledSphere = BoundingSphere;
            scaledSphere.Radius *= GameConstants.MissileBoundingSphereFactor;
            BoundingSphere = new BoundingSphere(Position, scaledSphere.Radius);

            IsFiring = true;
        }

        /// <summary>
        /// Updates the missile to it's new location and checks for a collision with the spaceship.
        /// </summary>
        /// <param name="missileVelocity">The velocity the missile travels at</param>
        public void Update(int missileVelocity)
        {
            //If the missile intersects the ship's bounding sphere, set it to exploded
            if (BoundingSphere.Intersects(spaceship.BoundingSphere))
            {
                IsExploded = true;
            }
            //If the missile has reached it's maximum age, stop it firing
            else if (Age >= GameConstants.MaxAge)
            {
                IsFiring = false;
            }
            //Otherwise update it to it's new location - based on direction and velocity
            else
            {
                Vector3 old_position = Position;
                Position = old_position + direction * missileVelocity;

                BoundingSphere updatedSphere = BoundingSphere;
                updatedSphere.Center = Position;
                BoundingSphere = new BoundingSphere(updatedSphere.Center, updatedSphere.Radius);

                Age++;
            }
        }

        /// <summary>
        /// Draws the fired missile.
        /// </summary>
        /// <param name="view">The camera view</param>
        /// <param name="projection">The camera projection</param>
        public void Draw(Matrix view, Matrix projection)
        {
            //Straightforward draw code
            Matrix[] transforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = Matrix.CreateWorld(Position, direction, Vector3.Up); ;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                }
                mesh.Draw();
            }
        }
    }

    /// <summary>
    /// Represents the planet.
    /// </summary>
    class Planet : GameObject
    {
        /// <summary>
        /// Loads the objects 3D model and calculates a bounding sphere for it.
        /// </summary>
        /// <param name="content">The game's content manager</param>
        /// <param name="modelName">The path of the model file</param>
        public void LoadContent(ContentManager content, string modelName)
        {
            Model = content.Load<Model>(modelName);
            Position = Vector3.Zero; //Set it's position to be the centre of the world
            BoundingSphere updatedSphere = CalculateBoundingSphere();
            updatedSphere.Center = Position;
            BoundingSphere = new BoundingSphere(updatedSphere.Center, updatedSphere.Radius);
        }

        /// <summary>
        /// Draws the planet.
        /// </summary>
        /// <param name="view">The camera view</param>
        /// <param name="projection">The camera projection</param>
        public void Draw(Matrix view, Matrix projection)
        {
            Matrix[] transforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(transforms);
            Matrix scale = Matrix.CreateScale(0.4f);
            Matrix translation = Matrix.CreateTranslation(Position);

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = scale * transforms[mesh.ParentBone.Index] * translation;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                }
                mesh.Draw();
            }
        }
    }

    /// <summary>
    /// Represents one of the fuelcells that the player must collect.
    /// </summary>
    class FuelCell : GameObject
    {
        //Orientation works in the same was as the gun turrets
        public Matrix Orientation { get; set; }
        //Whether or not the ship has retrieved it
        public bool Retrieved { get; set; }
        //Whether the spaceship was intersecting it's bounding sphere in the previous game step
        bool prevFuelCellIntersect = false;

        /// <summary>
        /// Constructor set's retreived to false.
        /// </summary>
        public FuelCell()
            : base()
        {
            Retrieved = false;
        }

        /// <summary>
        /// Loads the objects 3D model and calculates a bounding sphere for it.
        /// </summary>
        /// <param name="content">The game's content manager</param>
        /// <param name="modelName">The path of the model file</param>
        /// <param name="axis">A vector giving the location on the planet's surface</param>
        /// <param name="angle">The angle to rotate the turret by</param>
        /// <param name="planetBoundingSphere">The bounding sphere of the planet</param>
        public void LoadContent(ContentManager content, string modelName, Vector3 axis, float angle, BoundingSphere planetBoundingSphere)
        {
            //Method work in the same was as the one for Turret
            Model = content.Load<Model>(modelName);

            axis.Normalize();
            Matrix fuelCellOrientation = Matrix.CreateFromAxisAngle(axis, angle);
            fuelCellOrientation.Translation = planetBoundingSphere.Center + (fuelCellOrientation.Up * (planetBoundingSphere.Radius + 20)); //20 makes it come out from surface
            Orientation = fuelCellOrientation;
            Position = fuelCellOrientation.Translation;

            BoundingSphere = CalculateBoundingSphere();
            BoundingSphere scaledSphere = BoundingSphere;
            scaledSphere.Radius *= GameConstants.FuelCellBoundingSphereFactor;
            BoundingSphere = new BoundingSphere(Position, scaledSphere.Radius);
        }

        /// <summary>
        /// Update method for checking if the fuelcell has been retrieved.
        /// </summary>
        /// <param name="vehicleBoundingSphere">The bounding sphere of the spaceship</param>
        /// <param name="pickupSound">The sound effect to play if retrieved</param>
        internal void Update(BoundingSphere vehicleBoundingSphere, SoundEffect pickupSound)
        {
            //If the spaceship is intersecting the fuelcel, and the fuelcell is not already retrieved, set it to retrieved
            if (vehicleBoundingSphere.Intersects(this.BoundingSphere) && this.Retrieved == false)
            {
                this.Retrieved = true;
                if (prevFuelCellIntersect == false)
                {
                    //Player sound effect if this is the first game step where the spaceship has intersected the fuelcell's boundingsphere
                    pickupSound.Play();
                }
                prevFuelCellIntersect = true;
            }
            else
            {
                prevFuelCellIntersect = false;
            }
        }

        /// <summary>
        /// Draws the turret.
        /// </summary>
        /// <param name="view">The camera view</param>
        /// <param name="projection">The camera projection</param>
        public void Draw(Matrix view, Matrix projection)
        {
            //Works in the same was as in Turret
            Matrix[] transforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(transforms);
            Matrix orientation = Matrix.CreateRotationX(MathHelper.Pi + MathHelper.Pi / 2) * Orientation;

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = orientation;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                }
                mesh.Draw();
            }

        }
    }

    /// <summary>
    /// A class that reprecents currently occuring explosions.
    /// </summary>
    public class Explosion
    {
        ParticleEmitter explosionParticleEmitter; //The particle emitter
        Vector3 position; //The explosions position in space
        public int Age { private set; get; } //The age of the explosion in game steps

        /// <summary>
        /// Creates a new game explosion.
        /// </summary>
        /// <param name="particleSystem">The particle system to emit particles with</param>
        /// <param name="position">The position to place the particle emitter</param>
        public Explosion(ParticleSystem particleSystem, Vector3 position)
        {
            Age = 0;
            this.position = position;
            explosionParticleEmitter = new ParticleEmitter(particleSystem, 1000, position);
        }

        /// <summary>
        /// Updates the particle emitter
        /// </summary>
        /// <param name="gameTime">The game time</param>
        public void Update(GameTime gameTime)
        {
            Age++;
            explosionParticleEmitter.Update(gameTime, position);
        }
    }

    /// <summary>
    /// A camera which tracks the spaceship
    /// </summary>
    public class Camera
    {
        //Code for this class from: URL: http://www.riemers.net/eng/Tutorials/XNA/Csharp/Series2/Quaternions.php
        //Below are the view and projection matrices used in the game
        public Matrix ViewMatrix { get; set; }
        public Matrix ProjectionMatrix { get; set; }
        Quaternion cameraRotation; //Used to make the camera's rotation lag behind that of the spaceship

        /// <summary>
        /// Constructor for the camera
        /// </summary>
        public Camera()
        {
            ViewMatrix = Matrix.Identity;
            ProjectionMatrix = Matrix.Identity;
            cameraRotation = Quaternion.Identity;
        }

        /// <summary>
        /// Updates the camera to lag behind the rotation of the spaceship.
        /// </summary>
        /// <param name="spaceshipPos"></param>
        /// <param name="spaceshipRot"></param>
        /// <param name="aspectRatio"></param>
        public void Update(Vector3 spaceshipPos, Quaternion spaceshipRot, float aspectRatio)
        {
            //This is the code which lags the camera's rotation behind the spacehip with the lerp function
            cameraRotation = Quaternion.Lerp(cameraRotation, spaceshipRot, 0.1f);

            //The position of the camera relative to the spaceship
            Vector3 campos = new Vector3(0, 7, 15);
            campos = Vector3.Transform(campos, Matrix.CreateFromQuaternion(cameraRotation));
            campos += spaceshipPos;

            //The height of the camera relative to the spaceship
            Vector3 camup = new Vector3(0, 5, 0);
            camup = Vector3.Transform(camup, Matrix.CreateFromQuaternion(cameraRotation));

            ViewMatrix = Matrix.CreateLookAt(campos, spaceshipPos, camup);
            ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, GameConstants.NearClip, GameConstants.FarClip);
        }
    }
}
