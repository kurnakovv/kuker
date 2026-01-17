// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.Analyzers.Tests.Rules;

public class Kuk0003MinMaxOnEmptySequenceAnalyzerTests
{
    private readonly PortableExecutableReference _portableExecutableReference;

    public Kuk0003MinMaxOnEmptySequenceAnalyzerTests()
    {
        _portableExecutableReference = MetadataReference.CreateFromFile(typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.Location);
    }

    [Fact]
    public async Task ReportForMinMaxMethodsAsync()
    {
        string testCode = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            using Microsoft.EntityFrameworkCore;

            public class UserDto
            {
                public string Name { get; set; }
                public int Age { get; set; }
                public bool Gender { get; set; }
            }

            public class TestClass
            {
                public async Task M1()
                {
                    var users = new List<UserDto>();
                    var ct = CancellationToken.None;

                    var minAgeViolation = users.Min(x => x.Age); // Violation
                    var minAgeOK = users.Min(x => (int?)x.Age); // OK

                    var minAsyncAgeViolation = await users.AsQueryable().MinAsync(x => x.Age); // Violation
                    var minAsyncAgeWithTokenViolation = await users.AsQueryable().MinAsync(x => x.Age, ct); // Violation
                    var minAsyncAgeOK = await users.AsQueryable().MinAsync(x => (int?)x.Age); // OK
                    var minAsyncAgeWithTokenOK = await users.AsQueryable().MinAsync(x => (int?)x.Age, ct); // OK


                    var maxAgeViolation = users.Max(x => x.Age); // Violation
                    var maxAgeOK = users.Max(x => (int?)x.Age); // OK

                    var maxAsyncAgeViolation = await users.AsQueryable().MaxAsync(x => x.Age); // Violation
                    var maxAsyncAgeWithTokenViolation = await users.AsQueryable().MaxAsync(x => x.Age, ct); // Violation
                    var maxAsyncAgeOK = await users.AsQueryable().MaxAsync(x => (int?)x.Age); // OK
                    var maxAsyncAgeWithTokenOK = await users.AsQueryable().MaxAsync(x => (int?)x.Age, ct); // OK


                    var minAsyncAgeWithSelectViolation = await users.AsQueryable().Select(x => x.Age).MinAsync(); // Violation
                    var minAsyncAgeWithSelectAndTokenViolation = await users.AsQueryable().Select(x => x.Age).MinAsync(ct); // Violation
                    var minAsyncAgeWithSelectOK = await users.AsQueryable().Select(x => (int?)x.Age).MinAsync(); // OK
                    var minAsyncAgeWithSelectAndTokenOK = await users.AsQueryable().Select(x => (int?)x.Age).MinAsync(ct); // OK

                    var maxAsyncAgeWithSelectViolation = await users.AsQueryable().Select(x => x.Age).MaxAsync(); // Violation
                    var maxAsyncAgeWithSelectAndTokenViolation = await users.AsQueryable().Select(x => x.Age).MaxAsync(ct); // Violation
                    var maxAsyncAgeWithSelectOK = await users.AsQueryable().Select(x => (int?)x.Age).MaxAsync(); // OK
                    var maxAsyncAgeWithSelectAndTokenOK = await users.AsQueryable().Select(x => (int?)x.Age).MaxAsync(ct); // OK
                }
            }
        ";
        DiagnosticResult expected1 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(24, 53, 24, 63);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(27, 83, 27, 93);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(28, 92, 28, 102);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(33, 53, 33, 63);

        DiagnosticResult expected5 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(36, 83, 36, 93);

        DiagnosticResult expected6 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(37, 92, 37, 102);

        DiagnosticResult expected7 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(42, 64, 42, 113);

        DiagnosticResult expected8 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(47, 64, 47, 113);

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4, expected5, expected6, expected7, expected8, },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestState = { AdditionalReferences = { _portableExecutableReference }, },
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForNoMemberAccessExpressionSyntaxAsync()
    {
        string testCode = @"
            using System;
            using static System.Math;

            public class TestClass1
            {
                public void M1()
                {
                    var x = Max(1, 2);
                }
            }

            public class TestClass2
            {
                int Max(int a, int b) => a > b ? a : b;

                public void M1()
                {
                    var x = Max(1, 2);
                }
            }

            public class TestClass3
            {
                public void M1()
                {
                    Func<int, int, int> Max = Math.Max;
                    var x = Max(1, 2);
                }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForOtherMethodsAsync()
    {
        string testCode = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;

            public class TestClass
            {
                public void M1()
                {
                    var myList = new List<int>() { 1, 2, 3, 4, 5 };
                    var sum = myList.Sum();
                    var any = myList.Any();
                }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForNoMethodSymbolAsync()
    {
        string testCode = @"
            using System;

            class Test
            {
                void M(dynamic d)
                {
                    var x = d.Max(1, 2);
                }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForNonLinqMethodsAsync()
    {
        string testCode = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class UserDto
            {
                public string Name { get; set; }
                public int Age { get; set; }
                public bool Gender { get; set; }

                public int Min() { return 0; }
                public int Min(Func<UserDto, long?> selector) { return 0; }
                public int Min(int test) { return 0; }
                public int Min(Func<UserDto, long?> selector, int test) { return 0; }

                public async Task<int> MinAsync() { return 0; }
                public async Task<int> MinAsync(Func<UserDto, long?> selector) { return 0; }
                public async Task<int> MinAsync(CancellationToken ct) { return 0; }
                public async Task<int> MinAsync(Func<UserDto, long?> selector, CancellationToken ct) { return 0; }


                public int Max() { return 0; }
                public int Max(Func<UserDto, long?> selector) { return 0; }
                public int Max(int test) { return 0; }
                public int Max(Func<UserDto, long?> selector, int test) { return 0; }

                public async Task<int> MaxAsync() { return 0; }
                public async Task<int> MaxAsync(Func<UserDto, long?> selector) { return 0; }
                public async Task<int> MaxAsync(CancellationToken ct) { return 0; }
                public async Task<int> MaxAsync(Func<UserDto, long?> selector, CancellationToken ct) { return 0; }


                public int AnotherMethod() { return 0; }
                public int AnotherMethod(Func<UserDto, long?> selector) { return 0; }
                public int AnotherMethod(int test) { return 0; }
                public int AnotherMethod(Func<UserDto, long?> selector, int test) { return 0; }

                public async Task<int> AnotherMethodAsync() { return 0; }
                public async Task<int> AnotherMethodAsync(Func<UserDto, long?> selector) { return 0; }
                public async Task<int> AnotherMethodAsync(CancellationToken ct) { return 0; }
                public async Task<int> AnotherMethodAsync(Func<UserDto, long?> selector, CancellationToken ct) { return 0; }
            }

            public class CompanyDto
            {
                public int Salary { get; set; }
            }

            public static class CompanyDtoExtensions
            {
                public static int Max(this IEnumerable<CompanyDto> items, Func<UserDto, long?> selector) { return 0; }
                public static async Task<int> MaxAsync(this IEnumerable<CompanyDto> items, Func<UserDto, long?> selector, CancellationToken ct) { return 0; }
            }

            public class TestClass
            {
                public async Task M1()
                {
                    var userDto = new UserDto();
                    var ct = CancellationToken.None;

                    var minResult1 = userDto.Min();
                    var minResult2 = userDto.Min(x => x.Age);
                    var minResult3 = userDto.Min(1);
                    var minResult4 = userDto.Min(x => x.Age, 1);

                    var minResult5 = await userDto.MinAsync();
                    var minResult6 = await userDto.MinAsync(x => x.Age);
                    var minResult7 = await userDto.MinAsync(ct);
                    var minResult8 = await userDto.MinAsync(x => x.Age, ct);


                    var maxResult1 = userDto.Max();
                    var maxResult2 = userDto.Max(x => x.Age);
                    var maxResult3 = userDto.Max(1);
                    var maxResult4 = userDto.Max(x => x.Age, 1);

                    var maxResult5 = await userDto.MaxAsync();
                    var maxResult6 = await userDto.MaxAsync(x => x.Age);
                    var maxResult7 = await userDto.MaxAsync(ct);
                    var maxResult8 = await userDto.MaxAsync(x => x.Age, ct);


                    var anotherMethodResult1 = userDto.AnotherMethod();
                    var anotherMethodResult2 = userDto.AnotherMethod(x => x.Age);
                    var anotherMethodResult3 = userDto.AnotherMethod(1);
                    var anotherMethodResult4 = userDto.AnotherMethod(x => x.Age, 1);

                    var anotherMethodResult5 = await userDto.AnotherMethodAsync();
                    var anotherMethodResult6 = await userDto.AnotherMethodAsync(x => x.Age);
                    var anotherMethodResult7 = await userDto.AnotherMethodAsync(ct);
                    var anotherMethodResult8 = await userDto.AnotherMethodAsync(x => x.Age, ct);
                }

                public async Task M2()
                {
                    IEnumerable<CompanyDto> items = new List<CompanyDto>();
                    items.Max(x => x.Age);
                    await items.AsQueryable().MaxAsync(x => x.Age, CancellationToken.None);
                }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportDefaultIfEmptyAsync()
    {
        string testCode = @"
            using System.Collections.Generic;
            using System.Linq;
            using System;

            public class UserDto
            {
                public string Name { get; set; }
                public int Age { get; set; }
                public bool Gender { get; set; }
            }

            public class TestClass
            {
                public void M1()
                {
                    var users = new List<UserDto>();

                    var result1 = users.DefaultIfEmpty().Max(x => x.Age); // OK
                    var result2 = users.DefaultIfEmpty().Select(x => x.Age).Max(); // OK
                    var result3 = users.DefaultIfEmpty(new UserDto()).Select(x => x.Age).Max(); // OK
                    var result4 = users.Where(x => x.Gender).DefaultIfEmpty(new UserDto()).Select(x => x.Age).Max(); // OK

                    var usersWithDefaultIfEmpty = users.DefaultIfEmpty();
                    var copyUsersWithDefaultIfEmpty = usersWithDefaultIfEmpty;

                    var result01 = usersWithDefaultIfEmpty.Max(x => x.Age); // OK
                    var result02 = usersWithDefaultIfEmpty.Select(x => x.Age).Max(); // OK
                    var result03 = usersWithDefaultIfEmpty.Where(x => x.Gender).Select(x => x.Age).Max(); // OK
                    var result04 = copyUsersWithDefaultIfEmpty.Max(x => x.Age); // OK

                    var condition = false;
                    var conditionUsers = condition ? users.DefaultIfEmpty() : users;

                    var result001 = conditionUsers.Max(x => x.Age); // Violation

                    IEnumerable<UserDto> q;

                    if (condition)
                    {
                        q = users.DefaultIfEmpty();
                    }
                    else
                    {
                        q = users;
                    }

                    q.Max(x => x.Age); // Violation
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(35, 56, 35, 66);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(48, 27, 48, 37);

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected1, expected2, },
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForGroupByAsync()
    {
        string testCode = @"
            using System.Collections.Generic;
            using System.Linq;
            using System;

            public class UserDto
            {
                public string Name { get; set; }
                public int Age { get; set; }
                public bool Gender { get; set; }
            }

            public class TestClass
            {
                public void M1()
                {
                    var users = new List<UserDto>();

                    var result1 = users.GroupBy(x => x.Name).Max(); // OK
                    var result2 = users.GroupBy(x => x.Name).Max(x => x); // OK
                    var result3 = users.GroupBy(x => x.Name).Max(x => x.Key); // OK
                    var result4 = users.GroupBy(x => x.Name).Select(g => new { Key = g.Key, Max = g.Max(u => u.Age) }); // OK

                    var grouped = users.GroupBy(x => x.Name);
                    var result5 = grouped.Select(g => new { Key = g.Key, Max = g.Max(u => u.Age) }); // OK
                }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForMinByMaxByAsync()
    {
        string testCode = @"
            using System.Collections.Generic;
            using System.Linq;
            using System;

            public class UserDto
            {
                public string Name { get; set; }
                public int Age { get; set; }
                public bool Gender { get; set; }
            }

            public class TestClass
            {
                public void M1()
                {
                    var users = new List<UserDto>();

                    users.MinBy(x => x.Age); // Violation
                    users.MinBy(x => (int?)x.Age); // Violation
                    users.DefaultIfEmpty().MinBy(x => x.Age); // OK

                    users.MaxBy(x => x.Age); // Violation
                    users.MaxBy(x => (int?)x.Age); // Violation
                    users.DefaultIfEmpty().MaxBy(x => x.Age); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(19, 21, 19, 44);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(20, 21, 20, 50);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(23, 21, 23, 44);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(24, 21, 24, 50);

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4, },
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForParenthesizedLambdaAsync()
    {
        string testCode = @"
            using System.Collections.Generic;
            using System.Linq;
            using System;

            public class UserDto
            {
                public string Name { get; set; }
                public int Age { get; set; }
                public bool Gender { get; set; }
            }

            public class TestClass
            {
                public void M1()
                {
                    var users = new List<UserDto>();

                    var result1 = users.Max(x => (x)); // OK
                    var result2 = users.Max((x) => x); // OK
                    var result3 = users.Max((x) => (x)); // OK
                    var result4 = users.Max((x) => ((x))); // OK

                    var result5 = users.Max((x) => x.Age); // Violation
                    var result6 = users.Max((x) => (x.Age)); // Violation
                    var result7 = users.Max((x) => (((x.Age)))); // Violation
                    var result8 = users.Max(x => (((x.Age)))); // Violation
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(24, 45, 24, 57);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(25, 45, 25, 59);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(26, 45, 26, 63);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(27, 45, 27, 61);

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4, },
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForBlockLambdaAsync()
    {
        string testCode = @"
            using System.Collections.Generic;
            using System.Linq;
            using System;

            public class TestClass
            {
                public void M1()
                {
                    List<int> listInt = [];
                    int result1 = listInt.Max(
                        x =>
                        {
                            Console.WriteLine(x);
                            return 0;
                        }
                    ); // Violation

                    int result2 = listInt.Max(
                        x =>
                        {
                            if (x == 1)
                            {
                                return 1;
                            }

                            Console.WriteLine(x);
                            return 0;
                        }
                    ); // Violation

                    int result3 = listInt.Max(
                        x =>
                        {
                            int y = x * 2;
                            return y;
                        }
                    ); // Violation


                    List<string> listString = [];
                    string result4 = listString.Max(
                        x =>
                        {
                            Console.WriteLine(x);
                            return string.Empty;
                        }
                    ); // OK

                    int result5 = listString.Max(
                        x =>
                        {
                            Console.WriteLine(x);
                            return 0;
                        }
                    ); // Violation
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(12, 25, 16, 26);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(20, 25, 29, 26);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(33, 25, 37, 26);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(51, 25, 55, 26);

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4, },
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForNestedPropertiesAsync()
    {
        string testCode = @"
            using System.Collections.Generic;
            using System.Linq;

            public class UserDto
            {
                public string? Name { get; set; }
                public int Age { get; set; }
                public bool Gender { get; set; }

                public CompanyDto Company { get; set; }
            }

            public class CompanyDto
            {
                public int Salary { get; set; }
                public PrizeDto Prize { get; set; }
            }

            public class PrizeDto
            {
                public int Value { get; set; }
            }

            public class TestClass
            {
                public void M1()
                {
                    var users = new List<UserDto>();
                    int result1 = users.Max(x => x.Company.Salary); // Violation
                    int result2 = users.Max(x => x.Company?.Salary) ?? 0; // OK
                    int result3 = users.Max(x => (int)x.Company.Salary); // Violation
                    int result4 = users.Max(x => (int?)x.Company.Salary) ?? 0; // OK
                    int result5 = users.Max(x => x.Company.Prize.Value); // Violation
                    int result6 = users.Max(x => (int?)x.Company.Prize.Value) ?? 0; // OK
                    int result7 = users.Max(x => (int)x.Company.Prize.Value); // Violation
                    int result8 = users.Max(x => x.Company?.Prize.Value) ?? 0; // OK
                    int result9 = users.Max(x => x.Company?.Prize?.Value) ?? 0; // OK
                    int result10 = users.Max(x => (int?)x.Company?.Prize?.Value) ?? 0; // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(30, 45, 30, 66);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(32, 45, 32, 71);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(34, 45, 34, 71);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(36, 45, 36, 76);

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4, },
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportOnlyForValueTypeAsync()
    {
        string testCode = @"
            using System.Collections.Generic;
            using System.Linq;
            using System;

            public class UserDto
            {
                public int Age { get; set; }
                public bool Gender { get; set; }
                public DateTime Date { get; set; }
                public UserType Type { get; set; }

                public string Name { get; set; }
                public DateTime? NullableDate { get; set; }
                public CompanyDto Company { get; set; }
            }

            public class CompanyDto
            {
                public int Salary { get; set; }
            }

            public enum UserType
            {
                Manager = 1,
                Programmer = 2,
            }

            public class TestClass
            {
                public void M1()
                {
                    var users = new List<UserDto>();

                    var result1 = users.Max(x => x.Age); // Violation
                    var result2 = users.Max(x => x.Gender); // Violation
                    var result3 = users.Max(x => x.Date); // Violation
                    var result4 = users.Max(x => x.Type); // Violation
                    var result5 = users.Max(x => x.NullableDate.Value); // Violation
                    var result6 = users.Max(x => (DateTime)x.NullableDate.Value); // Violation

                    var result7 = users.Max(); // OK
                    var result8 = users.Max(x => x); // OK
                    var result9 = users.Max(x => x.Name); // OK
                    var result10 = users.Max(x => x.NullableDate); // OK
                    var result11 = users.Max(x => (DateTime?)x.NullableDate.Value); // OK
                    var result12 = users.Max(x => x.Company); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(35, 45, 35, 55);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(36, 45, 36, 58);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(37, 45, 37, 56);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(38, 45, 38, 56);

        DiagnosticResult expected5 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(39, 45, 39, 70);

        DiagnosticResult expected6 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(40, 45, 40, 80);

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4, expected5, expected6, },
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForStructureAsync()
    {
        string testCode = @"
            using System.Collections.Generic;
            using System.Linq;
            using System;

            public struct UserDto
            {
                public string Name { get; set; }
                public int Age { get; set; }
                public bool Gender { get; set; }
            }

            public class TestClass
            {
                public void M1()
                {
                    var users = new List<UserDto>();

                    var result1 = users.Max(x => x.Age); // Violation
                    var result2 = users.Max(x => (int?)x.Age); // OK
                    var result3 = users.Max(x => x.Name); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(19, 45, 19, 55);

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected1, },
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForPrimitiveTypesAsync()
    {
        string testCode = @"
            using System.Collections.Generic;
            using System.Linq;
            using System;

            public class TestClass
            {
                public void M1()
                {
                    var myNumbers = new List<int>();
                    var myStrings = new List<string>();

                    var result1 = myNumbers.Max(); // Violation
                    var result2 = myNumbers.Max(x => x); // Violation
                    var result3 = myNumbers.Max(x => (int)x); // Violation
                    var result4 = myNumbers.Max(x => (int?)x); // OK

                    var result5 = myStrings.Max(); // OK
                    var result6 = myStrings.Max(x => x); // OK
                    var result7 = myStrings.Max(x => (string)x); // OK
                    var result8 = myStrings.Max(x => (string?)x); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(13, 35, 13, 50);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(14, 49, 14, 55);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(15, 49, 15, 60);

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected1, expected2, expected3, },
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForDiffCollectionsAsync()
    {
        string testCode = @"
            using System.Collections.Generic;
            using System.Collections.Immutable;
            using System.Linq;
            using System;

            public class TestClass
            {
                public void M1()
                {
                    int[] arrayNumbers = Array.Empty<int>();
                    arrayNumbers.Max(); // Violation
                    arrayNumbers.Max(x => (int?)x); // OK

                    ImmutableArray<int> immutableArrayNumbers = ImmutableArray<int>.Empty;
                    immutableArrayNumbers.Max(); // Violation
                    immutableArrayNumbers.Max(x => (int?)x); // OK

                    var dictNumbers = new Dictionary<int, int>();
                    dictNumbers.Max(); // Violation
                    dictNumbers.Values.Max(); // Violation
                    dictNumbers.Keys.Max(); // Violation
                    dictNumbers.Values.Max(x => (int?)x); // OK
                    dictNumbers.Keys.Max(x => (int?)x); // OK

                    string[] arrayStrings = Array.Empty<string>();
                    arrayStrings.Max(); // OK

                    ImmutableArray<string> immutableArrayStrings = ImmutableArray<string>.Empty;
                    immutableArrayStrings.Max(); // OK

                    var dictStrings = new Dictionary<string, string>();
                    dictStrings.Max(); // Violation
                    dictStrings.Values.Max(); // OK
                    dictStrings.Keys.Max(); // OK

                    var dictCombine1 = new Dictionary<int, string>();
                    dictCombine1.Max(); // Violation
                    dictCombine1.Values.Max(); // OK
                    dictCombine1.Keys.Max(); // Violation

                    var dictCombine2 = new Dictionary<string, int>();
                    dictCombine2.Max(); // Violation
                    dictCombine2.Values.Max(); // Violation
                    dictCombine2.Keys.Max(); // OK
                }

                public void M2()
                {
                    HashSet<int> hashSetNumbers = new HashSet<int>();
                    hashSetNumbers.Max(); // Violation
                    hashSetNumbers.Max(x => (int?)x); // OK

                    HashSet<string> hashSetStrings = new HashSet<string>();
                    hashSetStrings.Max(); // OK

                    IQueryable<int> queryNumbers = new List<int>().AsQueryable();
                    queryNumbers.Max(); // Violation
                    queryNumbers.Max(x => (int?)x); // OK

                    IQueryable<string> queryStrings = new List<string>().AsQueryable();
                    queryStrings.Max(); // OK

                    Queue<int> queueNumbers = new Queue<int>();
                    queueNumbers.Max(); // Violation
                    queueNumbers.Max(x => (int?)x); // OK

                    Queue<string> queueStrings = new Queue<string>();
                    queueStrings.Max(); // OK

                    Stack<int> stackNumbers = new Stack<int>();
                    stackNumbers.Max(); // Violation
                    stackNumbers.Max(x => (int?)x); // OK

                    Stack<string> stackStrings = new Stack<string>();
                    stackStrings.Max(); // OK

                    SortedSet<int> sortedSetNumbers = new SortedSet<int>();
                    sortedSetNumbers.Max(); // Violation
                    sortedSetNumbers.Max(x => (int?)x); // OK

                    SortedSet<string> sortedSetStrings = new SortedSet<string>();
                    sortedSetStrings.Max(); // OK

                    IEnumerable<int> enumerableNumbers = Enumerable.Empty<int>();
                    enumerableNumbers.Max(); // Violation
                    enumerableNumbers.Max(x => (int?)x); // OK

                    IEnumerable<string> enumerableStrings = Enumerable.Empty<string>();
                    enumerableStrings.Max(); // OK

                    ICollection<int> collectionNumbers = new List<int>();
                    collectionNumbers.Max(); // Violation
                    collectionNumbers.Max(x => (int?)x); // OK

                    ICollection<string> collectionStrings = new List<string>();
                    collectionStrings.Max(); // OK

                    IReadOnlyCollection<int> readOnlyNumbers = Array.Empty<int>();
                    readOnlyNumbers.Max(); // Violation
                    readOnlyNumbers.Max(x => (int?)x); // OK

                    IReadOnlyCollection<string> readOnlyStrings = Array.Empty<string>();
                    readOnlyStrings.Max(); // OK

                    Enumerable.Range(0, 0).Max(); // Violation
                    Enumerable.Range(0, 0).Max(x => (int?)x); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(12, 21, 12, 39);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(16, 21, 16, 48);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(20, 21, 20, 38);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(21, 21, 21, 45);

        DiagnosticResult expected5 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(22, 21, 22, 43);

        DiagnosticResult expected6 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(33, 21, 33, 38);

        DiagnosticResult expected7 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(38, 21, 38, 39);

        DiagnosticResult expected8 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(40, 21, 40, 44);

        DiagnosticResult expected9 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(43, 21, 43, 39);

        DiagnosticResult expected10 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(44, 21, 44, 46);

        DiagnosticResult expected11 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(51, 21, 51, 41);

        DiagnosticResult expected12 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(58, 21, 58, 39);

        DiagnosticResult expected13 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(65, 21, 65, 39);

        DiagnosticResult expected14 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(72, 21, 72, 39);

        DiagnosticResult expected15 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(79, 21, 79, 43);

        DiagnosticResult expected16 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(86, 21, 86, 44);

        DiagnosticResult expected17 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(93, 21, 93, 44);

        DiagnosticResult expected18 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(100, 21, 100, 42);

        DiagnosticResult expected19 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(106, 21, 106, 49);

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics =
            {
                expected1,
                expected2,
                expected3,
                expected4,
                expected5,
                expected6,
                expected7,
                expected8,
                expected9,
                expected10,
                expected11,
                expected12,
                expected13,
                expected14,
                expected15,
                expected16,
                expected17,
                expected18,
                expected19,
            },
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForInvalidSelectAsync()
    {
        string testCode = @"
            using System.Collections.Generic;
            using System.Linq;
            using System;

            public class TestClass
            {
                public void M1()
                {
                    var myNumbers = new List<int>();
                    var myStrings = new List<string>();

                    var result1 = myNumbers.Max(); // Violation
                    var result2 = myNumbers.Select(x => x).Max(); // Violation
                    var result3 = myNumbers.Select(x => (int)x).Max(); // Violation

                    var result4 = myNumbers.Select(x => (int?)x).Max(); // OK


                    var result5 = myStrings.Max(); // OK
                    var result6 = myStrings.Select(x => x).Max(); // OK
                    var result7 = myStrings.Select(x => (string)x).Max(); // OK
                    var result8 = myStrings.Select(x => (string?)x).Max(); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(13, 35, 13, 50);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(14, 35, 14, 65);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0003", DiagnosticSeverity.Warning)
            .WithSpan(15, 35, 15, 70);

        await new CSharpAnalyzerTest<Kuk0003MinMaxOnEmptySequenceAnalyzer, DefaultVerifier>
        {
            ExpectedDiagnostics = { expected1, expected2, expected3, },
            TestCode = testCode,
        }.RunAsync();
    }
}
