using System;
using PostSharp.Serialization;

namespace HelloClassLibrary
{
    [PSerializable]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ApplyFiltersAttribute : FilterAttribute
    {
        // TODO: You may want to consider a design when Filter does not apply the filter on the current instance but clones the object and filters the clone.

        public override object ApplyFilter(object value)
        {
            if (value == null)
            {
                return null;
            }

            GetFilterable(value).ApplyFilter();

         
            return value;

        }


        private static IFilterable GetFilterable(object value)
        {
            IFilterable filterable = value as IFilterable;

            if (filterable == null)
            {
                throw new InvalidOperationException($"The type {value.GetType().FullName} is not IFilterable.");
            }
            return filterable;
        }

    }
}