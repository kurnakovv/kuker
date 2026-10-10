// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Kuker.Analyzers.Constants;
using Kuker.Core.Contants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Kuker.Analyzers.Rules
{
    /// <summary>
    /// KUK0009 rule - Enforce a blank line before declarations that are preceded by a header such as attributes, comments, or XML documentation comments.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Kuk0009DeclarationHeaderSeparationAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_title = "Missing blank line before declaration header";

        private static readonly LocalizableString s_messageFormat =
            "Add a blank line before the header of this declaration";

        private static readonly LocalizableString s_description =
            "When a declaration has a header above it (attributes, comments, or XML documentation comments), a blank line should be added before that header to keep it visually separated from the previous declaration.";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            id: DiagnosticIdContant.KUK0009,
            title: s_title,
            messageFormat: s_messageFormat,
            category: CategoryConstant.ALL_RULES,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_description,
            helpLinkUri: "https://github.com/kurnakovv/kuker/wiki/KUK0009"
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

            context.RegisterSyntaxNodeAction(
                AnalyzeMemberContainer,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.StructDeclaration,
                SyntaxKind.InterfaceDeclaration,
                SyntaxKind.RecordDeclaration,
                SyntaxKind.RecordStructDeclaration,
                SyntaxKind.CompilationUnit,
                SyntaxKind.NamespaceDeclaration,
                SyntaxKind.FileScopedNamespaceDeclaration
            );

            context.RegisterSyntaxNodeAction(AnalyzeEnumDeclaration, SyntaxKind.EnumDeclaration);
        }

        private static void AnalyzeMemberContainer(SyntaxNodeAnalysisContext context)
        {
            SyntaxList<MemberDeclarationSyntax> members;

            if (context.Node is TypeDeclarationSyntax typeDeclaration)
            {
                members = typeDeclaration.Members;
            }
            else if (context.Node is CompilationUnitSyntax compilationUnit)
            {
                members = compilationUnit.Members;
            }
            else if (context.Node is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                members = namespaceDeclaration.Members;
            }
            else if (context.Node is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclaration)
            {
                members = fileScopedNamespaceDeclaration.Members;
            }
            else
            {
                return;
            }

            AnalyzeMembers(context, members, m => m.AttributeLists);
        }

        private static void AnalyzeEnumDeclaration(SyntaxNodeAnalysisContext context)
        {
            EnumDeclarationSyntax enumDeclaration = (EnumDeclarationSyntax)context.Node;

            AnalyzeMembers(context, enumDeclaration.Members, m => m.AttributeLists);
        }

        private static void AnalyzeMembers<TMember>(
            SyntaxNodeAnalysisContext context,
            IReadOnlyList<TMember> members,
            System.Func<TMember, SyntaxList<AttributeListSyntax>> getAttributeLists
        )
            where TMember : SyntaxNode
        {
            if (members == null || members.Count == 0)
            {
                return;
            }

            SourceText sourceText = context.Node.SyntaxTree.GetText();

            for (int i = 1; i < members.Count; i++)
            {
                TMember member = members[i];
                TMember previousMember = members[i - 1];

                if (member is IncompleteMemberSyntax)
                {
                    continue;
                }

                SyntaxList<AttributeListSyntax> attributeLists = getAttributeLists(member);

                SyntaxToken? coreTokenOrNull = TryGetCoreToken(member, attributeLists);

                if (coreTokenOrNull == null)
                {
                    continue;
                }

                SyntaxToken coreToken = coreTokenOrNull.Value;
                SyntaxToken previousLastToken = previousMember.GetLastToken();

                int coreLine = sourceText.Lines.GetLineFromPosition(coreToken.SpanStart).LineNumber;
                int previousLine = sourceText.Lines.GetLineFromPosition(previousLastToken.Span.End).LineNumber;

                bool hasAttributes = attributeLists.Count > 0;

                int bottomScanLine = hasAttributes
                    ? sourceText.Lines.GetLineFromPosition(attributeLists.First().SpanStart).LineNumber - 1
                    : coreLine - 1;

                int? topHeaderLine = null;
                int? bottomHeaderLine = null;
                int topHeaderPosition = 0;

                for (int line = bottomScanLine; line > previousLine; line--)
                {
                    TextLine textLine = sourceText.Lines[line];
                    string lineText = textLine.ToString();
                    string trimmed = lineText.Trim();

                    if (trimmed.Length == 0)
                    {
                        break;
                    }

                    if (bottomHeaderLine == null)
                    {
                        bottomHeaderLine = line;
                    }

                    topHeaderLine = line;
                    topHeaderPosition = textLine.Start + (lineText.Length - lineText.TrimStart().Length);
                }

                bool hasPrecedingHeaderLines = topHeaderLine.HasValue;

                if (!hasAttributes && !hasPrecedingHeaderLines)
                {
                    continue;
                }

                if (!hasPrecedingHeaderLines && member is EnumMemberDeclarationSyntax)
                {
                    bool attributeInlineWithCore =
                        sourceText.Lines.GetLineFromPosition(attributeLists.First().SpanStart).LineNumber == coreLine &&
                        sourceText.Lines.GetLineFromPosition(attributeLists.Last().Span.End - 1).LineNumber == coreLine;

                    if (attributeInlineWithCore)
                    {
                        continue;
                    }
                }

                SyntaxTrivia? documentationTrivia = FindDocumentationCommentTrivia(member.GetLeadingTrivia());

                if (documentationTrivia.HasValue)
                {
                    SyntaxTrivia docTrivia = documentationTrivia.Value;
                    string docText = docTrivia.ToFullString();
                    int docTrimmedLength = docText.Length;

                    // Better than `docText.TrimEnd('\r', '\n');`, because avoids allocating a new string.
                    while (
                        docTrimmedLength > 0 &&
                        (
                            docText[docTrimmedLength - 1] == '\r' ||
                            docText[docTrimmedLength - 1] == '\n'
                        )
                    )
                    {
                        docTrimmedLength--;
                    }

                    int docStart = docTrivia.FullSpan.Start;
                    int docEnd = docTrivia.FullSpan.Start + docTrimmedLength;
                    int docStartLine = sourceText.Lines.GetLineFromPosition(docStart).LineNumber;

                    if (docStartLine <= previousLine + 1)
                    {
                        ReportDiagnostic(context, TextSpan.FromBounds(docStart, docEnd));
                    }

                    continue;
                }

                int effectiveTopLine = topHeaderLine ??
                    sourceText.Lines.GetLineFromPosition(attributeLists.First().SpanStart).LineNumber;

                bool hasBlankLineAbove = effectiveTopLine > previousLine + 1;

                if (hasBlankLineAbove)
                {
                    continue;
                }

                int spanStart = hasPrecedingHeaderLines
                    ? topHeaderPosition
                    : attributeLists.First().SpanStart;

                int spanEnd;

                if (hasPrecedingHeaderLines)
                {
                    TextLine bottomTextLine = sourceText.Lines[bottomHeaderLine.Value];
                    spanEnd = bottomTextLine.Start + bottomTextLine.ToString().TrimEnd().Length;
                    ReportDiagnostic(context, TextSpan.FromBounds(spanStart, spanEnd));
                    continue;
                }

                spanEnd = attributeLists.Last().Span.End;

                int attributeEndLine = sourceText.Lines.GetLineFromPosition(spanEnd - 1).LineNumber;

                for (int line = attributeEndLine + 1; line < coreLine; line++)
                {
                    TextLine textLine = sourceText.Lines[line];
                    string trimmed = textLine.ToString().Trim();

                    if (trimmed.Length == 0)
                    {
                        break;
                    }

                    spanEnd = textLine.Start + textLine.ToString().TrimEnd().Length;
                }

                ReportDiagnostic(context, TextSpan.FromBounds(spanStart, spanEnd));
            }
        }

        private static SyntaxTrivia? FindDocumentationCommentTrivia(SyntaxTriviaList leadingTrivia)
        {
            foreach (SyntaxTrivia trivia in leadingTrivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    return trivia;
                }
            }

            return null;
        }

        private static SyntaxToken? TryGetCoreToken(SyntaxNode member, SyntaxList<AttributeListSyntax> attributeLists)
        {
            if (attributeLists.Count == 0)
            {
                return member.GetFirstToken();
            }

            SyntaxToken nextToken = attributeLists.Last().GetLastToken().GetNextToken();

            if (!member.FullSpan.Contains(nextToken.SpanStart))
            {
                return null;
            }

            return nextToken;
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, TextSpan span)
        {
            Location location = Location.Create(context.Node.SyntaxTree, span);

            context.ReportDiagnostic(Diagnostic.Create(s_rule, location));
        }
    }
}
