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
            public List<Vector3D> PatrolRoute { get; set; }
            public Status Status { get; set; }
            public bool Enroute { get; set; }
            public int CurrentPatrolPoint { get; set; }
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

            public string Serialize()
            {
                var result = $"{nameof(DockPos)}|{DockPos};";
                result += $"{nameof(DockApproach)}|{DockApproach};";
                result += $"{nameof(Status)}|{Status};";
                result += $"{nameof(Enroute)}|{Enroute};";
                result += $"{nameof(CurrentPatrolPoint)}|{CurrentPatrolPoint};";               
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

                Vector3D dockPos = new Vector3D();
                if (values.ContainsKey(nameof(DockPos)) && Vector3D.TryParse(values[nameof(DockPos)], out dockPos))
                    result.DockPos = dockPos;

                Vector3D dockApproach = new Vector3D();
                if (values.ContainsKey(nameof(DockApproach)) && Vector3D.TryParse(values[nameof(DockApproach)], out dockApproach))
                    result.DockApproach = dockApproach;

                result.PendingTarget = Vector3D.Zero;

                Status status;               
                if (values.ContainsKey(nameof(Status)) && Enum.TryParse(values[nameof(Status)], out status))
                    result.Status = status;

                bool isEnroute;
                result.Enroute = values.ContainsKey(nameof(Enroute)) && bool.TryParse(values[nameof(Enroute)], out isEnroute) && isEnroute;

                int patrolPoint;
                if (values.ContainsKey(nameof(CurrentPatrolPoint)) && int.TryParse(values[nameof(CurrentPatrolPoint)], out patrolPoint))
                    result.CurrentPatrolPoint = patrolPoint;

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
}
