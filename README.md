# PostSharp.Dnx
`PostSharp.Dnx` is a temporary connector to use PostSharp with ASP.NET 5.

The connector is provided as an open-source project during until ASP.NET 5 will be RTMed. Then, we plan to merge the feature into the mainstream `PostSharp` NuGet package.

## Using PostSharp with an ASP.NET 5 project

For each project that requires PostSharp, you need to do this manually:

1. Add a dependency to `PostSharp.Dnx`. Note that we don't publish any public NuGet package for this project yet.

2. Edit `project.json` and add a `compiler` section so that your file looks like this:

```
{
  "dependencies": {
    "PostSharp.Dnx": "1.0.0-*"
  },

  "compiler": {
    "name": "PostSharp",
    "compilerAssembly": "PostSharp.Dnx",
    "compilerType" :  "PostSharp.Dnx.PostSharpProjectCompiler"
  },
  
  "frameworks": {
    "dnx451": {
      "frameworkAssemblies": {
        "System.Runtime": "",
      }
    }
  }
}
```

## Limitations

1. It works only with .NET Framework. CoreCLR is not yet supported.
2. The code has been tested against DNX v1.0.0-rc1-update1. You may need to modify the source code to build it for a different version of DNX.
