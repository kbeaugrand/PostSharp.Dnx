using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostSharp.Dnx
{
    internal static class Diagnostics
    {
        const string category = "PostSharp";
        public static DiagnosticDescriptor FileNotFound = new DiagnosticDescriptor("PSDNX01", "Tool does not exist.", "File {0} does not exist.", category, DiagnosticSeverity.Error, true);
        public static DiagnosticDescriptor StartingTool = new DiagnosticDescriptor("PSDNX02", "Invoking tool", "Invoking tool: \"{0} {1}\".", category, DiagnosticSeverity.Info, true);
        public static DiagnosticDescriptor InvalidReturnCode = new DiagnosticDescriptor("PSDNX03", "Tool failed", "Tool: \"{0}\" failed with exit code {1}.", category, DiagnosticSeverity.Error, true);
        public static DiagnosticDescriptor UnsupportedReference = new DiagnosticDescriptor("PSDNX04", "Unsupported reference type", "Unsupported reference type: {0} ({1})", category, DiagnosticSeverity.Warning, true);
        public static DiagnosticDescriptor PipeClientError = new DiagnosticDescriptor("PSDNX05", "Pipe client error", "{0}", category, DiagnosticSeverity.Error, true);
        public static DiagnosticDescriptor PipeClientWarning = new DiagnosticDescriptor("PSDNX06", "Pipe client warning", "{0}", category, DiagnosticSeverity.Warning, true);
        public static DiagnosticDescriptor PipeClientInfo = new DiagnosticDescriptor("PSDNX07", "Pipe client message", "{0}", category, DiagnosticSeverity.Info, true);
        public static DiagnosticDescriptor PipeClientVerbose = new DiagnosticDescriptor("PSDNX08", "Pipe client verbose message", "{0}", category, DiagnosticSeverity.Hidden, true);
        public static DiagnosticDescriptor CannotBuildDependency = new DiagnosticDescriptor("PSDNX09", "Cannot build dependency", "Cannot build the dependency {0}.", category, DiagnosticSeverity.Error, true);
        public static DiagnosticDescriptor UnhandledException = new DiagnosticDescriptor("PSDNX10", "Unhandled exception", "Unhandled {0} in PostSharp.Dnx:" + Environment.NewLine + "{1}", category, DiagnosticSeverity.Error, true);
        public static DiagnosticDescriptor CannotFindPostSharpDll = new DiagnosticDescriptor("PSDNX11", "Cannot find PostSharp.dll", "Cannot find PostSharp.dll in project references.", category, DiagnosticSeverity.Error, true);
        public static DiagnosticDescriptor PostSharpDllVersionMismatch = new DiagnosticDescriptor("PSDNX12", "Unexpected version of PostSharp package", "Version mismatch for {0}. Got {1}, expected {2}.", category, DiagnosticSeverity.Error, true);
    }
}
