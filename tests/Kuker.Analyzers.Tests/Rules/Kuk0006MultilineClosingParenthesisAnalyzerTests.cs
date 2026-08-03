// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Kuker.Core.Contants;
using Kuker.Core.Options;
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
        "NoReportOnObjectCreationWithoutArgumentList",
        """
        var uriBuilder = new System.UriBuilder
        {
            Host = "example.com",
        };
        """, 0, 0, 0, 0
    )]
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
        "NoReportOnValidMultilineImplicitObjectCreation",
        """
        Item item = new(
            1,
            "One"
        );
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnMultilineImplicitObjectCreationWhenClosingParenthesisIsOnSameLine",
        """
        Item item = new(
            1,
            "One");
        """, 11, 10, 11, 11
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
            $$"""
            root = true

            [*.cs]
            {{Kuk0006TargetSyntaxOption.KEY}} = {{Kuk0006TargetSyntaxOption.OBJECT_CREATION}}
            """
        ));

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(1));

        await test.RunAsync();
    }

#pragma warning disable RCS0053, SA1117 // Parameter should not span multiple lines
    [Theory]
    [InlineData("NoReportOnSingleLineMethodDeclaration", "public void Foo(int a, int b) { }", 0, 0, 0, 0)]
    [InlineData("NoReportOnMethodDeclarationWithoutParameters", "public void Foo() { }", 0, 0, 0, 0)]
    [InlineData(
        "NoReportOnValidMultilineMethodDeclaration",
        """
        public void Foo(
            int a,
            int b
        )
        {
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenClosingParenthesisIsNotOnOwnLine",
        """
        public void Foo(
            int a,
            int b)
        {
        }
        """, 8, 10, 8, 11
    )]
    [InlineData(
        "ReportWhenClosingParenthesisIsMisaligned",
        """
        public void Foo(
            int a,
            int b
          )
        {
        }
        """, 9, 3, 9, 4
    )]
    [InlineData(
        "NoReportOnValidMultilineMethodDeclarationWithReturnType",
        """
        public int Foo(
            int a,
            int b
        )
        {
            return a + b;
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnMultilineMethodDeclarationWithReturnTypeWhenClosingParenthesisIsOnSameLine",
        """
        public int Foo(
            int a,
            int b)
        {
            return a + b;
        }
        """, 8, 10, 8, 11
    )]
    [InlineData(
        "NoReportOnValidMultilineMethodDeclarationWithManyParameters",
        """
        public void Foo(
            int a,
            int b,
            int c,
            int d
        )
        {
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnMultilineMethodDeclarationWithManyParametersWhenClosingParenthesisIsOnSameLine",
        """
        public void Foo(
            int a,
            int b,
            int c,
            int d)
        {
        }
        """, 10, 10, 10, 11
    )]
    [InlineData(
        "NoReportOnValidMultilineMethodDeclarationWithReturnTypeOnSeparateLine",
        """
        public VeryLongClassName
            Foo(
                int a,
                int b
            ) => null;
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnMultilineMethodDeclarationWithReturnTypeOnSeparateLineWhenClosingParenthesisIsOnSameLine",
        """
        public VeryLongClassName
            Foo(
                int a,
                int b) => null;
        """, 9, 14, 9, 15
    )]
    [InlineData(
        "ReportOnMultilineMethodDeclarationWithReturnTypeOnSeparateLineWhenClosingParenthesisIsMisaligned",
        """
        public VeryLongClassName
            Foo(
                int a,
                int b
        ) => null;
        """, 10, 1, 10, 2
    )]
#pragma warning restore RCS0053, SA1117 // Parameter should not span multiple lines
    public async Task RunMethodDeclarationAsync(string name, string methodDeclarationCode, int startLine, int startColumn, int endLine, int endColumn)
    {
        string testCode = """
            using System;

            public class TestClass
            {
            {%methodDeclarationCode%}
                public class VeryLongClassName { }
            }
            """.Replace("{%methodDeclarationCode%}", "// " + name + "\n" + methodDeclarationCode, StringComparison.Ordinal);

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
            {{Kuk0006TargetSyntaxOption.KEY}} = {{Kuk0006TargetSyntaxOption.METHOD_DECLARATION}}
            """
        ));

        if (!(startLine == 0 && startColumn == 0 && endLine == 0 && endColumn == 0))
        {
            DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning)
                .WithSpan(startLine, startColumn, endLine, endColumn);

            test.ExpectedDiagnostics.Add(expected);
        }

        await test.RunAsync();
    }

#pragma warning disable RCS0053, SA1117 // Parameter should not span multiple lines
    [Theory]
    [InlineData("NoReportOnSingleLineLocalFunction", "void LocalFoo(int a, int b) { }", 0, 0, 0, 0)]
    [InlineData("NoReportOnLocalFunctionWithoutParameters", "void LocalFoo() { }", 0, 0, 0, 0)]
    [InlineData(
        "NoReportOnValidMultilineLocalFunction",
        """
        void LocalFoo(
            int a,
            int b
        )
        {
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenLocalFunctionClosingParenthesisIsNotOnOwnLine",
        """
        void LocalFoo(
            int a,
            int b) { }
        """, 10, 10, 10, 11
    )]
    [InlineData(
        "ReportWhenMultilineLocalFunctionClosingParenthesisIsMisaligned",
        """
        void LocalFoo(
            int a,
            int b
          ) { }
        """, 11, 3, 11, 4
    )]
#pragma warning restore RCS0053, SA1117 // Parameter should not span multiple lines
    public async Task RunLocalFunctionAsync(string name, string localFunctionCode, int startLine, int startColumn, int endLine, int endColumn)
    {
        string testCode = """
            using System;

            public class TestClass
            {
                public void ContainingMethod()
                {
            {%localFunctionCode%}
                }
            }
            """.Replace("{%localFunctionCode%}", "// " + name + "\n" + localFunctionCode, StringComparison.Ordinal);

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
            {{Kuk0006TargetSyntaxOption.KEY}} = {{Kuk0006TargetSyntaxOption.METHOD_DECLARATION}}
            """
        ));

        if (!(startLine == 0 && startColumn == 0 && endLine == 0 && endColumn == 0))
        {
            DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning)
                .WithSpan(startLine, startColumn, endLine, endColumn);

            test.ExpectedDiagnostics.Add(expected);
        }

        await test.RunAsync();
    }

#pragma warning disable RCS0053, SA1117 // Parameter should not span multiple lines
    [Theory]
    [InlineData("NoReportOnSingleLineInterfaceMethod", "void Foo(int a, int b);", 0, 0, 0, 0)]
    [InlineData("NoReportOnInterfaceMethodWithoutParameters", "void Foo();", 0, 0, 0, 0)]
    [InlineData(
        "NoReportOnValidMultilineInterfaceMethod",
        """
        void Foo(
            int a,
            int b
        );
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenInterfaceMethodClosingParenthesisIsNotOnOwnLine",
        """
        void Foo(
            int a,
            int b);
        """, 8, 10, 8, 11
    )]
    [InlineData(
        "ReportWhenMultilineInterfaceMethodClosingParenthesisIsMisaligned",
        """
        void Foo(
            int a,
            int b
          );
        """, 9, 3, 9, 4
    )]
#pragma warning restore RCS0053, SA1117 // Parameter should not span multiple lines
    public async Task RunInterfaceMethodAsync(string name, string methodDeclarationCode, int startLine, int startColumn, int endLine, int endColumn)
    {
        string testCode = """
            using System;

            public interface ITestInterface
            {
            {%methodDeclarationCode%}
            }
            """.Replace("{%methodDeclarationCode%}", "// " + name + "\n" + methodDeclarationCode, StringComparison.Ordinal);

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
            {{Kuk0006TargetSyntaxOption.KEY}} = {{Kuk0006TargetSyntaxOption.METHOD_DECLARATION}}
            """
        ));

        if (!(startLine == 0 && startColumn == 0 && endLine == 0 && endColumn == 0))
        {
            DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning)
                .WithSpan(startLine, startColumn, endLine, endColumn);

            test.ExpectedDiagnostics.Add(expected);
        }

        await test.RunAsync();
    }

#pragma warning disable RCS0053, SA1117 // Parameter should not span multiple lines
    [Theory]
    [InlineData("NoReportOnSingleLineAbstractMethod", "public abstract void Foo(int a, int b);", 0, 0, 0, 0)]
    [InlineData("NoReportOnAbstractMethodWithoutParameters", "public abstract void Foo();", 0, 0, 0, 0)]
    [InlineData(
        "NoReportOnValidMultilineAbstractMethod",
        """
        public abstract void Foo(
            int a,
            int b
        );
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenAbstractMethodClosingParenthesisIsNotOnOwnLine",
        """
        public abstract void Foo(
            int a,
            int b);
        """, 8, 10, 8, 11
    )]
    [InlineData(
        "ReportWhenMultilineAbstractMethodClosingParenthesisIsMisaligned",
        """
        public abstract void Foo(
            int a,
            int b
          );
        """, 9, 3, 9, 4
    )]
#pragma warning restore RCS0053, SA1117 // Parameter should not span multiple lines
    public async Task RunAbstractMethodAsync(string name, string methodDeclarationCode, int startLine, int startColumn, int endLine, int endColumn)
    {
        string testCode = """
            using System;

            public abstract class TestAbstractClass
            {
            {%methodDeclarationCode%}
            }
            """.Replace("{%methodDeclarationCode%}", "// " + name + "\n" + methodDeclarationCode, StringComparison.Ordinal);

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
            {{Kuk0006TargetSyntaxOption.KEY}} = {{Kuk0006TargetSyntaxOption.METHOD_DECLARATION}}
            """
        ));

        if (!(startLine == 0 && startColumn == 0 && endLine == 0 && endColumn == 0))
        {
            DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning)
                .WithSpan(startLine, startColumn, endLine, endColumn);

            test.ExpectedDiagnostics.Add(expected);
        }

        await test.RunAsync();
    }

#pragma warning disable RCS0053, SA1117 // Parameter should not span multiple lines
    [Theory]
    [InlineData("NoReportOnSingleLineConstructorDeclaration", "public TestClass(int a, int b) { }", 0, 0, 0, 0)]
    [InlineData("NoReportOnConstructorDeclarationWithoutParameters", "public TestClass() { }", 0, 0, 0, 0)]
    [InlineData(
        "NoReportOnValidMultilineConstructorDeclaration",
        """
        public TestClass(
            int a,
            int b
        )
        {
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenConstructorClosingParenthesisIsNotOnOwnLine",
        """
        public TestClass(
            int a,
            int b)
        {
        }
        """, 8, 10, 8, 11
    )]
    [InlineData(
        "ReportWhenConstructorClosingParenthesisIsMisaligned",
        """
        public TestClass(
            int a,
            int b
          )
        {
        }
        """, 9, 3, 9, 4
    )]
#pragma warning restore RCS0053, SA1117 // Parameter should not span multiple lines
    public async Task RunConstructorDeclarationAsync(string name, string constructorDeclarationCode, int startLine, int startColumn, int endLine, int endColumn)
    {
        string testCode = """
            using System;

            public class TestClass
            {
            {%constructorDeclarationCode%}
            }
            """.Replace("{%constructorDeclarationCode%}", "// " + name + "\n" + constructorDeclarationCode, StringComparison.Ordinal);

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
            {{Kuk0006TargetSyntaxOption.KEY}} = {{Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION}}
            """
        ));

        if (!(startLine == 0 && startColumn == 0 && endLine == 0 && endColumn == 0))
        {
            DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning)
                .WithSpan(startLine, startColumn, endLine, endColumn);

            test.ExpectedDiagnostics.Add(expected);
        }

        await test.RunAsync();
    }

#pragma warning disable RCS0053, SA1117 // Parameter should not span multiple lines
    [Theory]
    [InlineData("NoReportOnSingleLinePrimaryConstructorClass", "public class TestClass(int a, int b)\n{\n    public int Sum { get; } = a + b;\n}", 0, 0, 0, 0)]
    [InlineData(
        "NoReportOnValidMultilinePrimaryConstructorClass",
        """
        public class TestClass(
            int a,
            int b
        )
        {
            public int Sum { get; } = a + b;
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportOnValidMultilinePrimaryConstructorStruct",
        """
        public struct TestStruct(
            int a,
            int b
        )
        {
            public int Sum { get; } = a + b;
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenPrimaryConstructorClosingParenthesisIsNotOnOwnLine",
        """
        public record TestRecord(
            int a,
            int b)
        {
        }
        """, 6, 10, 6, 11
    )]
    [InlineData(
        "ReportWhenPrimaryConstructorClosingParenthesisIsMisaligned",
        """
        public record TestRecord(
            int a,
            int b
          )
        {
        }
        """, 7, 3, 7, 4
    )]
#pragma warning restore RCS0053, SA1117 // Parameter should not span multiple lines
    public async Task RunPrimaryConstructorAsync(string name, string primaryConstructorCode, int startLine, int startColumn, int endLine, int endColumn)
    {
        string testCode = """
            using System;

            // {%name%}
            {%primaryConstructorCode%}
            """
            .Replace("{%name%}", name, StringComparison.Ordinal)
            .Replace("{%primaryConstructorCode%}", primaryConstructorCode, StringComparison.Ordinal);

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
            {{Kuk0006TargetSyntaxOption.KEY}} = {{Kuk0006TargetSyntaxOption.PRIMARY_CONSTRUCTOR}}
            """
        ));

        if (!(startLine == 0 && startColumn == 0 && endLine == 0 && endColumn == 0))
        {
            DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning)
                .WithSpan(startLine, startColumn, endLine, endColumn);

            test.ExpectedDiagnostics.Add(expected);
        }

        await test.RunAsync();
    }

    [Theory]
    [InlineData(null, true, true, true, true, true)]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_INVOCATION},{Kuk0006TargetSyntaxOption.OBJECT_CREATION}", true, true, true, false, false)]
    [InlineData($"{Kuk0006TargetSyntaxOption.OBJECT_CREATION},{Kuk0006TargetSyntaxOption.METHOD_INVOCATION}", true, true, true, false, false)]
    [InlineData(Kuk0006TargetSyntaxOption.METHOD_INVOCATION, true, false, false, false, false)]
    [InlineData(Kuk0006TargetSyntaxOption.OBJECT_CREATION, false, true, true, false, false)]
    [InlineData(Kuk0006TargetSyntaxOption.METHOD_DECLARATION, false, false, false, true, false)]
    [InlineData(Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION, false, false, false, false, true)]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_INVOCATION},{Kuk0006TargetSyntaxOption.METHOD_DECLARATION}", true, false, false, true, false)]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_DECLARATION},{Kuk0006TargetSyntaxOption.OBJECT_CREATION}", false, true, true, true, false)]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_DECLARATION},{Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION}", false, false, false, true, true)]
    public async Task ReportDependingOnTargetSyntaxOptionAsync(
        string? targetSyntax,
        bool expectMethodInvocationDiagnostic,
        bool expectObjectCreationDiagnostic,
        bool expectImplicitObjectCreationDiagnostic,
        bool expectMethodDeclarationDiagnostic,
        bool expectConstructorDeclarationDiagnostic)
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

                    Item fromImplicitCreation = new(
                        2,
                        "Two"{|#2:)|};

                    return fromInvocation + fromCreation.Id + fromImplicitCreation.Id;
                }

                public void Declared(
                    int a,
                    int b{|#3:)|}
                {
                }

                public TestClass(
                    int a,
                    int b{|#4:)|}
                {
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
                {{Kuk0006TargetSyntaxOption.KEY}} = {{targetSyntax}}
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

        if (expectImplicitObjectCreationDiagnostic)
        {
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(2));
        }

        if (expectMethodDeclarationDiagnostic)
        {
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(3));
        }

        if (expectConstructorDeclarationDiagnostic)
        {
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(4));
        }

        await test.RunAsync();
    }

    [Theory]
    [InlineData("foobar")]
    [InlineData("INVALID")]
    [InlineData(",")]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_INVOCATION},")]
    [InlineData($",{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_INVOCATION},,{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_INVOCATION}/{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_INVOCATION}|{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_INVOCATION},{Kuk0006TargetSyntaxOption.METHOD_INVOCATION},{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
    [InlineData("  bad value  ")]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_DECLARATION},")]
    [InlineData($",{Kuk0006TargetSyntaxOption.METHOD_DECLARATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_DECLARATION},,{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.METHOD_DECLARATION},{Kuk0006TargetSyntaxOption.METHOD_DECLARATION},{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION},")]
    [InlineData($",{Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION},,{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
    [InlineData($"{Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION},{Kuk0006TargetSyntaxOption.CONSTRUCTOR_DECLARATION},{Kuk0006TargetSyntaxOption.OBJECT_CREATION}")]
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
            {{Kuk0006TargetSyntaxOption.KEY}} = {{invalidTargetSyntax}}
            """
        ));

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticIdContant.KUK0006, DiagnosticSeverity.Warning).WithLocation(0).WithArguments(invalidTargetSyntax.Trim()));

        await test.RunAsync();
    }
}
