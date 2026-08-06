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
    public async Task CodeFixIgnoresAssignmentsWhenLeftSideIsLocalVariableAsync()
    {
        string testCode = WrapCode(
            """
            private readonly ILogger<PaymentService> _logger;

            public OrderService(ILogger<{|#0:PaymentService|}> logger)
            {
                object local = null;
                local = logger;
                _logger = logger;
            }
            """
        );

        string fixedCode = WrapCode(
            """
            private readonly ILogger<OrderService> _logger;

            public OrderService(ILogger<OrderService> logger)
            {
                object local = null;
                local = logger;
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
    public async Task CodeFixIgnoresIndexerSymbolInAssignedMembersAsync()
    {
        string testCode = WrapCode(
            """
            private object this[int index]
            {
                get => null;
                set
                {
                }
            }

            public OrderService(ILogger<{|#0:PaymentService|}> logger)
            {
                this[0] = logger;
            }
            """
        );

        string fixedCode = WrapCode(
            """
            private object this[int index]
            {
                get => null;
                set
                {
                }
            }

            public OrderService(ILogger<OrderService> logger)
            {
                this[0] = logger;
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
    public async Task CodeFixIgnoresAssignmentsWhenLeftSideIsAnotherObjectsMemberAsync()
    {
        // audit._logger = logger assigns to a member of a *different* object.
        // The fix must not attempt to rewrite a declaration in another type.
        string testCode = WrapCode(
            """
            private readonly ILogger<PaymentService> _logger;
            private readonly AuditService _audit;

            public OrderService(ILogger<{|#0:PaymentService|}> logger, AuditService audit)
            {
                _logger = logger;
                _audit = audit;
                audit._logger = logger;
            }
            """
        );

        string fixedCode = WrapCode(
            """
            private readonly ILogger<OrderService> _logger;
            private readonly AuditService _audit;

            public OrderService(ILogger<OrderService> logger, AuditService audit)
            {
                _logger = logger;
                _audit = audit;
                audit._logger = logger;
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
    public async Task CodeFixUpdatesAssignmentsToPubliclySettablePropertyWithinSameFileAsync()
    {
        // For constructor-chain fix, members declared in the same file are updated
        // to keep assignment types consistent after constructor parameter rewrite.
        string testCode = WrapCode(
            """
            private readonly ILogger<PaymentService> _logger;

            public OrderService(ILogger<{|#0:PaymentService|}> logger)
            {
                _logger = logger;
                ExternalLogger = logger;
            }

            public ILogger<PaymentService> ExternalLogger { get; set; }
            """
        );

        string fixedCode = WrapCode(
            """
            private readonly ILogger<OrderService> _logger;

            public OrderService(ILogger<OrderService> logger)
            {
                _logger = logger;
                ExternalLogger = logger;
            }

            public ILogger<OrderService> ExternalLogger { get; set; }
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
    public async Task CodeFixDoesNotChangeMemberDeclaredInAnotherFileAsync()
    {
        const string ORDER_SERVICE_FILE_NAME = "OrderService.cs";
        const string AUDIT_SERVICE_FILE_NAME = "AuditService.cs";

        string orderServiceTestCode = """
            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public class PaymentService
            {
            }

            public class OrderService
            {
                private readonly ILogger<PaymentService> _logger;
                private readonly AuditService _audit;

                public OrderService(ILogger<{|#0:PaymentService|}> logger, AuditService audit)
                {
                    _logger = logger;
                    _audit = audit;
                    _audit.Logger = logger;
                }
            }
            """;

        string auditServiceTestCode = """
            namespace TestNamespace;

            public class AuditService
            {
                public ILogger<PaymentService> Logger { get; set; }
            }
            """;

        string orderServiceFixedCode = """
            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public class PaymentService
            {
            }

            public class OrderService
            {
                private readonly ILogger<OrderService> _logger;
                private readonly AuditService _audit;

                public OrderService(ILogger<OrderService> logger, AuditService audit)
                {
                    _logger = logger;
                    _audit = audit;
                    _audit.Logger = logger;
                }
            }
            """;

        CSharpCodeFixTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, Kuk0007ILoggerTypeMatchesContainingTypeCodeFixProvider, DefaultVerifier> test = new()
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add((ORDER_SERVICE_FILE_NAME, orderServiceTestCode));
        test.TestState.Sources.Add((AUDIT_SERVICE_FILE_NAME, auditServiceTestCode));
        test.FixedState.Sources.Add((ORDER_SERVICE_FILE_NAME, orderServiceFixedCode));
        test.FixedState.Sources.Add((AUDIT_SERVICE_FILE_NAME, auditServiceTestCode));

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0007, DiagnosticSeverity.Warning).WithLocation(0));
        test.FixedState.ExpectedDiagnostics.Add(
            new DiagnosticResult("CS0266", DiagnosticSeverity.Error).WithSpan(ORDER_SERVICE_FILE_NAME, 20, 25, 20, 31));

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

    [Fact]
    public async Task CodeFixUpdatesRecordPrimaryConstructorParameterAsync()
    {
        string testCode = """
            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public record OrderService(
                ILogger<{|#0:PaymentService|}> Logger);

            public class PaymentService
            {
            }
            """;

        string fixedCode = """
            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public record OrderService(
                ILogger<OrderService> Logger);

            public class PaymentService
            {
            }
            """;

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
    public async Task CodeFixUpdatesAliasConstructorParameterTypeAsync()
    {
        string testCode = """
            using LoggerAlias = TestNamespace.ILogger<TestNamespace.PaymentService>;

            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public class OrderService
            {
                public OrderService({|#0:LoggerAlias|} logger)
                {
                }
            }

            public class PaymentService
            {
            }
            """;

        string fixedCode = """
            using LoggerAlias = TestNamespace.ILogger<TestNamespace.PaymentService>;

            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public class OrderService
            {
                public OrderService(ILogger<OrderService> logger)
                {
                }
            }

            public class PaymentService
            {
            }
            """;

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
    public async Task CodeFixKeepsNullableWhenReplacingAliasTypeAsync()
    {
        string testCode = """
            using LoggerAlias = TestNamespace.ILogger<TestNamespace.PaymentService>;

            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public class OrderService
            {
                public {|#0:LoggerAlias?|} Logger { get; }
            }

            public class PaymentService
            {
            }
            """;

        string fixedCode = """
            using LoggerAlias = TestNamespace.ILogger<TestNamespace.PaymentService>;

            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public class OrderService
            {
                public ILogger<OrderService>? Logger { get; }
            }

            public class PaymentService
            {
            }
            """;

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

            public class AuditService
            {
                public object _logger;
            }
            """;

        return template.Replace("__CODE__", indentedCode, StringComparison.Ordinal);
    }
}
