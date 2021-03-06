# PostSharp.Dnx
`PostSharp.Dnx` is a temporary connector to use PostSharp with ASP.NET Core.

The connector is provided as an open-source project during until ASP.NET Core will be RTMed. Then, we plan to merge the feature into the mainstream `PostSharp` NuGet package.

For more information regarding our plans to support .NET Core, see https://www.postsharp.net/support/roadmap.

## Using PostSharp with an .NET Core project

1. Add kbeaugrand public repository : 
https://www.myget.org/F/kbeaugrand-public/api/v3/index.json

3. For each project that requires PostSharp, install Postsharp.dnx : 
```
PM> Install-Package PostSharp.Dnx -Version 1.0.0-beta1
```

3. Add a compiler section so that your `project.json` file looks like this: 
```
{
  "dependencies": {
    "PostSharp.Dnx": "1.0.0-beta1"
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
2. The code has been tested against DNX v1.0.0-rc1-final. You may need to modify the source code to build it for a different version of DNX.
