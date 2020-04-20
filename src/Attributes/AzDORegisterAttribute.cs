using System;
using System.Linq;

namespace Julmar.AzDOUtilities
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
