using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEModAPI.API;
using SEModAPIInternal.Support;

namespace NoGriefPlugin.Utility
{
    public class Utilities
    {
        public static Type FindTypeInAllAssemblies(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.FullName == typeName)
                        {
                            return type;
                        }
                    }
                }
                catch //(System.Reflection.ReflectionTypeLoadException ex)
                {
                    //if (ExtenderOptions.IsDebugging)
                    //    foreach (var excep in ex.LoaderExceptions)
                    //        ApplicationLog.Error(excep, "Reflection error in stats. You can probably safely ignore this.");
                }
            }
            return null;
        }
    }
}
