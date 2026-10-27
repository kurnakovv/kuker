// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Kuker.CodeFixes.CodeFixProviders;
using Kuker.Core.Contants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.CodeFixes.Tests.CodeFixProviders;

public class Kuk0009DeclarationHeaderSeparationCodeFixProviderTests
{
    [Fact]
    public async Task CodeFixAddsBlankLineBeforeAttributeHeaderAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }
                [Obsolete]
                public void Method1()
                {
                }
            }
            """;

        string fixedCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }

                [Obsolete]
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, fixedCode, 8, 5, 8, 15);
    }

    [Fact]
    public async Task CodeFixAddsBlankLineBeforeCommentHeaderAsync()
    {
        string testCode = """
            public class Test
            {
                public void Method0()
                {
                }
                // Method1 comment.
                public void Method1()
                {
                }
            }
            """;

        string fixedCode = """
            public class Test
            {
                public void Method0()
                {
                }

                // Method1 comment.
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, fixedCode, 6, 5, 6, 24);
    }

    [Fact]
    public async Task CodeFixAddsBlankLineBeforeXmlDocumentationHeaderAsync()
    {
        string testCode = """
            public class Test
            {
                public void Method0()
                {
                }
                /// <summary>
                /// Method1.
                /// </summary>
                public void Method1()
                {
                }
            }
            """;

        string fixedCode = """
            public class Test
            {
                public void Method0()
                {
                }

                /// <summary>
                /// Method1.
                /// </summary>
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, fixedCode, 6, 5, 8, 19);
    }

    [Fact]
    public async Task CodeFixAddsBlankLineAfterPreviousMemberMultilineTrailingCommentAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                int first; /* note
                continuation */
                [Obsolete]
                int second;
            }
            """;

        string fixedCode = """
            using System;

            public class Test
            {
                int first; /* note
                continuation */

                [Obsolete]
                int second;
            }
            """;

        await RunAsync(testCode, fixedCode, 7, 5, 7, 15);
    }

    [Fact]
    public async Task CodeFixAddsBlankLineAfterPreviousEnumMemberMultilineTrailingCommentAsync()
    {
        string testCode = """
            using System;

            public enum Test
            {
                Value0, /* note
                continuation */
                [Obsolete]
                Value1,
            }
            """;

        string fixedCode = """
            using System;

            public enum Test
            {
                Value0, /* note
                continuation */

                [Obsolete]
                Value1,
            }
            """;

        await RunAsync(testCode, fixedCode, 7, 5, 7, 15);
    }

    [Fact]
    public async Task CodeFixIsNotOfferedWhenHeaderStartsMidLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                int first; [Obsolete] int second;
            }
            """;

#pragma warning disable KUK0001 // Duplicate arguments passed to method
        await RunAsync(testCode, testCode, 5, 16, 5, 26);
#pragma warning restore KUK0001 // Duplicate arguments passed to method
    }

    [Fact]
    public async Task CodeFixIsNotOfferedWhenHeaderStartsMidLineForMethodAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0() { } [Obsolete] public void Method1() { }
            }
            """;

#pragma warning disable KUK0001 // Duplicate arguments passed to method
        await RunAsync(testCode, testCode, 5, 31, 5, 41);
#pragma warning restore KUK0001 // Duplicate arguments passed to method
    }

    private static async Task RunAsync(
        string testCode,
        string fixedCode,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn
    )
    {
        CSharpCodeFixTest<Kuk0009DeclarationHeaderSeparationAnalyzer, Kuk0009DeclarationHeaderSeparationCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0009, DiagnosticSeverity.Warning)
            .WithSpan(startLine, startColumn, endLine, endColumn);

        test.ExpectedDiagnostics.Add(expected);

        await test.RunAsync();
    }
}
