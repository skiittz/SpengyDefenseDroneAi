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
    public enum Status
    {
        Waiting,
        Attacking,
        Returning,
        Docking,
        Patrolling,
        PreparingToAttack
    }

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

        public bool IsSetUpFor(BrainType currentMode)
        {
            if (currentMode == BrainType.TargetOnly)
                return true;

            if (currentMode == BrainType.Patrol && !PatrolRoute.Any())
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

        public string Serialize()
        {
            var result = $"{nameof(DockPos)}|{DockPos};";
            result += $"{nameof(DockApproach)}|{DockApproach};";
            result += $"{nameof(Status)}|{Status};";
            result += $"{nameof(Enroute)}|{Enroute};";
            result += $"{nameof(CurrentPatrolPoint)}|{CurrentPatrolPoint};";
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

        public void CompleteStateAndChangeTo(Status newStatus, IAdvancedAiBrain brain)
        {
            Enroute = false;
            Status = newStatus;
            CurrentDestination = Vector3D.Zero;

            brain.remote.ClearWaypoints();
            brain.remote.SetAutoPilotEnabled(false);

            if (brain.samController != null)
                brain.samController.TryRun("STOP");
        }

        public void SetNextPatrolWaypoint(IAdvancedAiBrain brain)
        {
            CurrentPatrolPoint = CurrentPatrolPoint == (PatrolRoute.Count() - 1)
                ? 0
                : (CurrentPatrolPoint + 1);

            CompleteStateAndChangeTo(Status.Waiting, brain);
        }
    }

    public static class extensions
    {
        public static T Parse<T>(this Dictionary<string, string> values, string name) where T : struct
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

        public static int ParseInt(this Dictionary<string, string> values, string name)
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
