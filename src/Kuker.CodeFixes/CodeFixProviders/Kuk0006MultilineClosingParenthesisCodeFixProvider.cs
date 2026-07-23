// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Kuker.CodeFixes.CodeFixProviders
{
    /// <summary>
    /// Code fix for KUK0006 rule.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Kuk0006MultilineClosingParenthesisCodeFixProvider)), Shared]
    public class Kuk0006MultilineClosingParenthesisCodeFixProvider : CodeFixProvider
    {
        private const string DIAGNOSTIC_ID = "KUK0006";
        private const string TITLE = "Align closing parenthesis";

        /// <summary>
        /// FixableDiagnosticIds.
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DIAGNOSTIC_ID);

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

            SyntaxToken closeParen = root.FindToken(diagnostic.Location.SourceSpan.Start);
            if (!closeParen.IsKind(SyntaxKind.CloseParenToken))
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: TITLE,
                    createChangedDocument: token => AlignClosingParenthesisAsync(context.Document, closeParen, token),
                    equivalenceKey: TITLE
                ),
                diagnostic
            );
        }

        private static async Task<Document> AlignClosingParenthesisAsync(Document document, SyntaxToken closeParen, CancellationToken cancellationToken)
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            InvocationExpressionSyntax invocation = closeParen.Parent?.FirstAncestorOrSelf<InvocationExpressionSyntax>();

            if (invocation == null)
            {
                return document;
            }

            SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            SyntaxToken openParen = invocation.ArgumentList.OpenParenToken;

            int anchorColumn = GetAnchorColumn(text, openParen);
            string indentation = new string(' ', anchorColumn);
            bool shouldInsertLineBreak = IsCodeOnTheLeft(closeParen, text);

            SyntaxTriviaList newLeadingTrivia = shouldInsertLineBreak
                ? SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine(GetNewLine(text)), SyntaxFactory.Whitespace(indentation))
                : SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(indentation));

            SyntaxToken newCloseParen = closeParen.WithLeadingTrivia(newLeadingTrivia);
            SyntaxNode newRoot = root.ReplaceToken(closeParen, newCloseParen);

            return document.WithSyntaxRoot(newRoot);
        }

        private static int GetAnchorColumn(SourceText text, SyntaxToken openParen)
        {
            TextLine openLine = text.Lines.GetLineFromPosition(openParen.SpanStart);
            string openLineText = openLine.ToString();

            for (int index = 0; index < openLineText.Length; index++)
            {
                if (!char.IsWhiteSpace(openLineText[index]))
                {
                    return index;
                }
            }

            return 0;
        }

        private static bool IsCodeOnTheLeft(SyntaxToken closeParen, SourceText text)
        {
            TextLine closeLine = text.Lines.GetLineFromPosition(closeParen.SpanStart);
            string closeLineText = closeLine.ToString();
            int closeColumnInLine = closeParen.SpanStart - closeLine.Start;

            if (closeColumnInLine <= 0)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(closeLineText.Substring(0, closeColumnInLine));
        }

        private static string GetNewLine(SourceText text)
        {
            foreach (TextLine line in text.Lines)
            {
                if (line.EndIncludingLineBreak > line.End)
                {
                    return text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
                }
            }

            return "\r\n";
        }
    }
}
