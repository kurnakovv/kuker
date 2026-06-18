// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Kuker.Analyzers.Constants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Kuker.Analyzers.Rules
{
    /// <summary>
    /// KUK0006 rule - Incorrect multiline closing parenthesis placement.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Kuk0006MultilineClosingParenthesisAnalyzer : DiagnosticAnalyzer
    {
        private const string DIAGNOSTIC_ID = "KUK0006";
        private static readonly LocalizableString s_title = "Incorrect multiline closing parenthesis placement";
        private static readonly LocalizableString s_messageFormat =
            "Closing parenthesis of a multiline construct must be on a separate line and aligned " +
            "with the first non-whitespace character of the opening line.";

        private static readonly LocalizableString s_description =
            "For multiline method invocations, the closing parenthesis must be on its own line " +
            "and aligned with the first non-whitespace character of the opening line.";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            id: DIAGNOSTIC_ID,
            title: s_title,
            messageFormat: s_messageFormat,
            category: CategoryConstant.ALL_RULES,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_description,
            helpLinkUri: "https://github.com/kurnakovv/kuker/wiki/KUK0006"
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
                AnalyzeInvocation,
                SyntaxKind.InvocationExpression
            );
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is InvocationExpressionSyntax invocation))
            {
                return;
            }

            ArgumentListSyntax argumentList = invocation.ArgumentList;
            SyntaxToken openParen = argumentList.OpenParenToken;
            SyntaxToken closeParen = argumentList.CloseParenToken;

            if (openParen.IsMissing || closeParen.IsMissing)
            {
                return;
            }

            SourceText text = invocation.SyntaxTree.GetText(context.CancellationToken);
            TextLine openLine = text.Lines.GetLineFromPosition(openParen.SpanStart);
            TextLine closeLine = text.Lines.GetLineFromPosition(closeParen.SpanStart);

            if (openLine.LineNumber == closeLine.LineNumber)
            {
                return;
            }

            string closeLineText = closeLine.ToString();
            string trailingText = closeLineText.Substring(closeParen.Span.End - closeLine.Start).TrimStart();

            string openLineText = openLine.ToString();
            int anchorColumn = GetAnchorColumn(openLineText);
            int closeColumn = closeParen.GetLocation().GetLineSpan().StartLinePosition.Character;

            if (closeColumn != anchorColumn)
            {
                ReportDiagnostic(context, closeParen);
            }
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxToken closeParen)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                s_rule,
                closeParen.GetLocation()
            );

            context.ReportDiagnostic(diagnostic);
        }

        private static int GetAnchorColumn(string lineText)
        {
            for (int index = 0; index < lineText.Length; index++)
            {
                if (!char.IsWhiteSpace(lineText[index]))
                {
                    return index;
                }
            }

            return 0;
        }
    }
}
