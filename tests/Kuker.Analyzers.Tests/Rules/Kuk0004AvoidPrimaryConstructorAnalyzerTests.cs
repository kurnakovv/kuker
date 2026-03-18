// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.Analyzers.Tests.Rules;

public class Kuk0004AvoidPrimaryConstructorAnalyzerTests
{
    [Fact]
    public async Task ReportOnPrimaryConstructorInClassAsync()
    {
        string testCode = """
            // KUK0004: Avoid primary constructor for 'BadUserModel'.
            public class BadUserModel(string name, int age)
            {
                public string SayHello()
                {
                    return $"Hello, I'm {name} and {age} y.o.";
                }
            }

            // OK
            public class GoodUserModel
            {
                private readonly string _name;
                private readonly int _age;

                public GoodUserModel(
                    string name,
                    int age
                )
                {
                    _name = name;
                    _age = age;
                }

                public string SayHello()
                {
                    return $"Hello, I'm {_name} and {_age} y.o.";
                }
            }
""";

        DiagnosticResult expected = new DiagnosticResult("KUK0004", DiagnosticSeverity.Warning)
            .WithSpan(2, 26, 2, 38);

        await new CSharpAnalyzerTest<Kuk0004AvoidPrimaryConstructorAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected, },
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportOnPrimaryConstructorInStructAsync()
    {
        string testCode = """
            public struct BadStruct(int foo, double bar);

            public struct GoodUserModel
            {
                private readonly int _foo;
                private readonly double _bar;

                public GoodUserModel(
                    int foo,
                    double bar
                )
                {
                    _foo = foo;
                    _bar = bar;
                }
            }
""";

        DiagnosticResult expected = new DiagnosticResult("KUK0004", DiagnosticSeverity.Warning)
            .WithSpan(1, 27, 1, 36);

        await new CSharpAnalyzerTest<Kuk0004AvoidPrimaryConstructorAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected, },
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportOnPrimaryConstructorInRecordsAsync()
    {
        string testCode = """
            public record TestRecord1(int foo, double bar);
            public record class TestRecord2(int foo, double bar);
            public record struct TestRecord3(int foo, double bar);
            public record class TestRecord4;
""";

        await new CSharpAnalyzerTest<Kuk0004AvoidPrimaryConstructorAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportOnEmptyItemsAsync()
    {
        string testCode = """
            public class TestItem1;
            public struct TestItem2;
""";

        await new CSharpAnalyzerTest<Kuk0004AvoidPrimaryConstructorAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }
}
