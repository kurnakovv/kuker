// Copyright (c) 2025 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.Analyzers.Tests.Rules;

public class Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzerTests
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
            .WithSpan(18, 21, 18, 39)
            .WithArguments("stream");

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(19, 21, 19, 46)
            .WithArguments("stream");

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(22, 21, 22, 29)
            .WithArguments("s");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
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
            .WithSpan(12, 27, 12, 60)
            .WithArguments("stream");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
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
            .WithSpan(16, 21, 16, 53)
            .WithArguments("stream");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected },
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForStaticNoExtensionsAsync()
    {
        string testCode = @"
            using System.IO;
            using static MyStatic;

            public static class MyStatic
            {
                public static void Foo(Stream stream, Stream other) { }
            }

            class TestClass
            {
                void TestMethod()
                {
                    var stream = new MemoryStream();
                    var other = new MemoryStream();
                    MyStatic.Foo(stream, stream); // OK
                }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { },
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
            .WithSpan(8, 21, 8, 35)
            .WithArguments("this");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
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

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { },
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

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { },
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportForLambdasAsync()
    {
        string goodCode = @"
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

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
        {
            TestCode = goodCode,
            ExpectedDiagnostics = { },
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
            .WithSpan(16, 21, 16, 40)
            .WithArguments("a.Ids");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
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
            .WithSpan(21, 34, 21, 84)
            .WithArguments(".NullableCase2");

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(22, 48, 22, 92)
            .WithArguments(".Ids");

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(23, 48, 23, 91)
            .WithArguments(".Ids");

        DiagnosticResult expected4 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(24, 21, 24, 91)
            .WithArguments("nullableCase.NullableCase2.Ids");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
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
            .WithSpan(16, 21, 16, 42)
            .WithArguments("a.Ids");

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(17, 21, 17, 44)
            .WithArguments("a.Ids");

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(18, 21, 18, 42)
            .WithArguments("a.Ids");

        DiagnosticResult expected4 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(19, 21, 19, 66)
            .WithArguments("a.Ids");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
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
            .WithSpan(20, 21, 20, 46)
            .WithArguments("a.Ids[0]");

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
           .WithSpan(25, 21, 25, 50)
           .WithArguments("myList1[0]");

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
           .WithSpan(30, 21, 30, 50)
           .WithArguments("myList1[i]");

        DiagnosticResult expected4 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
           .WithSpan(31, 21, 31, 58)
           .WithArguments("myList1[i + 1]");

        DiagnosticResult expected5 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
           .WithSpan(32, 21, 32, 58)
           .WithArguments("myList1[1 + i]");

        // var expected6 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
        //   .WithSpan(40, 21, 40, 54)
        //   .WithArguments("myList1[i++]");

        // var expected7 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
        //   .WithSpan(41, 21, 41, 54)
        //   .WithArguments("myList1[++i]");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
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
            .WithSpan(8, 21, 8, 44)
            .WithArguments("this._a");

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
           .WithSpan(9, 21, 9, 39)
            .WithArguments("_a");

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(10, 21, 10, 44)
            .WithArguments("this._a");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
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
            .WithSpan(11, 21, 11, 62)
            .WithArguments("anonymousType1.A");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
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
            .WithSpan(29, 21, 29, 36)
            .WithArguments("i");

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(30, 21, 30, 54)
            .WithArguments("i");

        DiagnosticResult expected3 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(31, 21, 31, 36)
            .WithArguments("i");

        DiagnosticResult expected4 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(32, 21, 32, 50)
            .WithArguments("i");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
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
            .WithSpan(13, 21, 13, 34)
            .WithArguments("i");

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(14, 21, 14, 29)
            .WithArguments("i");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
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

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { },
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

            class TestClass2
            {
                void Test()
                {
                    var obj = new A();
                    var b = new B();

                    b.F().Foo(obj); // OK
                    b.F().Foo(b.F()); // OK
                }
            }
        ";

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { },
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

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { },
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
            .WithSpan(18, 21, 18, 32)
            .WithArguments("a");

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(19, 21, 19, 32)
            .WithArguments("a");

        CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier> test = new()
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

                    // ToDo: попробовать еще всякие * / ^ и прочее
                }
            }
        ";

        DiagnosticResult expected1 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(9, 21, 9, 42)
            .WithArguments("i + 1");

        DiagnosticResult expected2 = new DiagnosticResult("KUK0001", DiagnosticSeverity.Warning)
            .WithSpan(16, 21, 16, 52)
            .WithArguments("1 + i + 1");

        await new CSharpAnalyzerTest<Kuk0001ObjectUsedAsArgumentToItsOwnArgumentAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected1, expected2 },
        }.RunAsync();
    }
}
