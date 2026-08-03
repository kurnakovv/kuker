// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Kuker.Core.Contants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.Analyzers.Tests.Rules;

public class Kuk0007ILoggerTypeMatchesContainingTypeAnalyzerTests
{
#pragma warning disable RCS0053, SA1117 // Parameter should not span multiple lines
    [Theory]
    [InlineData(
        "NoReportWhenILoggerTypeMatchesContainingClass",
        """
        public OrderService(
            ILogger<OrderService> logger)
        {
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenILoggerTypeDoesNotMatchContainingClass",
        """
        public OrderService(
            ILogger<PaymentService> logger)
        {
        }
        """, 10, 13, 10, 27
    )]
    [InlineData(
        "NoReportWhenFieldILoggerTypeMatchesContainingClass",
        """
        private readonly ILogger<OrderService> _logger;
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenFieldILoggerTypeDoesNotMatchContainingClass",
        """
        private readonly ILogger<PaymentService> _logger;
        """, 9, 30, 9, 44
    )]
    [InlineData(
        "ReportOnlyOneDiagnosticWhenOnlyOneOfMultipleLoggerFieldsMismatches",
        """
        private readonly ILogger<OrderService> _logger1;
        private readonly ILogger<PaymentService> _logger2;
        """, 10, 26, 10, 40
    )]
    [InlineData(
        "NoReportWhenPropertyILoggerTypeMatchesContainingClass",
        """
        public ILogger<OrderService> Logger { get; }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenPropertyILoggerTypeDoesNotMatchContainingClass",
        """
        public ILogger<PaymentService> Logger { get; }
        """, 9, 20, 9, 34
    )]
    [InlineData(
        "NoReportWhenConstructorParameterMatchesContainingClassWithFieldAssignment",
        """
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            ILogger<OrderService> logger)
        {
            this._logger = logger;
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenConstructorParameterDoesNotMatchContainingClassWithFieldAssignment",
        """
        private readonly ILogger<PaymentService> _logger;

        public OrderService(
            ILogger<PaymentService> logger)
        {
            this._logger = logger;
        }
        """, 12, 13, 12, 27
    )]
    [InlineData(
        "NoReportWhenNestedClassUsesILoggerOfInnerClass",
        """
        public class Inner
        {
            private readonly ILogger<Inner> _logger;
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenNestedClassUsesILoggerOfOuterClass",
        """
        public class Inner
        {
            private readonly ILogger<OrderService> _logger;
        }
        """, 11, 30, 11, 42
    )]
    [InlineData(
        "NoReportWhenILoggerIsNonGeneric",
        """
        private interface ILogger
        {
        }

        private readonly ILogger _logger;
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportWhenInterfaceContainsMismatchedILoggerProperty",
        """
        public interface IOrderService
        {
            ILogger<PaymentService> Logger { get; }
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "NoReportWhenGenericClassUsesMatchingILoggerType",
        """
        public class GenericOrderService<T>
        {
            private readonly ILogger<GenericOrderService<T>> _logger;
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenGenericClassUsesMismatchedILoggerType",
        """
        public class GenericOrderService<T>
        {
            private readonly ILogger<OrderService> _logger;
        }
        """, 11, 30, 11, 42
    )]
    [InlineData(
        "NoReportWhenGenericClassUsesTypeParameterInILogger",
        """
        public class GenericOrderService<T>
        {
            private readonly ILogger<T> _logger;
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportOnlyMismatchedConstructorWhenMultipleConstructorsExist",
        """
        public OrderService(
            ILogger<OrderService> logger)
        {
        }

        public OrderService(
            int value,
            ILogger<PaymentService> logger)
        {
        }
        """, 16, 13, 16, 27
    )]
    [InlineData(
        "NoReportWhenRecordUsesMatchingILoggerType",
        """
        public record RecordOrderService(
            ILogger<RecordOrderService> logger);
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenRecordUsesMismatchedILoggerType",
        """
        public record RecordOrderService(
            ILogger<PaymentService> logger);
        """, 10, 13, 10, 27
    )]
    [InlineData(
        "NoReportWhenStructUsesMatchingILoggerType",
        """
        public struct StructOrderService
        {
            private readonly ILogger<StructOrderService> _logger;
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenStructUsesMismatchedILoggerType",
        """
        public struct StructOrderService
        {
            private readonly ILogger<PaymentService> _logger;
        }
        """, 11, 30, 11, 44
    )]
    [InlineData(
        "NoReportWhenRecordStructUsesMatchingILoggerType",
        """
        public record struct RecordStructOrderService(
            ILogger<RecordStructOrderService> logger);
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenRecordStructUsesMismatchedILoggerType",
        """
        public record struct RecordStructOrderService(
            ILogger<PaymentService> logger);
        """, 10, 13, 10, 27
    )]
    [InlineData(
        "NoReportWhenFullyQualifiedILoggerTypeMatchesContainingClass",
        """
        private readonly global::TestNamespace.ILogger<OrderService> _logger;
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenFullyQualifiedILoggerTypeDoesNotMatchContainingClass",
        """
        private readonly global::TestNamespace.ILogger<PaymentService> _logger;
        """, 9, 52, 9, 66
    )]
    [InlineData(
        "NoReportWhenFieldILoggerTypeIsNullableAndMatchesContainingClass",
        """
        private readonly ILogger<OrderService>? _logger;
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenFieldILoggerTypeIsNullableAndDoesNotMatchContainingClass",
        """
        private readonly ILogger<PaymentService>? _logger;
        """, 9, 30, 9, 44
    )]
    [InlineData(
        "NoReportWhenConstructorParameterILoggerTypeIsNullableAndMatchesContainingClass",
        """
        public OrderService(ILogger<OrderService>? logger)
        {
            _ = logger;
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenConstructorParameterILoggerTypeIsNullableAndDoesNotMatchContainingClass",
        """
        public OrderService(ILogger<PaymentService>? logger)
        {
            _ = logger;
        }
        """, 9, 33, 9, 47
    )]
    [InlineData(
        "NoReportWhenConstructorParameterHasAttributeAndILoggerTypeMatchesContainingClass",
        """
        private sealed class FromServicesAttribute : global::System.Attribute
        {
        }

        public OrderService(
            [FromServices] ILogger<OrderService> logger)
        {
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenConstructorParameterHasAttributeAndILoggerTypeDoesNotMatchContainingClass",
        """
        private sealed class FromServicesAttribute : global::System.Attribute
        {
        }

        public OrderService(
            [FromServices] ILogger<PaymentService> logger)
        {
        }
        """, 14, 28, 14, 42
    )]
    [InlineData(
        "NoReportWhenConstructorParameterUsesInModifierAndILoggerTypeMatchesContainingClass",
        """
        public OrderService(
            in ILogger<OrderService> logger)
        {
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenConstructorParameterUsesInModifierAndILoggerTypeDoesNotMatchContainingClass",
        """
        public OrderService(
            in ILogger<PaymentService> logger)
        {
        }
        """, 10, 16, 10, 30
    )]
    [InlineData(
        "ReportWhenILoggerTypeIsBaseClassOfContainingClass",
        """
        public class BaseService
        {
        }

        public class OrderService : BaseService
        {
            private readonly ILogger<BaseService> _logger;
        }
        """, 13, 30, 13, 41
    )]
    [InlineData(
        "NoReportWhenContainingClassInheritsBaseButILoggerTypeMatchesContainingClass",
        """
        public class BaseService
        {
        }

        public class OrderService : BaseService
        {
            private readonly ILogger<OrderService> _logger;
        }
        """, 0, 0, 0, 0
    )]
    [InlineData(
        "ReportWhenGenericClassUsesWrongGenericSpecializationInILogger",
        """
        public class OrderService<T>
        {
            private readonly ILogger<OrderService<int>> _logger;
        }
        """, 9, 30, 9, 47
    )]
    [InlineData(
        "NoReportWhenGenericClassUsesWrongGenericSpecializationInILogger",
        """
        public class OrderService<T>
        {
            private readonly ILogger<OrderService<T>> _logger;
        }
        """, 0, 0, 0, 0
    )]
#pragma warning restore RCS0053, SA1117 // Parameter should not span multiple lines
    public async Task RunAsync(string name, string loggerDeclarationCode, int startLine, int startColumn, int endLine, int endColumn)
    {
        _ = name;

        bool containsContainingTypeDeclaration =
            loggerDeclarationCode.Contains("class OrderService", StringComparison.Ordinal) ||
            loggerDeclarationCode.Contains("struct OrderService", StringComparison.Ordinal) ||
            loggerDeclarationCode.Contains("record OrderService", StringComparison.Ordinal);

        string testCode = containsContainingTypeDeclaration
            ? $$"""
            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            {{loggerDeclarationCode}}

            public class PaymentService
            {
            }
            """
            : $$"""
            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public class OrderService
            {
                {{loggerDeclarationCode}}
            }

            public class PaymentService
            {
            }
            """;

        CSharpAnalyzerTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        if (startLine > 0)
        {
            DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0007, DiagnosticSeverity.Warning)
                .WithSpan(startLine, startColumn, endLine, endColumn);

            test.ExpectedDiagnostics.Add(expected);
        }

        await test.RunAsync();
    }

    [Fact]
    public async Task NoReportWhenPartialClassUsesMatchingILoggerTypeAsync()
    {
        string testCode = """
            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public partial class OrderService
            {
            }

            public partial class OrderService
            {
                private readonly ILogger<OrderService> _logger;
            }
            """;

        await new CSharpAnalyzerTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportWhenPartialClassUsesMatchingILoggerTypeInConstructorAsync()
    {
        string testCode = """
            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public partial class OrderService
            {
                private readonly ILogger<OrderService> _logger;
            }

            public partial class OrderService
            {
                public OrderService(ILogger<OrderService> logger)
                {
                    _logger = logger;
                }
            }
            """;

        await new CSharpAnalyzerTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();
    }

    [Fact]
    public async Task ReportWhenPartialClassUsesMismatchedILoggerTypeAsync()
    {
        string testCode = """
            namespace TestNamespace;

            public interface ILogger<T>
            {
            }

            public partial class OrderService
            {
            }

            public partial class OrderService
            {
                private readonly ILogger<PaymentService> _logger;
            }

            public class PaymentService
            {
            }
            """;

        DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0007, DiagnosticSeverity.Warning)
            .WithSpan(13, 30, 13, 44);

        await new CSharpAnalyzerTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            ExpectedDiagnostics = { expected },
        }.RunAsync();
    }

    [Fact]
    public async Task ReportWhenTypeNameMatchesButNamespaceDiffersAsync()
    {
        string testCode = """
            public interface ILogger<T>
            {
            }

            namespace A
            {
                public class OrderService
                {
                    private readonly ILogger<B.OrderService> _logger;
                }
            }

            namespace B
            {
                public class OrderService
                {
                }
            }
            """;

        DiagnosticResult expected = new DiagnosticResult(DiagnosticIdContant.KUK0007, DiagnosticSeverity.Warning)
            .WithSpan(9, 34, 9, 48);

        await new CSharpAnalyzerTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            ExpectedDiagnostics = { expected },
        }.RunAsync();
    }

    [Fact]
    public async Task NoReportWhenTypeAndNamespaceMatchAsync()
    {
        string testCode = """
            public interface ILogger<T>
            {
            }

            namespace A
            {
                public class OrderService
                {
                    private readonly ILogger<A.OrderService> _logger;
                }
            }

            namespace B
            {
                public class OrderService
                {
                }
            }
            """;

        await new CSharpAnalyzerTest<Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();
    }
}
