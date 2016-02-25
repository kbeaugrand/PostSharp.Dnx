using System;
using System.Collections.Generic;
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
        Dictionary<LocationInfo,FilterAttribute> filteredMembers = new Dictionary<LocationInfo, FilterAttribute>();
        [PNonSerialized]
        private bool frozen;
        public List<ILocationBinding> bindings;

        public void Filter()
        {
            foreach (ILocationBinding binding in bindings)
            {
                FilterAttribute filter = this.filteredMembers[binding.LocationInfo];
                binding.SetValue( this.Instance, filter.FilterValue( binding.GetValue(this.Instance)) );
            }
        }

        IEnumerable<AdviceInstance> IAdviceProvider.ProvideAdvices(object targetElement)
        {
            this.frozen = true;

            // Ask PostSharp to populate the 'bindings' field at runtime.
            FieldInfo importField = this.GetType().GetField(nameof(bindings));
            foreach (var filteredMember in filteredMembers)
            {
                yield return new ImportLocationAdviceInstance( importField, filteredMember.Key );
            }
        }

        internal void SetFilter(LocationInfo locationInfo, FilterAttribute filter)
        {
            if ( this.frozen )
                throw new InvalidOperationException();

            this.filteredMembers.Add(locationInfo, filter);
        }
    }
}