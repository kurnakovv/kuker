// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.Analyzers.Tests.Rules;

public class Kuk0006MultilineClosingParenthesisAnalyzerTests
{
#pragma warning disable SA1118 // Parameter should not span multiple lines
    [Theory]
    [InlineData("NoReportOnSingleLineInvocation", "var result = Foo(1, 2, 3);", false)]
    [InlineData(
        "NoReportOnValidMultilineInvocation",
        """
        var result = Foo(
            1,
            2
        );
        """,
        false
    )]
    [InlineData(
        "ReportWhenClosingParenthesisIsNotOnOwnLine",
        """
        var result = Foo(
            1,
            2);
        """,
        true
    )]
    [InlineData(
        "ReportWhenClosingParenthesisIsMisaligned",
        """
        var result = Foo(
            1,
            2
          );
        """,
        true
    )]
    [InlineData(
        "NoReportOnFluentInvocationWithAlignedClosingParenthesis",
        """
        var result = Values()
            .Where(
                x => x > 0
            )
            .ToArray();
        """,
        false
    )]
    [InlineData(
        "ReportOnFluentInvocationWhenClosingParenthesisIsMisaligned",
        """
        var result = Values()
            .Where(
                x => x > 0
              )
            .ToArray();
        """,
        true
    )]
    [InlineData(
        "ReportOnFluentInvocationWhenClosingParenthesisIsOnSameLine",
        """
        var result = Values()
            .Where(
                x => x > 0)
            .ToArray();
        """,
        true
    )]
    [InlineData(
        "NoReportOnIndentedMultilineInvocationAfterAssignment",
        """
        var result =
            Foo(
                1,
                2
            );
        """,
        false
    )]
    [InlineData(
        "NoReportOnNestedMultilineInvocations",
        """
        var result = Foo(
            Foo(
                1,
                2
            ),
            Foo(
                3,
                4
            )
        );
        """,
        false
    )]
    [InlineData(
        "NoReportOnMultilineInvocationBeforeCommaInArgumentList",
        """
        var result = Foo(
            Bar(
                1,
                2
            ),
            3
        );
        """,
        false
    )]
    [InlineData(
        "NoReportOnMultiLineFluentChainWithSeveralAlignedClosings",
        """
        var result = Values()
            .Where(
                x => x > 0
            )
            .Select(
                x => x + 1
            )
            .ToArray();
        """,
        false
    )]
    [InlineData(
        "NoReportOnMultilineReturnInvocation",
        """
        return Foo(
            1,
            2
        );
        """,
        false
    )]
    [InlineData(
        "ReportOnMultilineReturnInvocationWhenClosingParenthesisIsOnSameLine",
        """
        return Foo(
            1,
            2);
        """,
        true
    )]
    [InlineData(
        "ReportOnMultilineReturnInvocationWhenClosingParenthesisIsMisaligned",
        """
        return Foo(
            1,
            2
          );
        """,
        true
    )]
    [InlineData(
        "NoReportOnNestedMultilineReturnInvocations",
        """
        return Foo(
            Foo(
                1,
                2
            ),
            Foo(
                3,
                4
            )
        );
        """,
        false
    )]
    [InlineData(
        "NoReportOnMultilineFluentChainInReturn",
        """
        return Values()
            .Where(
                x => x > 0
            )
            .Select(
                x => x + 1
            )
            .ToArray();
        """,
        false
    )]
    [InlineData(
        "ReportOnMultilineFluentChainInReturnWhenClosingParenthesisIsOnSameLine",
        """
        return Values()
            .Where(
                x => x > 0)
            .Select(
                x => x + 1
            )
            .ToArray();
        """,
        true
    )]
    [InlineData(
        "NoReportOnMultilineInvocationInIfCondition",
        """
        if (BoolFoo(
            1,
            2
        ))
        {
        }
        """,
        false
    )]
    [InlineData(
        "NoReportOnMultilineInvocationInIfConditionWithTrailingComment",
        """
        if (BoolFoo(
            1,
            2
        )) // trailing comment
        {
        }
        """,
        false
    )]
    [InlineData(
        "ReportOnMultilineInvocationInIfConditionWithTrailingComment",
        """
        if (BoolFoo(
            1,
            2
          )) // trailing comment
        {
        }
        """,
        true
    )]
    [InlineData(
        "NoReportWhenClosingParenthesisHasTrailingExpression",
        """
        var result = Foo(
            1,
            2
        ) + 1;
        """,
        false
    )]
    [InlineData(
        "NoReportOnMultilineReturnInvocationInsideIf",
        """
        if (BoolFoo(
            1,
            2
        ))
        {
            return Foo(
                1,
                2
            );
        }
        """,
        false
    )]
    [InlineData(
        "ReportOnMultilineReturnInvocationInsideIf",
        """
        if (BoolFoo(
            1,
            2
        ))
        {
            return Foo(
                1,
                2);
        }
        """,
        true
    )]
    [InlineData(
        "ReportOnMultilineInvocationInIfConditionWhenClosingParenthesisIsOnSameLine",
        """
        if (BoolFoo(
            1,
            2))
        {
        }
        """,
        true
    )]
    [InlineData(
        "NoReportOnMultilineInvocationInLambdaReturnValue",
        """
        Func<int> get = () => Foo(
            1,
            2
        );
        """,
        false
    )]
    [InlineData(
        "ReportOnMultilineInvocationInLambdaReturnValueWhenClosingParenthesisIsOnSameLine",
        """
        Func<int> get = () => Foo(
            1,
            2);
        """,
        true
    )]
    [InlineData(
        "NoReportOnMultilineInvocationInWhileCondition",
        """
        while (BoolFoo(
            1,
            2
        ))
        {
        }
        """,
        false
    )]
    [InlineData(
        "ReportOnMultilineInvocationInWhileConditionWhenClosingParenthesisIsOnSameLine",
        """
        while (BoolFoo(
            1,
            2))
        {
        }
        """,
        true
    )]
    [InlineData(
        "NoReportWhenClosingParenthesisHasTrailingComment",
        """
        var result = Foo(
            1,
            2
        ); // trailing comment
        """,
        false
    )]
    [InlineData(
        "NoReportWhenClosingParenthesisHasTrailingCommentOnSameLine",
        """
        var result = Foo(
            1,
            2
        ) // trailing comment
        ;
        """,
        false
    )]
    [InlineData(
        "ReportOnNestedMultilineInvocationWhenInnerClosingParenthesisIsMisaligned",
        """
        var result = Foo(
            Foo(
                1,
                2
            ),
            Foo(
                3,
                4
              )
        );
        """,
        true
    )]
    [InlineData(
        "ReportOnMultiLineFluentChainWhenClosingParenthesisIsMisaligned",
        """
        var result = Values()
            .Where(
                x => x > 0
              )
            .Select(
                x => x + 1
            )
            .ToArray();
        """,
        true
    )]
    [InlineData(
        "ReportOnMultiLineFluentChainWhenClosingParenthesisIsOnSameLine",
        """
        var result = Values()
            .Where(
                x => x > 0)
            .Select(
                x => x + 1
            )
            .ToArray();
        """,
        true
    )]
    [InlineData(
        "test1",
        """
        int a = 12;
        int b = 13;

        a.Equals(
            b);
        """,
        true
    )]
    [InlineData(
        "test2",
        """
        int a = 12;
        int b = 13;

                a.Equals(
                    b
        );
        """,
        true
    )]
    [InlineData(
        "test3",
        """
        int a = 12;
        int b = 13;

        a.Equals(
            b
        );
        """,
        false
    )]
#pragma warning restore SA1118 // Parameter should not span multiple lines
    public async Task RunAsync(string name, string invocationCode, bool expectDiagnostic)
    {
        string testCode = """
            using System;
            using System.Linq;

            public class TestClass
            {
                public object M1()
                {
                    {%invocationCode%}
                    return 1;
                }

                private static int Foo(params int[] args)
                {
                    return args.Sum();
                }

                private static int Bar(params int[] args)
                {
                    return args.Sum();
                }

                private static bool BoolFoo(params int[] args)
                {
                    return args.Length > 0;
                }

                private static int[] Values()
                {
                    return [1, 2, 3];
                }
            }
            """.Replace("{%invocationCode%}", "// " + name + "\n" + invocationCode, StringComparison.Ordinal);

        CSharpAnalyzerTest<Kuk0006MultilineClosingParenthesisAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        if (expectDiagnostic)
        {
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult("KUK0006", DiagnosticSeverity.Warning)
            );
        }

        await test.RunAsync();
    }
}
