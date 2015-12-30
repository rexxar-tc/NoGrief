namespace NoGriefPlugin.Utility
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Sandbox.ModAPI;
    using VRageMath;
    public static class MathUtility
    {
        public static Vector3D? TraceVector( Vector3D position, Vector3D velocity, int distance, int radius = 100 )
        {
            Vector3D result = Vector3D.Normalize( velocity ) * distance + position;

            //make sure the point is clear
            BoundingSphereD checkSphere = new BoundingSphereD( result, radius );
            if ( MyAPIGateway.Entities.GetIntersectionWithSphere( ref checkSphere ) != null )
                return result;

            return null;
        }
    }
}
