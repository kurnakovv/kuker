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

        private static readonly ImmutableHashSet<string> s_queryOperatorMethods =
            ImmutableHashSet.Create(
                "Where",
                "Select",
                "SelectMany",
                "OrderBy",
                "OrderByDescending",
                "ThenBy",
                "ThenByDescending",
                "GroupBy",
                "Join",
                "GroupJoin",
                "SkipWhile",
                "TakeWhile"
            );

        private static readonly ImmutableHashSet<string> s_queryPredicateMethods =
            ImmutableHashSet.Create(
                "Any",
                "All",
                "Count",
                "LongCount",
                "First",
                "FirstOrDefault",
                "Single",
                "SingleOrDefault",
                "Last",
                "LastOrDefault",
                "Contains",
                "Sum",
                "Min",
                "Max",
                "Average"
            );

        private static readonly ImmutableHashSet<string> s_queryLambdaMethodNames =
            s_queryOperatorMethods
                .Union(s_queryPredicateMethods)
                .Union(CreateAsyncVariants(s_queryPredicateMethods));

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

            if (IsInsideQueryableExpression(invocation, context, compilationSymbolsModel))
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

            if (!ImplementsIQueryable(type, compilationSymbolsModel))
            {
                return false;
            }

            if (type.ContainingNamespace.ToDisplayString()
                .StartsWith("Microsoft.EntityFrameworkCore"))
            {
                return true;
            }

            if (IsFromDbSet(expression, context, compilationSymbolsModel))
            {
                return true;
            }

            return type is INamedTypeSymbol named &&
                SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, compilationSymbolsModel.DbSetSymbol);
        }

        private static bool ImplementsIQueryable(ITypeSymbol type, CompilationSymbolsModel model)
        {
            if (type == null)
            {
                return false;
            }
            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, model.IQueryableSymbol))
            {
                return true;
            }
            return type.AllInterfaces.Any(i =>
                SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, model.IQueryableSymbol));
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

        private static bool IsInsideQueryableExpression(
            SyntaxNode node,
            SyntaxNodeAnalysisContext context,
            CompilationSymbolsModel model
        )
        {
            return node.Ancestors().Any(x =>
                x is QueryClauseSyntax ||
                (x is LambdaExpressionSyntax lambda && IsQueryOperatorLambda(lambda, context, model))
            );
        }

        private static bool IsQueryOperatorLambda(
            LambdaExpressionSyntax lambda,
            SyntaxNodeAnalysisContext context,
            CompilationSymbolsModel model
        )
        {
            if (!(lambda.Parent is ArgumentSyntax argument) ||
                !(argument.Parent is BaseArgumentListSyntax argumentList) ||
                !(argumentList.Parent is InvocationExpressionSyntax invocation))
            {
                return false;
            }

            IMethodSymbol methodSymbol = GetInvocationMethodSymbol(invocation, context);

            if (methodSymbol == null)
            {
                return false;
            }

            IMethodSymbol originalMethod = methodSymbol.ReducedFrom ?? methodSymbol;

            if (!s_queryLambdaMethodNames.Contains(originalMethod.Name))
            {
                return false;
            }

            if (!IsQueryableOperatorMethod(originalMethod, model))
            {
                return false;
            }

            ExpressionSyntax sourceExpression = GetQuerySourceExpression(invocation, originalMethod);

            if (sourceExpression == null)
            {
                return false;
            }

            ITypeSymbol sourceType = context.SemanticModel.GetTypeInfo(sourceExpression).Type;

            return ImplementsIQueryable(sourceType, model);
        }

        private static IMethodSymbol GetInvocationMethodSymbol(
            InvocationExpressionSyntax invocation,
            SyntaxNodeAnalysisContext context
        )
        {
            SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                return methodSymbol;
            }
            return symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
        }
        private static bool IsQueryableOperatorMethod(IMethodSymbol methodSymbol, CompilationSymbolsModel model)
        {
            INamedTypeSymbol containingType = methodSymbol.ContainingType;
            return containingType != null &&
                (
                    containingType.ToDisplayString() == "System.Linq.Queryable" ||
                    SymbolEqualityComparer.Default.Equals(containingType, model.EfExtensionsSymbol)
                );
        }
        private static ExpressionSyntax GetQuerySourceExpression(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol
        )
        {
            if (methodSymbol.IsExtensionMethod &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Expression;
            }
            return invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
        }

        private static ImmutableHashSet<string> CreateAsyncVariants(IEnumerable<string> methods)
        {
            ImmutableHashSet<string>.Builder builder = ImmutableHashSet.CreateBuilder<string>();

            foreach (string method in methods)
            {
                builder.Add(method + "Async");
            }

            return builder.ToImmutable();
        }

        private static bool IsFromDbSet(ExpressionSyntax expression, SyntaxNodeAnalysisContext context, CompilationSymbolsModel model)
        {
            while (expression is InvocationExpressionSyntax invocation)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax member)
                {
                    expression = member.Expression;
                }
                else
                {
                    break;
                }
            }

            ITypeSymbol type = context.SemanticModel.GetTypeInfo(expression).Type;

            return type is INamedTypeSymbol named &&
                   SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, model.DbSetSymbol);
        }

        /// <summary>
        /// https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions methods.
        /// </summary>
        private static ImmutableHashSet<string> CreateExecutingMethods()
        {
            HashSet<string> baseMethods = new HashSet<string>
            {
                "ToArray",
                "ToList",
                "ToDictionary",
                "ToHashSet",

                "ExecuteUpdate",
                "ExecuteDelete",

                "AsEnumerable",
                "AsAsyncEnumerable",

                "Load",
                "ForEachAsync",
            };

            baseMethods.UnionWith(s_queryPredicateMethods);
            baseMethods.UnionWith(CreateAsyncVariants(s_queryPredicateMethods));

            foreach (string method in baseMethods.ToArray())
            {
                if (method != "AsEnumerable" &&
                    method != "AsAsyncEnumerable" &&
                    !method.EndsWith("Async"))
                {
                    baseMethods.Add(method + "Async");
                }
            }

            return baseMethods.ToImmutableHashSet();
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
