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
        public static string SetupSuccessfulDroneIsReady = "Setup Successful - Drone is ready!";
        public static string SetupFailedDroneIsNotOperational = "Setup failed - Drone is not operational";
        public static string RETURN = "RETURN";
        public static string DockAndRunSetup = "Dock and run setup";
        public static string CurrentMode = "Current Mode";
        public static string CurrentStatus = "Current Status";
        public static string NavigationModel = "Navigation Model";
        public static string Enroute = "Enroute";
        public static string MovingTo = "Moving to";
        public static string _null = "null";
        public static string LowAmmo = "Low Ammo";
        public static string AmmoGood = "Ammo Good";
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
        private static readonly Dictionary<ConfigName, string> configDecodes = new Dictionary<ConfigName, string>
        {
            {ConfigName.Tag,"Tag"},
            {ConfigName.BrainType, "Mode"},
            {ConfigName.RadioChannel,"RadioChannel" },
            {ConfigName.SAMAutoPilotTag,"SAMAutoPilotTag" },
            {ConfigName.AttackSpeedLimit, "AttackSpeedLimit" },
            {ConfigName.DockSpeedLimit, "DockSpeedLimit" },
            {ConfigName.GeneralSpeedLimit,"GeneralSpeedLimit" },
            {ConfigName.LowPowerThreshold,"LowPowerThreshold" },
            {ConfigName.LowH2Threshold,"LowH2Threshold" },
            {ConfigName.LowReactorThreshold,"LowReactorThreshold" },
            {ConfigName.DockClearance,"DockClearance" },
            {ConfigName.PersonalKey,"PersonalKey" },
            {ConfigName.FactionKey,"FactionKey" },
            {ConfigName.EnableLowAmmoCheck,"EnableLowAmmoCheck"},
            {ConfigName.UseBurstTransmissions,"UseBurstTransmissions" },
            {ConfigName.EnableSuicide,"EnableSuicide" },
            {ConfigName.EnableRelayBroadcast, "EnableRelayBroadcast"}
        };

        private static readonly Dictionary<BrainType, string> brainTypeDecodes = new Dictionary<BrainType, string>
        {
            {BrainType.Patrol,"Patrol" },
            {BrainType.Defend,"Defend" },
            {BrainType.TargetOnly,"TargetOnly" }
        };

        private static readonly Dictionary<Status, string> statusDecodes = new Dictionary<Status, string> {
            {Status.Waiting,"Waiting" },
            {Status.Attacking,"Attacking" },
            {Status.Returning,"Returning" },
            {Status.Docking,"Docking" },
            {Status.Patrolling,"Patrolling" },
            {Status.PreparingToAttack,"Preparing to Attack" }
        };

        private static readonly Dictionary<NavigationModel, string> navigationModelDecodes = new Dictionary<NavigationModel, string> 
        {
            {NavigationModel.Keen,"Keen" },
            {NavigationModel.SAM,"SAM" }
        };

        private static readonly Dictionary<CommandType, string> commandTypeDecodes = new Dictionary<CommandType, string>
        {
            {CommandType.Return, "RETURN" },
            {CommandType.Setup, "SETUP" },
            {CommandType.DebugEnroute,"DEBUG ENROUTE" },
            {CommandType.DebugStatus,"DEBUG STATUS" },
            {CommandType.Off,"OFF" },
            {CommandType.On,"ON" },
            {CommandType.Scan,"SCAN" },
            {CommandType.Reset,"RESET" }
        };

         public static string ToHumanReadableName(this ConfigName config)
        {
            return configDecodes[config];
           
        }

        public static ConfigName ConfigFromHumanReadableName(this string input)
        {
            return configDecodes.Single(x => x.Value == input).Key;
        }

        public static string ToHumanReadableName(this BrainType mode)
        {
            return brainTypeDecodes[mode];
        }

        public static BrainType BrainTypeFromHumanReadableName(this string input)
        {
            return brainTypeDecodes.Single(x => x.Value == input).Key;
        }

        public static string ToHumanReadableName(this Status status)
        {
            return statusDecodes[status];
        }

        public static Status StatusFromHumanReadableName(this string input)
        {
            return statusDecodes.Single(x => x.Value == input).Key;
        }

        public static string ToHumanReadableName(this NavigationModel model)
        {
            return navigationModelDecodes[model];
        }

        public static string ToHumanReadableName(this CommandType commandType)
        {
            return commandTypeDecodes[commandType];
        }

        public static CommandType CommandTypeFromHumanReadableName(this string input)
        {
            return commandTypeDecodes.Single(x => x.Value == input).Key;
        }
    }
}
