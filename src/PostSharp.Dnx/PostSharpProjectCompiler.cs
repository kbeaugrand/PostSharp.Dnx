using System;
using System.Collections.Generic;
using Microsoft.Dnx.Compilation.Caching;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Compilation;
using Microsoft.Dnx.Compilation.CSharp;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Linq;

namespace PostSharp.Dnx
{
    public class PostSharpProjectCompiler : IProjectCompiler
    {
        private readonly RoslynCompiler _compiler;
        private readonly IServiceProvider _services;

        public PostSharpProjectCompiler(
            ICache cache,
            ICacheContextAccessor cacheContextAccessor,
            INamedCacheDependencyProvider namedCacheProvider,
            IAssemblyLoadContext loadContext,
            IFileWatcher watcher,
            IApplicationEnvironment environment,
            IServiceProvider services)
        {
            _services = services;
            _compiler = new RoslynCompiler(
                cache,
                cacheContextAccessor,
                namedCacheProvider,
                loadContext,
                watcher,
                environment,
                services);
        }

        public IMetadataProjectReference CompileProject(
            CompilationProjectContext projectContext,
            Func<LibraryExport> referenceResolver,
            Func<IList<ResourceDescriptor>> resourcesResolver)
        {
            List<DiagnosticResult> diagnosticResults = new List<DiagnosticResult>();

            var module = new PostSharpCompilerModule(_services);

            var export = referenceResolver();
            if (export == null)
            {
                return null;
            }

            var incomingReferences = export.MetadataReferences;
            var incomingSourceReferences = export.SourceReferences;

            var processedIncomingReferences = new List<IMetadataReference>(incomingReferences.Count);

            foreach ( var reference in incomingReferences )
            {
                
                var projectReference = reference as IMetadataProjectReference;
                if ( projectReference != null )
                {
                    // If we have a PostSharpProjectReference, we have to compile it using EmitAssembly and replace the reference by a MetadataFileReference.
                    DiagnosticResult diagnostics = projectReference.EmitAssembly(module.OutputDirectory);
                    processedIncomingReferences.Add(new MetadataFileReference(projectReference.Name, module.OutputDirectory + "\\" + projectReference.Name + ".dll"));
                    diagnosticResults.Add(diagnostics);
                }
                else
                {
                    processedIncomingReferences.Add(reference);
                }

                
            }

            var compilationContext = _compiler.CompileProject(
                projectContext,
                processedIncomingReferences,
                incomingSourceReferences,
                resourcesResolver);

            if (compilationContext == null)
            {
                return null;
            }

            compilationContext.Modules.Add(module);

            // Project reference
            return new PostSharpProjectReference( new RoslynProjectReference(compilationContext), diagnosticResults );
        }

      
    }

    public class PostSharpProjectReference : IRoslynMetadataReference, IMetadataProjectReference
    {
        RoslynProjectReference _underlyingReference;
        List<DiagnosticResult> _diagnosticResults;

        public PostSharpProjectReference(RoslynProjectReference underlyingReference, List<DiagnosticResult> diagnosticResults)
        {
            _diagnosticResults = diagnosticResults;
            _underlyingReference = underlyingReference;
        }

        MetadataReference IRoslynMetadataReference.MetadataReference
        {
            get
            {
                return _underlyingReference.MetadataReference;
            }
        }

        public string Name
        {
            get
            {
                return _underlyingReference.Name;
            }
        }

        string IMetadataProjectReference.ProjectPath
        {
            get
            {
                return _underlyingReference.ProjectPath;
            }
        }

        public DiagnosticResult EmitAssembly(string outputPath)
        {
            Console.WriteLine("*** PostSharpProjectReference.EmitAssembly {1}\\{0}.dll", _underlyingReference.Name, outputPath);

            return _underlyingReference.EmitAssembly(outputPath);
        }

        void IMetadataProjectReference.EmitReferenceAssembly(Stream stream)
        {
            throw new NotImplementedException();
        }

        DiagnosticResult IMetadataProjectReference.GetDiagnostics()
        {
            _diagnosticResults.Add( _underlyingReference.GetDiagnostics() );
            return new DiagnosticResult(_diagnosticResults.TrueForAll(d => d.Success), _diagnosticResults.SelectMany(d => d.Diagnostics));
        }

        IList<ISourceReference> IMetadataProjectReference.GetSources()
        {
            return _underlyingReference.GetSources();
        }

  
        Assembly IMetadataProjectReference.Load(AssemblyName assemblyName, IAssemblyLoadContext loadContext)
        {
            Console.WriteLine("*** PostSharpProjectReference.Load {0}.dll", _underlyingReference.Name);

            return _underlyingReference.Load(assemblyName, loadContext);
        }
    }
}
