// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.Analyzers.Tests.Rules;

public class Kuk0002FileNameMismatchAnalyzerTests
{
    [Fact]
    public async Task IgnoreEmptyFileNameAsync()
    {
        string testCode = @"
            public class ClassName { }
        ";

        await new CSharpAnalyzerTest<Kuk0002FileNameMismatchAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { },
            TestState = { Sources = { (string.Empty, testCode), }, },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForInvalidClassWithoutNamespaceAsync()
    {
        string testCode = @"
            public class InvalidName { }
        ";

        DiagnosticResult expected = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyClass.cs", 2, 13, 3, 9)
            .WithArguments("InvalidName");

        await new CSharpAnalyzerTest<Kuk0002FileNameMismatchAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected },
            TestState = { Sources = { ("MyClass.cs", testCode), }, },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForInvalidClassWithBlockNamespaceAsync()
    {
        string testCode = @"
            namespace MyTestNamespace
            {
                public class InvalidName { }
            }
        ";

        DiagnosticResult expected = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyClass.cs", 2, 13, 6, 9)
            .WithArguments("InvalidName");

        await new CSharpAnalyzerTest<Kuk0002FileNameMismatchAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected },
            TestState = { Sources = { ("MyClass.cs", testCode), }, },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForInvalidClassWithFilescopedNamespaceAsync()
    {
        string testCode = @"
            namespace MyTestNamespace;

            public class InvalidName { }
        ";

        DiagnosticResult expected = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyClass.cs", 2, 13, 5, 9)
            .WithArguments("InvalidName");

        await new CSharpAnalyzerTest<Kuk0002FileNameMismatchAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected },
            TestState = { Sources = { ("MyClass.cs", testCode), }, },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForInvalidClassesAsync()
    {
        string testCode = @"
            namespace MyTestNamespace
            {
                public class InvalidName1 { }
                public class InvalidName2 { }
                public class InvalidName3 { }
            }
        ";

        DiagnosticResult expected = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyClass.cs", 2, 13, 8, 9)
            .WithArguments("InvalidName1, InvalidName2, InvalidName3");

        await new CSharpAnalyzerTest<Kuk0002FileNameMismatchAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected },
            TestState = { Sources = { ("MyClass.cs", testCode), }, },
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForCorrectClassesAsync()
    {
        string testCode = @"
            namespace MyTestNamespace
            {
                public class SomeClass { }
                public class MyClass { }
                public class MyClassDto { }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0002FileNameMismatchAnalyzer, DefaultVerifier>
        {
            TestState = { Sources = { ("MyClass.cs", testCode), }, },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForInvalidTypesAsync()
    {
        string invalidClassTestCode = "public class InvalidClass { }";
        string invalidInterfaceTestCode = "public interface IInvalidInterface { }";
        string invalidEnumTestCode = "public enum InvalidEnum { }";
        string invalidStructTestCode = "public struct InvalidStruct { }";
        string invalidRecordTestCode = "public record InvalidRecord { }";
        string invalidRecordClassTestCode = "public record class InvalidRecordClass { }";
        string invalidRecordStructTestCode = "public record struct InvalidRecordStruct { }";

        string allTypesTestCode = $@"
            namespace MyTestNamespace;

            {invalidClassTestCode}
            {invalidInterfaceTestCode}
            {invalidEnumTestCode}
            {invalidStructTestCode}
            {invalidRecordTestCode}
            {invalidRecordClassTestCode}
            {invalidRecordStructTestCode}
        ";

        DiagnosticResult expectedClass = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyClass.cs", 1, 1, 1, 30)
            .WithArguments("InvalidClass");

        DiagnosticResult expectedInterface = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("IMyInterface.cs", 1, 1, 1, 39)
            .WithArguments("IInvalidInterface");

        DiagnosticResult expectedEnum = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyEnum.cs", 1, 1, 1, 28)
            .WithArguments("InvalidEnum");

        DiagnosticResult expectedStruct = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyStruct.cs", 1, 1, 1, 32)
            .WithArguments("InvalidStruct");

        DiagnosticResult expectedRecord = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyRecord.cs", 1, 1, 1, 32)
            .WithArguments("InvalidRecord");

        DiagnosticResult expectedRecordClass = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyRecordClass.cs", 1, 1, 1, 43)
            .WithArguments("InvalidRecordClass");

        DiagnosticResult expectedRecordStruct = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyRecordStruct.cs", 1, 1, 1, 45)
            .WithArguments("InvalidRecordStruct");

        DiagnosticResult expectedAllTypes = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("AllTypes.cs", 2, 13, 11, 9)
            .WithArguments("InvalidClass, IInvalidInterface, InvalidEnum, InvalidStruct, InvalidRecord, InvalidRecordClass, InvalidRecordStruct");

        await new CSharpAnalyzerTest<Kuk0002FileNameMismatchAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics =
            {
                expectedClass,
                expectedInterface,
                expectedEnum,
                expectedStruct,
                expectedRecord,
                expectedRecordClass,
                expectedRecordStruct,
                expectedAllTypes,
            },
            TestState =
            {
                Sources =
                {
                    ("MyClass.cs", invalidClassTestCode),
                    ("IMyInterface.cs", invalidInterfaceTestCode),
                    ("MyEnum.cs", invalidEnumTestCode),
                    ("MyStruct.cs", invalidStructTestCode),
                    ("MyRecord.cs", invalidRecordTestCode),
                    ("MyRecordClass.cs", invalidRecordClassTestCode),
                    ("MyRecordStruct.cs", invalidRecordStructTestCode),
                    ("AllTypes.cs", allTypesTestCode),
                },
            },
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForCorrectTypesAsync()
    {
        string correctClassTestCode = "public class MyClass { }";
        string correctInterfaceTestCode = "public interface IMyInterface { }";
        string correctEnumTestCode = "public enum MyEnum { }";
        string correctStructTestCode = "public struct MyStruct { }";
        string correctRecordTestCode = "public record MyRecord { }";
        string correctRecordClassTestCode = "public record class MyRecordClass { }";
        string correctRecordStructTestCode = "public record struct MyRecordStruct { }";

        string allTypesTestCode = $@"
            namespace MyTestNamespace;

            {correctClassTestCode}
            {correctInterfaceTestCode}
            {correctEnumTestCode}
            {correctStructTestCode}
            {correctRecordTestCode}
            {correctRecordClassTestCode}
            {correctRecordStructTestCode}
        ";

        await new CSharpAnalyzerTest<Kuk0002FileNameMismatchAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources =
                {
                    ("MyClass.cs", correctClassTestCode),
                    ("IMyInterface.cs", correctInterfaceTestCode),
                    ("MyEnum.cs", correctEnumTestCode),
                    ("MyStruct.cs", correctStructTestCode),
                    ("MyRecord.cs", correctRecordTestCode),
                    ("MyRecordClass.cs", correctRecordClassTestCode),
                    ("MyRecordStruct.cs", correctRecordStructTestCode),
                    ("MyClass.cs", allTypesTestCode),
                },
            },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForInvalidInternalClassAsync()
    {
        string invalidInternalTestCode = "class InvalidInternal { }";
        string invalidInternalWithKeywordTestCode = "internal class InvalidInternalWithKeyword { }";

        string allTypesTestCode = $@"
            namespace MyTestNamespace;

            {invalidInternalTestCode}
            {invalidInternalWithKeywordTestCode}
        ";

        DiagnosticResult expectedInternal = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyInternalClass.cs", 1, 1, 1, 26)
            .WithArguments("InvalidInternal");

        DiagnosticResult expectedInternalWithKeyword = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyInternalClassWithKeyword.cs", 1, 1, 1, 46)
            .WithArguments("InvalidInternalWithKeyword");

        DiagnosticResult expectedAllTypes = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("AllTypes.cs", 2, 13, 6, 9)
            .WithArguments("InvalidInternal, InvalidInternalWithKeyword");

        await new CSharpAnalyzerTest<Kuk0002FileNameMismatchAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics =
            {
                expectedInternal,
                expectedInternalWithKeyword,
                expectedAllTypes,
            },
            TestState =
            {
                Sources =
                {
                    ("MyInternalClass.cs", invalidInternalTestCode),
                    ("MyInternalClassWithKeyword.cs", invalidInternalWithKeywordTestCode),
                    ("AllTypes.cs", allTypesTestCode),
                },
            },
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForPartialClassAsync()
    {
        string testCode = @"
            namespace MyTestNamespace
            {
                public partial class User { }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0002FileNameMismatchAnalyzer, DefaultVerifier>
        {
            TestState = { Sources = { ("UserAdd.cs", testCode), }, },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportNestedClassAsync()
    {
        string testCode = @"
            namespace MyTestNamespace
            {
                public class InvalidName
                {
                    private class MyClass { }
                }
            }
        ";

        DiagnosticResult expected = new DiagnosticResult("KUK0002", DiagnosticSeverity.Warning)
            .WithSpan("MyClass.cs", 2, 13, 9, 9)
            .WithArguments("InvalidName");

        await new CSharpAnalyzerTest<Kuk0002FileNameMismatchAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected },
            TestState = { Sources = { ("MyClass.cs", testCode), }, },
        }.RunAsync();
    }
}
