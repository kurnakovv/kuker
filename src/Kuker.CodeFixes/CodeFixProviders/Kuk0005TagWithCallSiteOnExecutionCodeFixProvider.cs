// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kuker.Core.Contants;
using Kuker.Core.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Kuker.CodeFixes.CodeFixProviders
{
    /// <summary>
    /// Code fix for KUK0005 rule.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Kuk0005TagWithCallSiteOnExecutionCodeFixProvider)), Shared]
    public class Kuk0005TagWithCallSiteOnExecutionCodeFixProvider : CodeFixProvider
    {
        private const string TITLE = "Add .TagWithCallSite()";

        /// <summary>
        /// FixableDiagnosticIds.
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIdContant.KUK0005);

        /// <summary>
        /// GetFixAllProvider.
        /// </summary>
        /// <returns>A <see cref="FixAllProvider"/> that can fix all occurrences of a diagnostic.</returns>
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <summary>
        /// Register code fixes.
        /// </summary>
        /// <param name="context">The context in which the code fix is being registered.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics.First();
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            if (!(root.FindNode(diagnostic.Location.SourceSpan) is InvocationExpressionSyntax invocation))
            {
                return;
            }

            AnalyzerConfigOptions configOptions = context.Document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(invocation.SyntaxTree);
            if (configOptions.TryGetValue(Kuk0005CodeFixStyleOption.KEY, out string configValue)
                && !Kuk0005CodeFixStyleOption.IsValid(configValue))
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: TITLE,
                    createChangedDocument: token => AddTagWithCallSiteAsync(context.Document, invocation, configValue, token),
                    equivalenceKey: TITLE
                ),
                diagnostic
            );
        }

        private static async Task<Document> AddTagWithCallSiteAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            string configValue,
            CancellationToken cancellationToken
        )
        {
            if (!(invocation.Expression is MemberAccessExpressionSyntax invocationMemberAccess))
            {
                return document;
            }

            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return document;
            }

            ExpressionSyntax sourceExpression = invocationMemberAccess.Expression;
            ExpressionSyntax insertionTarget = GetInsertionTarget(sourceExpression, semanticModel, cancellationToken);

            string insertion = BuildTagInsertion(sourceExpression, insertionTarget, configValue);

            SourceText sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            TextChange change = new TextChange(new TextSpan(insertionTarget.Span.End, 0), insertion);

            return document.WithText(sourceText.WithChanges(change));
        }

        private static string BuildTagInsertion(
            ExpressionSyntax sourceExpression,
            ExpressionSyntax insertionTarget,
            string configValue
        )
        {
            if (!string.Equals(configValue?.Trim(), Kuk0005CodeFixStyleOption.INLINE, StringComparison.OrdinalIgnoreCase)
                && TryGetMultilineContinuationPrefix(insertionTarget, sourceExpression, out string continuationPrefix))
            {
                return continuationPrefix + ".TagWithCallSite()";
            }

            return ".TagWithCallSite()";
        }

        private static ExpressionSyntax GetInsertionTarget(ExpressionSyntax sourceExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            ExpressionSyntax current = sourceExpression;

            while (current is InvocationExpressionSyntax currentInvocation
                && currentInvocation.Expression is MemberAccessExpressionSyntax currentMemberAccess
                && CanUnwrapInvocation(currentInvocation, semanticModel, cancellationToken))
            {
                current = currentMemberAccess.Expression;
            }

            return current;
        }

        private static bool CanUnwrapInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
            IMethodSymbol methodSymbol = symbolInfo.Symbol as IMethodSymbol
                ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

            if (methodSymbol == null)
            {
                return false;
            }

            IMethodSymbol originalMethod = methodSymbol.ReducedFrom ?? methodSymbol;
            string containingType = originalMethod.ContainingType?.ToDisplayString();

            return containingType == "System.Linq.Queryable"
                || containingType == "Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions";
        }

        private static bool TryGetMultilineContinuationPrefix(ExpressionSyntax insertionTarget, ExpressionSyntax sourceExpression, out string continuationPrefix)
        {
            if (insertionTarget.Parent is MemberAccessExpressionSyntax nextMemberAccess
                && nextMemberAccess.Expression == insertionTarget)
            {
                string parentLeadingTrivia = nextMemberAccess.OperatorToken.LeadingTrivia.ToFullString();
                if (parentLeadingTrivia.IndexOf('\n') >= 0)
                {
                    int parentNewLineIndex = parentLeadingTrivia.LastIndexOf('\n');
                    int parentLineBreakStart = parentNewLineIndex > 0 && parentLeadingTrivia[parentNewLineIndex - 1] == '\r'
                        ? parentNewLineIndex - 1
                        : parentNewLineIndex;

                    continuationPrefix = parentLeadingTrivia.Substring(parentLineBreakStart);
                    return true;
                }

                string trailingTrivia = insertionTarget.GetTrailingTrivia().ToFullString();
                int trailingNewLineIndex = trailingTrivia.LastIndexOf('\n');
                if (trailingNewLineIndex >= 0)
                {
                    int trailingLineBreakStart = trailingNewLineIndex > 0 && trailingTrivia[trailingNewLineIndex - 1] == '\r'
                        ? trailingNewLineIndex - 1
                        : trailingNewLineIndex;

                    string trailingLineBreak = trailingTrivia.Substring(trailingLineBreakStart, trailingNewLineIndex - trailingLineBreakStart + 1);
                    continuationPrefix = trailingLineBreak + parentLeadingTrivia;
                    return true;
                }
            }

            string sourceText = sourceExpression.ToFullString();
            int newLineIndex = sourceText.IndexOf('\n');
            if (newLineIndex < 0)
            {
                continuationPrefix = string.Empty;
                return false;
            }

            int lineBreakStart = newLineIndex > 0 && sourceText[newLineIndex - 1] == '\r'
                ? newLineIndex - 1
                : newLineIndex;

            int indentationStart = newLineIndex + 1;
            int indentationEnd = indentationStart;

            while (indentationEnd < sourceText.Length
                && (sourceText[indentationEnd] == ' ' || sourceText[indentationEnd] == '\t'))
            {
                indentationEnd++;
            }

            string lineBreak = sourceText.Substring(lineBreakStart, newLineIndex - lineBreakStart + 1);
            string indentation = sourceText.Substring(indentationStart, indentationEnd - indentationStart);

            continuationPrefix = lineBreak + indentation;
            return true;
        }
    }
}
