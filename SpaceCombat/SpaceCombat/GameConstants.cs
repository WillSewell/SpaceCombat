using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace SpaceCombat
{
    /// <summary>
    /// Class keeps track of constants used in the game for easy tweaking.
    /// </summary>
    class GameConstants
    {
        //camera constants
        public const float NearClip = 1.0f;
        public const float FarClip = 5000.0f;
        //ship constants
        public const float ShipVelocity = 1.5f;
        public const float TurnSpeed = 0.01f;
        //Missile constants
        public const int MaxAge = 150;
        public const int NumMissiles = 50;
        //Explosion constants
        public const int ExplosionDuration = 50;
        //general
        public const int PlayerStartHealth = 10;
        //Values to increase difficulty settings by per level
        public const int MissileVelocityIncrease = 1;
        public const int NumTurretsIncrease = 5;
        public const int TimeBetweenMissilesDecrease = -5;
        public const int MissileAccuracyDecrease = -3;
        public const int NumFuelCellsIncrease = 1;
        //bounding sphere scaling factors
        public const float SpaceshipBoundingSphereFactor = .8f;
        public const float PlanetBoundingSphereFactor = 1f;
        public const float TurretBoundingSphereFactor = 1.2f;
        public const float MissileBoundingSphereFactor = .8f;
        public const float FuelCellBoundingSphereFactor = 1.2f;
        //HUD strings
        public const string StrPlayerHealth = "Health: ";
        public const string StrCellsFound = "Fuel Cells Retrieved: ";
        public const string StrGameWon = "Level Complete! Entering level ";
        public const string StrGameLost = "You blew up! Restart!";
    }
}
