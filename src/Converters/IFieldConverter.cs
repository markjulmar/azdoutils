using System;

namespace AzDOUtilities
{
    public interface IFieldComparer
    {
        bool Compare(object initialValue, object currentValue);
    }

    public interface IFieldConverter
    {
        object Convert(object value, Type toType);
        object ConvertBack(object value);
    }

    public abstract class BaseFieldConverter<T,TR> : IFieldConverter
    {
        object IFieldConverter.Convert(object value, Type toType)
        {
            if (toType != typeof(TR))
                throw new ArgumentException($"{GetType().Name} type mismatch: {toType.Name} != {typeof(TR).Name}.");

            return this.Convert((T)value);
        }

        object IFieldConverter.ConvertBack(object value)
        {
            return this.ConvertBack((TR)value);
        }


        public abstract TR Convert(T value);
        public abstract object ConvertBack(TR value);
    }
}
