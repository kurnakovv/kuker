// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kuker.Core.Contants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Kuker.CodeFixes.CodeFixProviders
{
    /// <summary>
    /// Code fix for KUK0009 rule.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Kuk0009DeclarationHeaderSeparationCodeFixProvider)), Shared]
    public class Kuk0009DeclarationHeaderSeparationCodeFixProvider : CodeFixProvider
    {
        private const string TITLE = "Add blank line before declaration header";

        /// <summary>
        /// FixableDiagnosticIds.
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIdContant.KUK0009);

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
        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics.First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: TITLE,
                    createChangedDocument: token => AddBlankLineAsync(context.Document, diagnostic, token),
                    equivalenceKey: TITLE
                ),
                diagnostic
            );

            return Task.CompletedTask;
        }

        private static async Task<Document> AddBlankLineAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            SourceText sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            TextLine line = sourceText.Lines.GetLineFromPosition(diagnostic.Location.SourceSpan.Start);

            string newLineText = sourceText.Lines.Count > 1
                ? sourceText.ToString(TextSpan.FromBounds(sourceText.Lines[0].End, sourceText.Lines[0].EndIncludingLineBreak))
                : "\r\n";

            if (string.IsNullOrEmpty(newLineText))
            {
                newLineText = "\r\n";
            }

            SourceText newSourceText = sourceText.WithChanges(new TextChange(new TextSpan(line.Start, 0), newLineText));

            return document.WithText(newSourceText);
        }
    }
}
