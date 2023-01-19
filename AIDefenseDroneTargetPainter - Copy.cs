using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace SpengyScripts
{
    public class AiDefenseDrone3 : MyGridProgram
    {
        /*
 * R e a d m e
 * -----------
 * 
 * In this file you can include any instructions or other comments you want to have injected onto the 
 * top of your final script. You can safely delete this file if you do not want any such comments.
 */

        string Channel = "DTS";
        /*
                List of commands (arguments to run)
                OFF         Shutdowns the drone (Use only for temporary shutdown)
                ON          Starts the drone (Use for countering the OFF command and Intializing the drone after setup
                */
        Vector3D TargetPos;
        Vector3D EmptyVector = new Vector3D();

        List<string> Data = new List<string>();
        int DetectCounter;


        List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
        MyDetectedEntityInfo TargetEntity;
        IMyLargeTurretBase RefTurret;
        List<IMyLargeTurretBase> RefTurrets = new List<IMyLargeTurretBase>();
        
        //public AiDefenseDrone2()
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            IGC.RegisterBroadcastListener(Channel);
            IGC.GetBroadcastListeners(listeners);
            listeners[0].SetMessageCallback("NewTarget");
            Data.Clear();
            Data = Storage.Split(';').ToList();



            Main("ON", UpdateType.Terminal);

        }


        public void Main(string argument, UpdateType updateSource)
        {
            EnemyCheck();
        }
        


        
        public void EnemyCheck()
        {
            //var radar = GridTerminalSystem.GetBlockGroupWithName("Radar") as IMyRadioAntenna;
            //radar.
            GridTerminalSystem.GetBlocksOfType(RefTurrets, block => block.IsSameConstructAs(Me));
            foreach (IMyLargeTurretBase turret in RefTurrets)
            {
                if (turret.HasTarget)
                {
                    RefTurret = turret;
                    TargetEntity = RefTurret.GetTargetedEntity();
                    DetectCounter++;
                    TargetPos = TargetEntity.Position;
                    Echo("Detect Counter:" + DetectCounter);
                    if (DetectCounter > 0)
                    {
                        //Echo(TargetPos.ToString());
                        IGC.SendBroadcastMessage(Channel, TargetPos.ToString(), TransmissionDistance.TransmissionDistanceMax);
                    }
                }
                else {
                    Echo("No target");
                    DetectCounter = Math.Max(0, (DetectCounter - 1));
                }

                if(DetectCounter == 0)
                {
                    Echo("Empty Target List");
                    TargetPos = EmptyVector;
                }
            }
        }
    }
}
