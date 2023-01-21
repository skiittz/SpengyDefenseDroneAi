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
        public class State
        {
            public Vector3D DockPos { get; set; }
            public Vector3D DockApproach { get; set; }
            public Vector3D PendingTarget { get; set; }
            public Vector3D CurrentDestination { get; set; }
            public List<Vector3D> PatrolRoute { get; set; }
            public Status Status { get; set; }
            public bool Enroute { get; set; }
            public int CurrentPatrolPoint { get; set; }
            public NavigationModel NavigationModel { get; set; }
            public IMyRemoteControl keen_controller { get; set; }
            public IMyProgrammableBlock sam_controller { get; set; }
            public bool IsSetUpFor(Mode currentMode)
            {
                if (currentMode == Mode.TargetOnly)
                    return true;

                if (currentMode == Mode.Patrol && !PatrolRoute.Any())
                    return false;

                return !DockPos.IsZero() && !DockApproach.IsZero();              
            }
            public State()
            {
                DockPos = Vector3D.Zero;
                DockApproach = Vector3D.Zero;
                PendingTarget = Vector3D.Zero;
                PatrolRoute = new List<Vector3D>();                
            }
            public void SetControllers(IMyRemoteControl keen_controller, IMyProgrammableBlock sam_controller)
            {
                this.keen_controller = keen_controller;
                this.sam_controller = sam_controller;
            }

            public string Serialize()
            {
                var result = $"{nameof(DockPos)}|{DockPos};";
                result += $"{nameof(DockApproach)}|{DockApproach};";
                result += $"{nameof(Status)}|{Status};";
                result += $"{nameof(Enroute)}|{Enroute};";
                result += $"{nameof(CurrentPatrolPoint)}|{CurrentPatrolPoint};";         
                result += $"{nameof(NavigationModel)}|{NavigationModel};";
                result += $"{nameof(CurrentDestination)}|{CurrentDestination};";
                for (int i = 0; i < PatrolRoute.Count; i++)
                {
                    result += $"{nameof(PatrolRoute)}{i}|{PatrolRoute[i]};";
                }
                result = result.TrimEnd(';');

                return result;
            }

            public static State Deserialize(string data)
            {
                var result = new State();
                var lines = data.Split(';');
                var values = lines.ToDictionary(x => x.Split('|')[0], x => x.Split('|')[1]);
                               
                result.DockPos = values.Parse(nameof(DockPos));
                result.DockApproach = values.Parse(nameof(DockApproach));
                result.PendingTarget = Vector3D.Zero;
                result.Status = values.Parse<Status>(values[nameof(Status)]);
                result.Enroute = values.ParseBool(nameof(Enroute));
                result.CurrentPatrolPoint = values.ParseInt(nameof(CurrentPatrolPoint));
                result.CurrentDestination = values.Parse(nameof(CurrentDestination));
                result.PatrolRoute = new List<Vector3D>();
                foreach (var waypoint in values.Where(x => x.Key.Contains(nameof(PatrolRoute))))
                {
                    Vector3D vector = new Vector3D();
                    if (Vector3D.TryParse(waypoint.Value, out vector))
                        result.PatrolRoute.Add(vector);
                }

                return result;
            }

            public void CompleteStateAndChangeTo(Status newStatus)
            {
                Enroute = false;
                Status = newStatus;
                CurrentDestination = Vector3D.Zero;
                
                keen_controller.ClearWaypoints();
                keen_controller.SetAutoPilotEnabled(false);

                if (sam_controller != null)
                    sam_controller.TryRun("STOP");
            }

            public void SetNextPatrolWaypoint()
            {
                CurrentPatrolPoint = CurrentPatrolPoint == (PatrolRoute.Count() - 1)
                    ? 0 
                    : (CurrentPatrolPoint + 1);

                CompleteStateAndChangeTo(Status.Waiting);
            }
        }
    }

    public static class extensions
    {
        public static T Parse<T>(this Dictionary<string, string> values, string name)where T : struct
        {
            T parseResult;
            if (values.ContainsKey(name) && Enum.TryParse(values[name], out parseResult))
                return parseResult;
            else
                return default(T);
        }

        public static Vector3D Parse(this Dictionary<string, string> values, string name)
        {
            Vector3D vector;
            if (values.ContainsKey(name) && Vector3D.TryParse(values[name], out vector))
                return vector;
            else
                return Vector3D.Zero;
        }

        public static int ParseInt(this Dictionary<string,string> values, string name)
        {
            int result;
            if (values.ContainsKey(name) && int.TryParse(values[name], out result))
                return result;

            return default(int);
        }

        public static bool ParseBool(this Dictionary<string, string> values, string name)
        {
            bool result;
            if (values.ContainsKey(name) && bool.TryParse(values[name], out result))
                return result;

            return default(bool);
        }
    }
}
