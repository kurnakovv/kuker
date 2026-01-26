// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Kuker.Analyzers.Constants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Kuker.Analyzers.Rules
{
    /// <summary>
    /// KUK0001 rule - Duplicate arguments passed to method analyzer.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Kuk0001DuplicateArgumentsPassedToMethodAnalyzer : DiagnosticAnalyzer
    {
        private const string EXCLUDED_METHODS = "dotnet_diagnostic.KUK0001.excluded_methods";
        private const string DIAGNOSTIC_ID = "KUK0001";

        private static readonly LocalizableString s_title = "Duplicate arguments passed to method";
        private static readonly LocalizableString s_messageFormat = "Argument '{0}' is passed multiple times to the same method call";
        private static readonly LocalizableString s_description =
            "Reports method calls where identical arguments are passed multiple times. " +
            "In most cases this is unintentional and caused by a typo or copy-paste error.";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            s_title,
            s_messageFormat,
            CategoryConstant.ALL_RULES,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_description,
            helpLinkUri: "https://github.com/kurnakovv/kuker/wiki/KUK0001"
        );

        /// <summary>
        /// Supported diagnostics.
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

            SemanticModel semanticModel = context.SemanticModel;
            SymbolInfo methodSymbolInfo = semanticModel.GetSymbolInfo(invocation.Expression);

            if (!(methodSymbolInfo.Symbol is IMethodSymbol methodSymbol))
            {
                return;
            }

            if (IsMethodExcluded(methodSymbol.Name, context, invocation))
            {
                return;
            }

            if (invocation.ArgumentList.Arguments.Count == 0)
            {
                return;
            }

            if (methodSymbol.IsStatic && invocation.ArgumentList.Arguments.Count == 1)
            {
                return;
            }

            ExpressionSyntax receiverExpression;

            if (methodSymbol.IsStatic)
            {
                receiverExpression = invocation.ArgumentList.Arguments[0].Expression;
            }
            else if (invocation.Expression is MemberBindingExpressionSyntax)
            {
                receiverExpression = GetConditionalReceiver(invocation);
            }
            else if (invocation.Expression is IdentifierNameSyntax)
            {
                receiverExpression = invocation.ArgumentList.Arguments[0].Expression;
            }
            else if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                receiverExpression = memberAccess.Expression;
            }
            else
            {
                return;
            }

            if (receiverExpression == null)
            {
                return;
            }

            IOperation receiverOperation = GetOperationWithoutParentheses(receiverExpression, semanticModel);
            List<ReferencedSymbol> receiverChain = GetReferencedSymbolChain(receiverOperation);

            for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                if (i == 0)
                {
                    if (methodSymbol.IsStatic)
                    {
                        continue;
                    }

                    if (!(invocation.Expression is MemberAccessExpressionSyntax) &&
                        !(invocation.Expression is MemberBindingExpressionSyntax))
                    {
                        continue;
                    }
                }

                ArgumentSyntax currentArgument = invocation.ArgumentList.Arguments[i];
                IOperation currentArgumentOperation = GetOperationWithoutParentheses(currentArgument.Expression, semanticModel);
                List<ReferencedSymbol> currentArgumentChain = GetReferencedSymbolChain(currentArgumentOperation);

                if (currentArgumentChain.Count == 0)
                {
                    continue;
                }

                if (receiverChain.Count != 0
                    && AreChainsEqual(receiverChain, currentArgumentChain)
                    && AreIndexerArgumentsEqual(receiverOperation, currentArgumentOperation))
                {
                    Diagnostic diagnostic = Diagnostic.Create(s_rule, invocation.GetLocation(), receiverOperation.Syntax.ToString());
                    context.ReportDiagnostic(diagnostic);
                    return;
                }

                for (int j = i + 1; j < invocation.ArgumentList.Arguments.Count; j++)
                {
                    ArgumentSyntax otherArgument = invocation.ArgumentList.Arguments[j];
                    IOperation otherArgumentOperation = GetOperationWithoutParentheses(otherArgument.Expression, semanticModel);
                    List<ReferencedSymbol> otherArgumentChain = GetReferencedSymbolChain(otherArgumentOperation);

                    if (otherArgumentChain.Count == 0)
                    {
                        continue;
                    }

                    if (AreChainsEqual(currentArgumentChain, otherArgumentChain)
                        && AreIndexerArgumentsEqual(currentArgumentOperation, otherArgumentOperation))
                    {
                        Diagnostic diagnostic = Diagnostic.Create(s_rule, invocation.GetLocation(), currentArgumentOperation.Syntax.ToString());
                        context.ReportDiagnostic(diagnostic);
                        return;
                    }
                }
            }
        }

        private static ExpressionSyntax GetConditionalReceiver(SyntaxNode node)
        {
            while (node != null)
            {
                if (node is ConditionalAccessExpressionSyntax conditional)
                {
                    return conditional.Expression;
                }

                node = node.Parent;
            }

            return null;
        }

        private static bool IsMethodExcluded(
            string name,
            SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax invocation
        )
        {
            AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(invocation.SyntaxTree);

            if (!options.TryGetValue(EXCLUDED_METHODS, out string excludedRaw))
            {
                return false;
            }

            ImmutableHashSet<string> excluded = excludedRaw
                .Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToImmutableHashSet();

            return excluded.Contains(name);
        }

        private static List<ReferencedSymbol> GetReferencedSymbolChain(IOperation operation)
        {
            List<ReferencedSymbol> result = new List<ReferencedSymbol>();
            HashSet<IOperation> visited = new HashSet<IOperation>();

            while (operation != null)
            {
                if (!visited.Add(operation))
                {
                    break;
                }

                if (operation is ILocalReferenceOperation localOperation)
                {
                    result.Add(new ReferencedSymbol(localOperation.Local));
                    return result;
                }
                else if (operation is IParameterReferenceOperation parameterOperation)
                {
                    result.Add(new ReferencedSymbol(parameterOperation.Parameter));
                    return result;
                }
                else if (operation is IFieldReferenceOperation fieldOperation)
                {
                    result.Add(new ReferencedSymbol(fieldOperation.Field));
                    operation = fieldOperation.Instance;
                }
                else if (operation is IPropertyReferenceOperation propertyOperation)
                {
                    result.Add(new ReferencedSymbol(propertyOperation.Property));
                    operation = propertyOperation.Instance;
                }
                else if (operation is IInstanceReferenceOperation)
                {
                    if (operation.Type != null)
                    {
                        result.Add(new ReferencedSymbol(operation.Type));
                    }
                    return result;
                }
                else if (operation is IConditionalAccessOperation conditionalAccessOperation)
                {
                    List<ReferencedSymbol> inner = GetReferencedSymbolChain(conditionalAccessOperation.WhenNotNull);
                    if (inner.Count > 0)
                    {
                        return inner;
                    }

                    return GetReferencedSymbolChain(conditionalAccessOperation.Operation);
                }
                else if (operation is IConditionalAccessInstanceOperation conditionalAccessInstanceOperation)
                {
                    IOperation parent = conditionalAccessInstanceOperation.Parent;
                    while (parent != null && !(parent is IConditionalAccessOperation))
                    {
                        parent = parent.Parent;
                    }

                    if (parent is IConditionalAccessOperation conditionalAccess)
                    {
                        operation = conditionalAccess.Operation;
                    }
                }
                else if (operation is IBinaryOperation binaryOperation)
                {
                    object leftOperantValue = null;
                    object rightOperantValue = null;

                    if (binaryOperation.LeftOperand.ConstantValue.HasValue)
                    {
                        leftOperantValue = binaryOperation.LeftOperand.ConstantValue;
                    }
                    else
                    {
                        List<ReferencedSymbol> leftChain = GetReferencedSymbolChain(binaryOperation.LeftOperand);
                        if (leftChain.Count > 0)
                        {
                            result.AddRange(leftChain);
                        }
                    }

                    if (binaryOperation.RightOperand.ConstantValue.HasValue)
                    {
                        rightOperantValue = binaryOperation.RightOperand.ConstantValue;
                    }
                    else
                    {
                        List<ReferencedSymbol> rightChain = GetReferencedSymbolChain(binaryOperation.RightOperand);
                        if (rightChain.Count > 0)
                        {
                            result.AddRange(rightChain);
                        }
                    }

                    if (leftOperantValue != null && rightOperantValue != null)
                    {
                        return result;
                    }

                    if (leftOperantValue != null)
                    {
                        result.Add(
                            new ReferencedSymbol(null)
                            {
                                BinaryOperation = new BinaryOperation(binaryOperation.OperatorKind, leftOperantValue),
                            }
                        );
                    }

                    if (rightOperantValue != null)
                    {
                        result.Add(
                            new ReferencedSymbol(null)
                            {
                                BinaryOperation = new BinaryOperation(binaryOperation.OperatorKind, rightOperantValue),
                            }
                        );
                    }

                    return result;
                }
                else
                {
                    return new List<ReferencedSymbol>();
                }
            }

            return result;
        }

        private static bool AreChainsEqual(List<ReferencedSymbol> leftSymbols, List<ReferencedSymbol> rightSymbols)
        {
            if (leftSymbols.Count != rightSymbols.Count)
            {
                return false;
            }

            for (int i = 0; i < leftSymbols.Count; i++)
            {
                ReferencedSymbol leftSymbol = leftSymbols[i];
                ReferencedSymbol rightSymbol = rightSymbols[i];

                bool areNullSymbols = leftSymbol.Symbol == null && rightSymbol.Symbol == null;
                if (areNullSymbols && leftSymbol.BinaryOperation == null && rightSymbol.BinaryOperation == null)
                {
                    throw new NotImplementedException();
                }

                if (!areNullSymbols)
                {
                    if (!SymbolEqualityComparer.Default.Equals(leftSymbol.Symbol, rightSymbol.Symbol))
                    {
                        return false;
                    }
                }
                else
                {
                    if (leftSymbol.BinaryOperation == null)
                    {
                        return false;
                    }

                    if (rightSymbol.BinaryOperation == null)
                    {
                        return false;
                    }

                    if (leftSymbol.BinaryOperation.Kind != rightSymbol.BinaryOperation.Kind)
                    {
                        return false;
                    }

                    if (!leftSymbol.BinaryOperation.Value.Equals(rightSymbol.BinaryOperation.Value))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool AreIndexerArgumentsEqual(IOperation leftOperation, IOperation rightOperation)
        {
            leftOperation = UnwrapConditional(leftOperation);
            rightOperation = UnwrapConditional(rightOperation);

            if (leftOperation is IArrayElementReferenceOperation arLeft &&
                rightOperation is IArrayElementReferenceOperation arRight)
            {
                if (arLeft.Indices.Length != arRight.Indices.Length)
                {
                    return false;
                }

                for (int i = 0; i < arLeft.Indices.Length; i++)
                {
                    IOperation leftIndex = arLeft.Indices[i];
                    IOperation rightIndex = arRight.Indices[i];

                    if (leftIndex.ConstantValue.HasValue && rightIndex.ConstantValue.HasValue)
                    {
                        if (!Equals(leftIndex.ConstantValue.Value, rightIndex.ConstantValue.Value))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        List<ReferencedSymbol> leftChain = GetReferencedSymbolChain(leftIndex);
                        List<ReferencedSymbol> rightChain = GetReferencedSymbolChain(rightIndex);

                        if (!AreChainsEqual(leftChain, rightChain))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            if (leftOperation is IPropertyReferenceOperation prLeftOperation && prLeftOperation.Arguments.Length > 0 &&
                rightOperation is IPropertyReferenceOperation prRightOperation && prRightOperation.Arguments.Length > 0)
            {
                if (prLeftOperation.Arguments.Length != prRightOperation.Arguments.Length)
                {
                    return false;
                }

                for (int i = 0; i < prLeftOperation.Arguments.Length; i++)
                {
                    IOperation leftArgumentOperation = prLeftOperation.Arguments[i].Value;
                    IOperation rightArgumentOperation = prRightOperation.Arguments[i].Value;

                    if (leftArgumentOperation.ConstantValue.HasValue && rightArgumentOperation.ConstantValue.HasValue)
                    {
                        if (!Equals(leftArgumentOperation.ConstantValue.Value, rightArgumentOperation.ConstantValue.Value))
                        {
                            return false;
                        }

                        continue;
                    }

                    List<ReferencedSymbol> leftChain = GetReferencedSymbolChain(leftArgumentOperation);
                    List<ReferencedSymbol> rightChain = GetReferencedSymbolChain(rightArgumentOperation);

                    if (!AreChainsEqual(leftChain, rightChain))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static IOperation GetOperationWithoutParentheses(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            while (expression is ParenthesizedExpressionSyntax parenthesizedExpressionSyntax)
            {
                expression = parenthesizedExpressionSyntax.Expression;
            }

            return semanticModel.GetOperation(expression);
        }

        private static IOperation UnwrapConditional(IOperation operation)
        {
            while (operation is IConditionalAccessOperation c)
            {
                operation = c.WhenNotNull;
            }

            return operation;
        }
    }

    internal class ReferencedSymbol
    {
        public ReferencedSymbol(
            ISymbol symbol
        )
        {
            Symbol = symbol;
        }

        public ISymbol Symbol { get; }
        public BinaryOperation BinaryOperation { get; set; }
    }

    internal class BinaryOperation
    {
        public BinaryOperation(
            BinaryOperatorKind kind,
            object value
        )
        {
            Kind = kind;
            Value = value;
        }

        public BinaryOperatorKind Kind { get; }
        public object Value { get; }
    }
}
