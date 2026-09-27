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

public class Kuk0008ConstructorMappingOrderCodeFixProviderTests
{
    [Fact]
    public async Task CodeFixReordersFieldsAndAssignmentsToMatchParameterOrderAsync()
    {
        string testCode = """
            public class Point
            {
                private readonly int _x;
                private readonly int _y;

                public Point(int y, int x)
                {
                    _x = x;
                    {|#0:_y = y;|}
                }
            }
            """;

        string fixedCode = """
            public class Point
            {
                private readonly int _y;
                private readonly int _x;

                public Point(int y, int x)
                {
                    _y = y;
                    _x = x;
                }
            }
            """;

        await RunAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task CodeFixReordersOnlyAssignmentsWhenFieldOrderAlreadyMatchesParametersAsync()
    {
        string testCode = """
            public class Point
            {
                private readonly int _y;
                private readonly int _x;

                public Point(int y, int x)
                {
                    _x = x;
                    {|#0:_y = y;|}
                }
            }
            """;

        string fixedCode = """
            public class Point
            {
                private readonly int _y;
                private readonly int _x;

                public Point(int y, int x)
                {
                    _y = y;
                    _x = x;
                }
            }
            """;

        await RunAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task CodeFixPreservesXmlDocCommentsWhenReorderingFieldsAsync()
    {
        string testCode = """
            public class Point
            {
                /// <summary>
                /// X coordinate.
                /// </summary>
                private readonly int _x;

                /// <summary>
                /// Y coordinate.
                /// </summary>
                private readonly int _y;

                public Point(int y, int x)
                {
                    _x = x;
                    {|#0:_y = y;|}
                }
            }
            """;

        string fixedCode = """
            public class Point
            {
                /// <summary>
                /// Y coordinate.
                /// </summary>
                private readonly int _y;

                /// <summary>
                /// X coordinate.
                /// </summary>
                private readonly int _x;

                public Point(int y, int x)
                {
                    _y = y;
                    _x = x;
                }
            }
            """;

        await RunAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task CodeFixReordersThreeFieldsToMatchParameterOrderAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly string _email;

                public User(string email, int age, string name)
                {
                    _name = name;
                    {|#0:_age = age;|}
                    _email = email;
                }
            }
            """;

        string fixedCode = """
            public class User
            {
                private readonly string _email;
                private readonly int _age;
                private readonly string _name;

                public User(string email, int age, string name)
                {
                    _email = email;
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task CodeFixPreservesAttributesWhenReorderingFieldsAsync()
    {
        string testCode = """
            using System;

            public class Point
            {
                [Obsolete("x is obsolete")]
                private readonly int _x;

                [Obsolete("y is obsolete")]
                private readonly int _y;

                public Point(int y, int x)
                {
                    _x = x;
                    {|#0:_y = y;|}
                }
            }
            """;

        string fixedCode = """
            using System;

            public class Point
            {
                [Obsolete("y is obsolete")]
                private readonly int _y;

                [Obsolete("x is obsolete")]
                private readonly int _x;

                public Point(int y, int x)
                {
                    _y = y;
                    _x = x;
                }
            }
            """;

        await RunAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task CodeFixIsNotOfferedWhenClassHasMultipleConstructorsAsync()
    {
        string testCode = """
            public class Point
            {
                private readonly int _x;
                private readonly int _y;

                public Point(int x, int y)
                {
                    _x = x;
                    _y = y;
                }

                public Point(int x, int y, int c)
                {
                    _y = y;
                    {|#0:_x = x;|}
                }
            }
            """;

#pragma warning disable KUK0001 // Duplicate arguments passed to method
        await RunAsync(testCode, testCode);
#pragma warning restore KUK0001 // Duplicate arguments passed to method
    }

    [Fact]
    public async Task CodeFixIsNotOfferedWhenConstructorsHaveConflictingParameterOrderAsync()
    {
        string testCode = """
            public class Point
            {
                private readonly int _x;
                private readonly int _y;

                public Point(int x, int y)
                {
                    _x = x;
                    _y = y;
                }

                public Point(int y, int x, int c)
                {
                    _x = x;
                    {|#0:_y = y;|}
                }
            }
            """;

#pragma warning disable KUK0001 // Duplicate arguments passed to method
        await RunAsync(testCode, testCode);
#pragma warning restore KUK0001 // Duplicate arguments passed to method
    }

    [Fact]
    public async Task CodeFixReordersAssignmentsAndIgnoresUnrelatedStatementsInBetweenAsync()
    {
        string testCode = """
            using System;

            public class Point
            {
                private readonly int _y;
                private readonly int _x;

                public Point(int x, int y)
                {
                    if (x < 0) throw new ArgumentException();
                    _y = y;
                    {|#0:_x = x;|}
                }
            }
            """;

        string fixedCode = """
            using System;

            public class Point
            {
                private readonly int _x;
                private readonly int _y;

                public Point(int x, int y)
                {
                    if (x < 0) throw new ArgumentException();
                    _x = x;
                    _y = y;
                }
            }
            """;

        await RunAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task CodeFixIsNotOfferedWhenFieldsAreDeclaredInSameStatementAsync()
    {
        string testCode = """
            public class Point
            {
                private readonly int _y, _x;

                public Point(int y, int x)
                {
                    _x = x;
                    {|#0:_y = y;|}
                }
            }
            """;

#pragma warning disable KUK0001 // Duplicate arguments passed to method
        await RunAsync(testCode, testCode);
#pragma warning restore KUK0001 // Duplicate arguments passed to method
    }

    [Fact]
    public async Task CodeFixReordersOnlyFieldsFromSamePartialBlockAsConstructorAsync()
    {
        string testCode = """
            using System;

            public partial class User
            {
                private readonly Guid _id;
            }

            public partial class User
            {
                private readonly int _age;
                private readonly string _name;

                public User(string name, int age)
                {
                    _age = age;
                    {|#0:_name = name;|}
                }
            }
            """;

        string fixedCode = """
            using System;

            public partial class User
            {
                private readonly Guid _id;
            }

            public partial class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task CodeFixIsNotOfferedWhenFieldsAreDeclaredInAnotherPartialFileThanTheConstructorAsync()
    {
        string fieldsFile = """
            public partial class User
            {
                private readonly int _age;
                private readonly string _name;
            }
            """;

        string constructorFile = """
            public partial class User
            {
                public User(string name, int age)
                {
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunNoDiagnosticAsync([fieldsFile, constructorFile]);
    }

    [Fact]
    public async Task CodeFixIsNotOfferedWhenTypeIsStructAsync()
    {
        string testCode = """
            public struct Point
            {
                private readonly int _x;
                private readonly int _y;

                public Point(int x, int y)
                {
                    _y = y;
                    _x = x;
                }
            }
            """;

        await RunNoDiagnosticAsync([testCode]);
    }

    [Fact]
    public async Task CodeFixIsNotOfferedWhenClassHasSequentialStructLayoutAsync()
    {
        string testCode = """
            using System.Runtime.InteropServices;

            [StructLayout(LayoutKind.Sequential)]
            public class Point
            {
                private readonly int _x;
                private readonly int _y;

                public Point(int x, int y)
                {
                    _y = y;
                    _x = x;
                }
            }
            """;

        await RunNoDiagnosticAsync([testCode]);
    }

    [Fact]
    public async Task CodeFixIsNotOfferedWhenClassHasExplicitStructLayoutAsync()
    {
        string testCode = """
            using System.Runtime.InteropServices;

            [StructLayout(LayoutKind.Explicit)]
            public class Point
            {
                [FieldOffset(0)]
                private readonly int _x;
                [FieldOffset(4)]
                private readonly int _y;

                public Point(int x, int y)
                {
                    _y = y;
                    _x = x;
                }
            }
            """;

        await RunNoDiagnosticAsync([testCode]);
    }

    [Fact]
    public async Task CodeFixDoesNotTouchStaticFieldAssignedWithSameParameterNamePatternAsync()
    {
        string testCode = """
            public class User
            {
                private static int s_age;
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _age = age;
                    {|#0:_name = name;|}
                    s_age = age;
                }
            }
            """;

        string fixedCode = """
            public class User
            {
                private static int s_age;
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _name = name;
                    _age = age;
                    s_age = age;
                }
            }
            """;

        await RunAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task CodeFixDoesNotTouchAssignmentToLocalVariableShadowingFieldAsync()
    {
        string testCode = """
            public class User
            {
                private string _second;
                private string _third;
                private string _first;

                public User(string third, string second, string first)
                {
                    string _first;
                    _third = third;
                    {|#0:_second = second;|}
                    _first = first;
                }
            }
            """;

        string fixedCode = """
            public class User
            {
                private string _third;
                private string _second;
                private string _first;

                public User(string third, string second, string first)
                {
                    string _first;
                    _third = third;
                    _second = second;
                    _first = first;
                }
            }
            """;

        await RunAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task CodeFixIsNotOfferedWhenFieldIsDeclaredWithMultipleVariablesAsync()
    {
        string testCode = """
            public class Point
            {
                private readonly int _x, _y;

                public Point(int y, int x)
                {
                    _x = x;
                    {|#0:_y = y;|}
                }
            }
            """;

#pragma warning disable KUK0001 // Duplicate arguments passed to method
        await RunAsync(testCode, testCode);
#pragma warning restore KUK0001 // Duplicate arguments passed to method
    }

    [Fact]
    public async Task CodeFixIsNotOfferedWhenAnyFieldIsDeclaredWithMultipleVariablesAsync()
    {
        string testCode = """
            public class Point
            {
                private readonly int _x, _unused;
                private readonly int _y;

                public Point(int x, int y)
                {
                    _unused = 0;
                    _y = y;
                    {|#0:_x = x;|}
                }
            }
            """;

#pragma warning disable KUK0001 // Duplicate arguments passed to method
        await RunAsync(testCode, testCode);
#pragma warning restore KUK0001 // Duplicate arguments passed to method
    }

    private static async Task RunAsync(string testCode, string fixedCode)
    {
        CSharpCodeFixTest<Kuk0008ConstructorMappingOrderAnalyzer, Kuk0008ConstructorMappingOrderCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0008, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    private static async Task RunNoDiagnosticAsync(string[] testSources)
    {
        CSharpCodeFixTest<Kuk0008ConstructorMappingOrderAnalyzer, Kuk0008ConstructorMappingOrderCodeFixProvider, DefaultVerifier> test = new()
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        for (int i = 0; i < testSources.Length; i++)
        {
            string fileName = $"/0/Test{i}.cs";
            test.TestState.Sources.Add((fileName, testSources[i]));
            test.FixedState.Sources.Add((fileName, testSources[i]));
        }

        await test.RunAsync();
    }
}
