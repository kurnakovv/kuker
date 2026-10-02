// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Kuker.Core.Contants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.Analyzers.Tests.Rules;

public class Kuk0009DeclarationHeaderSeparationAnalyzerTests
{
    [Fact]
    public async Task ReportWhenThirdMemberHasNoBlankLineRegardlessOfPreviousMembersHeadersAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public void Method0()
                {
                }

                [Obsolete]
                public void Method1()
                {
                }
                [Obsolete]
                public void Method2()
                {
                }
            }
            """;

        await RunAsync(testCode, 14, 5, 14, 15);
    }

    [Fact]
    public async Task NoReportWhenAllMembersHaveBlankLineBeforeTheirHeaderAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public void Method0()
                {
                }

                [Obsolete]
                public void Method1()
                {
                }

                [Obsolete]
                public void Method2()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenBlankLineIsBetweenHeaderAndDeclarationInsteadOfBeforeHeaderAsync()
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

        await RunAsync(testCode, 8, 5, 8, 15);
    }

    [Fact]
    public async Task NoReportWhenMultipleBlankLinesAreBeforeHeaderAsync()
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

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenFieldsHaveNoHeaderAsync()
    {
        string testCode = """
            public class Test
            {
                private int _value0;
                private int _value1;
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenPropertiesHaveNoHeaderAsync()
    {
        string testCode = """
            public class Test
            {
                public int Value0 { get; set; }
                public int Value1 { get; set; }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenMethodsHaveNoHeaderAsync()
    {
        string testCode = """
            public class Test
            {
                public void Method0()
                {
                }
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenNestedTypesHaveNoHeaderAsync()
    {
        string testCode = """
            public class Test
            {
                public class Nested0
                {
                }
                public class Nested1
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenEnumMembersHaveNoHeaderAsync()
    {
        string testCode = """
            public enum Test
            {
                Value0 = 0,
                Value1 = 1,
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenCommentIsBeforeFirstFieldAsync()
    {
        string testCode = """
            public class Test
            {
                // Comment0.
                private int _value0;
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenCommentIsBeforeSecondDeclarationWithoutBlankLineAsync()
    {
        string testCode = """
            public class Test
            {
                // Comment0.
                private int _value0;
                // Comment1.
                private int _value1;
            }
            """;

        await RunAsync(testCode, 5, 5, 5, 17);
    }

    [Fact]
    public async Task NoReportWhenCommentIsBeforeSecondDeclarationWithBlankLineAsync()
    {
        string testCode = """
            public class Test
            {
                // Comment0.
                private int _value0;

                // Comment1.
                private int _value1;
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAttributeIsBeforeFirstNestedTypeAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public class Nested0
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenAttributeIsBeforeSecondNestedTypeWithoutBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public class Nested0
                {
                }
                [Obsolete]
                public class Nested1
                {
                }
            }
            """;

        await RunAsync(testCode, 9, 5, 9, 15);
    }

    [Fact]
    public async Task NoReportWhenAttributeIsBeforeSecondNestedTypeWithBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public class Nested0
                {
                }

                [Obsolete]
                public class Nested1
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenAttributeStackIsBeforeSecondNestedTypeWithoutBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public class Nested0
                {
                }
                [Obsolete]
                [Serializable]
                public class Nested1
                {
                }
            }
            """;

        await RunAsync(testCode, 9, 5, 9, 15);
    }

    [Fact]
    public async Task NoReportWhenAttributeStackIsBeforeSecondNestedTypeWithBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public class Nested0
                {
                }

                [Obsolete]
                [Serializable]
                public class Nested1
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAttributeIsBeforeFirstEnumMemberAsync()
    {
        string testCode = """
            using System;

            public enum Test
            {
                [Obsolete]
                Value0 = 0,
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenXmlDocAndAttributeAreBeforeSecondMethodWithoutBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }
                /// <summary>
                /// Method1.
                /// </summary>
                [Obsolete]
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, 8, 5, 10, 19);
    }

    [Fact]
    public async Task NoReportWhenXmlDocAndAttributeAreBeforeSecondMethodWithBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }

                /// <summary>
                /// Method1.
                /// </summary>
                [Obsolete]
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenAttributeIsBeforeSecondEnumMemberWithoutBlankLineAsync()
    {
        string testCode = """
            using System;

            public enum Test
            {
                [Obsolete]
                Value0 = 0,
                [Obsolete]
                Value1 = 1,
            }
            """;

        await RunAsync(testCode, 7, 5, 7, 15);
    }

    [Fact]
    public async Task NoReportWhenAttributeIsBeforeSecondEnumMemberWithBlankLineAsync()
    {
        string testCode = """
            using System;

            public enum Test
            {
                [Obsolete]
                Value0 = 0,

                [Obsolete]
                Value1 = 1,
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenXmlDocIsBeforeFirstMethodAsync()
    {
        string testCode = """
            public class Test
            {
                /// <summary>
                /// Method0.
                /// </summary>
                public void Method0()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenXmlDocIsBeforeSecondMethodWithoutBlankLineAsync()
    {
        string testCode = """
            public class Test
            {
                /// <summary>
                /// Method0.
                /// </summary>
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

        await RunAsync(testCode, 9, 5, 11, 19);
    }

    [Fact]
    public async Task NoReportWhenXmlDocIsBeforeSecondMethodWithBlankLineAsync()
    {
        string testCode = """
            public class Test
            {
                /// <summary>
                /// Method0.
                /// </summary>
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

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAttributeIsBeforeFirstMethodAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public void Method0()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenAttributeIsBeforeSecondMethodWithoutBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public void Method0()
                {
                }
                [Obsolete]
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, 9, 5, 9, 15);
    }

    [Fact]
    public async Task NoReportWhenAttributeIsBeforeSecondMethodWithBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public void Method0()
                {
                }

                [Obsolete]
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAttributeIsBeforeFirstPropertyAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public int Value0 { get; set; }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenAttributeIsBeforeSecondPropertyWithoutBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public int Value0 { get; set; }
                [Obsolete]
                public int Value1 { get; set; }
            }
            """;

        await RunAsync(testCode, 7, 5, 7, 15);
    }

    [Fact]
    public async Task NoReportWhenAttributeIsBeforeSecondPropertyWithBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public int Value0 { get; set; }

                [Obsolete]
                public int Value1 { get; set; }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAttributeIsBeforeFirstFieldAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                private int _value0;
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenAttributeIsBeforeSecondFieldWithoutBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                private int _value0;
                [Obsolete]
                private int _value1;
            }
            """;

        await RunAsync(testCode, 7, 5, 7, 15);
    }

    [Fact]
    public async Task NoReportWhenAttributeIsBeforeSecondFieldWithBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                private int _value0;

                [Obsolete]
                private int _value1;
            }
            """;

        await RunAsync(testCode);
    }

    private static async Task RunAsync(
        string testCode,
        int startLine = 0,
        int startColumn = 0,
        int endLine = 0,
        int endColumn = 0
    )
    {
        CSharpAnalyzerTest<Kuk0009DeclarationHeaderSeparationAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        if (startLine > 0)
        {
            DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0009, DiagnosticSeverity.Warning)
                .WithSpan(startLine, startColumn, endLine, endColumn);

            test.ExpectedDiagnostics.Add(expected);
        }

        await test.RunAsync();
    }
}
