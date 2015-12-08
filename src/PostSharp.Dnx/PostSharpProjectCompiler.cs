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
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;

namespace PostSharp.Dnx
{
    public class PostSharpProjectCompiler : IProjectCompiler
    {
        private readonly RoslynCompiler _compiler;
        private readonly IServiceProvider _services;
        private readonly ICache _cache;
        private readonly string _workingDirectory;

        public PostSharpProjectCompiler(
            ICache cache,
            ICacheContextAccessor cacheContextAccessor,
            INamedCacheDependencyProvider namedCacheProvider,
            IAssemblyLoadContext loadContext,
            IApplicationEnvironment environment,
            IServiceProvider services)
        {
            _services = services;
            _compiler = new RoslynCompiler(
                cache,
                cacheContextAccessor,
                namedCacheProvider,
                loadContext,
                environment,
                services);
            _cache = cache;
            _workingDirectory = cache.Get<string>("PostSharp.Dnx.WorkingDirectory", cacheContext => Path.Combine(Path.GetTempPath(), "PostSharp.Dnx", Guid.NewGuid().ToString()));
            Task.Run(() => PurgeWorkingDirectories());
            CreateWorkingDirectory();
        }

        private void PurgeWorkingDirectories()
        {
            foreach ( string directory in Directory.GetDirectories(Path.Combine(Path.GetTempPath(), "PostSharp.Dnx") ))
            {
                string pidFile = Path.Combine(directory, ".pid");
                if (!File.Exists(pidFile))
                    continue;

                string pidString = File.ReadAllText(pidFile);
                int pid;
                if (!int.TryParse(pidString, out pid))
                    continue;
                try
                {
                    Process.GetProcessById(pid);
                }
                catch ( ArgumentException )
                {
                    Console.WriteLine("*** Removing directory {0}", directory);
                    try
                    {
                        Directory.Delete(directory, true);
                    }
                    catch ( IOException )
                    {

                    }
                }
            }
        }

        private void CreateWorkingDirectory()
        {
            if (!Directory.Exists(_workingDirectory))
            {
                Directory.CreateDirectory(_workingDirectory);
                File.WriteAllText(Path.Combine(_workingDirectory, ".pid"), Process.GetCurrentProcess().Id.ToString());
                AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
                {
                    Console.WriteLine("*** Removing directory {0}", _workingDirectory);
                    Directory.Delete(_workingDirectory);
                };
            }
        }

        public IMetadataProjectReference CompileProject(
            CompilationProjectContext projectContext,
            Func<LibraryExport> referenceResolver,
            Func<IList<ResourceDescriptor>> resourcesResolver)
        {
            List<DiagnosticResult> diagnosticResults = new List<DiagnosticResult>();

            var module = new PostSharpCompilerModule(_services, _workingDirectory);

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
                    string referencePath = Path.Combine(_workingDirectory, projectReference.Name + ".dll");
                    if (!File.Exists(referencePath))
                    {
                        DiagnosticResult diagnostics = projectReference.EmitAssembly(_workingDirectory);
                        diagnosticResults.Add(diagnostics);
                    }

                    processedIncomingReferences.Add(new MetadataFileReference(projectReference.Name, referencePath));

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
            return new PostSharpProjectReference( new RoslynProjectReference(compilationContext), diagnosticResults, _workingDirectory );
        }

      
    }

    public class PostSharpProjectReference : IRoslynMetadataReference, IMetadataProjectReference
    {
        RoslynProjectReference _underlyingReference;
        List<DiagnosticResult> _diagnosticResults;
        string _workingDirectory;

        public PostSharpProjectReference(RoslynProjectReference underlyingReference, List<DiagnosticResult> diagnosticResults, string workingDirectory)
        {
            _diagnosticResults = diagnosticResults;
            _underlyingReference = underlyingReference;
            _workingDirectory = workingDirectory;
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

            string referencePath = Path.Combine(_workingDirectory, assemblyName.Name + ".dll");
            if ( File.Exists( referencePath ))
            {
                return loadContext.LoadFile(referencePath);
            }

            return _underlyingReference.Load(assemblyName, loadContext);
        }

    
    }
}
