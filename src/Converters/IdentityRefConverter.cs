using System;
using Microsoft.VisualStudio.Services.WebApi;

namespace Julmar.AzDOUtilities
{
    public class IdentityRefConverter : IFieldConverter, IFieldComparer
    {
        public bool Compare(object initialValue, object currentValue)
        {
            if (initialValue == null &&
                string.IsNullOrEmpty(currentValue?.ToString()))
                return true;

            return initialValue is IdentityRef identity
                ? string.Compare(currentValue?.ToString()??"", Convert(identity), true) == 0
                : false;
        }

        static string Convert(IdentityRef identity) => identity.DisplayName + $" <{identity.UniqueName}>";

        public object Convert(object value, Type toType)
        {
            if (toType != typeof(string))
                throw new Exception(nameof(IdentityRefConverter) + " can only convert to " + nameof(String));
            if (value == null)
                return null;

            if (value is IdentityRef identity)
            {
                return Convert(identity);
            }

            if (value is string s)
            {
                return s;
            }

            return string.Empty;
        }

        public object ConvertBack(object value)
        {
            return value;
        }
    }
}
