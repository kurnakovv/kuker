// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
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
            "', '" + Kuk0006TargetSyntaxOption.METHOD_DECLARATION +
            "', or a comma-separated combination.";

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

        private static readonly HashSet<string> s_defaultTargetSyntaxes = InitDefaultTargetSyntaxes();

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

            context.RegisterSyntaxNodeAction(
                AnalyzeImplicitObjectCreation,
                SyntaxKind.ImplicitObjectCreationExpression
            );

            context.RegisterSyntaxNodeAction(
                AnalyzeMethodDeclaration,
                SyntaxKind.MethodDeclaration
            );

            context.RegisterSyntaxNodeAction(
                AnalyzeLocalFunction,
                SyntaxKind.LocalFunctionStatement
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
            ObjectCreationExpressionSyntax objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            ArgumentListSyntax argumentList = objectCreation.ArgumentList;

            if (argumentList is null)
            {
                return;
            }

            AnalyzeArgumentList(context, objectCreation, argumentList, Kuk0006TargetSyntaxOption.OBJECT_CREATION);
        }

        private static void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context)
        {
            ImplicitObjectCreationExpressionSyntax implicitObjectCreation = (ImplicitObjectCreationExpressionSyntax)context.Node;

            ArgumentListSyntax argumentList = implicitObjectCreation.ArgumentList;
            AnalyzeArgumentList(context, implicitObjectCreation, argumentList, Kuk0006TargetSyntaxOption.OBJECT_CREATION);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax)context.Node;
            AnalyzeParameterList(context, methodDeclaration, methodDeclaration.ParameterList);
        }

        private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
        {
            LocalFunctionStatementSyntax localFunction = (LocalFunctionStatementSyntax)context.Node;
            AnalyzeParameterList(context, localFunction, localFunction.ParameterList);
        }

        private static void AnalyzeArgumentList(
            SyntaxNodeAnalysisContext context,
            SyntaxNode node,
            ArgumentListSyntax argumentList,
            string targetSyntax)
        {
            SyntaxToken openParen = argumentList.OpenParenToken;
            SyntaxToken closeParen = argumentList.CloseParenToken;

            AnalyzeParentheses(
                context,
                node,
                openParen,
                closeParen,
                targetSyntax,
                skipCheck: (text, closeLine, placement) => ShouldSkipArgumentList(node, text, closeLine, placement, closeParen, targetSyntax));
        }

        private static void AnalyzeParameterList(
            SyntaxNodeAnalysisContext context,
            SyntaxNode node,
            ParameterListSyntax parameterList)
        {
            AnalyzeParentheses(
                context,
                node,
                parameterList.OpenParenToken,
                parameterList.CloseParenToken,
                Kuk0006TargetSyntaxOption.METHOD_DECLARATION,
                skipCheck: null);
        }

        private static void AnalyzeParentheses(
            SyntaxNodeAnalysisContext context,
            SyntaxNode node,
            SyntaxToken openParen,
            SyntaxToken closeParen,
            string targetSyntax,
            Func<SourceText, TextLine, MultilineClosingParenthesisPlacement, bool> skipCheck)
        {
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

            if (skipCheck != null && skipCheck(text, closeLine, placement))
            {
                return;
            }

            AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree);
            HashSet<string> targetSyntaxes;

            if (options.TryGetValue(Kuk0006TargetSyntaxOption.KEY, out string configuredTargetSyntax))
            {
                if (!Kuk0006TargetSyntaxOption.TryParse(configuredTargetSyntax, out targetSyntaxes))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        s_invalidConfigRule,
                        closeParen.GetLocation(),
                        configuredTargetSyntax.Trim()));
                    return;
                }
            }
            else
            {
                targetSyntaxes = s_defaultTargetSyntaxes;
            }

            if (!targetSyntaxes.Contains(targetSyntax))
            {
                return;
            }

            ReportDiagnostic(context, closeParen, placement.ExpectedLineNumber, placement.ExpectedCharacter);
        }

        private static bool ShouldSkipArgumentList(
            SyntaxNode node,
            SourceText text,
            TextLine closeLine,
            MultilineClosingParenthesisPlacement placement,
            SyntaxToken closeParen,
            string targetSyntax)
        {
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
                    return true;
                }
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
                    return true;
                }
            }

            return false;
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

        private static HashSet<string> InitDefaultTargetSyntaxes()
        {
            Kuk0006TargetSyntaxOption.TryParse(string.Empty, out HashSet<string> defaults);
            return defaults;
        }
    }
}
