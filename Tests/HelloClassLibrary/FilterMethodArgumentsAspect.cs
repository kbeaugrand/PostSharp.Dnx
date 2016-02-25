using PostSharp.Aspects;
using PostSharp.Serialization;
using System.Reflection;
using System.Threading.Tasks;
using PostSharp.Extensibility;

namespace HelloClassLibrary
{
    [PSerializable]
    public class FilterMethodArgumentsAspect : MethodInterceptionAspect
    {
        private FilterAttribute[] filters;

        internal FilterMethodArgumentsAspect(MethodBase method)
        {
            this.filters = new FilterAttribute[method.GetParameters().Length];
        }


        internal void SetFilter(ParameterInfo parameter, FilterAttribute filter)
        {
            if ( this.filters[parameter.Position] != null )
            {
                // If you want to support more than 1 filter, you will need a more complex data structure and to cope with priorities.
                Message.Write(parameter, SeverityType.Error, "MY01", "There cannot be more than 1 filter on parameter {0}.", parameter);
                return;
            }

            this.filters[parameter.Position] = filter;

        }

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            for (int i = 0; i < this.filters.Length; i++)
            {
                FilterAttribute filter = this.filters[i];
                if (filter != null)
                {
                    args.Arguments[i] = filter.ApplyFilter(args.Arguments[i]);
                }
            }

            args.Proceed();
        }
    }
}
