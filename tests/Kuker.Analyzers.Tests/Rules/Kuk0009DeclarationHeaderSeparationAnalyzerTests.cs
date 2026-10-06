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
    public async Task NoReportWhenFieldWithMultipleDeclaratorsHasNoHeaderAsync()
    {
        string testCode = """
            public class Test
            {
                private int _value0, _value1;
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenFieldWithMultipleDeclaratorsIsFirstDeclarationWithHeaderAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                private int _value0, _value1;
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenFieldWithMultipleDeclaratorsIsSecondDeclarationWithoutBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                private int _value0;
                [Obsolete]
                private int _value1, _value2;
            }
            """;

        await RunAsync(testCode, 7, 5, 7, 15);
    }

    [Fact]
    public async Task NoReportWhenFieldWithMultipleDeclaratorsIsSecondDeclarationWithBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                private int _value0;

                [Obsolete]
                private int _value1, _value2;
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

        await RunAsync(testCode, 9, 5, 10, 19);
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

    [Fact]
    public async Task ReportWhenBlockCommentIsBeforeSecondDeclarationWithoutBlankLineAsync()
    {
        string testCode = """
            public class Test
            {
                public void Method0()
                {
                }
                /* Comment1. */
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, 6, 5, 6, 20);
    }

    [Fact]
    public async Task NoReportWhenBlockCommentIsBeforeSecondDeclarationWithBlankLineAsync()
    {
        string testCode = """
            public class Test
            {
                public void Method0()
                {
                }

                /* Comment1. */
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenMultilineBlockCommentIsBeforeSecondDeclarationWithoutBlankLineAsync()
    {
        string testCode = """
            public class Test
            {
                public void Method0()
                {
                }
                /* Comment1.
                Continuation. */
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, 6, 5, 7, 21);
    }

    [Fact]
    public async Task NoReportWhenMultilineBlockCommentIsBeforeSecondDeclarationWithBlankLineAsync()
    {
        string testCode = """
            public class Test
            {
                public void Method0()
                {
                }

                /* Comment1.
                Continuation. */
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenCommentAndAttributeAreBeforeSecondDeclarationWithoutBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }
                // Comment1.
                [Obsolete]
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, 8, 5, 8, 17);
    }

    [Fact]
    public async Task NoReportWhenCommentAndAttributeAreBeforeSecondDeclarationWithBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }

                // Comment1.
                [Obsolete]
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenMultipleCommentsInARowAreBeforeSecondDeclarationWithoutBlankLineAsync()
    {
        string testCode = """
            public class Test
            {
                public void Method0()
                {
                }
                // Comment1.
                // Comment2.
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, 6, 5, 7, 17);
    }

    [Fact]
    public async Task NoReportWhenMultipleCommentsInARowAreBeforeSecondDeclarationWithBlankLineAsync()
    {
        string testCode = """
            public class Test
            {
                public void Method0()
                {
                }

                // Comment1.
                // Comment2.
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenCommentIsBetweenAttributesWithoutBlankLineBeforeStackAsync()
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
                // Comment1.
                [Serializable]
                public class Nested1
                {
                }
            }
            """;

        await RunAsync(testCode, 9, 5, 11, 19);
    }

    [Fact]
    public async Task NoReportWhenCommentIsBetweenAttributesWithBlankLineBeforeStackAsync()
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
                // Comment1.
                [Serializable]
                public class Nested1
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAssemblyAttributeIsBeforeFirstDeclarationAsync()
    {
        string testCode = """
            using System.Reflection;

            [assembly: AssemblyTitle("Test")]

            public class Test
            {
                public void Method0()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAssemblyAttributeIsBeforeSecondDeclarationWithoutBlankLineAsync()
    {
        string testCode = """
            using System.Reflection;

            [assembly: AssemblyTitle("Test")]
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
    public async Task NoReportWhenMethodTargetedAttributeIsBeforeFirstDeclarationAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [method: Obsolete]
                public void Method0()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenMethodTargetedAttributeIsBeforeSecondDeclarationWithoutBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }
                [method: Obsolete]
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, 8, 5, 8, 23);
    }

    [Fact]
    public async Task NoReportWhenMethodTargetedAttributeIsBeforeSecondDeclarationWithBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }

                [method: Obsolete]
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenReturnTargetedAttributeDoesNotActAsHeaderForNextDeclarationAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [method: Obsolete]
                public int Method0()
                {
                    return 0;
                }
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenMixedDeclarationKindsAllHaveBlankLineBeforeTheirHeaderAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public int Value0 { get; set; }

                [Obsolete]
                private int _value1;

                // Comment
                public void Method()
                {
                }

                /// <summary>
                /// Nested type.
                /// </summary>
                public class Nested
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenHeaderlessDeclarationIsFollowedByHeaderedDeclarationWithoutBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public int Value { get; set; }
                [Obsolete]
                private int _field;
            }
            """;

        await RunAsync(testCode, 6, 5, 6, 15);
    }

    [Fact]
    public async Task NoReportWhenHeaderlessDeclarationIsFollowedByHeaderedDeclarationWithBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public int Value { get; set; }

                [Obsolete]
                private int _field;
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenMixedDeclarationKindsAlternateWithAndWithoutHeadersAndLastHasNoBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public void Method()
                {
                }

                public int Value { get; set; }

                [Obsolete]
                private int _field;
                [Obsolete]
                public class Nested
                {
                }
            }
            """;

        await RunAsync(testCode, 14, 5, 14, 15);
    }

    [Fact]
    public async Task NoReportWhenMixedDeclarationKindsAlternateWithAndWithoutHeadersAndEachHasBlankLineWhereNeededAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public void Method()
                {
                }

                public int Value { get; set; }

                [Obsolete]
                private int _field;

                public class Nested
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenPreviousDeclarationIsMultilineAndHeaderHasNoBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method(
                    int value)
                {
                }
                [Obsolete]
                public void Method2()
                {
                }
            }
            """;

        await RunAsync(testCode, 9, 5, 9, 15);
    }

    [Fact]
    public async Task NoReportWhenPreviousDeclarationIsMultilineAndHeaderHasBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method(
                    int value)
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
    public async Task ReportWhenBlankLineIsBetweenStackedAttributesWithoutBlankLineBeforeFirstAttributeAsync()
    {
        string testCode = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method0()
                {
                }
                [Obsolete]

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, 9, 5, 11, 47);
    }

    [Fact]
    public async Task NoReportWhenBlankLineIsBetweenStackedAttributesAndBlankLineIsBeforeFirstAttributeAsync()
    {
        string testCode = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method0()
                {
                }

                [Obsolete]

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenBlankLineIsBetweenXmlDocAndAttributeWithoutBlankLineBeforeXmlDocAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }
                /// <summary>
                /// Test.
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
    public async Task NoReportWhenBlankLineIsBetweenXmlDocAndAttributeAndBlankLineIsBeforeXmlDocAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }

                /// <summary>
                /// Test.
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
    public async Task NoReportWhenCommentSeparatedByBlankLineFromAttributeIsNotPartOfHeaderAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }
                // Comment1.

                [Obsolete]
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenAttributeInsidePreprocessorDirectiveHasNoBlankLineBeforeDirectiveAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }
            #if DEBUG
                [Obsolete]
            #endif
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, 8, 1, 10, 7);
    }

    [Fact]
    public async Task NoReportWhenAttributeInsidePreprocessorDirectiveHasBlankLineBeforeDirectiveAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }

            #if DEBUG
                [Obsolete]
            #endif
                public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenFirstDeclarationInsideConditionalDirectiveHasHeaderDirectlyAfterDirectiveAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
            #if true
                [Obsolete]
                public void Method0()
                {
                }
            #endif
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenSecondDeclarationInsideConditionalDirectiveHasNoBlankLineBeforeHeaderAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
            #if true
                [Obsolete]
                public void Method0()
                {
                }
                [Obsolete]
                public void Method1()
                {
                }
            #endif
            }
            """;

        await RunAsync(testCode, 10, 5, 10, 15);
    }

    [Fact]
    public async Task NoReportWhenSecondDeclarationInsideConditionalDirectiveHasBlankLineBeforeHeaderAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
            #if true
                [Obsolete]
                public void Method0()
                {
                }

                [Obsolete]
                public void Method1()
                {
                }
            #endif
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenEnumMembersAlternateWithAndWithoutHeadersAndEachHasBlankLineWhereNeededAsync()
    {
        string testCode = """
            using System;

            public enum Test
            {
                Value0 = 0,

                [Obsolete]
                Value1 = 1,

                Value2 = 2,

                [Obsolete]
                Value3 = 3,
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenEnumMemberHeaderHasNoBlankLineRegardlessOfPreviousMemberHavingNoHeaderAsync()
    {
        string testCode = """
            using System;

            public enum Test
            {
                [Obsolete]
                Value0 = 0,

                Value1 = 1,
                [Obsolete]
                Value2 = 2,
            }
            """;

        await RunAsync(testCode, 9, 5, 9, 15);
    }

    [Fact]
    public async Task ReportWhenSecondRecordPropertyHeaderHasNoBlankLineAsync()
    {
        string testCode = """
            using System;

            public record Test
            {
                [Obsolete]
                public int Value0 { get; init; }
                [Obsolete]
                public int Value1 { get; init; }
            }
            """;

        await RunAsync(testCode, 7, 5, 7, 15);
    }

    [Fact]
    public async Task NoReportWhenSecondRecordPropertyHeaderHasBlankLineAsync()
    {
        string testCode = """
            using System;

            public record Test
            {
                [Obsolete]
                public int Value0 { get; init; }

                [Obsolete]
                public int Value1 { get; init; }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenSecondConstructorHeaderHasNoBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public Test()
                {
                }
                [Obsolete]
                public Test(int value)
                {
                }
            }
            """;

        await RunAsync(testCode, 9, 5, 9, 15);
    }

    [Fact]
    public async Task NoReportWhenSecondConstructorHeaderHasBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public Test()
                {
                }

                [Obsolete]
                public Test(int value)
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenSecondOperatorHeaderHasNoBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public static Test operator +(Test a, Test b) => a;
                [Obsolete]
                public static Test operator -(Test a, Test b) => a;
            }
            """;

        await RunAsync(testCode, 7, 5, 7, 15);
    }

    [Fact]
    public async Task NoReportWhenSecondOperatorHeaderHasBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                [Obsolete]
                public static Test operator +(Test a, Test b) => a;

                [Obsolete]
                public static Test operator -(Test a, Test b) => a;
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenSecondTopLevelDelegateHeaderHasNoBlankLineAsync()
    {
        string testCode = """
            using System;

            [Obsolete]
            public delegate void Method0();
            [Obsolete]
            public delegate void Method1();
            """;

        await RunAsync(testCode, 5, 1, 5, 11);
    }

    [Fact]
    public async Task NoReportWhenSecondTopLevelDelegateHeaderHasBlankLineAsync()
    {
        string testCode = """
            using System;

            [Obsolete]
            public delegate void Method0();

            [Obsolete]
            public delegate void Method1();
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenInlineAttributeSharesLineWithFirstEnumMemberAsync()
    {
        string testCode = """
            using System;

            public enum Test
            {
                [Obsolete] Value0 = 0,
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenInlineAttributeSharesLineWithSubsequentEnumMemberAsync()
    {
        string testCode = """
            using System;

            public enum Test
            {
                Value0 = 0,
                [Obsolete] Value1 = 1,
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenMultipleInlineAttributesShareLineWithEnumMemberAsync()
    {
        string testCode = """
            using System;

            public enum Test
            {
                Value0 = 0,
                [Obsolete, CLSCompliant(true)] Value1 = 1,
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenStackedAttributesEndWithLastAttributeInlineWithEnumMemberAsync()
    {
        string testCode = """
            using System;

            public enum Test
            {
                Value0 = 0,
                [Obsolete]
                [CLSCompliant(true)] Value1 = 1,
            }
            """;

        await RunAsync(testCode, 6, 5, 7, 25);
    }

    [Fact]
    public async Task NoReportWhenMultipleConsecutiveEnumMembersHaveInlineAttributesAsync()
    {
        string testCode = """
            using System;

            public enum Test
            {
                [Obsolete] Value0 = 0,
                [Obsolete] Value1 = 1,
                [Obsolete] Value2 = 2,
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenCommentIsBeforeInlineAttributeWithoutBlankLineAsync()
    {
        string testCode = """
            using System;

            public enum Test
            {
                Value0 = 0,
                // Comment
                [Obsolete] Value1 = 1,
            }
            """;

        await RunAsync(testCode, 6, 5, 6, 15);
    }

    [Fact]
    public async Task ReportWhenInlineAttributeSharesLineWithSubsequentMethodAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }
                [Obsolete] public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode, 8, 5, 8, 15);
    }

    [Fact]
    public async Task NoReportWhenInlineAttributeSharesLineWithSubsequentMethodAndHasBlankLineAsync()
    {
        string testCode = """
            using System;

            public class Test
            {
                public void Method0()
                {
                }

                [Obsolete] public void Method1()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenMemberInsideBlockScopedNamespaceHasNoBlankLineBeforeHeaderAsync()
    {
        string testCode = """
            using System;

            namespace MyNamespace
            {
                [Obsolete]
                public class Test0
                {
                }
                [Obsolete]
                public class Test1
                {
                }
            }
            """;

        await RunAsync(testCode, 9, 5, 9, 15);
    }

    [Fact]
    public async Task NoReportWhenMemberInsideBlockScopedNamespaceHasBlankLineBeforeHeaderAsync()
    {
        string testCode = """
            using System;

            namespace MyNamespace
            {
                [Obsolete]
                public class Test0
                {
                }

                [Obsolete]
                public class Test1
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenMemberInsideFileScopedNamespaceHasNoBlankLineBeforeHeaderAsync()
    {
        string testCode = """
            using System;

            namespace MyNamespace;

            [Obsolete]
            public class Test0
            {
            }
            [Obsolete]
            public class Test1
            {
            }
            """;

        await RunAsync(testCode, 9, 1, 9, 11);
    }

    [Fact]
    public async Task NoReportWhenMemberInsideFileScopedNamespaceHasBlankLineBeforeHeaderAsync()
    {
        string testCode = """
            using System;

            namespace MyNamespace;

            [Obsolete]
            public class Test0
            {
            }

            [Obsolete]
            public class Test1
            {
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
