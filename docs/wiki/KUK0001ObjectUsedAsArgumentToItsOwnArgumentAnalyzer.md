# KUK0001

## Title
Object Used As Argument To Its Own Argument Analyzer

## Description
A violation of this rule occurs when the first and other arguments are equal.
Sometimes you duplicate parameters with same type, especially if you copy-paste parameters

## Configuration
```.editorconfig
[*.cs]
dotnet_diagnostic.KUK0001.severity = warning # Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer
dotnet_diagnostic.KUK0001.excluded_methods = Foo,Bar # Optional | Ignore selected methods
```

## Code
```cs
async void TestMethod(CancellationToken token)
{
    var stream = new MemoryStream();
    var other = new MemoryStream();
    await stream.CopyToAsync(stream, token); // Violation, because the first and second arguments are equal
    await stream.CopyToAsync(other, token); // OK
}
```

## Links
* Issues: [#8](https://github.com/kurnakovv/kuker/issues/8)
* [Source code](https://github.com/kurnakovv/kuker/blob/main/src/Kuker.Analyzers/Rules/Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer.cs)
* More examples in [tests](https://github.com/kurnakovv/kuker/blob/main/tests/Kuker.Analyzers.Tests/Rules/Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzerTests.cs)
* [Severity levels](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options#severity-level)