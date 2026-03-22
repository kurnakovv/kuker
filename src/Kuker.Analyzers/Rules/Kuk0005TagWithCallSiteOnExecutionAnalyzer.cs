// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Kuker.Analyzers.Constants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Kuker.Analyzers.Rules
{
    /// <summary>
    /// KUK0005 rule - Require .TagWithCallSite() on EF Core query execution.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Kuk0005TagWithCallSiteOnExecutionAnalyzer : DiagnosticAnalyzer
    {
        private const string DIAGNOSTIC_ID = "KUK0005";
        private static readonly LocalizableString s_title = "Require .TagWithCallSite() on EF Core query execution";
        private static readonly LocalizableString s_messageFormat = "Use .TagWithCallSite() for '{0}' method";
        private static readonly LocalizableString s_description =
            "Entity Framework Core queries should be annotated with .TagWithCallSite() before materialization or execution (e.g., ToList, First, Count, ExecuteUpdate).\n" +
            "This helps improve observability, debugging, and tracing of generated SQL by including call site information.";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            id: DIAGNOSTIC_ID,
            title: s_title,
            messageFormat: s_messageFormat,
            category: CategoryConstant.ALL_RULES,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_description,
            helpLinkUri: "https://github.com/kurnakovv/kuker/wiki/KUK0005"
        );

        private static readonly ImmutableHashSet<string> s_executingMethods = CreateExecutingMethods();

        /// <summary>
        /// SupportedDiagnostics.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        /// <summary>
        /// Initialize.
        /// </summary>
        /// <param name="context">context.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(
                startContext =>
                {
                    INamedTypeSymbol efExtensions = startContext.Compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions");
                    INamedTypeSymbol dbSetSymbol = startContext.Compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbSet`1");
                    INamedTypeSymbol iqueryableSymbol = startContext.Compilation.GetTypeByMetadataName("System.Linq.IQueryable`1");

                    if (efExtensions == null || dbSetSymbol == null || iqueryableSymbol == null)
                    {
                        return;
                    }

                    startContext.RegisterSyntaxNodeAction(
                        ctx => AnalyzeInvocation(ctx, new CompilationSymbolsModel(efExtensions, dbSetSymbol, iqueryableSymbol)),
                        SyntaxKind.InvocationExpression
                    );
                }
            );
        }

        private static void AnalyzeInvocation(
            SyntaxNodeAnalysisContext context,
            CompilationSymbolsModel compilationSymbolsModel
        )
        {
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return;
            }

            string methodName = memberAccess.Name.Identifier.Text;

            if (!s_executingMethods.Contains(methodName))
            {
                return;
            }

            ExpressionSyntax sourceExpression = memberAccess.Expression;

            if (!IsEfQueryable(invocation, sourceExpression, context, compilationSymbolsModel))
            {
                return;
            }

            if (HasTagWithCallSiteInChain(sourceExpression, context, compilationSymbolsModel))
            {
                return;
            }

            Diagnostic diagnostic = Diagnostic.Create(
                s_rule,
                invocation.GetLocation(),
                methodName
            );

            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsEfQueryable(
            InvocationExpressionSyntax invocation,
            ExpressionSyntax expression,
            SyntaxNodeAnalysisContext context,
            CompilationSymbolsModel compilationSymbolsModel
        )
        {
            if (!(context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol symbol))
            {
                return false;
            }

            INamedTypeSymbol containingType = symbol.ContainingType;

            if (containingType == null)
            {
                return false;
            }

            if (SymbolEqualityComparer.Default.Equals(containingType, compilationSymbolsModel.EfExtensionsSymbol))
            {
                return true;
            }

            ITypeSymbol type = context.SemanticModel.GetTypeInfo(expression).Type;

            if (type == null)
            {
                return false;
            }

            if (!ImplementsIQueryable(type, compilationSymbolsModel))
            {
                return false;
            }

            if (type.ContainingNamespace.ToDisplayString()
                .StartsWith("Microsoft.EntityFrameworkCore"))
            {
                return true;
            }

            return type is INamedTypeSymbol named &&
                SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, compilationSymbolsModel.DbSetSymbol);
        }

        private static bool ImplementsIQueryable(
            ITypeSymbol type,
            CompilationSymbolsModel compilationSymbolsModel
        )
        {
            if (type is INamedTypeSymbol named &&
                SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, compilationSymbolsModel.IQueryableSymbol)
            )
            {
                return true;
            }

            return type.AllInterfaces.Any(i =>
                SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, compilationSymbolsModel.IQueryableSymbol)
            );
        }

        private static bool HasTagWithCallSiteInChain(
            ExpressionSyntax expression,
            SyntaxNodeAnalysisContext context,
            CompilationSymbolsModel compilationSymbolsModel
        )
        {
            while (expression is InvocationExpressionSyntax invocation)
            {
                if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
                {
                    break;
                }

                if (memberAccess.Name.Identifier.Text == "TagWithCallSite"
                    && context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol symbol
                    && SymbolEqualityComparer.Default.Equals(symbol.ContainingType, compilationSymbolsModel.EfExtensionsSymbol)
                )
                {
                    return true;
                }

                expression = memberAccess.Expression;
            }

            return false;
        }

        private static ImmutableHashSet<string> CreateExecutingMethods()
        {
            HashSet<string> baseMethods = new HashSet<string>()
            {
                "ToList",
                "ToDictionary",
                "ToHashSet",

                "First",
                "FirstOrDefault",
                "Single",
                "SingleOrDefault",
                "Last",
                "LastOrDefault",

                "Any",
                "All",

                "Count",
                "LongCount",

                "Sum",
                "Min",
                "Max",
                "Average",

                "ExecuteUpdate",
                "ExecuteDelete",

                "AsEnumerable",
                "AsAsyncEnumerable",
            };

            return baseMethods
                .Concat(baseMethods.Select(x => x + "Async"))
                .ToImmutableHashSet();
        }
    }

    internal class CompilationSymbolsModel
    {
        public CompilationSymbolsModel(
            INamedTypeSymbol efExtensionsSymbol,
            INamedTypeSymbol dbSetSymbol,
            INamedTypeSymbol iQueryableSymbol
        )
        {
            EfExtensionsSymbol = efExtensionsSymbol;
            DbSetSymbol = dbSetSymbol;
            IQueryableSymbol = iQueryableSymbol;
        }

        public INamedTypeSymbol EfExtensionsSymbol { get; }
        public INamedTypeSymbol DbSetSymbol { get; }
        public INamedTypeSymbol IQueryableSymbol { get; }
    }
}
