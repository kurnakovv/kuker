// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Kuker.Core.Contants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.Analyzers.Tests.Rules;

public class Kuk0008ConstructorMappingOrderAnalyzerTests
{
    [Fact]
    public async Task NoReportWhenFieldConstructorParameterAndAssignmentOrderMatchAsync()
    {
        string testCode = """
            using System;

            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly Guid _id;

                public User(string name, int age, Guid id)
                {
                    _name = name;
                    _age = age;
                    _id = id;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenConstructorParameterAndAssignmentOrderDoNotMatchFieldOrderAsync()
    {
        string testCode = """
            using System;

            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly Guid _id;

                public User(string name, Guid id, int age)
                {
                    _name = name;
                    _id = id;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode, 9, 12, 9, 16);
    }

    [Fact]
    public async Task NoReportWhenFieldNotInitializedFromConstructorParameterIsIgnoredAsync()
    {
        string testCode = """
            using System;

            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly Guid _cachedId;

                public User(string name, int age)
                {
                    _name = name;
                    _age = age;
                    _cachedId = Guid.NewGuid();
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAssignmentUsesThisQualifierAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    this._name = name;
                    this._age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenParameterOrderMatchesFieldsButAssignmentOrderDoesNotAsync()
    {
        string testCode = """
            using System;

            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly Guid _id;

                public User(string name, int age, Guid id)
                {
                    _name = name;
                    _id = id;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode, 9, 12, 9, 16);
    }

    [Fact]
    public async Task ReportWhenAssignmentOrderMatchesFieldsButParameterOrderDoesNotAsync()
    {
        string testCode = """
            using System;

            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly Guid _id;

                public User(string name, Guid id, int age)
                {
                    _name = name;
                    _age = age;
                    _id = id;
                }
            }
            """;

        await RunAsync(testCode, 9, 12, 9, 16);
    }

    [Fact]
    public async Task ReportOnlyForMismatchedOverloadWhenMultipleConstructorsExistAsync()
    {
        string testCode = """
            using System;

            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly Guid _id;

                public User(string name, int age, Guid id)
                {
                    _name = name;
                    _age = age;
                    _id = id;
                }

                public User(string name, Guid id, int age)
                {
                    _name = name;
                    _id = id;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode, 16, 12, 16, 16);
    }

    [Fact]
    public async Task ReportWhenFieldOrderDoesNotMatchParameterAndAssignmentOrderAsync()
    {
        string testCode = """
            using System;

            public class User
            {
                private readonly int _age;
                private readonly string _name;
                private readonly Guid _id;

                public User(string name, int age, Guid id)
                {
                    _name = name;
                    _age = age;
                    _id = id;
                }
            }
            """;

        await RunAsync(testCode, 9, 12, 9, 16);
    }

    [Fact]
    public async Task NoReportWhenConstructorDoesNotAssignAnyFieldFromParameterAsync()
    {
        string testCode = """
            using System;

            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly Guid _id;

                public User()
                {
                    _name = string.Empty;
                    _age = 0;
                    _id = Guid.Empty;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAssignmentIsNotSimpleParameterAssignmentAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _name = name.Trim();
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenFieldIsAssignedFromLiteralNotParameterAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _name = "default";
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenOrderIsMismatchedAlongsideNonSimpleAssignmentsAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly string _email;

                public User(string name, int age, string email)
                {
                    _age = age;
                    _name = name;
                    _email = email.Trim();
                }
            }
            """;

        await RunAsync(testCode, 9, 12, 9, 16);
    }

    [Fact]
    public async Task ReportWhenOrderIsMismatchedAlongsideLiteralAssignmentAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly string _email;

                public User(string name, int age, string email)
                {
                    _age = age;
                    _name = name;
                    _email = "default";
                }
            }
            """;

        await RunAsync(testCode, 9, 12, 9, 16);
    }

    [Fact]
    public async Task NoReportWhenParameterIsAssignedToLocalVariableNotFieldAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    var x = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenMismatchedAssignmentHappensOutsideConstructorAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _name = name;
                    _age = age;
                }

                public void Init(string name, int age)
                {
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenConstructorUsesThisInitializerAndBodyOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                    : this()
                {
                    _name = name;
                    _age = age;
                }

                public User()
                {
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenConstructorUsesBaseInitializerAndBodyOrderMatchesAsync()
    {
        string testCode = """
            public class Person
            {
                protected readonly string _name;

                public Person(string name)
                {
                    _name = name;
                }
            }

            public class User : Person
            {
                private readonly int _age;

                public User(string name, int age)
                    : base(name)
                {
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenConstructorUsesThisInitializerAndBodyOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly string _email;

                public User(string name, int age, string email)
                    : this()
                {
                    _age = age;
                    _name = name;
                    _email = email;
                }

                public User()
                {
                }
            }
            """;

        await RunAsync(testCode, 10, 12, 10, 15);
    }

    [Fact]
    public async Task ReportWhenConstructorUsesBaseInitializerAndBodyOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class Person
            {
                protected readonly string _name;

                public Person(string name)
                {
                    _name = name;
                }
            }

            public class User : Person
            {
                private readonly int _age;
                private readonly string _email;

                public User(string name, int age, string email)
                    : base(name)
                {
                    _email = email;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode, 19, 12, 19, 17);
    }

    [Fact]
    public async Task NoReportWhenStaticConstructorIsAnalyzedAsync()
    {
        string testCode = """
            public class User
            {
                private static readonly int _age;
                private static readonly string _name;

                static User()
                {
                    _name = "default";
                    _age = 0;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenStructConstructorOrderDoesNotMatchFieldOrderAsync()
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

        await RunAsync(testCode, 7, 12, 7, 15);
    }

    [Fact]
    public async Task NoReportWhenRecordPrimaryConstructorIsUsedAsync()
    {
        string testCode = """
            public record Person(string Name, int Age);
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenRecordExplicitConstructorOrderDoesNotMatchFieldOrderAsync()
    {
        string testCode = """
            public record Person
            {
                private readonly string _name;
                private readonly int _age;

                public Person(string name, int age)
                {
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, 7, 12, 7, 16);
    }

    [Fact]
    public async Task NoReportWhenOverloadsAssignSameFieldsWithDifferentButConsistentOrderAsync()
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

                public Point(int y, int x, bool swapped)
                {
                    _y = y;
                    _x = x;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportOnlyForOverloadWithMismatchedOrderWhenOthersAreConsistentAsync()
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

                public Point(int y, int x, bool swapped)
                {
                    _x = x;
                    _y = y;
                }
            }
            """;

        await RunAsync(testCode, 14, 12, 14, 15);
    }

    [Fact]
    public async Task NoReportWhenNonReadonlyFieldOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private string _name;
                private int _age;

                public User(string name, int age)
                {
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenNonReadonlyFieldOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private string _name;
                private int _age;

                public User(string name, int age)
                {
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, 9, 12, 9, 16);
    }

    [Fact]
    public async Task NoReportWhenMultipleFieldsDeclaredOnSameLineAndOrderMatchesAsync()
    {
        string testCode = """
            public class Point
            {
                private readonly int _a, _b;
                private readonly string _c;

                public Point(int a, int b, string c)
                {
                    _a = a;
                    _b = b;
                    _c = c;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenMultipleFieldsDeclaredOnSameLineAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class Point
            {
                private readonly int _a, _b;
                private readonly string _c;

                public Point(int a, int b, string c)
                {
                    _b = b;
                    _a = a;
                    _c = c;
                }
            }
            """;

        await RunAsync(testCode, 9, 12, 9, 15);
    }

    [Fact]
    public async Task ReportWhenOnlyOneFieldOutOfThreeIsMisorderedAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly string _email;

                public User(string name, int age, string email)
                {
                    _name = name;
                    _email = email;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode, 10, 12, 10, 18);
    }

    private static async Task RunAsync(
        string testCode,
        int startLine = 0,
        int startColumn = 0,
        int endLine = 0,
        int endColumn = 0
    )
    {
        CSharpAnalyzerTest<Kuk0008ConstructorMappingOrderAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        if (startLine > 0)
        {
            DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0008, DiagnosticSeverity.Warning)
                .WithSpan(startLine, startColumn, endLine, endColumn);

            test.ExpectedDiagnostics.Add(expected);
        }

        await test.RunAsync();
    }
}
