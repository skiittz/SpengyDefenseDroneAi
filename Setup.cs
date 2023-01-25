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
    partial class Program
    {
        public bool SetUp()
        {
            GetBasicBlocks();
            switch (CurrentMode())
            {
                case Mode.TargetOnly:
                    return true;
                case Mode.Patrol:
                    var waypoints = new List<MyWaypointInfo>();
                    remote.GetWaypointInfo(waypoints);
                    if (!waypoints.Any())
                    {
                        Echo(Prompts.RemoteNeedsWaypointsToPatrol);
                        return false;
                    }
                    else
                    {
                        foreach (var waypoint in waypoints)
                        {
                            MyState.PatrolRoute.Add(waypoint.Coords);
                        }
                    }
                    break;
            }

            if (connector.Status != MyShipConnectorStatus.Connected)
            {
                Echo(Prompts.MustBeDockedToHomeConnectorToRunSetup);
                return false;
            }

            MyState.DockPos = remote.GetPosition();
            MyState.DockApproach = remote.GetPosition() + (connector.WorldMatrix.Backward * int.Parse(configuration.For(ConfigName.DockClearance)));
            Save();
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            return true;
        }

        //public static void GetBasicBlocks(MyGridProgram mgp)
        //{
        //    myBrain = GetBrain(MyState);
        //    if (CurrentMode() != Mode.TargetOnly)
        //    {
        //        remote = FirstTaggedOrDefault<IMyRemoteControl>();
        //        connector = FirstTaggedOrDefault<IMyShipConnector>();
        //        bool samFound;
        //        sam_controller = SingleTagged<IMyProgrammableBlock>(configuration.For(ConfigName.SAMAutoPilotTag), out samFound);
        //        if (samFound && sam_controller.IsWorking)
        //            MyState.NavigationModel = NavigationModel.SAM;
        //        else
        //            MyState.NavigationModel = NavigationModel.Keen;
        //    }

        //    h2Tanks = new List<IMyGasTank>();
        //    GridTerminalSystem.GetBlocksOfType(h2Tanks, block => block.IsSameConstructAs(Me) && block.BlockDefinition.SubtypeName.Contains("Hydro"));

        //    batteries = new List<IMyBatteryBlock>();
        //    GridTerminalSystem.GetBlocksOfType(batteries, block => block.IsSameConstructAs(Me));

        //    reactors = new List<IMyReactor>();
        //    GridTerminalSystem.GetBlocksOfType(reactors, block => block.IsSameConstructAs(Me));
        //}
    }   
}
