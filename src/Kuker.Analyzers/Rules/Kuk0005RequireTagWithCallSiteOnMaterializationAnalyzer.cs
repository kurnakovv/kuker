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
    /// KUK0005 rule - Require .TagWithCallSite() on materialization methods.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Kuk0005RequireTagWithCallSiteOnMaterializationAnalyzer : DiagnosticAnalyzer
    {
        private const string DIAGNOSTIC_ID = "KUK0005";
        private static readonly LocalizableString s_title = "Require .TagWithCallSite() on materialization methods";
        private static readonly LocalizableString s_messageFormat = "Use .TagWithCallSite() for '{0}' method";
        private static readonly LocalizableString s_description = "Use .TagWithCallSite() on materialization methods.";

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

        private static readonly HashSet<string> s_materializationMethods = new HashSet<string>()
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

            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return;
            }

            string methodName = memberAccess.Name.Identifier.Text;

            if (!(s_materializationMethods.Contains(methodName) || s_materializationMethods.Select(x => x + "Async").Contains(methodName)))
            {
                return;
            }

            ExpressionSyntax sourceExpression = memberAccess.Expression;

            if (!IsEfQueryable(invocation, sourceExpression, context))
            {
                return;
            }

            if (HasTagWithCallSiteInChain(sourceExpression, context))
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

        private static bool IsEfQueryable(InvocationExpressionSyntax invocation, ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
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

            if (containingType.ToDisplayString()
                .Contains("Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions"))
            {
                return true;
            }

            ITypeSymbol type = context.SemanticModel.GetTypeInfo(expression).Type;

            if (type == null)
            {
                return false;
            }

            if (!ImplementsIQueryable(type))
            {
                return false;
            }

            if (type.ContainingNamespace.ToDisplayString()
                .StartsWith("Microsoft.EntityFrameworkCore"))
            {
                return true;
            }

            if (type.Name == "DbSet")
            {
                return true;
            }

            return false;
        }

        private static bool ImplementsIQueryable(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol named &&
                named.ConstructedFrom?.ToDisplayString() == "System.Linq.IQueryable<T>")
            {
                return true;
            }

            foreach (INamedTypeSymbol iface in type.AllInterfaces)
            {
                if (iface.ConstructedFrom?.ToDisplayString() == "System.Linq.IQueryable<T>")
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasTagWithCallSiteInChain(
            ExpressionSyntax expression,
            SyntaxNodeAnalysisContext context
        )
        {
            while (expression is InvocationExpressionSyntax invocation)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol symbol
                        && symbol.Name == "TagWithCallSite"
                        && symbol.ContainingType.ToDisplayString()
                            .Contains("Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions"))
                    {
                        return true;
                    }

                    expression = memberAccess.Expression;
                }
                else
                {
                    break;
                }
            }

            return false;
        }
    }
}
