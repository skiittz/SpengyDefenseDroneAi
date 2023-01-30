using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public interface ITurretTargetFinderCortex : ICortex
    {
        MyDetectedEntityInfo? FindTarget();
    }
}
