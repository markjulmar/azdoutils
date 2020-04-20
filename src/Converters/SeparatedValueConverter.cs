using System;
using System.Collections.Generic;
using System.Linq;

namespace Julmar.AzDOUtilities
{
    public class SemicolonSeparatedConverter : SeparatedValueConverter
    {
        public SemicolonSeparatedConverter() : base("; ") {}
    }

    public class CommaSeparatedConverter : SeparatedValueConverter
    {
        public CommaSeparatedConverter() : base(", ") {}
    }

    public class SeparatedValueConverter : IFieldConverter, IFieldComparer
    {
        private readonly string separator;

        protected SeparatedValueConverter(string separator)
        {
            this.separator = separator;
        }

        public bool Compare(object initialValue, object currentValue)
        {
            var initial = (List<string>) Convert(initialValue, typeof(List<string>));
            var current = ((IEnumerable<string>)currentValue)?.ToList() ?? new List<string>();

            if (initial.Count == current.Count)
            {
                if (initial.Count == 0)
                    return true;

                initial.Sort(); current.Sort(); // ensure same order.
                return initial.SequenceEqual(current);
            }

            return false;
        }

        public virtual object Convert(object value, Type toType)
        {
            if (toType != typeof(string[]) && toType != typeof(List<string>))
            {
                throw new ArgumentException($"Cannot convert {value.GetType().Name} to {toType.Name}", nameof(value));
            }

            string text = (value ?? "").ToString();
            string[] values = text.Split(separator.Trim(), StringSplitOptions.RemoveEmptyEntries);
            for (int n = 0; n < values.Length; n++)
                values[n] = values[n].Trim();

            return (toType == typeof(List<string>))
                ? (object) values.ToList() : values;
        }

        public virtual object ConvertBack(object value)
        {
            if (value == null) return string.Empty;

            if (value.GetType() != typeof(string[]) && value.GetType() != typeof(List<string>))
                throw new ArgumentException($"Cannot convert {value} to string", nameof(value));

            IEnumerable<string> values = (IEnumerable<string>)value;
            var result = string.Join(separator, values);
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
    }
}
