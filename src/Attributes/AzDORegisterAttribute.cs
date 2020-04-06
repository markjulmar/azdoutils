using System;
using System.Linq;

namespace AzDOUtilities
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class AzDORegisterAttribute : Attribute
    {
        public Type[] Types { get; }

        public AzDORegisterAttribute(params Type[] typedClasses)
        {
            Types = typedClasses.ToArray();
        }
    }
}
