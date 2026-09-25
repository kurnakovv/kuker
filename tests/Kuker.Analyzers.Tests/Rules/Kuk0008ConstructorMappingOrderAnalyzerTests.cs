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

        await RunAsync(testCode, 13, 9, 13, 13);
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
    public async Task ReportWhenAssignmentUsesThisQualifierAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    this._age = age;
                    this._name = name;
                }
            }
            """;

        await RunAsync(testCode, 9, 9, 9, 19);
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

        await RunAsync(testCode, 13, 9, 13, 13);
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

        await RunAsync(testCode, 13, 9, 13, 12);
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

        await RunAsync(testCode, 20, 9, 20, 13);
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

        await RunAsync(testCode, 12, 9, 12, 13);
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
                    _age = age;
                    _name = name.Trim();
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAssignmentUsesCastExpressionAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(object name, int age)
                {
                    _age = age;
                    _name = (string)name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAssignmentUsesNullCoalescingExpressionAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _age = age;
                    _name = name ?? "";
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAssignmentUsesMethodCallOnAnotherTypeAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _age = age;
                    _name = string.Copy(name);
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAssignmentUsesNullForgivingOperatorAsync()
    {
        string testCode = """
            #nullable enable

            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string? name, int age)
                {
                    _age = age;
                    _name = name!;
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
                    _age = age;
                    _name = "default";
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

        await RunAsync(testCode, 10, 9, 10, 14);
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

        await RunAsync(testCode, 10, 9, 10, 14);
    }

    [Fact]
    public async Task NoReportWhenParameterHasDefaultValueAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age = 18)
                {
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenParameterHasDefaultValueAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age = 18)
                {
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, 9, 9, 9, 14);
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
    public async Task NoReportWhenFieldIsAssignedFromLocalVariableInsteadOfParameterDirectlyAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    var value = name;
                    _name = value;
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
                private string _name;
                private int _age;

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

        await RunAsync(testCode, 11, 9, 11, 14);
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

        await RunAsync(testCode, 20, 9, 20, 13);
    }

    [Fact]
    public async Task NoReportWhenThisInitializerPassesParameterAndBodyOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name)
                {
                    _name = name;
                }

                public User(string name, int age)
                    : this(name)
                {
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenThisInitializerPassesParameterAndOnlyOneAssignmentInBodyAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(int age)
                {
                    _age = age;
                }

                public User(string name, int age)
                    : this(age)
                {
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenDerivedClassOrderMismatchesIgnoringBaseClassFieldAsync()
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

        await RunAsync(testCode, 20, 9, 20, 13);
    }

    [Fact]
    public async Task ReportWhenDerivedClassOwnFieldOrderDoesNotMatchIgnoringBaseFieldAsync()
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
                private readonly string _email;
                private readonly int _age;

                public User(string name, string email, int age)
                    : base(name)
                {
                    _age = age;
                    _email = email;
                }
            }
            """;

        await RunAsync(testCode, 20, 9, 20, 15);
    }

    [Fact]
    public async Task NoReportWhenBaseAndDerivedParametersShareSameNameAndOrderMatchesAsync()
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
                private readonly string _name;

                public User(string name)
                    : base(name)
                {
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenPrivateAndProtectedFieldsAreOrderedCorrectlyAcrossHierarchyAsync()
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
    public async Task ReportWhenConstructorChainingBetweenBaseAndDerivedHasDerivedOrderMismatchAsync()
    {
        string testCode = """
            public class Person
            {
                protected readonly string _name;
                protected readonly int _age;

                public Person(string name, int age)
                {
                    _age = age;
                    _name = name;
                }
            }

            public class User : Person
            {
                private readonly string _email;

                public User(string name, int age, string email)
                    : base(name, age)
                {
                    _email = email;
                }
            }
            """;

        await RunAsync(testCode, 9, 9, 9, 14);
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

        await RunAsync(testCode, 9, 9, 9, 11);
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

        await RunAsync(testCode, 9, 9, 9, 14);
    }

    [Fact]
    public async Task ReportWhenOverloadParameterAndAssignmentOrderDoesNotMatchFieldOrderAsync()
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

        await RunAsync(testCode, 15, 9, 15, 11);
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

        await RunAsync(testCode, 15, 9, 15, 11);
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

        await RunAsync(testCode, 9, 9, 9, 14);
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

        await RunAsync(testCode, 9, 9, 9, 11);
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

        await RunAsync(testCode, 11, 9, 11, 13);
    }

    [Fact]
    public async Task NoReportWhenUnrelatedLogicBetweenAssignmentsAndOrderMatchesAsync()
    {
        string testCode = """
            using System;

            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _name = name;
                    Console.WriteLine("Creating user");
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenUnrelatedLogicBetweenAssignmentsAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            using System;

            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _age = age;
                    Console.WriteLine("Creating user");
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, 12, 9, 12, 14);
    }

    [Fact]
    public async Task NoReportWhenOnlySingleFieldIsAssignedAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;

                public User(string name)
                {
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenNoFieldsExistButConstructorHasParametersAsync()
    {
        string testCode = """
            public class User
            {
                public User(string name, int age)
                {
                    var displayName = name;
                    var displayAge = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenMoreParametersThanFieldsAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age, string comment)
                {
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenMoreParametersThanFieldsAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age, string comment)
                {
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, 9, 9, 9, 14);
    }

    [Fact]
    public async Task NoReportWhenMoreFieldsThanParametersAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly string _comment;

                public User(string name, int age)
                {
                    _name = name;
                    _age = age;
                    _comment = string.Empty;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenMoreFieldsThanParametersAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly string _comment;

                public User(string name, int age)
                {
                    _age = age;
                    _name = name;
                    _comment = string.Empty;
                }
            }
            """;

        await RunAsync(testCode, 10, 9, 10, 14);
    }

    [Fact]
    public async Task NoReportWhenSomeFieldsAreParameterInitializedAndOthersAreNotAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly string _id;

                public User(string name, int age)
                {
                    _name = name;
                    _age = age;
                    _id = System.Guid.NewGuid().ToString();
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenSomeFieldsAreParameterInitializedAndOthersAreNotAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;
                private readonly string _id;

                public User(string name, int age)
                {
                    _age = age;
                    _name = name;
                    _id = System.Guid.NewGuid().ToString();
                }
            }
            """;

        await RunAsync(testCode, 10, 9, 10, 14);
    }

    [Fact]
    public async Task NoReportWhenAssignmentsAreOnSameLineAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _name = name; _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenAssignmentsAreOnSameLineAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _age = age; _name = name;
                }
            }
            """;

        await RunAsync(testCode, 8, 21, 8, 26);
    }

    [Fact]
    public async Task NoReportWhenParametersAreMultilineAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(
                    string name,
                    int age)
                {
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenParametersAreMultilineAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(
                    string name,
                    int age)
                {
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, 11, 9, 11, 14);
    }

    [Fact]
    public async Task NoReportWhenSimilarlyNamedFieldsAndParametersOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly string _displayName;

                public User(string name, string displayName)
                {
                    _name = name;
                    _displayName = displayName;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenSimilarlyNamedFieldsAndParametersOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly string _displayName;

                public User(string name, string displayName)
                {
                    _displayName = displayName;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, 9, 9, 9, 14);
    }

    [Fact]
    public async Task NoReportWhenFieldIsReassignedAfterDirectParameterMappingAndOrderMatchesAsync()
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
                    _name = "override";
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenFieldIsReassignedAfterDirectParameterMappingAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _age = age;
                    _name = name;
                    _name = "override";
                }
            }
            """;

        await RunAsync(testCode, 9, 9, 9, 14);
    }

    [Fact]
    public async Task NoReportWhenFieldIsAssignedNonParameterValueBeforeDirectParameterMappingAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    _name = "default";
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenFieldIsAssignedNonParameterValueBeforeDirectParameterMappingAndOrderDoesNotMatchAsync()
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
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, 10, 9, 10, 14);
    }

    [Fact]
    public async Task NoReportWhenAssignmentInsideIfIsFollowedByUnconditionalAssignmentAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age, bool condition)
                {
                    _age = age;

                    if (condition)
                        _name = name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenUnconditionalAssignmentIsFollowedByAssignmentInsideIfAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age, bool condition)
                {
                    if (condition)
                        _age = age;

                    _name = name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenBothAssignmentsAreInsideIfBlockRegardlessOfOrderAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age, bool condition)
                {
                    if (condition)
                    {
                        _age = age;
                        _name = name;
                    }
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAssignmentInsideSwitchIsIgnoredAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age, int mode)
                {
                    switch (mode)
                    {
                        case 1:
                            _age = age;
                            break;
                    }

                    _name = name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAssignmentInsideTryCatchIsIgnoredAsync()
    {
        string testCode = """
            using System;

            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age)
                {
                    try
                    {
                        _age = age;
                    }
                    catch (Exception)
                    {
                    }

                    _name = name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenAssignmentInsideLoopIsIgnoredAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                public User(string name, int age, int count)
                {
                    for (int i = 0; i < count; i++)
                    {
                        _age = age;
                    }

                    _name = name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task NoReportWhenStaticFieldPrecedesInstanceFieldsAndInstanceOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private static readonly int _version;
                private readonly int _age;

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
    public async Task ReportWhenStaticFieldPrecedesInstanceFieldsAndInstanceOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private static readonly int _version;
                private readonly int _age;

                public User(string name, int age)
                {
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, 10, 9, 10, 14);
    }

    [Fact]
    public async Task NoReportWhenFieldsAndConstructorAreProtectedAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                protected readonly string _name;
                protected readonly int _age;

                protected User(string name, int age)
                {
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenFieldsAndConstructorAreProtectedAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                protected readonly string _name;
                protected readonly int _age;

                protected User(string name, int age)
                {
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, 9, 9, 9, 14);
    }

    [Fact]
    public async Task NoReportWhenFieldsAndConstructorAreInternalAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                internal readonly string _name;
                internal readonly int _age;

                internal User(string name, int age)
                {
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenFieldsAndConstructorAreInternalAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                internal readonly string _name;
                internal readonly int _age;

                internal User(string name, int age)
                {
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, 9, 9, 9, 14);
    }

    [Fact]
    public async Task NoReportWhenFieldsAndConstructorArePrivateAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                private User(string name, int age)
                {
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenFieldsAndConstructorArePrivateAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly string _name;
                private readonly int _age;

                private User(string name, int age)
                {
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode, 9, 9, 9, 14);
    }

    [Fact]
    public async Task NoReportWhenParameterTypesIncludeObjectAndNullableAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly object _tag;
                private readonly int? _age;
                private readonly string _name;

                public User(object tag, int? age, string name)
                {
                    _tag = tag;
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenParameterTypesIncludeObjectAndNullableAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly object _tag;
                private readonly int? _age;
                private readonly string _name;

                public User(object tag, int? age, string name)
                {
                    _tag = tag;
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode, 11, 9, 11, 13);
    }

    [Fact]
    public async Task NoReportWhenParameterUsesRefModifierAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly int _age;
                private readonly string _name;

                public User(ref int age, string name)
                {
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenParameterUsesRefModifierAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly int _age;
                private readonly string _name;

                public User(ref int age, string name)
                {
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode, 9, 9, 9, 13);
    }

    [Fact]
    public async Task NoReportWhenParameterUsesInModifierAndOrderMatchesAsync()
    {
        string testCode = """
            public class User
            {
                private readonly int _age;
                private readonly string _name;

                public User(in int age, string name)
                {
                    _age = age;
                    _name = name;
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Fact]
    public async Task ReportWhenParameterUsesInModifierAndOrderDoesNotMatchAsync()
    {
        string testCode = """
            public class User
            {
                private readonly int _age;
                private readonly string _name;

                public User(in int age, string name)
                {
                    _name = name;
                    _age = age;
                }
            }
            """;

        await RunAsync(testCode, 9, 9, 9, 13);
    }

    [Fact]
    public async Task ReportWhenParameterUsesOutModifierAndOutFieldIsIgnoredAsync()
    {
        string testCode = """
            public class User
            {
                private readonly int _age;
                private readonly string _name;

                public User(string name, int age, out bool success)
                {
                    _age = age;
                    _name = name;
                    success = true;
                }
            }
            """;

        await RunAsync(testCode, 9, 9, 9, 14);
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
