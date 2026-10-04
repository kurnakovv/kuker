// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            if (members == null)
            {
                return;
            }

            for (int i = 1; i < members.Count; i++)
            {
                TMember member = members[i];

                SyntaxTriviaList leadingTrivia = member.GetLeadingTrivia();
                SyntaxList<AttributeListSyntax> attributeLists = getAttributeLists(member);

                List<SyntaxTrivia> commentTrivia = leadingTrivia.Where(IsCommentTrivia).ToList();

                if (commentTrivia.Count > 0)
                {
                    SyntaxTrivia firstComment = commentTrivia[0];
                    SyntaxTrivia lastComment = commentTrivia[commentTrivia.Count - 1];

                    if (!HasBlankLineBeforeIndex(leadingTrivia, firstComment))
                    {
                        TextSpan span = TextSpan.FromBounds(
                            firstComment.FullSpan.Start,
                            GetTrimmedTriviaEnd(lastComment)
                        );
                        ReportDiagnostic(context, span);
                    }
                }
                else if (attributeLists.Count > 0)
                {
                    if (!HasBlankLine(leadingTrivia))
                    {
                        TextSpan span = TextSpan.FromBounds(
                            attributeLists.First().SpanStart,
                            attributeLists.Last().Span.End
                        );
                        ReportDiagnostic(context, span);
                    }
                }
            }
        }

        private static bool IsCommentTrivia(SyntaxTrivia trivia)
        {
            return trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
        }

        private static int GetTrimmedTriviaEnd(SyntaxTrivia trivia)
        {
            string text = trivia.ToFullString();
            int trimmedLength = text.Length;

            while (trimmedLength > 0 && (text[trimmedLength - 1] == '\r' || text[trimmedLength - 1] == '\n'))
            {
                trimmedLength--;
            }

            return trivia.FullSpan.Start + trimmedLength;
        }

        private static bool HasBlankLineBeforeIndex(SyntaxTriviaList leadingTrivia, SyntaxTrivia beforeTrivia)
        {
            int endOfLineCount = 0;

            foreach (SyntaxTrivia trivia in leadingTrivia)
            {
                if (trivia == beforeTrivia)
                {
                    break;
                }

                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    endOfLineCount++;
                }
            }

            return endOfLineCount >= 1;
        }

        private static bool HasBlankLine(SyntaxTriviaList leadingTrivia)
        {
            int endOfLineCount = leadingTrivia.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia));

            return endOfLineCount >= 1;
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, TextSpan span)
        {
            Location location = Location.Create(context.Node.SyntaxTree, span);

            context.ReportDiagnostic(Diagnostic.Create(s_rule, location));
        }
    }
}
