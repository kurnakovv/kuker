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
        private const string TITLE = "Add TagWithCallSite()";
        private const string CODE_FIX_STYLE_OPTION = "dotnet_diagnostic.KUK0005.code_fix_style";
        private const string STYLE_INLINE = "inline";

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

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: TITLE,
                    createChangedDocument: token => AddTagWithCallSiteAsync(context.Document, root, invocation, token),
                    equivalenceKey: TITLE
                ),
                diagnostic
            );
        }

        private static async Task<Document> AddTagWithCallSiteAsync(Document document, SyntaxNode root, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            _ = root;

            if (!(invocation.Expression is MemberAccessExpressionSyntax invocationMemberAccess))
            {
                return document;
            }

            ExpressionSyntax sourceExpression = invocationMemberAccess.Expression;
            ExpressionSyntax insertionTarget = GetInsertionTarget(sourceExpression);

            bool isInlineStyle = IsInlineStyle(document, invocation.SyntaxTree);
            string insertion = BuildTagInsertion(sourceExpression, isInlineStyle);

            SourceText sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            TextChange change = new TextChange(new TextSpan(insertionTarget.Span.End, 0), insertion);

            return document.WithText(sourceText.WithChanges(change));
        }

        private static bool IsInlineStyle(Document document, SyntaxTree syntaxTree)
        {
            AnalyzerConfigOptions options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);

            return options.TryGetValue(CODE_FIX_STYLE_OPTION, out string style)
                && string.Equals(style.Trim(), STYLE_INLINE, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildTagInsertion(ExpressionSyntax sourceExpression, bool isInlineStyle)
        {
            if (!isInlineStyle && TryGetMultilineContinuationPrefix(sourceExpression, out string continuationPrefix))
            {
                return continuationPrefix + ".TagWithCallSite()";
            }

            return ".TagWithCallSite()";
        }

        private static ExpressionSyntax GetInsertionTarget(ExpressionSyntax sourceExpression)
        {
            ExpressionSyntax current = sourceExpression;

            while (current is InvocationExpressionSyntax currentInvocation
                && currentInvocation.Expression is MemberAccessExpressionSyntax currentMemberAccess)
            {
                current = currentMemberAccess.Expression;
            }

            return current;
        }

        private static bool TryGetMultilineContinuationPrefix(ExpressionSyntax sourceExpression, out string continuationPrefix)
        {
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
