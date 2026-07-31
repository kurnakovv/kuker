// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Kuker.Analyzers.Constants;
using Kuker.Core.Contants;
using Kuker.Core.Formatting;
using Kuker.Core.Models;
using Kuker.Core.Options;
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
        private static readonly LocalizableString s_title = "Incorrect multiline closing parenthesis placement";
        private static readonly LocalizableString s_messageFormat =
            "Closing parenthesis of a multiline construct must be on a separate line and aligned " +
            "with the first non-whitespace character of the opening line. Expected location Line:{0}, Character:{1}.";

        private static readonly LocalizableString s_description =
            "For multiline method invocations, the closing parenthesis must be on its own line " +
            "and aligned with the first non-whitespace character of the opening line.";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            id: DiagnosticIdContant.KUK0006,
            title: s_title,
            messageFormat: s_messageFormat,
            category: CategoryConstant.ALL_RULES,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_description,
            helpLinkUri: "https://github.com/kurnakovv/kuker/wiki/KUK0006"
        );

        private static readonly LocalizableString s_invalidConfigTitle = "Invalid KUK0006 target syntax option";
        private static readonly LocalizableString s_invalidConfigMessageFormat =
            "Invalid value '{0}' for option '" + Kuk0006TargetSyntaxOption.KEY + "'. " +
            "Expected '" + Kuk0006TargetSyntaxOption.METHOD_INVOCATION +
            "', '" + Kuk0006TargetSyntaxOption.OBJECT_CREATION +
            "', or both comma-separated.";

        private static readonly DiagnosticDescriptor s_invalidConfigRule = new DiagnosticDescriptor(
            id: DiagnosticIdContant.KUK0006,
            title: s_invalidConfigTitle,
            messageFormat: s_invalidConfigMessageFormat,
            category: CategoryConstant.ALL_RULES,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_invalidConfigMessageFormat,
            helpLinkUri: "https://github.com/kurnakovv/kuker/wiki/KUK0006"
        );

        /// <summary>
        /// SupportedDiagnostics.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule, s_invalidConfigRule);

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

            context.RegisterSyntaxNodeAction(
                AnalyzeObjectCreation,
                SyntaxKind.ObjectCreationExpression
            );
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is InvocationExpressionSyntax invocation))
            {
                return;
            }

            ArgumentListSyntax argumentList = invocation.ArgumentList;
            AnalyzeArgumentList(context, invocation, argumentList, Kuk0006TargetSyntaxOption.METHOD_INVOCATION);
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is ObjectCreationExpressionSyntax objectCreation)
                || objectCreation.ArgumentList == null)
            {
                return;
            }

            ArgumentListSyntax argumentList = objectCreation.ArgumentList;
            AnalyzeArgumentList(context, objectCreation, argumentList, Kuk0006TargetSyntaxOption.OBJECT_CREATION);
        }

        private static void AnalyzeArgumentList(
            SyntaxNodeAnalysisContext context,
            SyntaxNode node,
            ArgumentListSyntax argumentList,
            string targetSyntax)
        {
            SyntaxToken openParen = argumentList.OpenParenToken;
            SyntaxToken closeParen = argumentList.CloseParenToken;

            if (openParen.IsMissing || closeParen.IsMissing)
            {
                return;
            }

            SourceText text = node.SyntaxTree.GetText(context.CancellationToken);
            TextLine openLine = text.Lines.GetLineFromPosition(openParen.SpanStart);
            TextLine closeLine = text.Lines.GetLineFromPosition(closeParen.SpanStart);

            if (openLine.LineNumber == closeLine.LineNumber)
            {
                return;
            }

            MultilineClosingParenthesisPlacement placement = MultilineClosingParenthesisPlacementHelper.GetPlacement(text, openParen, closeParen);
            int closeColumn = closeParen.GetLocation().GetLineSpan().StartLinePosition.Character;

            if (placement.AnchorColumn == closeColumn)
            {
                return;
            }

            SyntaxToken previousToken = closeParen.GetPreviousToken();
            if (previousToken.IsKind(SyntaxKind.CloseBraceToken) ||
                previousToken.IsKind(SyntaxKind.CloseBracketToken) ||
                previousToken.IsKind(SyntaxKind.CloseParenToken)
            )
            {
                int previousTokenLine = text.Lines.GetLineFromPosition(previousToken.SpanStart).LineNumber;
                int previousTokenColumn = previousToken.GetLocation().GetLineSpan().StartLinePosition.Character;
                if (previousTokenLine == closeLine.LineNumber && previousTokenColumn == placement.AnchorColumn)
                {
                    return;
                }
            }

            Kuk0006TargetSyntaxOption.TryParse(string.Empty, out HashSet<string> targetSyntaxes);

            AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree);
            if (options.TryGetValue(Kuk0006TargetSyntaxOption.KEY, out string configuredTargetSyntax)
                && !Kuk0006TargetSyntaxOption.TryParse(configuredTargetSyntax, out targetSyntaxes))
            {
                Diagnostic configDiagnostic = Diagnostic.Create(
                    s_invalidConfigRule,
                    closeParen.GetLocation(),
                    configuredTargetSyntax.Trim());

                context.ReportDiagnostic(configDiagnostic);
                return;
            }

            if (!targetSyntaxes.Contains(targetSyntax))
            {
                return;
            }

            if (targetSyntax == Kuk0006TargetSyntaxOption.OBJECT_CREATION &&
                node.Parent is ArgumentSyntax &&
                node.Parent.Parent is ArgumentListSyntax parentArgumentList &&
                parentArgumentList.Parent is InvocationExpressionSyntax parentInvocation)
            {
                SyntaxToken parentCloseParen = parentInvocation.ArgumentList.CloseParenToken;
                if (!parentCloseParen.IsMissing &&
                    parentCloseParen.GetPreviousToken() == closeParen &&
                    text.Lines.GetLineFromPosition(parentCloseParen.SpanStart).LineNumber == closeLine.LineNumber)
                {
                    return;
                }
            }

            ReportDiagnostic(context, closeParen, placement.ExpectedLineNumber, placement.ExpectedCharacter);
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxToken closeParen, int expectedLineNumber, int expectedCharacter)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                s_rule,
                closeParen.GetLocation(),
                expectedLineNumber,
                expectedCharacter
            );

            context.ReportDiagnostic(diagnostic);
        }
    }
}
