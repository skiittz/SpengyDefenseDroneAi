using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
    public interface INavigationCortex : IAdvancedCortex
    {
        void Go(Vector3D destination, bool forceKeenModel = false);
    }
}
