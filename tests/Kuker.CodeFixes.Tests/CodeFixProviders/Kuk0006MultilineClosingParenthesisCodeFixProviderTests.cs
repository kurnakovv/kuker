// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Kuker.CodeFixes.CodeFixProviders;
using Kuker.Core.Contants;
using Kuker.Core.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.CodeFixes.Tests.CodeFixProviders;

public class Kuk0006MultilineClosingParenthesisCodeFixProviderTests
{
#pragma warning disable RCS0053, SA1117 // Parameter should not span multiple lines
    [Theory]
    [InlineData(
        "CodeFixMovesClosingParenthesisToOwnLineAsync",
        """
        var result = Foo(
            1,
            2{|#0:)|};
        return 1;
        """,
        """
        var result = Foo(
            1,
            2
        );
        return 1;
        """
    )]
    [InlineData(
        "CodeFixAlignsClosingParenthesisAsync",
        """
        var result = Foo(
            1,
            2
          {|#0:)|};
        return 1;
        """,
        """
        var result = Foo(
            1,
            2
        );
        return 1;
        """
    )]
    [InlineData(
        "CodeFixPreservesTrailingCommentAfterSemicolonAsync",
        """
        var result = Foo(
            1,
            2{|#0:)|}; // Keep this comment
        return 1;
        """,
        """
        var result = Foo(
            1,
            2
        ); // Keep this comment
        return 1;
        """
    )]
    [InlineData(
        "CodeFixPreservesTrailingMemberAccessAfterClosingParenthesisAsync",
        """
        var result = Foo(
            1,
            2{|#0:)|}.ToString();
        return result;
        """,
        """
        var result = Foo(
            1,
            2
        ).ToString();
        return result;
        """
    )]
    [InlineData(
        "CodeFixMovesObjectCreationClosingParenthesisToOwnLineAsync",
        """
        var item = new Item(
            1,
            "One"{|#0:)|};
        return item.Id;
        """,
        """
        var item = new Item(
            1,
            "One"
        );
        return item.Id;
        """
    )]
    [InlineData(
        "CodeFixMovesImplicitObjectCreationClosingParenthesisToOwnLineAsync",
        """
        Item item = new(
            1,
            "One"{|#0:)|};
        return item.Id;
        """,
        """
        Item item = new(
            1,
            "One"
        );
        return item.Id;
        """
    )]
    [InlineData(
        "CodeFixAlignsObjectCreationClosingParenthesisAsync",
        """
        var item = new Item(
            1,
            "One"
          {|#0:)|};
        return item.Id;
        """,
        """
        var item = new Item(
            1,
            "One"
        );
        return item.Id;
        """
    )]
    [InlineData(
        "CodeFixPreservesTrailingCommentAfterObjectCreationSemicolonAsync",
        """
        var item = new Item(
            1,
            "One"{|#0:)|}; // Keep this comment
        return item.Id;
        """,
        """
        var item = new Item(
            1,
            "One"
        ); // Keep this comment
        return item.Id;
        """
    )]
#pragma warning restore RCS0053, SA1117 // Parameter should not span multiple lines
    public async Task CodeFixAppliesExpectedChangeAsync(string name, string testCode, string fixedCode)
    {
        _ = name;

        string wrappedTestCode = WrapCode(testCode);
        string wrappedFixedCode = WrapCode(fixedCode);

        CSharpCodeFixTest<Kuk0006MultilineClosingParenthesisAnalyzer, Kuk0006MultilineClosingParenthesisCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = wrappedTestCode,
            FixedCode = wrappedFixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFixFixAllInDocumentAppliesToMethodInvocationAndObjectCreationAsync()
    {
        string testCode = WrapCode(
            """
            var fromInvocation = Foo(
                1,
                2{|#0:)|};

            var fromCreation = new Item(
                1,
                "One"{|#1:)|};

            Item fromImplicitCreation = new(
                2,
                "Two"{|#2:)|};

            return fromInvocation + fromCreation.Id + fromImplicitCreation.Id;
            """
        );

        string fixedCode = WrapCode(
            """
            var fromInvocation = Foo(
                1,
                2
            );

            var fromCreation = new Item(
                1,
                "One"
            );

            Item fromImplicitCreation = new(
                2,
                "Two"
            );

            return fromInvocation + fromCreation.Id + fromImplicitCreation.Id;
            """
        );

        CSharpCodeFixTest<Kuk0006MultilineClosingParenthesisAnalyzer, Kuk0006MultilineClosingParenthesisCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            NumberOfFixAllIterations = 1,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(1));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(2));

        await test.RunAsync();
    }

    [Theory]
    [InlineData("foobar")]
    [InlineData("INVALID")]
    [InlineData(",")]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_INVOCATION},")]
    [InlineData($",{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_INVOCATION},,{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_DECLARATION},")]
    [InlineData($",{Kuk0006TargetSyntaxOption.METHOD_DECLARATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_DECLARATION},,{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION},")]
    [InlineData($",{Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION},,{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
    public async Task CodeFixDoesNotApplyWhenTargetSyntaxOptionIsInvalidAsync(string invalidTargetSyntax)
    {
        string testCode = WrapCode(
            """
            var result = Foo(
                1,
                2{|#0:)|};
            return 1;
            """
        );

        CSharpCodeFixTest<Kuk0006MultilineClosingParenthesisAnalyzer, Kuk0006MultilineClosingParenthesisCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.AnalyzerConfigFiles.Add((
            "/.editorconfig",
            $"""
            root = true

            [*.cs]
            {Kuk0006TargetSyntaxOption.KEY} = {invalidTargetSyntax}
            """
        ));

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0).WithArguments(invalidTargetSyntax));

        await test.RunAsync();
    }

#pragma warning disable RCS0053, SA1117 // Parameter should not span multiple lines
    [Theory]
    [InlineData(
        "CodeFixMovesMethodDeclarationClosingParenthesisToOwnLineAsync",
        """
        public void Foo(
            int a,
            int b{|#0:)|}
        {
        }
        """,
        """
        public void Foo(
            int a,
            int b
        )
        {
        }
        """
    )]
    [InlineData(
        "CodeFixAlignsMethodDeclarationClosingParenthesisAsync",
        """
        public void Foo(
            int a,
            int b
          {|#0:)|}
        {
        }
        """,
        """
        public void Foo(
            int a,
            int b
        )
        {
        }
        """
    )]
    [InlineData(
        "CodeFixMovesLocalFunctionClosingParenthesisToOwnLineAsync",
        """
        public void M()
        {
            void Local(
                int a,
                int b{|#0:)|}
            {
            }
        }
        """,
        """
        public void M()
        {
            void Local(
                int a,
                int b
            )
            {
            }
        }
        """
    )]
#pragma warning restore RCS0053, SA1117 // Parameter should not span multiple lines
    public async Task CodeFixAppliesExpectedChangeToMethodDeclarationAsync(string name, string testCode, string fixedCode)
    {
        _ = name;

        string wrappedTestCode = WrapMethodDeclarationCode(testCode);
        string wrappedFixedCode = WrapMethodDeclarationCode(fixedCode);

        CSharpCodeFixTest<Kuk0006MultilineClosingParenthesisAnalyzer, Kuk0006MultilineClosingParenthesisCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = wrappedTestCode,
            FixedCode = wrappedFixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.AnalyzerConfigFiles.Add((
            "/.editorconfig",
            $$"""
            root = true

            [*.cs]
            {{Kuk0006TargetSyntaxOption.KEY}} = {{Kuk0006TargetSyntaxOption.METHOD_DECLARATION}}
            """
        ));

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFixFixAllInDocumentAppliesToMethodDeclarationAsync()
    {
        string testCode = WrapMethodDeclarationCode(
            """
            public void Foo(
                int a,
                int b{|#0:)|}
            {
            }

            public void Bar(
                string x,
                string y{|#1:)|}
            {
            }
            """
        );

        string fixedCode = WrapMethodDeclarationCode(
            """
            public void Foo(
                int a,
                int b
            )
            {
            }

            public void Bar(
                string x,
                string y
            )
            {
            }
            """
        );

        CSharpCodeFixTest<Kuk0006MultilineClosingParenthesisAnalyzer, Kuk0006MultilineClosingParenthesisCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            NumberOfFixAllIterations = 1,
        };

        test.TestState.AnalyzerConfigFiles.Add((
            "/.editorconfig",
            $$"""
            root = true

            [*.cs]
            {{Kuk0006TargetSyntaxOption.KEY}} = {{Kuk0006TargetSyntaxOption.METHOD_DECLARATION}}
            """
        ));

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(1));

        await test.RunAsync();
    }

#pragma warning disable RCS0053, SA1117 // Parameter should not span multiple lines
    [Theory]
    [InlineData(
        "CodeFixMovesConstructorDeclarationClosingParenthesisToOwnLineAsync",
        """
        public TestClass(
            int a,
            int b{|#0:)|}
        {
        }
        """,
        """
        public TestClass(
            int a,
            int b
        )
        {
        }
        """
    )]
    [InlineData(
        "CodeFixAlignsConstructorDeclarationClosingParenthesisAsync",
        """
        public TestClass(
            int a,
            int b
          {|#0:)|}
        {
        }
        """,
        """
        public TestClass(
            int a,
            int b
        )
        {
        }
        """
    )]
#pragma warning restore RCS0053, SA1117 // Parameter should not span multiple lines
    public async Task CodeFixAppliesExpectedChangeToConstructorDeclarationAsync(string name, string testCode, string fixedCode)
    {
        _ = name;

        string wrappedTestCode = WrapMethodDeclarationCode(testCode);
        string wrappedFixedCode = WrapMethodDeclarationCode(fixedCode);

        CSharpCodeFixTest<Kuk0006MultilineClosingParenthesisAnalyzer, Kuk0006MultilineClosingParenthesisCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = wrappedTestCode,
            FixedCode = wrappedFixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.AnalyzerConfigFiles.Add((
            "/.editorconfig",
            $$"""
            root = true

            [*.cs]
            {{Kuk0006TargetSyntaxOption.KEY}} = {{Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION}}
            """
        ));

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFixFixAllInDocumentAppliesToConstructorDeclarationAsync()
    {
        string testCode = WrapMethodDeclarationCode(
            """
            public TestClass(
                int a,
                int b{|#0:)|}
            {
            }

            public TestClass(
                string x,
                string y{|#1:)|}
            {
            }
            """
        );

        string fixedCode = WrapMethodDeclarationCode(
            """
            public TestClass(
                int a,
                int b
            )
            {
            }

            public TestClass(
                string x,
                string y
            )
            {
            }
            """
        );

        CSharpCodeFixTest<Kuk0006MultilineClosingParenthesisAnalyzer, Kuk0006MultilineClosingParenthesisCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            NumberOfFixAllIterations = 1,
        };

        test.TestState.AnalyzerConfigFiles.Add((
            "/.editorconfig",
            $$"""
            root = true

            [*.cs]
            {{Kuk0006TargetSyntaxOption.KEY}} = {{Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION}}
            """
        ));

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(1));

        await test.RunAsync();
    }

    private static string WrapMethodDeclarationCode(string code)
    {
        string normalizedCode = code.ReplaceLineEndings(Environment.NewLine).Trim('\r', '\n');
        string indentedCode = normalizedCode.Replace(Environment.NewLine, $"{Environment.NewLine}    ", StringComparison.Ordinal);

        string template = """
            using System;

            public class TestClass
            {
                __CODE__
            }
            """;

        return template.Replace("__CODE__", indentedCode, StringComparison.Ordinal);
    }

    private static string WrapCode(string code)
    {
        string normalizedCode = code.ReplaceLineEndings(Environment.NewLine).Trim('\r', '\n');
        string indentedCode = normalizedCode.Replace(Environment.NewLine, $"{Environment.NewLine}        ", StringComparison.Ordinal);

        string template = """
            using System;
            using System.Linq;

            public class TestClass
            {
                public object M1()
                {
                    __CODE__
                }

                private static int Foo(params int[] args)
                {
                    return args.Sum();
                }

                private sealed class Item
                {
                    public Item(int id, string name)
                    {
                        Id = id;
                        Name = name;
                    }

                    public int Id { get; }
                    public string Name { get; }
                }
            }
            """;

        return template.Replace("__CODE__", indentedCode, StringComparison.Ordinal);
    }
}
