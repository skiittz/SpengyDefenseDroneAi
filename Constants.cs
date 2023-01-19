using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    // This template is intended for extension classes. For most purposes you're going to want a normal
    // utility class.
    // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods
    static class Prompts
    {
        public static string CouldNotParseDateFrom = "could not parse date from";
        public static string IsNotFormattedProperly = "is not formatted properly";
        public static string EnemyDetected = "Enemy Detected";
        public static string Attacking = "Attacking";
        public static string PatrolPoint = "Patrol Point";
        public static string DistanceToWaypoint = "Distance to waypoint";
        public static string Destination = "Destination";
        public static string CouldNotDeserializeState = "Could not deserialize state";
        public static string RESET = "RESET";
        public static string ResettingInternalData = "Resetting internal data";
        public static string ON = "ON";
        public static string OFF = "OFF";
        public static string SETUP = "SETUP";
        public static string YourOwnerIdFactionTag = "Your OwnerId/Faction Tag";
        public static string AttemptingAutoSetUp = "Attempting Auto-Setup";
        public static string SetupSuccessfulDroneIsReady = "Setp Successful - Drone is ready!";
        public static string SetupFailedDroneIsNotOperational = "Setup failed - Drone is not operational";
        public static string RETURN = "RETURN";
        public static string DockAndRunSetup = "Dock and run setup";
        public static string CurrentMode = "Current Mode";
        public static string CurrentStatus = "Current Status";
        public static string Enroute = "Enroute";
        public static string MovingTo = "Moving to";
        public static string _null = "null";
        public static string LowAmmo = "Low Ammo";
        public static string AmmoGood = "AmmoGood";
        public static string LowPower = "Low Power";
        public static string PowerGood = "Power Good";
        public static string LowH2 = "Low H2";
        public static string H2Good = "H2 Good";
        public static string ReactorsLowOnFuel = "Reactors low on fuel";
        public static string ReactorFuelGood = "Reactor Fuel Good";
        public static string RemoteNeedsWaypointsToPatrol = "Remote needs waypoints to patrol!";
        public static string MustBeDockedToHomeConnectorToRunSetup = "Must be docked to home connector to run setup";
        public static string Areyoutryingtohackme = "Hmmmm....are you trying to hack me?";
        public static string DaysLeft = "Days left";
        public static string Invalid = "Invalid";
        public static string WaitingForSignal = "Waiting for signal";
    } 

    static class Special
    {
        //public static string NewTaraget_RadioSignal = "NewTarget";
        public static string Debug_ArgFlag = "_dbg";
        public static string Debug_StateFlag = "_State_";
        public static string Debug_Enroute = "_Enroute";
    }

    static class AuthConst
    {
        public static readonly int shift1 = 13;
        public static readonly int shift3 = 5;
        public static readonly int shift2 = 17;
        public static readonly string salt3 = "#$kkjsd35:fj!l4365";
        public static readonly string salt1 = "Aw9!n04$er4";
        public static readonly string salt2 = "jjasdff=M*n@33#";
        public static readonly string saltObfuscation1 = "!n04$";
        public static readonly string saltObfuscation3 = ":35";
        public static readonly string saltObfuscation2 = "=M*n@";
    }

    static class ConfigNameExtensions
    {
        private static readonly Dictionary<Program.ConfigName, string> configDecodes = new Dictionary<Program.ConfigName, string>
        {
            {Program.ConfigName.Tag,"Tag"},
            {Program.ConfigName.Mode, "Mode"},
            {Program.ConfigName.RadioChannel,"RadioChannel" },
            {Program.ConfigName.AttackSpeedLimit, "AttackSpeedLimit" },
            {Program.ConfigName.DockSpeedLimit, "DockSpeedLimit" },
            {Program.ConfigName.GeneralSpeedLimit,"GeneralSpeedLimit" },
            {Program.ConfigName.LowPowerThreshold,"LowPowerThreshold" },
            {Program.ConfigName.LowH2Threshold,"LowH2Threshold" },
            {Program.ConfigName.LowReactorThreshold,"LowReactorThreshold" },
            {Program.ConfigName.DockClearance,"DockClearance" },
            {Program.ConfigName.PersonalKey,"PersonalKey" },
            {Program.ConfigName.FactionKey,"FactionKey" },
            {Program.ConfigName.EnableLowAmmoCheck,"EnableLowAmmoCheck"},
            {Program.ConfigName.UseBurstTransmissions,"UseBurstTransmissions" }
        };

        private static readonly Dictionary<Program.Mode, string> modeDecodes = new Dictionary<Program.Mode, string>
        {
            {Program.Mode.Patrol,"Patrol" },
            {Program.Mode.Defend,"Defend" },
            {Program.Mode.TargetOnly,"TargetOnly" }
        };

        private static readonly Dictionary<Program.Status, string> statusDecodes = new Dictionary<Program.Status, string> {
            {Program.Status.Waiting,"Waiting" },
            {Program.Status.Attacking,"Attacking" },
            {Program.Status.Returning,"Returning" },
            {Program.Status.Docking,"Docking" },
            {Program.Status.Patrolling,"Patrolling" },
            {Program.Status.PreparingToAttack,"Preparing to Attack" }
        };

         public static string ToHumanReadableName(this Program.ConfigName config)
        {
            return configDecodes[config];
           
        }

        public static Program.ConfigName ConfigFromHumanReadableName(this string input)
        {
            return configDecodes.Single(x => x.Value == input).Key;
        }

        public static string ToHumanReadableName(this Program.Mode mode)
        {
            return modeDecodes[mode];
        }

        public static Program.Mode ModeFromHumanReadableName(this string input)
        {
            return modeDecodes.Single(x => x.Value == input).Key;
        }

        public static string ToHumanReadableName(this Program.Status status)
        {
            return statusDecodes[status];
        }
    }
}
