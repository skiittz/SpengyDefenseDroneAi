//using Sandbox.Game.EntityComponents;
//using Sandbox.ModAPI.Ingame;
//using Sandbox.ModAPI.Interfaces;
//using SpaceEngineers.Game.ModAPI.Ingame;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;
//using System.Text;
//using VRage;
//using VRage.Collections;
//using VRage.Game;
//using VRage.Game.Components;
//using VRage.Game.GUI.TextPanel;
//using VRage.Game.ModAPI.Ingame;
//using VRage.Game.ModAPI.Ingame.Utilities;
//using VRage.Game.ObjectBuilders.Definitions;
//using VRageMath;

//namespace IngameScript
//{
//    partial class Program
//    {
//        public class Navigation_CustomModel
//        {

//        }

//		/*      
      

//Block names      
//-----------      
//"Align": the block labeled this will be what it tries to center on      
//"AUTOPILOT": the autopilot will use this block to tell you what it's doing.      
      
//"CONTROL [WAYPOINT] [x] [y] [z] [roll] [pitch] [yaw]": this tells the autopilot where to navigate to      
//"VECTORS [dx] [dy] [dz] [max accel forward] [max accel left] [max accel up] [max accel backward] [max accel right] [max accel down]": the autopilot will use this block to identify its speed and how fast it can accelerate in each direction      
      
//Block groups      
//------------      
//"Gyros": the block group for the gyros the autopilot will use.  they should all have the same orientation.      
      
//"Thrust Forward" : put all forward thrusters in this block group.  note these are the thrusters that _push_ in this direction.  meaning e.g. the left thrusters are the ones on the _right_ side.      
//"Thrust Backward": put all forward thrusters in this block group.  note these are the thrusters that _push_ in this direction.  meaning e.g. the left thrusters are the ones on the _right_ side.      
//"Thrust Left"    : put all forward thrusters in this block group.  note these are the thrusters that _push_ in this direction.  meaning e.g. the left thrusters are the ones on the _right_ side.      
//"Thrust Right"   : put all forward thrusters in this block group.  note these are the thrusters that _push_ in this direction.  meaning e.g. the left thrusters are the ones on the _right_ side.      
//"Thrust Up"      : put all forward thrusters in this block group.  note these are the thrusters that _push_ in this direction.  meaning e.g. the left thrusters are the ones on the _right_ side.      
//"Thrust Down"    : put all forward thrusters in this block group.  note these are the thrusters that _push_ in this direction.  meaning e.g. the left thrusters are the ones on the _right_ side.      
      
//*/

//		//0 = forward, 1 = left, 2 = up       

//		double slow_multiple = 100.0;
//		double angleSpeed = 20.0;

//		String block_prefix = "";

//		double distanceAccuracy = 0.25;
//		double orientationAccuracy = 0.25;

//		double maxSpeed = 250;
//		double gyro_power_multiplier = 6;
//		int resetingGyro = 4;
//		int gyroResetTicks = 4;

//		int cur_command = 0;
//		List<int> commands = new List<int>();

//		double thrust_multiplier = 120000;

//		double lastAngle = 0;
//		double lastDim = -1;
//		double lastLevel = 0;
//		double lastRate = 1;
//		double angleDelta = 0;

//		int[] APPROACH_STAGES = null;// new int[3]; // {1,1,1}; //1 = accel, 0 = coast, -1 = decel      
//		List<IMyTerminalBlock>[] thrusters_0 = new List<IMyTerminalBlock>[] { new List<IMyTerminalBlock>(), new List<IMyTerminalBlock>(), new List<IMyTerminalBlock>() };
//		List<IMyTerminalBlock>[] thrusters_1 = new List<IMyTerminalBlock>[] { new List<IMyTerminalBlock>(), new List<IMyTerminalBlock>(), new List<IMyTerminalBlock>() };
//		List<IMyTerminalBlock> gyros = new List<IMyTerminalBlock>();
//		//[]{new List<IMyTerminalBlock>(),new List<IMyTerminalBlock>(),new List<IMyTerminalBlock>()};      

//		void Main()
//		{
//			if (APPROACH_STAGES == null)
//			{
//				APPROACH_STAGES = new int[3];
//				APPROACH_STAGES[0] = 1;
//				APPROACH_STAGES[1] = 1;
//				APPROACH_STAGES[2] = 1;
//			}

//			List<IMyTerminalBlock> statusBlocks = new List<IMyTerminalBlock>();
//			GridTerminalSystem.SearchBlocksOfName(block_prefix + "APSTATUS", statusBlocks);
//			IMyTerminalBlock apstatus = null;
//			if (statusBlocks.Count > 0) apstatus = statusBlocks[0];



//			List<IMyTerminalBlock> outputBlocks = new List<IMyTerminalBlock>();
//			GridTerminalSystem.SearchBlocksOfName(block_prefix + "AUTOPILOT", outputBlocks);
//			var output = outputBlocks[0];

//			var align = GridTerminalSystem.GetBlockWithName(block_prefix + "Align");
//			Vector3D alignpos = align.GetPosition();
//			output.SetCustomName(block_prefix + "AUTOPILOT: " + alignpos.GetDim(0) + "," + alignpos.GetDim(1) + "," + alignpos.GetDim(2));


//			double[] maxAccel = new double[6];
//			for (int i = 0; i < 6; i++)
//			{
//				maxAccel[i] = 0.01;
//			}


//			Vector3D target = new Vector3D(0, 0, 0);
//			Vector3D targetOrientation = new Vector3D(0, 0, 0);
//			Vector3D vectorToTarget = target - align.GetPosition();
//			Vector3D velocityToTarget;


//			List<IMyTerminalBlock> waypointBlocks = new List<IMyTerminalBlock>();
//			GridTerminalSystem.SearchBlocksOfName(block_prefix + "CONTROL", waypointBlocks);
//			var waypoint = waypointBlocks[0];
//			String[] swaypoint_parts = waypoint.CustomName.Split(' ');
//			int ack = 0;
//			if (swaypoint_parts[1] == "WAYPOINTACK")
//			{
//				output.SetCustomName(block_prefix + "AUTOPILOT: WAYPOINTACK");
//				ack = 1;
//			}
//			else
//			if (swaypoint_parts[1] == "WAYPOINT")
//			{
//			}
//			else
//			{
//				output.SetCustomName(block_prefix + "AUTOPILOT: off");
//				return;
//			}
//			target = new Vector3D(
//				double.Parse(swaypoint_parts[2]),
//				double.Parse(swaypoint_parts[3]),
//				double.Parse(swaypoint_parts[4])
//			);
//			targetOrientation = new Vector3D(
//				double.Parse(swaypoint_parts[5]),
//				double.Parse(swaypoint_parts[6]),
//				double.Parse(swaypoint_parts[7])
//			);

//			vectorToTarget = target - align.GetPosition();


//			List<IMyTerminalBlock> vectorBlocks = new List<IMyTerminalBlock>();
//			GridTerminalSystem.SearchBlocksOfName(block_prefix + "VECTORS", vectorBlocks);
//			var vectors = vectorBlocks[0];
//			String[] svector_parts = vectors.CustomName.Split(' ');
//			velocityToTarget = new Vector3D(
//				double.Parse(svector_parts[1]),
//				double.Parse(svector_parts[2]),
//				double.Parse(svector_parts[3])
//			);
//			maxAccel = new double[]{
//		Math.Abs(double.Parse(svector_parts[4]) * 0.5),
//		Math.Abs(double.Parse(svector_parts[5]) * 0.5),
//		Math.Abs(double.Parse(svector_parts[6]) * 0.5),
//		Math.Abs(double.Parse(svector_parts[7]) * 0.5),
//		Math.Abs(double.Parse(svector_parts[8]) * 0.5),
//		Math.Abs(double.Parse(svector_parts[9]) * 0.5)
//	};
//			for (int i = 0; i < 6; i++)
//			{
//				if (maxAccel[i] < 0.001) maxAccel[i] = 0.001;
//			}

//			thrusters_0[0] = getBlockGroup(block_prefix + "Thrust Forward");
//			thrusters_0[1] = getBlockGroup(block_prefix + "Thrust Left");
//			thrusters_0[2] = getBlockGroup(block_prefix + "Thrust Up");
//			thrusters_1[0] = getBlockGroup(block_prefix + "Thrust Backward");
//			thrusters_1[1] = getBlockGroup(block_prefix + "Thrust Right");
//			thrusters_1[2] = getBlockGroup(block_prefix + "Thrust Down");

//			Vector3D[] thrustNorms = new Vector3D[]{
//		averagePosition(thrusters_0[0])-averagePosition(thrusters_1[0]),
//		averagePosition(thrusters_0[1])-averagePosition(thrusters_1[1]),
//		averagePosition(thrusters_0[2])-averagePosition(thrusters_1[2]),
//	};
//			for (int i = 0; i < 3; i++)
//			{
//				thrustNorms[i] /= thrustNorms[i].Length();
//			}

//			double angleOffset = 0;
//			if (swaypoint_parts.Length > 13)
//			{
//				Vector3D targetOrientation2 = new Vector3D(0, 0, 0);
//				targetOrientation2 = new Vector3D(
//					double.Parse(swaypoint_parts[8]),
//					double.Parse(swaypoint_parts[9]),
//					double.Parse(swaypoint_parts[10])
//				);
//				Vector3D targetOrientation3 = new Vector3D(0, 0, 0);
//				targetOrientation3 = new Vector3D(
//					double.Parse(swaypoint_parts[11]),
//					double.Parse(swaypoint_parts[12]),
//					double.Parse(swaypoint_parts[13])
//				);

//				angleOffset = 0
//					+ Math.Abs(Math.Acos(thrustNorms[0].Dot(targetOrientation)) / (thrustNorms[0].Length() * targetOrientation.Length()))
//					+ Math.Abs(Math.Acos(thrustNorms[1].Dot(targetOrientation2)) / (thrustNorms[1].Length() * targetOrientation2.Length()))
//					+ Math.Abs(Math.Acos(thrustNorms[2].Dot(targetOrientation3)) / (thrustNorms[2].Length() * targetOrientation3.Length()))
//				;
//				//handle NaN's
//				if (angleOffset != angleOffset || angleOffset > 1000 || angleOffset < -1000)
//				{
//					angleOffset = lastAngle;
//				}
//				if (angleOffset != angleOffset || angleOffset > 1000 || angleOffset < -1000)
//				{
//					angleOffset = 0;
//				}
//			}

//			overrideGyros(gyros, true);
//			if (vectorToTarget.Length() < distanceAccuracy && angleOffset < orientationAccuracy)
//			{
//				overrideGyros(gyros, false);
//				resetGyros(gyros, 0.0);
//				lastDim = -1;
//				lastRate = 1;
//				lastAngle = angleOffset;

//				for (int i = 0; i < 3; i++)
//				{
//					APPROACH_STAGES[i] = 1;
//				}
//				if (ack == 0)
//					output.SetCustomName(block_prefix + "AUTOPILOT: READY");
//				for (int i = 0; i < 3; i++)
//				{
//					setThrust(thrusters_0[i], 120);
//					setThrust(thrusters_1[i], 120);
//				}
//				return;
//			}
//			else
//			{
//				gyros = getBlockGroup(block_prefix + "Gyros");
//				resetGyros(gyros, 0.0);

//				if (lastDim == -1)
//				{
//					lastDim++;
//					resetingGyro = gyroResetTicks;
//				}
//				else
//				{
//					angleDelta = angleOffset - lastAngle;
//					if (angleDelta > 0 && resetingGyro == 0)
//					{ //if made worse, go to next option. 
//						lastDim++;
//						resetingGyro = gyroResetTicks;
//						if (lastDim == 6)
//						{
//							lastDim = 0;
//							//    if( lastRate > 0.2)
//							//      lastRate *= 0.75;
//						}
//						lastLevel = 0;
//					}
//					//lastLevel+=lastRate;
//					lastLevel = angleOffset * angleSpeed;
//					if (lastLevel < 2) lastLevel = 2;
//					if (resetingGyro > 0)
//					{
//						lastLevel = 0;
//						resetingGyro--;
//					}
//					//lastRate = lastAngle*angleSpeed/2.0;
//					for (int i = 0; i < gyros.Count; i++)
//					{
//						IMyGyro theGyro = (IMyGyro)gyros[i];
//						if (false)
//						{
//						}
//						else if (lastDim == 0)
//						{
//							for (int j = 0; j < lastLevel; j++)
//								theGyro.GetActionWithName("IncreaseRoll").Apply(theGyro);
//						}
//						else if (lastDim == 1)
//						{
//							for (int j = 0; j < lastLevel; j++)
//								theGyro.GetActionWithName("IncreaseYaw").Apply(theGyro);
//						}
//						else if (lastDim == 2)
//						{
//							for (int j = 0; j < lastLevel; j++)
//								theGyro.GetActionWithName("IncreasePitch").Apply(theGyro);
//						}
//						else if (lastDim == 3)
//						{
//							for (int j = 0; j < lastLevel; j++)
//								theGyro.GetActionWithName("DecreaseRoll").Apply(theGyro);
//						}
//						else if (lastDim == 4)
//						{
//							for (int j = 0; j < lastLevel; j++)
//								theGyro.GetActionWithName("DecreaseYaw").Apply(theGyro);
//						}
//						else if (lastDim == 5)
//						{
//							for (int j = 0; j < lastLevel; j++)
//								theGyro.GetActionWithName("DecreasePitch").Apply(theGyro);
//						}
//					}
//				}
//				lastAngle = angleOffset;
//			}

//			double[] speeds = new double[]{
//		velocityToTarget.Dot(thrustNorms[0]),
//		velocityToTarget.Dot(thrustNorms[1]),
//		velocityToTarget.Dot(thrustNorms[2]),
//	};

//			double[] distances = new double[]{
//		vectorToTarget.Dot(thrustNorms[0]),
//		vectorToTarget.Dot(thrustNorms[1]),
//		vectorToTarget.Dot(thrustNorms[2]),
//	};
//			double[] target_forces = new double[] { 0, 0, 0 };

//			//	for( int i = 0; i < 3; i++) {      
//			//if( Math.Abs(distances[i]) < distanceAccuracy) distances[i] = 0;      
//			//	}  

//			double[] towards_or_away = new double[3];
//			double[] vel_at_distance_at_max_accel = new double[3];
//			for (int i = 0; i < 3; i++)
//			{
//				//1 = towards, -1 = away  
//				towards_or_away[i] = speeds[i] * distances[i] >= 0 ? 1.0 : -1.0;
//				double target_velocity = 0;

//				//double time_to_reach_speed = Math.Abs(speeds[i])/maxAccel[i];  
//				//double distance_to_reach_speed = maxAccel[i]*time_to_reach_speed*time_to_reach_speed*0.5;      

//				double time_at_distance_at_max_accel = Math.Sqrt(Math.Abs(distances[i]) * 2.0 / maxAccel[i]);
//				vel_at_distance_at_max_accel[i] = time_at_distance_at_max_accel * maxAccel[i] / slow_multiple;
//				if (towards_or_away[i] < 0)
//				{ //if going away, accel  
//					APPROACH_STAGES[i] = 1;
//				}
//				else
//				if (Math.Abs(speeds[i]) >= Math.Abs(vel_at_distance_at_max_accel[i]))
//				{  //if approach is too fast, decel  
//					APPROACH_STAGES[i] = -1;
//				}
//				else
//				if (velocityToTarget.Length() < maxSpeed && Math.Abs(speeds[i]) < Math.Abs(vel_at_distance_at_max_accel[i]))
//				{
//					//if not at max speed yet, and not going too fast for approach, accel  
//					APPROACH_STAGES[i] = 1;
//				}
//				else
//				{    //otherwise, coast  
//					APPROACH_STAGES[i] = 0;
//				}

//				if (APPROACH_STAGES[i] != 0)
//				{
//					if (vel_at_distance_at_max_accel[i] < maxSpeed)
//					{
//						target_velocity = thrustNorms[i].Dot(vectorToTarget / vectorToTarget.Length()) * vel_at_distance_at_max_accel[i];
//					}
//					else
//					{
//						target_velocity = thrustNorms[i].Dot(vectorToTarget / vectorToTarget.Length()) * maxSpeed;
//					}
//				}
//				else
//				{
//					target_velocity = speeds[i];
//				}
//				target_forces[i] = thrust_multiplier * (target_velocity - speeds[i]) / maxAccel[i];
//			}

//			int plus = 0;
//			int minus = 0;
//			int zero = 0;
//			for (int i = 0; i < 3; i++)
//			{
//				if (APPROACH_STAGES[i] < 0) minus++;
//				if (APPROACH_STAGES[i] > 0) plus++;
//				if (APPROACH_STAGES[i] == 0) zero++;
//				if (target_forces[i] == 0)
//				{
//					setThrust(thrusters_1[i], 120);
//					setThrust(thrusters_0[i], 120);
//				}
//				else if (target_forces[i] > 0)
//				{
//					setThrust(thrusters_1[i], (float)(120 + target_forces[i]));
//					setThrust(thrusters_0[i], 120);
//				}
//				else if (target_forces[i] < 0)
//				{
//					setThrust(thrusters_1[i], 120);
//					setThrust(thrusters_0[i], 120 - ((float)target_forces[i]));
//				}
//			}
//			if (ack == 0)
//			{
//				if (minus == 0 && plus == 0)
//				{
//					output.SetCustomName(block_prefix + "AUTOPILOT: Coasting");
//				}
//				else
//				if (minus > 0 && plus == 0)
//				{
//					output.SetCustomName(block_prefix + "AUTOPILOT: Decelerating");
//				}
//				else
//				if (plus > 0)
//				{
//					output.SetCustomName(block_prefix + "AUTOPILOT: Accelerating");
//				}
//				else
//				{
//					output.SetCustomName(block_prefix + "AUTOPILOT: Adjusting");
//				}
//			}
//			if (apstatus != null)
//			{
//				double v2 = Math.Sqrt(speeds[0] * speeds[0] + speeds[1] * speeds[1] + speeds[2] * speeds[2]);
//				apstatus.SetCustomName("APSTATUS: "
//		+ " " + (velocityToTarget.Length() / v2)
//		);


//				apstatus.SetCustomName("APSTATUS: "
//		+ " " + lastAngle
//		+ " " + angleDelta
//					+ " " + APPROACH_STAGES[0]
//					+ " " + APPROACH_STAGES[1]
//					+ " " + APPROACH_STAGES[2]
//					+ " " + towards_or_away[0]
//					+ " " + towards_or_away[1]
//					+ " " + towards_or_away[2]
//					+ " " + vectorToTarget.Length()
//					+ " " + Math.Abs(speeds[0] / vel_at_distance_at_max_accel[0])
//					+ " " + Math.Abs(speeds[1] / vel_at_distance_at_max_accel[1])
//					+ " " + Math.Abs(speeds[2] / vel_at_distance_at_max_accel[2])
//				);

//			}

//		}
//		void setThrust(List<IMyTerminalBlock> thrusters, Single value)
//		{
//			for (int i = 0; i < thrusters.Count; i++)
//			{
//				thrusters[i].SetValue<Single>("Override", value);
//			}
//		}


//		Vector3D averagePosition(List<IMyTerminalBlock> thrusters)
//		{
//			Vector3D sum = new Vector3D(0, 0, 0);
//			for (int i = 0; i < thrusters.Count; i++)
//			{
//				sum += thrusters[i].GetPosition();
//			}
//			return sum / (double)thrusters.Count;
//		}


//		List<IMyTerminalBlock> getBlockGroup(String name)
//		{
//			for (int i = 0; i < GridTerminalSystem.BlockGroups.Count; i++)
//			{
//				if (GridTerminalSystem.BlockGroups[i].Name == name)
//				{
//					return GridTerminalSystem.BlockGroups[i].Blocks;
//				}
//			}
//			return new List<IMyTerminalBlock>();
//		}

//		void overrideGyros(List<IMyTerminalBlock> gyros, bool yesno)
//		{
//			for (int i = 0; i < gyros.Count; i++)
//			{
//				IMyGyro thegyro = (IMyGyro)gyros[i];
//				if (thegyro.GyroOverride != yesno)
//					thegyro.GetActionWithName("Override").Apply(thegyro);
//			}
//		}

//		void resetGyros(List<IMyTerminalBlock> gyros, double noise)
//		{
//			Random r = new Random();
//			for (int i = 0; i < gyros.Count; i++)
//			{
//				IMyGyro thegyro = (IMyGyro)gyros[i];
//				while (thegyro.Roll < r.NextDouble() * noise * 2 - noise) { thegyro.GetActionWithName("IncreaseRoll").Apply(thegyro); }
//				while (thegyro.Roll > r.NextDouble() * noise * 2 - noise) { thegyro.GetActionWithName("DecreaseRoll").Apply(thegyro); }
//				while (thegyro.Pitch < r.NextDouble() * noise * 2 - noise) { thegyro.GetActionWithName("IncreasePitch").Apply(thegyro); }
//				while (thegyro.Pitch > r.NextDouble() * noise * 2 - noise) { thegyro.GetActionWithName("DecreasePitch").Apply(thegyro); }
//				while (thegyro.Yaw < r.NextDouble() * noise * 2 - noise) { thegyro.GetActionWithName("IncreaseYaw").Apply(thegyro); }
//				while (thegyro.Yaw > r.NextDouble() * noise * 2 - noise) { thegyro.GetActionWithName("DecreaseYaw").Apply(thegyro); }
//			}
//		}


//	}
//}
