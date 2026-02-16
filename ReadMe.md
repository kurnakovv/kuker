<div align="center">
 <img src="docs/images/kuker-icon.png" weight="250px" height="250" />
 <h1>kuker</h1>
 
 ![Visitors](https://api.visitorbadge.io/api/VisitorHit?user=kurnakovv&repo=kuker&countColor=%237B1E7A&style=flat)
 [![NuGet](https://img.shields.io/nuget/v/kurnakovv.kuker.svg)](https://www.nuget.org/packages/kurnakovv.kuker)
 [![NuGet download](https://img.shields.io/nuget/dt/kurnakovv.kuker.svg)](https://www.nuget.org/packages/kurnakovv.kuker)
 [![NuGet](https://img.shields.io/nuget/v/kurnakovv.kuker-preview.svg)](https://www.nuget.org/packages/kurnakovv.kuker-preview)
 [![NuGet download](https://img.shields.io/nuget/dt/kurnakovv.kuker-preview.svg)](https://www.nuget.org/packages/kurnakovv.kuker-preview)
 [![Build and tests](https://github.com/kurnakovv/kuker/actions/workflows/build-and-tests.yml/badge.svg)](https://github.com/kurnakovv/kuker/actions/workflows/build-and-tests.yml)
 [![.NET format](https://github.com/kurnakovv/kuker/actions/workflows/dotnet-format.yml/badge.svg)](https://github.com/kurnakovv/kuker/actions/workflows/dotnet-format.yml)
 [![InspectCode](https://github.com/kurnakovv/kuker/actions/workflows/inspect-code.yml/badge.svg)](https://github.com/kurnakovv/kuker/actions/workflows/inspect-code.yml)
 [![PVS studio](https://custom-icon-badges.demolab.com/badge/pvs-studio-blue.svg?logo=codescan-checkmark&logoColor=white)](https://pvs-studio.com/en/pvs-studio/?utm_source=website&utm_medium=github&utm_campaign=open_source)
 [![CodeQL](https://github.com/kurnakovv/kuker/actions/workflows/codeql.yml/badge.svg)](https://github.com/kurnakovv/kuker/actions/workflows/codeql.yml)
 [![Codecov](https://codecov.io/gh/kurnakovv/kuker/branch/dev/graph/badge.svg)](https://app.codecov.io/gh/kurnakovv/kuker)
 [![MIT License](https://img.shields.io/github/license/kurnakovv/kuker?color=%230b0&style=flat)](https://github.com/kurnakovv/kuker/blob/dev/LICENSE)

</div>

![line](docs/images/line.gif)


## 📙 Description
👨‍🍳 <b>kuker</b> is a C# analyzer that helps you ‍🍳 "kuk" ‍🔍 tasty and clean code 😸

This analyzer based on [Roslyn API](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/) | [Tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)

## 🚀 Quick start
1️⃣ Install kuker via NuGet:
```
dotnet add package kurnakovv.kuker
```
This command adds the following reference to your project:
```xml
<!-- Use the latest available version -->
<PackageReference Include="kurnakovv.kuker" Version="0.2.1">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

> [!NOTE]
> It is recommended to define this dependency in `Directory.Build.props` ([what is it?](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory?view=visualstudio))

2️⃣ Build the project:
```
dotnet build
```
You may see output similar to the following:

```bash
PS D:\C#\JustApplication> dotnet build
Restore complete (0.7s)
  JustApplication net8 succeeded with 7 warning(s) (3.5s) → JustApplication\bin\Debug\net8\JustApplication.dll
    D:\C#\JustApplication\JustApplication\Program.cs(2414,21): warning CS0219: The variable 'afdf' is assigned but its value is never used
    D:\C#\JustApplication\JustApplication\MyTest\MyTest.cs(1,1): warning KUK0002: The file name should match the name of one of the public or internal types: 'UserDto, TestClass, Point' (https://github.com/kurnakovv/kuker/wiki/KUK0002)
    D:\C#\JustApplication\JustApplication\Program.cs(2388,44): warning KUK0001: Argument 'a.MyTest.Ids' is passed multiple times to the same method call (https://github.com/kurnakovv/kuker/wiki/KUK0001)
    D:\C#\JustApplication\JustApplication\Program.cs(2390,48): warning KUK0001: Argument 'a.MyTest?.Ids' is passed multiple times to the same method call (https://github.com/kurnakovv/kuker/wiki/KUK0001)
    D:\C#\JustApplication\JustApplication\MyTest\MyTest.cs(20,41): warning KUK0003: 'Max' on a sequence of non-nullable value types may throw InvalidOperationException if the sequence is empty. Use a nullable selector or DefaultIfEmpty(). (https://github.com/kurnakovv/kuker/wiki/KUK0003)
    D:\C#\JustApplication\JustApplication\MyTest\MyTest.cs(23,38): warning KUK0003: 'MaxAsync' on a sequence of non-nullable value types may throw InvalidOperationException if the sequence is empty. Use a nullable selector or DefaultIfEmpty(). (https://github.com/kurnakovv/kuker/wiki/KUK0003)
    D:\C#\JustApplication\JustApplication\Program.cs(2393,34): warning KUK0001: Argument '(Test)a' is passed multiple times to the same method call (https://github.com/kurnakovv/kuker/wiki/KUK0001)

Build succeeded with 7 warning(s) in 4.6s
```

That's it - kuker is now up and running. Happy coding! 🚀


## ⚙️ Configuration
You can configure all rules via `.editorconfig` file
```.editorconfig
[*.cs]

##
## kuker
##
# Docs here https://github.com/kurnakovv/kuker

# Setup all rules with global category
dotnet_analyzer_diagnostic.category-KukerAllRules.severity = warning

dotnet_diagnostic.KUK0001.severity = warning # Duplicate arguments passed to method | https://github.com/kurnakovv/kuker/wiki/KUK0001
dotnet_diagnostic.KUK0002.severity = warning # File name mismatch | https://github.com/kurnakovv/kuker/wiki/KUK0002
dotnet_diagnostic.KUK0003.severity = warning # Min/Max (Async) and MinBy/MaxBy may throw InvalidOperationException on empty sequences | https://github.com/kurnakovv/kuker/wiki/KUK0003

dotnet_diagnostic.KUK0001.excluded_methods = Foo,Bar # Optional | Ignore selected methods

# ...
```

For more information about the configuration, please visit our [wiki](https://github.com/kurnakovv/kuker/wiki)

## ❔ Reason
The .NET ecosystem offers many powerful analyzers, including built-in ones like [CAxxxx](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/categories?view=visualstudio) and [IDExxxx](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/?view=visualstudio), as well as well-known third-party analyzers based on the [Roslyn API](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/), such as [Roslynator](https://josefpihrt.github.io/docs/roslynator/) and [StyleCop](https://github.com/DotNetAnalyzers/StyleCopAnalyzers).

Despite their wide coverage, certain rules remain uncovered. 👨‍🍳 <b>kuker</b> analyzer is designed to address these gaps by providing additional, highly focused analysis rules.

## ⭐ Give a star
I hope this analyzer is useful for you, if so, please give this repository a star, thank you :)

## SAST Tools

[PVS-Studio](https://pvs-studio.com/en/pvs-studio/?utm_source=website&utm_medium=github&utm_campaign=open_source) - static analyzer for C, C++, C#, and Java code.
