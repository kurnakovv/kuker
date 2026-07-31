// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Kuker.Core.Contants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.Analyzers.Tests.Rules;

public class Kuk0006MultilineClosingParenthesisAnalyzerTests
{
#pragma warning disable RCS0053, SA1117 // Parameter should not span multiple lines
    [Theory]
    [InlineData("NoReportOnSingleLineInvocation", "var result = Foo(1, 2, 3);", 0, 0, 0, 0)]
    [InlineData(
        "NoReportOnValidMultilineInvocation",
        """
        var result = Foo(
            1,
            2
        );
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenClosingParenthesisIsNotOnOwnLine",
        """
        var result = Foo(
            1,
            2);
        """, 11, 6, 11, 7
    )]
    [InlineData(
        "ReportWhenClosingParenthesisIsMisaligned",
        """
        var result = Foo(
            1,
            2
          );
        """, 12, 3, 12, 4
    )]
    [InlineData(
        "NoReportOnFluentInvocationWithAlignedClosingParenthesis",
        """
        var result = Values()
            .Where(
                x => x > 0
            )
            .ToArray();
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnFluentInvocationWhenClosingParenthesisIsMisaligned",
        """
        var result = Values()
            .Where(
                x => x > 0
              )
            .ToArray();
        """, 12, 7, 12, 8
    )]
    [InlineData(
        "ReportOnFluentInvocationWhenClosingParenthesisIsOnSameLine",
        """
        var result = Values()
            .Where(
                x => x > 0)
            .ToArray();
        """, 11, 19, 11, 20
    )]
    [InlineData(
        "NoReportOnIndentedMultilineInvocationAfterAssignment",
        """
        var result =
            Foo(
                1,
                2
            );
        """, 0, 0, 0, 0
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
        """, 0, 0, 0, 0
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
        """, 0, 0, 0, 0
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
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnMultilineReturnInvocation",
        """
        return Foo(
            1,
            2
        );
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnMultilineReturnInvocationWhenClosingParenthesisIsOnSameLine",
        """
        return Foo(
            1,
            2);
        """, 11, 6, 11, 7
    )]
    [InlineData(
        "ReportOnMultilineReturnInvocationWhenClosingParenthesisIsMisaligned",
        """
        return Foo(
            1,
            2
          );
        """, 12, 3, 12, 4
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
        """, 0, 0, 0, 0
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
        """, 0, 0, 0, 0
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
        """, 11, 19, 11, 20
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
        """, 0, 0, 0, 0
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
        """, 0, 0, 0, 0
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
        """, 12, 3, 12, 4
    )]
    [InlineData(
        "NoReportWhenClosingParenthesisHasTrailingExpression",
        """
        var result = Foo(
            1,
            2
        ) + 1;
        """, 0, 0, 0, 0
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
        """, 0, 0, 0, 0
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
        """, 16, 10, 16, 11
    )]
    [InlineData(
        "ReportOnMultilineInvocationInIfConditionWhenClosingParenthesisIsOnSameLine",
        """
        if (BoolFoo(
            1,
            2))
        {
        }
        """, 11, 6, 11, 7
    )]
    [InlineData(
        "NoReportOnMultilineInvocationInLambdaReturnValue",
        """
        Func<int> get = () => Foo(
            1,
            2
        );
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnMultilineInvocationInLambdaReturnValueWhenClosingParenthesisIsOnSameLine",
        """
        Func<int> get = () => Foo(
            1,
            2);
        """, 11, 6, 11, 7
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
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnMultilineInvocationInWhileConditionWhenClosingParenthesisIsOnSameLine",
        """
        while (BoolFoo(
            1,
            2))
        {
        }
        """, 11, 6, 11, 7
    )]
    [InlineData(
        "NoReportWhenClosingParenthesisHasTrailingComment",
        """
        var result = Foo(
            1,
            2
        ); // trailing comment
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportWhenClosingParenthesisHasTrailingCommentOnSameLine",
        """
        var result = Foo(
            1,
            2
        ) // trailing comment
        ;
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnFluentInvocationWithBlockLambdaArgument",
        """
        var result = Values()
            .Select(x =>
            {
                var computed = Foo(
                    x,
                    x + 1
                );

                return computed;
            })
            .ToArray();
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnFluentInvocationWithAnonymousObjectInitializerArgument",
        """
        var result = Values()
            .Select(x => new
            {
                Original = x,
                Computed = Foo(
                    x,
                    x + 1
                ),
            })
            .ToArray();
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnInvocationWithAnonymousObjectInitializerArgument",
        """
        global::System.Collections.Generic.List<object> items = [];

        items.Add(new
        {
            Value = 1,
            Computed = Foo(
                1,
                2
            ),
        });
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnInvocationWithObjectCreationArgumentAndMultipleNamedArguments1",
        """
        global::System.Collections.Generic.List<Item> items = [];

        items.Add(new Item(
            id: 1,
            name: "One"
        ));
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnInvocationWithObjectCreationArgumentAndMultipleNamedArguments2",
        """
        global::System.Collections.Generic.List<Item> items = [];

        items.Add(
            new Item(
                id: 1,
                name: "One"
            )
        );
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnInvocationWithObjectCreationArgumentAndMultipleNamedArguments1",
        """
        global::System.Collections.Generic.List<Item> items = [];

        items.Add(new Item(
            id: 1,
            name: "One"));
        """, 13, 17, 13, 18
    )]
    [InlineData(
        "ReportOnInvocationWithObjectCreationArgumentAndMultipleNamedArguments2",
        """
        global::System.Collections.Generic.List<Item> items = [];

        items.Add(
            new Item(
                id: 1,
                name: "One"
            ));
        """, 15, 6, 15, 7
    )]
    [InlineData(
        "NoReportOnInvocationWithCollectionExpressionArgument",
        """
        var result = Foo([
            1,
            2
        ]);
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnFluentInvocationWithCollectionExpressionArgument",
        """
        var result = Values()
            .Select(
                x => Foo([
                    x,
                    x + 1
                ])
            )
            .ToArray();
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnNestedInvocationWithCollectionExpressionArgument",
        """
        var result = Foo(
            Bar([
                1,
                2
            ]),
            3
        );
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnInvocationWithCollectionExpressionArgument",
        """
        var result = Foo([
            1,
            2]);
        """, 11, 7, 11, 8
    )]
    [InlineData(
        "ReportOnFluentInvocationWithCollectionExpressionArgument",
        """
        var result = Values()
            .Select(
                x => Foo([
                    x,
                    x + 1
           ])
            )
            .ToArray();
        """, 14, 5, 14, 6
    )]
    [InlineData(
        "ReportOnNestedInvocationWithCollectionExpressionArgument",
        """
        var result = Foo(
            Bar([
                1,
                2      ]),
            3
        );
        """, 12, 17, 12, 18
    )]
    [InlineData(
        "ReportOnFluentInvocationWithBlockLambdaArgument",
        """
        var result = Values()
            .Select(x =>
            {
                var computed = Foo(
                    x,
                    x + 1
                );

                return computed;})
            .ToArray();
        """, 17, 26, 17, 27
    )]
    [InlineData(
        "ReportOnFluentInvocationWithAnonymousObjectInitializerArgument",
        """
        var result = Values()
            .Select(x => new
            {
                Original = x,
                Computed = Foo(
                    x,
                    x + 1
                )})
            .ToArray();
        """, 16, 11, 16, 12
    )]
    [InlineData(
        "ReportOnInvocationWithAnonymousObjectInitializerArgument",
        """
        global::System.Collections.Generic.List<object> items = [];

        items.Add(new
        {
            Value = 1,
            Computed = Foo(
                1,
                2
            )       });
        """, 17, 14, 17, 15
    )]
    [InlineData(
        "ReportOnInvocationInBlockLambdaWhenClosingParenthesisIsOnSameLine",
        """
        Func<int, int> map = x =>
        {
            var computed = Foo(
                x,
                x + 1);

            return computed;
        };
        """, 13, 14, 13, 15
    )]
    [InlineData(
        "ReportOnInvocationInAnonymousObjectInitializerWhenClosingParenthesisIsOnSameLine",
        """
        var item = new
        {
            Value = Foo(
                1,
                2),
        };
        """, 13, 10, 13, 11
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
        """, 17, 7, 17, 8
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
        """, 12, 7, 12, 8
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
        """, 11, 19, 11, 20
    )]
    [InlineData(
        "ReportOnSimpleInvocationWhenClosingParenthesisIsOnSameLine",
        """
        int a = 12;
        int b = 13;

        a.Equals(
            b);
        """, 13, 6, 13, 7
    )]
    [InlineData(
        "ReportOnSimpleInvocationWhenClosingParenthesisIsShiftedTooFarRight",
        """
        int a = 12;
        int b = 13;

                a.Equals(
                    b
        );
        """, 14, 1, 14, 2
    )]
    [InlineData(
        "NoReportOnSimpleInvocationWithAlignedClosingParenthesis",
        """
        int a = 12;
        int b = 13;

        a.Equals(
            b
        );
        """, 0, 0, 0, 0
    )]
    [InlineData("NoReportOnSingleLineObjectCreation", "var item = new Item(1, \"One\");", 0, 0, 0, 0)]
    [InlineData(
        "NoReportOnValidMultilineObjectCreation",
        """
        var item = new Item(
            1,
            "One"
        );
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnMultilineObjectCreationWhenClosingParenthesisIsOnSameLine",
        """
        var item = new Item(
            1,
            "One");
        """, 11, 10, 11, 11
    )]
    [InlineData(
        "ReportOnMultilineObjectCreationWhenClosingParenthesisIsMisaligned",
        """
        var item = new Item(
            1,
            "One"
          );
        """, 12, 3, 12, 4
    )]
    [InlineData(
        "NoReportOnMultilineObjectCreationWithInitializerAndAlignedClosingParenthesis",
        """
        var item = new Item(
            1,
            "One"
        )
        {
            Name = "Updated",
        };
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnMultilineObjectCreationWithInitializerWhenClosingParenthesisIsOnSameLine",
        """
        var item = new Item(
            1,
            "One")
        {
            Name = "Updated",
        };
        """, 11, 10, 11, 11
    )]
    [InlineData(
        "ReportOnMultilineObjectCreationWithInitializerWhenClosingParenthesisIsMisaligned",
        """
        var item = new Item(
            1,
            "One"
          )
        {
            Name = "Updated",
        };
        """, 12, 3, 12, 4
    )]
    [InlineData(
        "NoReportOnMultilineObjectCreationWithBlankLineBeforeAlignedClosingParenthesis",
        """
        var item = new Item(
            1,
            "One"

        )
        {
            Name = "Updated",
        };
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnMultilineObjectCreationWithTrailingCommentOnClosingParenthesisLine",
        """
        var item = new Item(
            1,
            "One"
        ) // trailing comment
        {
            Name = "Updated",
        };
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnNestedObjectCreationArgumentWithAlignedClosingParentheses",
        """
        var item = new Wrapper(
            new Item(
                1,
                "One"
            ),
            10
        );
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnNestedObjectCreationArgumentWhenInnerClosingParenthesisIsMisaligned",
        """
        var item = new Wrapper(
            new Item(
                1,
                "One"
              ),
            10
        );
        """, 13, 7, 13, 8
    )]
    [InlineData(
        "NoReportOnLongAssignmentWithObjectCreationOnSeparateLine",
        """
        var veryVeryVeryVeryVeryVeryVeryVeryVeryVeryLongClassName =
            new Item(
                1,
                "One"
            );
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnLongAssignmentWithObjectCreationOnSeparateLineWhenClosingParenthesisIsOnSameLine",
        """
        var veryVeryVeryVeryVeryVeryVeryVeryVeryVeryLongClassName =
            new Item(
                1,
                "One");
        """, 12, 14, 12, 15
    )]
    [InlineData(
        "NoReportOnObjectCreationInIfConditionWithAlignedClosingParenthesis",
        """
        if (
            new Item(
                1,
                "One"
            ).Id > 0
        )
        {
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnObjectCreationInIfConditionWhenClosingParenthesisIsOnSameLine",
        """
        if (new Item(
            1,
            "One").Id > 0)
        {
        }
        """, 11, 10, 11, 11
    )]
    [InlineData(
        "NoReportOnObjectCreationInWhileConditionWithAlignedClosingParenthesis",
        """
        while (new Item(
            1,
            "One"
        ).Id > 0)
        {
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnObjectCreationInWhileConditionWhenClosingParenthesisIsOnSameLine",
        """
        while (new Item(
            1,
            "One").Id > 0)
        {
        }
        """, 11, 10, 11, 11
    )]
    [InlineData(
        "NoReportOnObjectCreationInBlockLambdaWithAlignedClosingParenthesis",
        """
        Func<Item> factory = () =>
        {
            return new Item(
                1,
                "One"
            );
        };
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnObjectCreationInBlockLambdaWhenClosingParenthesisIsOnSameLine",
        """
        Func<Item> factory = () =>
        {
            return new Item(
                1,
                "One");
        };
        """, 13, 14, 13, 15
    )]
    [InlineData(
        "NoReportOnObjectCreationInAnonymousObjectInitializerWithAlignedClosingParenthesis",
        """
        var payload = new
        {
            Value = new Item(
                1,
                "One"
            ),
        };

        return payload;
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnObjectCreationInAnonymousObjectInitializerWhenClosingParenthesisIsOnSameLine",
        """
        var payload = new
        {
            Value = new Item(
                1,
                "One"),
        };

        return payload;
        """, 13, 14, 13, 15
    )]
    [InlineData(
        "NoReportOnObjectCreationInCollectionInitializerWithAlignedClosingParenthesis",
        """
        global::System.Collections.Generic.List<Item> items = [
            new Item(
                1,
                "One"
            ),
        ];
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnObjectCreationInCollectionInitializerWhenClosingParenthesisIsMisaligned",
        """
        global::System.Collections.Generic.List<Item> items = [
            new Item(
                1,
                "One"
              ),
        ];
        """, 13, 7, 13, 8
    )]
    [InlineData(
        "NoReportForMixedSingleLineAndValidMultilineObjectCreationInSameMethod",
        """
        var singleLine = new Item(1, "One");

        var multiline = new Item(
            2,
            "Two"
        );

        return singleLine.Id + multiline.Id;
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnObjectCreationWithTrailingMemberAccessAndAlignedClosingParenthesis",
        """
        var text = new Item(
            1,
            "One"
        ).ToString();
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnObjectCreationWithTrailingMemberAccessWhenClosingParenthesisIsOnSameLine",
        """
        var text = new Item(
            1,
            "One").ToString();
        """, 11, 10, 11, 11
    )]
    [InlineData(
        "NoReportOnObjectCreationWhenClosingParenthesisHasTrailingComment",
        """
        var item = new Item(
            1,
            "One"
        ); // trailing comment
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnObjectCreationWhenCommentIsBetweenLastArgumentAndClosingParenthesis",
        """
        var item = new Item(
            1,
            "One"
            // comment between argument and closing parenthesis
        );
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnObjectCreationWhenClosingParenthesisIsMisalignedAndCommentIsBetweenLastArgumentAndClosingParenthesis",
        """
        var item = new Item(
            1,
            "One"
            // comment between argument and closing parenthesis
          );
        """, 13, 3, 13, 4
    )]
    [InlineData(
        "NoReportOnTripleNestedObjectCreationWithAlignedClosingParentheses",
        """
        var nested = new Item(
            new Wrapper(
                new Item(
                    1,
                    "One"
                ),
                10
            ).Priority,
            "Nested"
        );

        return nested.Id;
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnTripleNestedObjectCreationInSingleLongLine",
        "var nested = new Item(new Wrapper(new Item(1, \"One\"), 10).Priority, \"Nested\"); return nested.Id;", 0, 0, 0, 0
    )]
#pragma warning restore RCS0053, SA1117 // Parameter should not span multiple lines
    public async Task RunAsync(string name, string invocationCode, int startLine, int startColumn, int endLine, int endColumn)
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

                private sealed class Item
                {
                    public Item(int id, string name)
                    {
                        Id = id;
                        Name = name;
                    }

                    public int Id { get; }
                    public string Name { get; set; }
                }

                private sealed class Wrapper
                {
                    public Wrapper(Item item, int priority)
                    {
                        Item = item;
                        Priority = priority;
                    }

                    public Item Item { get; }
                    public int Priority { get; }
                }
            }
            """.Replace("{%invocationCode%}", "// " + name + "\n" + invocationCode, StringComparison.Ordinal);

        CSharpAnalyzerTest<Kuk0006MultilineClosingParenthesisAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        if (!(startLine == 0 && startColumn == 0 && endLine == 0 && endColumn == 0))
        {
            DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning)
                .WithSpan(startLine, startColumn, endLine, endColumn);

            test.ExpectedDiagnostics.Add(expected);
        }

        await test.RunAsync();
    }

    [Fact]
    public async Task NoReportWhenClosingParenthesisIsMissingAsync()
    {
        string testCode = """
            using System;
            using System.Linq;

            public class TestClass
            {
                public object M1()
                {
                    // NoReportWhenClosingParenthesisIsMissing
                    var result = Foo(
                        1,
                        2
                    return 1;
                }

                private static int Foo(params int[] args)
                {
                    return args.Sum();
                }
            }
            """;

        CSharpAnalyzerTest<Kuk0006MultilineClosingParenthesisAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1002").WithSpan(11, 14, 11, 14));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1026").WithSpan(11, 14, 11, 14));

        await test.RunAsync();
    }

    [Theory]
    [InlineData(null, true, true)]
    [InlineData("method_invocation,object_creation", true, true)]
    [InlineData("object_creation,method_invocation", true, true)]
    [InlineData("method_invocation", true, false)]
    [InlineData("object_creation", false, true)]
    public async Task ReportDependingOnTargetSyntaxOptionAsync(string? targetSyntax, bool expectMethodInvocationDiagnostic, bool expectObjectCreationDiagnostic)
    {
        string testCode = """
            using System;
            using System.Linq;

            public class TestClass
            {
                public object M1()
                {
                    var fromInvocation = Foo(
                        1,
                        2{|#0:)|};

                    var fromCreation = new Item(
                        1,
                        "One"{|#1:)|};

                    return fromInvocation + fromCreation.Id;
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

        CSharpAnalyzerTest<Kuk0006MultilineClosingParenthesisAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        if (targetSyntax is not null)
        {
            test.TestState.AnalyzerConfigFiles.Add((
                "/.editorconfig",
                $$"""
                root = true

                [*.cs]
                dotnet_code_quality.KUK0006.target_syntax = {{targetSyntax}}
                """
            ));
        }

        if (expectMethodInvocationDiagnostic)
        {
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0));
        }

        if (expectObjectCreationDiagnostic)
        {
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(1));
        }

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportAllObjectCreationViolationsInSingleMethodWithAccurateLocationsAsync()
    {
        string testCode = """
            using System;

            public class TestClass
            {
                public object M1()
                {
                    var first = new Item(
                        1,
                        "One"{|#0:)|};

                    var second = new Item(
                        2,
                        "Two"
                      {|#1:)|};

                    return first.Id + second.Id;
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

        CSharpAnalyzerTest<Kuk0006MultilineClosingParenthesisAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.AnalyzerConfigFiles.Add((
            "/.editorconfig",
            """
            root = true

            [*.cs]
            dotnet_code_quality.KUK0006.target_syntax = object_creation
            """
        ));

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(1));

        await test.RunAsync();
    }

    [Theory]
    [InlineData("foobar")]
    [InlineData("INVALID")]
    [InlineData("method_invocation/object_creation")]
    [InlineData("method_invocation|object_creation")]
    [InlineData("method_invocation,method_invocation,object_creation")]
    [InlineData("  bad value  ")]
    public async Task InvalidTargetSyntaxOptionReportsOnlyConfigDiagnosticAsync(string invalidTargetSyntax)
    {
        string testCode = """
            using System;
            using System.Linq;

            public class TestClass
            {
                public object M1()
                {
                    var fromInvocation = Foo(
                        1,
                        2{|#0:)|};

                    return fromInvocation;
                }

                private static int Foo(params int[] args)
                {
                    return args.Sum();
                }
            }
            """;

        CSharpAnalyzerTest<Kuk0006MultilineClosingParenthesisAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.AnalyzerConfigFiles.Add((
            "/.editorconfig",
            $$"""
            root = true

            [*.cs]
            dotnet_code_quality.KUK0006.target_syntax = {{invalidTargetSyntax}}
            """
        ));

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0).WithArguments(invalidTargetSyntax.Trim()));

        await test.RunAsync();
    }
}
