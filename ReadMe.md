<div align="center">
 <img src="docs/images/kuker-icon.png" weight="250px" height="250" />
 <h1>kuker</h1>
 
 ![Visitors](https://api.visitorbadge.io/api/VisitorHit?user=kurnakovv&repo=kuker&countColor=%237B1E7A&style=flat)
 [![NuGet](https://img.shields.io/nuget/v/Kurnakov.kuker.svg)](https://www.nuget.org/packages/Kurnakov.kuker)
 [![NuGet download](https://img.shields.io/nuget/dt/Kurnakov.kuker.svg)](https://www.nuget.org/packages/Kurnakov.kuker) 
 [![Build and tests](https://github.com/kurnakovv/kuker/actions/workflows/build-and-tests.yml/badge.svg)](https://github.com/kurnakovv/kuker/actions/workflows/build-and-tests.yml)
 [![.NET format](https://github.com/kurnakovv/kuker/actions/workflows/dotnet-format.yml/badge.svg)](https://github.com/kurnakovv/kuker/actions/workflows/dotnet-format.yml)
 [![InspectCode](https://github.com/kurnakovv/kuker/actions/workflows/inspect-code.yml/badge.svg)](https://github.com/kurnakovv/kuker/actions/workflows/inspect-code.yml)
 [![PVS studio](https://custom-icon-badges.demolab.com/badge/pvs-studio-blue.svg?logo=codescan-checkmark&logoColor=white)](https://pvs-studio.com/en/pvs-studio/?utm_source=website&utm_medium=github&utm_campaign=open_source)
 [![CodeQL](https://github.com/kurnakovv/kuker/actions/workflows/codeql.yml/badge.svg)](https://github.com/kurnakovv/kuker/actions/workflows/codeql.yml)
 [![Codecov](https://codecov.io/gh/kurnakovv/kuker/branch/main/graph/badge.svg)](https://codecov.io/gh/kurnakovv/kuker)
 [![MIT License](https://img.shields.io/github/license/kurnakovv/kuker?color=%230b0&style=flat)](https://github.com/kurnakovv/kuker/blob/main/LICENSE)

</div>

## 📙 Description
👨‍🍳 <b>kuker</b> is a C# analyzer that helps you ‍🍳 "kuk" ‍🔍 tasty and clean code 😸

This analyzer based on [Roslyn API](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/) | [Tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)

## 🚀 Quick start
ToDo

## ⚙️ Configuration
You can configure all rules via `.editorconfig` file
```.editorconfig
[*.cs]

##
## kuker
##
# Docs here https://github.com/kurnakovv/kuker

# Setup all rules with global category
dotnet_diagnostic.KukerAllRules.severity = warning

dotnet_diagnostic.KUK0001.severity = warning # Object used as argument to its own argument analyzer | https://github.com/kurnakovv/kuker/wiki/KUK0001
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
