
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PostSharp.Compiler.Client;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using Microsoft.Dnx.Compilation.CSharp;
using Microsoft.Dnx.Compilation;
using Microsoft.Dnx.Runtime;
using System.Threading;
using Microsoft.CodeAnalysis.Emit;

namespace PostSharp.Dnx
{
    /// <summary>
    /// A DNX compile module that post-processes the project using PostSharp.
    /// </summary>
    internal class PostSharpCompilerModule : ICompileModule
    {
    
        IServiceProvider _provider;
        LogAdapter _logAdapter;
        private string _inputDirectory;
        private string _outputDirectory;

        public PostSharpCompilerModule(IServiceProvider provider, string workingDirectory) 
        {
            _provider = provider;
            _logAdapter = new LogAdapter();
            BuildClient = new BuildClient(this._logAdapter);
            _inputDirectory = Path.Combine(workingDirectory, "pre");
            _outputDirectory = workingDirectory;

            Directory.CreateDirectory(_inputDirectory);

        }

        public BuildClient BuildClient { get; private set; }

        protected virtual void BeforePostCompile()
        {

        }

        public virtual void AfterCompile(AfterCompileContext context)
        {
            Console.WriteLine("*** Running PostSharp on {0}", context.Compilation.AssemblyName);

            try
            {
                if ( context.Diagnostics == null )
                {
                    context.Diagnostics = new List<Diagnostic>();
                }

                // Do not execute our module if the compilation failed.
                if (context.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    return;
                }

                Stopwatch stopwatch = Stopwatch.StartNew();

                // Copy dll and pdb streams to a temp directory.
              

                string inputAssemblyPath = Path.Combine(_inputDirectory, context.ProjectContext.Name + ".dll");
                string outputAssemblyPath = Path.Combine(_outputDirectory, context.ProjectContext.Name + ".dll");
                string inputSymbolPath;
                string outputSymbolPath;

                List<Task> tasks = new List<Task>();
                try
                {

                    tasks.Add(Task.Run(() =>
                    {

                        using (FileStream assemblyStream = File.Create(inputAssemblyPath))
                        {
                            context.AssemblyStream.Seek(0, SeekOrigin.Begin);
                            context.AssemblyStream.CopyTo(assemblyStream);
                        }
                    }));

                    if (context.SymbolStream != null)
                    {

                        inputSymbolPath = Path.Combine(_inputDirectory, context.ProjectContext.Name + ".pdb");
                        outputSymbolPath = Path.Combine(_outputDirectory, context.ProjectContext.Name + ".pdb");

                        tasks.Add(Task.Run(() =>
                        {

                            using (FileStream symbolStream = File.Create(inputSymbolPath))
                            {
                                context.SymbolStream.Seek(0, SeekOrigin.Begin);
                                context.SymbolStream.CopyTo(symbolStream);
                            }
                        }));
                    }
                    else
                    {
                        inputSymbolPath = null;
                        outputSymbolPath = null;
                    }


                    // Configure PostSharp build client.
                    this.BuildClient.InputAssembly = inputAssemblyPath;
                    this.BuildClient.Projects = new[] { "default" };
                    this.BuildClient.Properties["Configuration"] = context.ProjectContext.Configuration;
                    this.BuildClient.Properties["DnxProjectFullPath"] = context.ProjectContext.ProjectFilePath;
                    this.BuildClient.Properties["Platform"] = context.ProjectContext.TargetFramework.FullName;
                    this.BuildClient.Properties["Output"] = outputAssemblyPath;
                    this.BuildClient.Properties["ReferenceDirectory"] = context.ProjectContext.ProjectDirectory;
                    this.BuildClient.Properties["Language"] = "C#";
                    this.BuildClient.TargetPlatform = IntPtr.Size == 8 ? "4.0-x64" : "4.0-x86";
                    this.BuildClient.Host = HostKind.PipeServer;


                    string postsharpDllPath = null;

                    // Resolving dependencies.
                    StringBuilder referenceBuilder = new StringBuilder();
                    foreach (MetadataReference reference in context.Compilation.References)
                    {
                        if (referenceBuilder.Length > 0)
                        {
                            referenceBuilder.Append(";");
                        }

                        PortableExecutableReference portableExecutableReference;
                
                        if ( (portableExecutableReference = reference as PortableExecutableReference ) != null)
                        {

                            if (portableExecutableReference.FilePath != null)
                            {
                                // We have a reference to a file on disk.
                                referenceBuilder.Append(portableExecutableReference.FilePath);

                                if (string.Equals(Path.GetFileName(portableExecutableReference.FilePath), "PostSharp.dll", StringComparison.OrdinalIgnoreCase))
                                {
                                    postsharpDllPath = portableExecutableReference.FilePath;
                                }

                                continue;
                            }
                        }
                        

                        context.Diagnostics.Add(Diagnostic.Create(Diagnostics.UnsupportedReference, null, reference.Display, reference.GetType().Name));

                    }

                    // If we didn't find PostSharp.dll, we fail because we don't know where to find the compiler.
                    if (postsharpDllPath == null)
                    {
                        context.Diagnostics.Add(Diagnostic.Create(Diagnostics.CannotFindPostSharpDll, null));
                        return;
                    }

                    FileVersionInfo postsharpVersion = FileVersionInfo.GetVersionInfo(postsharpDllPath);
                    FileVersionInfo expectedVersion = FileVersionInfo.GetVersionInfo(typeof(BuildClient).Assembly.Location);
                    if (postsharpVersion.FileVersion !=  expectedVersion.FileVersion )
                    {
                        context.Diagnostics.Add(Diagnostic.Create(Diagnostics.PostSharpDllVersionMismatch, null, postsharpDllPath, postsharpVersion.FileVersion, expectedVersion.FileVersion));
                        return;
                    }

                    this.BuildClient.ArchiveFile = Path.GetFullPath( Path.Combine(Path.GetDirectoryName(postsharpDllPath), "..\\..\\tools\\PostSharp-Tools.exe") );
                    this.BuildClient.Properties["ResolvedReferences"] = referenceBuilder.ToString();

                    Task.WaitAll(tasks.ToArray());

                    
                    // Execute custom logic.
                    this.BeforePostCompile();

                    
                    if (!this.BuildClient.Execute(CancellationToken.None))
                        return;


                    using (Stream outputAssemblyStream = File.OpenRead(outputAssemblyPath))
                    {
                        context.AssemblyStream = new MemoryStream();
                        outputAssemblyStream.CopyTo(context.AssemblyStream);
                    }


                    if (outputSymbolPath != null)
                    {
                        context.SymbolStream = new MemoryStream();
                        using (Stream symbolAssemblyStream = File.OpenRead(outputSymbolPath))
                        {
                            context.SymbolStream = new MemoryStream();
                            symbolAssemblyStream.CopyTo(context.AssemblyStream);
                        }
                    }
                }
                finally
                {
                    
                    foreach (Diagnostic diagnostic in this._logAdapter.Diagnostics)
                    {
                        context.Diagnostics.Add(diagnostic);
                    }

                }
            }
            catch ( Exception e )
            {
                context.Diagnostics.Add(Diagnostic.Create(Diagnostics.UnhandledException, null, e.GetType().Name, e.ToString()));
            }

        }

        public void BeforeCompile(BeforeCompileContext context)
        {
            
        }


    }

    class LogAdapter : ILogger
    {

        public LogAdapter()
        {
            this.Diagnostics = new List<Diagnostic>();
        }

        public List<Diagnostic> Diagnostics { get; private set; }


        public void LogError(string format, params object[] args)
        {
            this.Diagnostics.Add(Diagnostic.Create(Dnx.Diagnostics.PipeClientError, null, string.Format(format, args)));
        }

        public void LogException(Exception e)
        {
            this.Diagnostics.Add(Diagnostic.Create(Dnx.Diagnostics.PipeClientError, null, e.ToString()));
        }

        public void LogMessage(ClientMessage message)
        {
            var severity = ToDiagnosticSeverity(message.Severity);

            Location location;
            if (message.LocationFile != null)
            {
                location = Location.Create(message.LocationFile, default(TextSpan), new LinePositionSpan(new LinePosition(message.LocationStartLine, message.LocationStartColumn), new LinePosition(message.LocationEndLine, message.LocationEndColumn)));
            }
            else
            {
                location = null;
            }

            string messageId = message.MessageId;
            if (string.IsNullOrWhiteSpace(messageId))
            {
                messageId = "PSXXXX";
            }

            int warningLevel = severity == DiagnosticSeverity.Error ? 0 : 1;

            Diagnostic diagnostic = Diagnostic.Create(messageId, "PostSharp", message.MessageText, severity, severity, true, warningLevel, location: location);

            this.Diagnostics.Add(diagnostic);


        }

        public void LogMessage(string format, params object[] args)
        {
            this.Diagnostics.Add(Diagnostic.Create(Dnx.Diagnostics.PipeClientInfo, null, string.Format(format, args)));
        }

        public void LogVerbose(string format, params object[] args)
        {

        }

        public void LogWarning(string format, params object[] args)
        {
            this.Diagnostics.Add(Diagnostic.Create(Dnx.Diagnostics.PipeClientWarning, null, string.Format(format, args)));
        }

        private static DiagnosticSeverity ToDiagnosticSeverity(ClientMessageSeverity severity)
        {
            switch (severity)
            {
                case ClientMessageSeverity.Error:
                case ClientMessageSeverity.Fatal:
                    return DiagnosticSeverity.Error;

                case ClientMessageSeverity.ImportantInfo:
                case ClientMessageSeverity.Info:
                    return DiagnosticSeverity.Info;

                case ClientMessageSeverity.Warning:
                    return DiagnosticSeverity.Warning;

                case ClientMessageSeverity.CommandLine:
                case ClientMessageSeverity.Verbose:
                default:
                    // May be Hidden too.
                    return DiagnosticSeverity.Info;

            }
        }
    }
}

