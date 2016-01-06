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
            Vector3D normVelocity = Vector3D.Normalize( velocity );
            Vector3D result = normVelocity * distance + position;

            //make sure the point is clear
            int trycount = 1;
            BoundingSphereD checkSphere = new BoundingSphereD( result, radius );
            while ( MyAPIGateway.Entities.GetIntersectionWithSphere( ref checkSphere ) != null )
            {
                //try to find a location 20 times, increasing distance from start each try                
                trycount++;
                result = normVelocity * (distance * trycount) + position;
                checkSphere = new BoundingSphereD( result, radius );

                if ( trycount > 20 )
                    return null;
            }
            return result;
        }

        public static Vector3D? SphereEdgePosition( Vector3D center, int radius )
        {
            return null;
        }
    }
}
