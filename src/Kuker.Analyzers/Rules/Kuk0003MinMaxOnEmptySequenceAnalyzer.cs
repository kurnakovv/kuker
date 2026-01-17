// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

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
    /// KUK0003 rule - Min/Max (Async) and MinBy/MaxBy may throw on InvalidOperationException empty sequences.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Kuk0003MinMaxOnEmptySequenceAnalyzer : DiagnosticAnalyzer
    {
        private const string DIAGNOSTIC_ID = "KUK0003";
        private static readonly LocalizableString s_title = "Min/Max (Async) and MinBy/MaxBy may throw InvalidOperationException on empty sequences";
        private static readonly LocalizableString s_messageFormat = "'{0}' on a sequence of non-nullable value types may throw InvalidOperationException if the sequence is empty. {1}.";
        private static readonly LocalizableString s_description =
            "Min/Max (including async equivalents) and MinBy/MaxBy can throw InvalidOperationException when applied to empty sequences of non-nullable value types.\n" +
            "Min/Max (Async) can be made safe by using a nullable selector or DefaultIfEmpty().\n" +
            "MinBy/MaxBy always require DefaultIfEmpty(), as a nullable selector does not prevent the exception.";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            id: DIAGNOSTIC_ID,
            title: s_title,
            messageFormat: s_messageFormat,
            category: CategoryConstant.ALL_RULES,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_description,
            helpLinkUri: "https://github.com/kurnakovv/kuker/wiki/KUK0003"
        );

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

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return;
            }

            string methodName = memberAccess.Name.Identifier.Text;

            bool isMinMax =
                methodName == "Min" ||
                methodName == "Max" ||
                methodName == "MinAsync" ||
                methodName == "MaxAsync";

            bool isMinMaxBy =
                methodName == "MinBy" ||
                methodName == "MaxBy";

            if (!(isMinMax || isMinMaxBy))
            {
                return;
            }

            SemanticModel semanticModel = context.SemanticModel;
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

            if (!(symbolInfo.Symbol is IMethodSymbol methodSymbol))
            {
                return;
            }

            IMethodSymbol method = methodSymbol.ReducedFrom ?? methodSymbol;

            if (!methodSymbol.IsExtensionMethod)
            {
                return;
            }

            string methodNamespace = method.ContainingNamespace?.ToDisplayString();

            if (methodNamespace != "System.Linq" &&
                !methodNamespace.StartsWith("Microsoft.EntityFrameworkCore")
            )
            {
                return;
            }

            ITypeSymbol argumentType = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
            string argumentTypeDisplay = argumentType?.ToDisplayString();

            if (argumentTypeDisplay?.Contains("IEnumerable") == true &&
                HasDefaultIfEmpty(memberAccess.Expression, semanticModel)
            )
            {
                return;
            }

            if (argumentTypeDisplay?.Contains("IGrouping") == true)
            {
                return;
            }

            string details = isMinMax
                ? "Use a nullable selector or DefaultIfEmpty()"
                : "Use DefaultIfEmpty() to make the operation safe";

            if (isMinMaxBy)
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, invocation.GetLocation(), methodName, details));
                return;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

            if (arguments.Count == 0)
            {
                ITypeSymbol elementType = GetSequenceElementType(argumentType);

                if (elementType == null)
                {
                    return;
                }

                if (IsNonNullableValueType(elementType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_rule, invocation.GetLocation(), methodName, details));
                }

                return;
            }

            if (!(arguments[0].Expression is LambdaExpressionSyntax lambdaArgument))
            {
                return;
            }

            IParameterSymbol parameter = null;
            if (lambdaArgument is SimpleLambdaExpressionSyntax simpleLambda)
            {
                parameter = semanticModel.GetDeclaredSymbol(simpleLambda.Parameter) as IParameterSymbol;
            }
            else if (lambdaArgument is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
            {
                parameter = semanticModel.GetDeclaredSymbol(parenthesizedLambda.ParameterList.Parameters.First()) as IParameterSymbol;
            }

            ITypeSymbol lambdaBodyType = null;

            if (lambdaArgument.Body is ExpressionSyntax exprBody)
            {
                lambdaBodyType = semanticModel.GetTypeInfo(exprBody).Type;
            }
            else if (lambdaArgument.Body is BlockSyntax blockBody)
            {
                ReturnStatementSyntax returnStatement = blockBody.Statements
                    .OfType<ReturnStatementSyntax>()
                    .Last();

                lambdaBodyType = semanticModel.GetTypeInfo(returnStatement.Expression).Type;
            }

            bool isNullableValueType =
                lambdaBodyType.IsValueType &&
                lambdaBodyType is INamedTypeSymbol namedTypeSymbol &&
                namedTypeSymbol.IsGenericType &&
                namedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

            if (isNullableValueType)
            {
                return;
            }

            if (lambdaBodyType.IsValueType)
            {
                Diagnostic diagnostic = Diagnostic.Create(s_rule, arguments[0].GetLocation(), methodName, details);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static ITypeSymbol GetSequenceElementType(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol arrayType)
            {
                return arrayType.ElementType;
            }

            if (!(type is INamedTypeSymbol namedType))
            {
                return null;
            }

            if (namedType.IsGenericType &&
                (namedType.ConstructedFrom.ToDisplayString() == "System.Linq.IQueryable<T>" ||
                 namedType.ConstructedFrom.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
            )
            {
                return namedType.TypeArguments[0];
            }

            foreach (INamedTypeSymbol iface in namedType.AllInterfaces)
            {
                if (iface.IsGenericType &&
                    iface.ConstructedFrom.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>"
                )
                {
                    return iface.TypeArguments[0];
                }
            }

            return null;
        }

        private static bool IsNonNullableValueType(ITypeSymbol type)
        {
            if (!type.IsValueType)
            {
                return false;
            }

            if (type is INamedTypeSymbol named &&
                named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            )
            {
                return false;
            }

            return true;
        }

        private static bool HasDefaultIfEmpty(
            ExpressionSyntax expression,
            SemanticModel semanticModel
        )
        {
            if (expression == null)
            {
                return false;
            }

            if (expression is InvocationExpressionSyntax invocation)
            {
                IMethodSymbol symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (symbol?.Name == "DefaultIfEmpty" &&
                    symbol.ContainingNamespace.ToDisplayString() == "System.Linq")
                {
                    return true;
                }

                if (invocation.Expression is MemberAccessExpressionSyntax ma)
                {
                    return HasDefaultIfEmpty(ma.Expression, semanticModel);
                }
            }

            if (expression is IdentifierNameSyntax identifier)
            {
                ISymbol symbol = semanticModel.GetSymbolInfo(identifier).Symbol;

                if (symbol is ILocalSymbol local)
                {
                    foreach (SyntaxReference decl in local.DeclaringSyntaxReferences)
                    {
                        if (decl.GetSyntax() is VariableDeclaratorSyntax vd)
                        {
                            ExpressionSyntax init = vd.Initializer?.Value;
                            if (HasDefaultIfEmpty(init, semanticModel))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
