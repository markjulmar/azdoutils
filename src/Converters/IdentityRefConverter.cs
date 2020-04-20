using Microsoft.VisualStudio.Services.WebApi;

namespace Julmar.AzDOUtilities
{
    public class IdentityRefConverter : BaseFieldConverter<IdentityRef,string>, IFieldComparer
    {
        public bool Compare(object initialValue, object currentValue)
        {
            if (initialValue == null &&
                string.IsNullOrEmpty(currentValue?.ToString()))
                return true;

            return initialValue is IdentityRef identity
                ? string.Compare(currentValue?.ToString()??"", identity.UniqueName, true) == 0
                : false;
        }

        public override string Convert(IdentityRef value)
        {
            if (value == null) return null;
            return (value is IdentityRef identity)
                ? identity.UniqueName : string.Empty;
        }

        public override object ConvertBack(string value) => value;
    }
}
