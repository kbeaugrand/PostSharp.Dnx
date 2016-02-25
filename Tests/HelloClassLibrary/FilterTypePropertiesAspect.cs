using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Reflection;
using PostSharp.Serialization;

namespace HelloClassLibrary
{
    [PSerializable]
    [IntroduceInterface(typeof(IFilterable))]
    public class FilterTypePropertiesAspect : InstanceLevelAspect, IFilterable, IAdviceProvider
    {
        [PNonSerialized]
        private bool frozen;

        Dictionary<LocationInfo,FilterAttribute> filteredMembers = new Dictionary<LocationInfo, FilterAttribute>();
        public List<ILocationBinding> bindings;

        public void ApplyFilter()
        {
            foreach (ILocationBinding binding in bindings)
            {
                FilterAttribute filter = this.filteredMembers[binding.LocationInfo];
                binding.SetValue( this.Instance, filter.ApplyFilter( binding.GetValue(this.Instance)) );
            }
        }

        IEnumerable<AdviceInstance> IAdviceProvider.ProvideAdvices(object targetElement)
        {
            this.frozen = true;

            // Ask PostSharp to populate the 'bindings' field at runtime.
            FieldInfo importField = this.GetType().GetField(nameof(bindings));

            return filteredMembers.Select(filteredMember => new ImportLocationAdviceInstance( importField, filteredMember.Key ));
        }

        internal void SetFilter(LocationInfo locationInfo, FilterAttribute filter)
        {
            if ( this.frozen )
                throw new InvalidOperationException();

            this.filteredMembers.Add(locationInfo, filter);
        }
    }
}