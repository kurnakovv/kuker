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

public class Kuk0007ILoggerTypeMatchesContainingTypeCodeFixProviderTests
{
    [Fact]
    public async Task CodeFixUpdatesConstructorParameterAndDirectlyAssignedMembersAsync()
    {
        string testCode = WrapCode(
            """
            private readonly ILogger<PaymentService> _logger;
            public ILogger<PaymentService> Logger { get; }

            public OrderService(ILogger<{|#0:PaymentService|}> logger)
            {
                _logger = logger;
                Logger = logger;
            }
            """
        );

        string fixedCode = WrapCode(
            """
            private readonly ILogger<OrderService> _logger;
            public ILogger<OrderService> Logger { get; }

            public OrderService(ILogger<OrderService> logger)
            {
                _logger = logger;
                Logger = logger;
            }
            """
        );

        CSharpCodeFixTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, Kuk0007ILoggerTypeMatchesContainingTypeCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0007, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFixUpdatesConstructorParameterWithoutTouchingUnrelatedWritablePropertyAsync()
    {
        string testCode = WrapCode(
            """
            private readonly ILogger<PaymentService> _logger;
            public ILogger<PaymentService> ExternalLogger { get; set; }

            public OrderService(ILogger<{|#0:PaymentService|}> logger)
            {
                _logger = logger;
            }
            """
        );

        string fixedCode = WrapCode(
            """
            private readonly ILogger<OrderService> _logger;
            public ILogger<PaymentService> ExternalLogger { get; set; }

            public OrderService(ILogger<OrderService> logger)
            {
                _logger = logger;
            }
            """
        );

        CSharpCodeFixTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, Kuk0007ILoggerTypeMatchesContainingTypeCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0007, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFixHandlesNonSimpleAndNestedAssignmentsInConstructorAsync()
    {
        string testCode = WrapCode(
            """
            private readonly ILogger<PaymentService> _logger;
            private int _counter;
            public object Holder { get; set; }

            public OrderService(ILogger<{|#0:PaymentService|}> logger)
            {
                _counter += 1;
                global::System.Action assign = () => Holder = logger;
                _logger = logger;
            }
            """
        );

        string fixedCode = WrapCode(
            """
            private readonly ILogger<OrderService> _logger;
            private int _counter;
            public object Holder { get; set; }

            public OrderService(ILogger<OrderService> logger)
            {
                _counter += 1;
                global::System.Action assign = () => Holder = logger;
                _logger = logger;
            }
            """
        );

        CSharpCodeFixTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, Kuk0007ILoggerTypeMatchesContainingTypeCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0007, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFixIgnoresAssignmentsWhenRightSideIsNotConstructorParameterAsync()
    {
        string testCode = WrapCode(
            """
            private object _holder;

            public OrderService(ILogger<{|#0:PaymentService|}> logger)
            {
                var fallbackLogger = logger;
                _holder = fallbackLogger;
            }
            """
        );

        string fixedCode = WrapCode(
            """
            private object _holder;

            public OrderService(ILogger<OrderService> logger)
            {
                var fallbackLogger = logger;
                _holder = fallbackLogger;
            }
            """
        );

        CSharpCodeFixTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, Kuk0007ILoggerTypeMatchesContainingTypeCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0007, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFixUpdatesFieldOnlyAsync()
    {
        string testCode = WrapCode("private readonly ILogger<{|#0:PaymentService|}> _logger;");
        string fixedCode = WrapCode("private readonly ILogger<OrderService> _logger;");

        CSharpCodeFixTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, Kuk0007ILoggerTypeMatchesContainingTypeCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0007, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFixUpdatesPropertyOnlyAsync()
    {
        string testCode = WrapCode("public ILogger<{|#0:PaymentService|}> Logger { get; }");
        string fixedCode = WrapCode("public ILogger<OrderService> Logger { get; }");

        CSharpCodeFixTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, Kuk0007ILoggerTypeMatchesContainingTypeCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0007, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    private static string WrapCode(string code)
    {
        string normalizedCode = code.ReplaceLineEndings(Environment.NewLine).Trim('\r', '\n');
        string indentedCode = normalizedCode.Replace(Environment.NewLine, $"{Environment.NewLine}        ", StringComparison.Ordinal);

        string template = """
            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public class OrderService
            {
                __CODE__
            }

            public class PaymentService
            {
            }
            """;

        return template.Replace("__CODE__", indentedCode, StringComparison.Ordinal);
    }
}
