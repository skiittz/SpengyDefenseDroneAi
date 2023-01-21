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

        public void GetBasicBlocks()
        {
            if (CurrentMode() != Mode.TargetOnly)
            {
                remote = FirstTaggedOrDefault<IMyRemoteControl>();
                connector = FirstTaggedOrDefault<IMyShipConnector>();
                bool samFound;
                sam_controller = SingleTagged<IMyProgrammableBlock>(configuration.For(ConfigName.SAMAutoPilotTag), out samFound);
                if (samFound)
                    MyState.NavigationModel = NavigationModel.SAM;
            }

            h2Tanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType(h2Tanks, block => block.IsSameConstructAs(Me) && block.BlockDefinition.SubtypeName.Contains("Hydro"));

            batteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(batteries, block => block.IsSameConstructAs(Me));

            reactors = new List<IMyReactor>();
            GridTerminalSystem.GetBlocksOfType(reactors, block => block.IsSameConstructAs(Me));
        }

        public T FirstTaggedOrDefault<T>(string tag = null) where T : class
        {
            var list = new List<T>();
            GridTerminalSystem.GetBlocksOfType(list, block => ((block as IMyTerminalBlock).IsSameConstructAs(Me)));
            return list.FirstOrDefault(x => IsTaggedForUse(x, tag ?? configuration.For(ConfigName.Tag))) ?? list.FirstOrDefault();
        }

        public T SingleTagged<T>(string tag, out bool found) where T : class
        {
            var list = new List<T>();
            GridTerminalSystem
                .GetBlocksOfType(list, block => ((block as IMyTerminalBlock).IsSameConstructAs(Me)) && ((block as IMyTerminalBlock).Name != Me.Name));

            list = list.Where(x => IsTaggedForUse(x, tag)).ToList();
            found = list.Count == 1;
            return found ? list.Single() : null;
        }

        public T FirstTaggedOrDefault<T>(IEnumerable<T> obj, string tag)
        {
            return obj.FirstOrDefault(x => IsTaggedForUse(x, tag));
        }

        public bool IsTaggedForUse(object obj, string tag)
        {
            return IsTaggedForUse((obj as IMyTerminalBlock), tag);
        }

        public bool IsTaggedForUse(IMyTerminalBlock block, string tag)
        {
            return block.CustomName.Contains(tag) || block.CustomData.Contains(tag);
        }
    }
}
