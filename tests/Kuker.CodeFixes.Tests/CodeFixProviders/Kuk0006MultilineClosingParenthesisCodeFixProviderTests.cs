// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Kuker.Core.Contants;
using Kuker.CodeFixes.CodeFixProviders;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.CodeFixes.Tests.CodeFixProviders;

public class Kuk0006MultilineClosingParenthesisCodeFixProviderTests
{
    [Fact]
    public async Task CodeFixMovesClosingParenthesisToOwnLineAsync()
    {
        string testCode = """
            using System;
            using System.Linq;

            public class TestClass
            {
                public object M1()
                {
                    var result = Foo(
                        1,
                        2{|#0:)|};
                    return 1;
                }

                private static int Foo(params int[] args)
                {
                    return args.Sum();
                }
            }
            """;

        string fixedCode = """
            using System;
            using System.Linq;

            public class TestClass
            {
                public object M1()
                {
                    var result = Foo(
                        1,
                        2
                    );
                    return 1;
                }

                private static int Foo(params int[] args)
                {
                    return args.Sum();
                }
            }
            """;

        CSharpCodeFixTest<Kuk0006MultilineClosingParenthesisAnalyzer, Kuk0006MultilineClosingParenthesisCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFixAlignsClosingParenthesisAsync()
    {
        string testCode = """
            using System;
            using System.Linq;

            public class TestClass
            {
                public object M1()
                {
                    var result = Foo(
                        1,
                        2
                      {|#0:)|};
                    return 1;
                }

                private static int Foo(params int[] args)
                {
                    return args.Sum();
                }
            }
            """;

        string fixedCode = """
            using System;
            using System.Linq;

            public class TestClass
            {
                public object M1()
                {
                    var result = Foo(
                        1,
                        2
                    );
                    return 1;
                }

                private static int Foo(params int[] args)
                {
                    return args.Sum();
                }
            }
            """;

        CSharpCodeFixTest<Kuk0006MultilineClosingParenthesisAnalyzer, Kuk0006MultilineClosingParenthesisCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }
}
