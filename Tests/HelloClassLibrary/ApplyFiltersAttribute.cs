using System;
using PostSharp.Serialization;

namespace HelloClassLibrary
{
    [PSerializable]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ApplyFiltersAttribute : FilterAttribute
    {
        public override object FilterValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            IFilterable filterable = value as IFilterable;

            if (filterable == null)
            {
                throw new InvalidOperationException(string.Format("The type {0} is not IFilterable.", value.GetType().FullName));
            }

            filterable.Filter();

            // TODO: You may want to consider a design when Filter does not apply the filter on the current instance but clones the object and filters the clone.

            return value;

        }
    }
}