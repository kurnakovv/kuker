// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.Analyzers.Tests.Rules;

public class Kuk0001DuplicateArgumentsPassedToMethodAnalyzerTests
{
    [Fact]
    public async Task ReportForExtensionMethodsAsync()
    {
        string testCode = @"
            using System;
            using System.IO;
            using System.Threading;

            public static class MyExtensions
            {
                public static void Foo(this Stream stream, Stream other) { }
                public static void Foo(this Stream stream, Stream other1, Stream other2) { }
            }

            class TestClass
            {
                void TestMethod()
                {
                    var stream = new MemoryStream();
                    var other = new MemoryStream();
                    stream.Foo(stream); // Violation 1
                    stream.Foo(other, stream); // Violation 2

                    var s = GetStream();
                    s.Foo(s); // Violation 3

                    stream.Foo(other); // OK
                    GetStream().Foo(GetStream()); // OK, because the method can return different streams
                }

                private Stream GetStream()
                {
                    throw new NotImplementedException();
                }
            }
        ";
        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(18, 32, 18, 38);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(19, 39, 19, 45);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(22, 27, 22, 28);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2, expected3 },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForMethodsAsync()
    {
        string testCode = @"
            using System.IO;
            using System.Threading;

            class TestClass
            {
                async void TestMethod()
                {
                    var stream = new MemoryStream();
                    var other = new MemoryStream();
                    var token = CancellationToken.None;
                    await stream.CopyToAsync(stream, token); // Violation
                    await stream.CopyToAsync(other, token); // OK
                }
            }
        ";

        DiagnosticResult expected = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(12, 46, 12, 52);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForStaticExtensionsAsync()
    {
        string testCode = @"
            using System.IO;
            using static MyExtensions;

            public static class MyExtensions
            {
                public static void Foo(this Stream stream, Stream other) { }
            }

            class TestClass
            {
                void TestMethod()
                {
                    var stream = new MemoryStream();
                    var other = new MemoryStream();
                    MyExtensions.Foo(stream, stream); // Violation
                    MyExtensions.Foo(stream, other); // OK
                }
            }
        ";

        DiagnosticResult expected = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(16, 46, 16, 52);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForThisAsync()
    {
        string testCode = @"
            class C
            {
                void Foo(C x) { }

                void Test()
                {
                    this.Foo(this); // Violation
                    this.Foo(new C()); // OK
                }
            }
        ";

        DiagnosticResult expected = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(8, 30, 8, 34);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected },
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForSamePropertiesAsync()
    {
        string testCode = @"
            using System.Collections.Generic;

            public class Test
            {
                public List<long> Ids { get; set; }
                public List<long> OtherIds { get; set; }
                public Test2 MyTest { get; set; }
            }

            public class Test2
            {
                public List<long> Ids { get; set; }
            }

            class TestClass
            {
                void TestMethod()
                {
                    var a = new Test()
                    {
                        Ids = new List<long>() { 1, 2, 3 },
                        OtherIds = new List<long>() { 1, 2, 3 },
                        MyTest = new Test2()
                        {
                            Ids = new List<long>() { 1, 2, 3 }
                        }
                    };
                    var b = new Test()
                    {
                        Ids = new List<long>() { 1, 2, 3 },
                        OtherIds = new List<long>() { 1, 2, 3 },
                        MyTest = new Test2()
                        {
                            Ids = new List<long>() { 1, 2, 3 }
                        }
                    };
                    a.Ids.Equals(b.Ids); // OK
                    a.Ids.Equals(a.MyTest.Ids); // OK
                    a.Ids.Equals(a.OtherIds); // OK
                }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForSingleParametersAsync()
    {
        string testCode = @"
            using System;
            using System.Linq;
            using System.IO;
            using System.Threading;

            public static class MyExtensions
            {
                public static void Foo(this Stream stream) { }
            }

            class TestClass
            {
                void TestMethod()
                {
                    var stream = new MemoryStream();
                    stream.Foo(); // OK
                    MyExtensions.Foo(stream); // OK
                }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForLambdasAsync()
    {
        string testCode = @"
            using System;
            using System.Linq;

            class TestClass
            {
                void TestMethod()
                {
                    var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes());
                }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForVariablesWithPropertyAsync()
    {
        string testCode = @"
            using System.Collections.Generic;

            public class Test
            {
                public List<long> Ids { get; set; }
            }

            class TestClass
            {
                void TestMethod()
                {
                    var a = new Test() { Ids = new List<long>() { 1, 2, 3 } };
                    var b = new Test() { Ids = new List<long>() { 3, 4, 5 } };

                    a.Ids.Equals(a.Ids); // Violation
                    a.Ids.Equals(b.Ids); // OK
                }
            }
        ";

        DiagnosticResult expected = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(16, 34, 16, 39);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForNullableTypesAsync()
    {
        string testCode = @"
            using System.Collections.Generic;

            public class NullableCase
            {
                public NullableCase2 NullableCase2 { get; set; }
            }

            public class NullableCase2
            {
                public List<long> Ids { get; set; }
            }

            class TestClass
            {
                void TestMethod()
                {
                    var nullableCase = new NullableCase() { NullableCase2 = new NullableCase2() { Ids = new List<long>() } };
                    var otherNullableCase = new NullableCase() { NullableCase2 = new NullableCase2() { Ids = new List<long>() } };

                    nullableCase?.NullableCase2.Equals(nullableCase?.NullableCase2); // Violation 1
                    nullableCase.NullableCase2?.Ids.Equals(nullableCase.NullableCase2?.Ids); // Violation 2
                    nullableCase.NullableCase2?.Ids.Equals(nullableCase.NullableCase2.Ids); // Violation 3
                    nullableCase.NullableCase2.Ids.Equals(nullableCase.NullableCase2?.Ids); // Violation 4

                    nullableCase?.NullableCase2.Equals(otherNullableCase?.NullableCase2); // OK
                    nullableCase.NullableCase2?.Ids.Equals(otherNullableCase.NullableCase2?.Ids); // OK
                    nullableCase.NullableCase2?.Ids.Equals(otherNullableCase.NullableCase2.Ids); // OK
                    nullableCase.NullableCase2.Ids.Equals(otherNullableCase.NullableCase2?.Ids); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(21, 56, 21, 83);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(22, 60, 22, 91);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(23, 60, 23, 90);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(24, 59, 24, 90);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4 },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForBracesAsync()
    {
        string testCode = @"
            using System.Collections.Generic;

            public class Test
            {
                public List<long> Ids { get; set; }
            }

            class TestClass
            {
                void TestMethod()
                {
                    var a = new Test() { Ids = new List<long>() { 1, 2, 3 } };
                    var b = new Test() { Ids = new List<long>() { 3, 4, 5 } };

                    (a.Ids).Equals(a.Ids); // Violation 1
                    (a.Ids).Equals((a.Ids)); // Violation 2
                    a.Ids.Equals((a.Ids)); // Violation 3
                    ((((((a.Ids)))))).Equals((((((((a.Ids)))))))); // Violation 4

                    (a.Ids).Equals(b.Ids); // OK
                    (a.Ids).Equals((b.Ids)); // OK
                    a.Ids.Equals((b.Ids)); // OK
                    ((((((a.Ids)))))).Equals((((((((b.Ids)))))))); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(16, 36, 16, 41);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(17, 36, 17, 43);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(18, 34, 18, 41);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(19, 46, 19, 65);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4 },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForIndexersAsync()
    {
        string testCode = @"
            using System.Collections.Generic;

            public class Test
            {
                public List<long> Ids { get; set; }
            }

            class TestClass
            {
                void TestMethod()
                {
                    var a = new Test() { Ids = new List<long>() { 1, 2, 3 } };
                    var b = new Test() { Ids = new List<long>() { 3, 4, 5 } };
                    var myList1 = new List<long>() { 1, 2, 3 };
                    var myList2 = new List<long>() { 1, 2, 3 };
                    var i = 1;
                    var j = 2;

                    a.Ids[0].Equals(a.Ids[0]); // Violation 1
                    a.Ids[0].Equals(a.Ids[1]); // OK
                    a.Ids[0].Equals(b.Ids[0]); // OK
                    a.Ids[0].Equals(b.Ids[1]); // OK

                    myList1[0].Equals(myList1[0]); // Violation 2
                    myList1[0].Equals(myList1[1]); // OK
                    myList1[0].Equals(myList2[0]); // OK
                    myList1[0].Equals(myList2[1]); // OK

                    myList1[i].Equals(myList1[i]); // Violation 3
                    myList1[i + 1].Equals(myList1[i + 1]); // Violation 4
                    myList1[1 + i].Equals(myList1[1 + i]); // Violation 5
                    myList1[i].Equals(myList1[j]); // OK
                    myList1[i].Equals(myList2[i]); // OK
                    myList1[i].Equals(myList2[j]); // OK
                    myList1[i + 1].Equals(myList2[i + 1]); // OK
                    myList1[i + 1].Equals(myList1[j + 1]); // OK

                    // ToDo: Fix it
                    //myList1[i++].Equals(myList1[i++]); // Violation 6
                    //myList1[++i].Equals(myList1[++i]); // Violation 7
                    //myList1[i++].Equals(myList1[j++]); // OK
                    //myList1[++i].Equals(myList1[++j]); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(20, 37, 20, 45);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
           .WithSpan(25, 39, 25, 49);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
           .WithSpan(30, 39, 30, 49);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
           .WithSpan(31, 43, 31, 57);

        DiagnosticResult expected5 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
           .WithSpan(32, 43, 32, 57);

        // var expected6 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
        //   .WithSpan(40, 21, 40, 54);

        // var expected7 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
        //   .WithSpan(41, 21, 41, 54);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics =
            {
                expected1, expected2, expected3, expected4, expected5, // expected6, expected7
            },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForFieldsAsync()
    {
        string testCode = @"
            class TestClass
            {
                private readonly int _a;
                private readonly int _b;
                void TestMethod()
                {
                    this._a.Equals(this._a); // Violation 1
                    _a.Equals(this._a); // Violation 2
                    this._a.Equals(this._a); // Violation 3

                    this._a.Equals(this._b); // OK
                    _a.Equals(this._b); // OK
                    this._a.Equals(this._b); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(8, 36, 8, 43);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
           .WithSpan(9, 31, 9, 38);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(10, 36, 10, 43);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2, expected3 },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForAnonymousTypeAsync()
    {
        string testCode = @"
            using System.Collections.Generic;

            class TestClass
            {
                void TestMethod()
                {
                    var anonymousType1 = new { A = new List<int>() { 1, 2, 3, }, B = new List<int>() { 1, 2, 3, } };
                    var anonymousType2 = new { A = new List<int>() { 1, 2, 3, }, B = new List<int>() { 1, 2, 3, } };

                    anonymousType1.A.Equals(anonymousType1.A); // Violation
                    anonymousType1.A.Equals(anonymousType1.B); // OK
                    anonymousType1.A.Equals(anonymousType2.A); // OK
                    anonymousType2.A.Equals(anonymousType1.A); // OK
                }
            }
        ";

        DiagnosticResult expected = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(11, 45, 11, 61);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForRefAndOutAsync()
    {
        string testCode = @"
            public static class MyExtensions
            {
                public static ref int RefMax(this ref int left, ref int right)
                {
                    if (left > right)
                    {
                        return ref left;
                    }
                    else
                    {
                        return ref right;
                    }
                }

                public static int OutMax(this int a, out int b)
                {
                    b = 0;
                    return 0;
                }
            }

            class TestClass
            {
                void TestMethod()
                {
                    var i = 0;
                    var j = 1;
                    i.RefMax(ref i); // Violation 1
                    MyExtensions.RefMax(ref i, ref i); // Violation 2
                    i.OutMax(out i); // Violation 3
                    MyExtensions.OutMax(i, out i); // Violation 4

                    i.RefMax(ref j); // OK
                    MyExtensions.RefMax(ref i, ref j); // OK
                    i.OutMax(out j); // OK
                    MyExtensions.OutMax(i, out j); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(29, 30, 29, 35);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(30, 48, 30, 53);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(31, 30, 31, 35);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(32, 44, 32, 49);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4 },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForGenericMethodsAsync()
    {
        string testCode = @"
            public static class MyExtensions
            {
                public static void Foo<T>(this T a, T b) { }
            }

            class TestClass
            {
                void TestMethod()
                {
                    var i = 0;
                    var j = 1;
                    i.Foo<int>(i); // Violation 1
                    i.Foo(i); // Violation 2
                    i.Foo<int>(j); // OK
                    i.Foo(j); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(13, 32, 13, 33);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(14, 27, 14, 28);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2, },
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForCastsAsync()
    {
        string testCode = @"
            using System.Collections.Generic;

            public class Test
            {
                public List<long> Ids { get; set; }
            }

            class TestClass
            {
                void TestMethod()
                {
                    var i = 0;
                    var a = new Test() { Ids = new List<long>() { 1, 2, 3 } };
                    i.Equals((int)i); // OK
                    a.Equals((Test)a); // OK
                    a.Ids.Equals((List<long>)a.Ids); // OK
                }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForMethodChainsAsync()
    {
        string testCode = @"
            class A
            {
                public void Foo(A other) { }
            }

            class B
            {
                public A F() => new A();
            }

            static class C
            {
                public static A M1(int number, A a1, A a2, A a3) => new A();
            }

            class TestClass2
            {
                void Test()
                {
                    var obj = new A();
                    var b = new B();

                    b.F().Foo(obj); // OK
                    b.F().Foo(b.F()); // OK
                    C.M1(1, b.F(), b.F(), b.F()); // OK
                }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForConstantsAsync()
    {
        string testCode = """
            public static class MyStatic
            {
                public static void Foo(int a, int b) { }
            }

            class TestClass
            {
                void TestMethod()
                {
                    10.Equals(10); // OK
                    Equals(10, 10); // OK
                    MyStatic.Foo(10, 10); // OK
                    "test".Equals("test"); // OK
                }
            }
        """;

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForUnfilteredMethodsAsync()
    {
        string code = @"
            using System;

            class A
            {
                public void Foo(A x) { }
                public void Bar(A x) { }
                public void Amogus(A x) { }
            }

            class TestClass
            {
                void Test()
                {
                    var a = new A();
                    a.Foo(a); // OK
                    a.Bar(a); // OK
                    a.Amogus(a); // Violation
                    a.Equals(a); // Violation
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(18, 30, 18, 31);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(19, 30, 19, 31);

        CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = code,
            ExpectedDiagnostics = { expected1, expected2 },
        };

        test.TestState.AnalyzerConfigFiles.Add((
            "/.editorconfig",
            @"
            root = true

            [*.cs]
            dotnet_diagnostic.KUK0001.excluded_methods = Foo,Bar
            "
        ));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportForBinaryOperatorsAsync()
    {
        string testCode = @"
            class TestClass
            {
                void TestMethod()
                {
                    var i = 1;
                    var j = 2;

                    (i + 1).Equals(i + 1); // Violation 1
                    (i + 1).Equals(j + 1); // OK
                    (i + 1).Equals(i); // OK
                    i.Equals(i + 1); // OK
                    i.Equals(i + 1 + 1 + i + 1); // OK
                    (1 + i + 2 + 3).Equals(i + 1 + 1 + i + 1); // OK
                    (1 + 1).Equals(1 + 1); // OK
                    (1 + i + 1).Equals((1 + i + 1)); // Violation 2
                    (1 + i + 1).Equals((1 + j + 1)); // OK

                    // ToDo: Try * / ^ etc
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(9, 36, 9, 41);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(16, 40, 16, 51);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2 },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportMultipleParametersInstanceAsync()
    {
        string testCode = @"
            public class TestService
            {
                public int M1(int a, int b, int c)
                {
                    return a + b + c;
                }
            }

            class TestClass
            {
                void TestMethod()
                {
                    int a = 1;
                    int b = 2;
                    int c = 3;

                    TestService testService = new TestService();

                    testService.M1(a, b, c); // OK
                    testService.M1(a, a, a); // Violation 1
                    testService.M1(a, a, c); // Violation 2
                    testService.M1(a, b, b); // Violation 3
                    testService.M1(a, b, a); // Violation 4

                    new TestService().M1(a, b, c); // OK
                    new TestService().M1(a, a, a); // Violation 5
                    new TestService().M1(a, a, c); // Violation 6
                    new TestService().M1(a, b, b); // Violation 7
                    new TestService().M1(a, b, a); // Violation 8
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(21, 39, 21, 40);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(22, 39, 22, 40);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(23, 42, 23, 43);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(24, 42, 24, 43);

        DiagnosticResult expected5 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(27, 45, 27, 46);

        DiagnosticResult expected6 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(28, 45, 28, 46);

        DiagnosticResult expected7 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(29, 48, 29, 49);

        DiagnosticResult expected8 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(30, 48, 30, 49);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4, expected5, expected6, expected7, expected8, },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportMultipleParametersStaticAsync()
    {
        string testCode = @"
            public static class TestStatic
            {
                public static int M1(int a, int b, int c)
                {
                    return a + b + c;
                }
            }

            class TestClass
            {
                void TestMethod()
                {
                    int a = 1;
                    int b = 2;
                    int c = 3;

                    TestStatic.M1(a, b, c); // OK
                    TestStatic.M1(a, a, a); // Violation 1
                    TestStatic.M1(a, a, c); // Violation 2
                    TestStatic.M1(a, b, b); // Violation 3
                    TestStatic.M1(a, b, a); // Violation 4
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(19, 38, 19, 39);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(20, 38, 20, 39);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(21, 41, 21, 42);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(22, 41, 22, 42);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4, },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportIfFirstReceiverChainIsEmptyAsync()
    {
        string testCode = @"
            public class Test
            {
                void M(object x, object y)
                {
                    receiver.Test().Foo(GetObject(), x, x); // Violation 1
                    receiver.Test().Foo(x, GetObject(), x); // Violation 2
                    receiver.Test().Foo(x, x, GetObject()); // Violation 3
                    receiver.Test().Foo(x, x, x); // Violation 4

                    receiver.Test().Foo(x, y, GetObject()); // OK
                }

                Receiver receiver;

                object GetObject() => new object();
            }

            class Receiver
            {
                public Receiver Test() => this;

                public void Foo(object a, object b, object c)
                {
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(6, 57, 6, 58);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(7, 57, 7, 58);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(8, 44, 8, 45);

        DiagnosticResult expected4 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(9, 44, 9, 45);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2, expected3, expected4, },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportNullableIndexerAsync()
    {
        string testCode = @"
            class A
            {
                public B B { get; set; }
            }

            class B
            {
                public string[] C { get; set; }

                public void M(string x, string y) { }
            }

            class Test
            {
                void TestMethod(A a, B b)
                {
                    var i = 1;
                    var j = 2;

                    b.M(a?.B?.C?[0], a?.B?.C?[0]); // Violation 1
                    b.M(a?.B?.C?[0], a?.B?.C?[1]); // OK

                    b.M(a?.B?.C?[i], a?.B?.C?[i]); // Violation 2
                    b.M(a?.B?.C?[i], a?.B?.C?[j]); // OK
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(21, 38, 21, 49);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(24, 38, 24, 49);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2, },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportForNonMemberAccessMethodAsync()
    {
        string testCode = @"
            using System.IO;

            class Test
            {
                void TestMethod()
                {
                    var a = 1;
                    var b = 2;

                    M1(a, a); // Violation 1
                    M1(a, b); // OK

                    GetA().M1(a, a); // Violation 2
                    GetA().M1(a, b); // OK
                }

                void ConditionalAccessExpressionMethod()
                {
                    Stream stream = null;
                    Stream otherStream = null;

                    stream?.CopyTo(stream); // Violation 3
                    stream?.CopyTo(otherStream); // OK
                }

                void M1(int a, int b) { }

                A GetA() { return new A(); }
            }

            class A
            {
                public void M1(int a, int b) { }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(11, 27, 11, 28);

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(14, 34, 14, 35);

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(23, 36, 23, 42);

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2, expected3, },
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForNullableConditionalLambdaAsync()
    {
        string testCode = @"
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                void TestMethod(Company company, Cover cover)
                {
                    var userName = company?.Users?
                        .Where(x => x.Id == cover.UserId)
                        .Select(x => x.Name)
                        .FirstOrDefault();
                }
            }

            class Company
            {
                public IEnumerable<User> Users { get; set; }
            }

            class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            class Cover
            {
                public int UserId { get; set; }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0001DuplicateArgumentsPassedToMethodAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        }.RunAsync();
    }
}
