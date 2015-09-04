using PostSharp.Aspects;
using PostSharp.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HelloClassLibrary
{
    [PSerializable]
    public sealed class MyAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs args)
        {
            Console.WriteLine("Congratulations! PostSharp enhanced the project {0}.", args.Method.DeclaringType.Assembly);
        }
    }
}
