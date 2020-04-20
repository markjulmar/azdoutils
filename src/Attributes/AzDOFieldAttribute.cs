using System;

namespace Julmar.AzDOUtilities
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AzDOFieldAttribute : Attribute
    {
        public string FieldName { get; set; }
        public Type Converter { get; set; }
        public bool IsReadOnly { get; set; }

        public AzDOFieldAttribute(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentNullException("Missing field name.", nameof(fieldName));
            }

            FieldName = fieldName;
        }
    }
}
