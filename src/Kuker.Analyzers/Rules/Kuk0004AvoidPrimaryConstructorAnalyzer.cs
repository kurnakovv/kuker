// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Kuker.Analyzers.Constants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Kuker.Analyzers.Rules
{
    /// <summary>
    /// KUK0004 rule - Avoid primary constructor.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Kuk0004AvoidPrimaryConstructorAnalyzer : DiagnosticAnalyzer
    {
        private const string DIAGNOSTIC_ID = "KUK0004";
        private static readonly LocalizableString s_title = "Avoid primary constructor";
        private static readonly LocalizableString s_messageFormat = "Avoid primary constructor for '{0}'";
        private static readonly LocalizableString s_description = "Convert to regular constructor.";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            id: DIAGNOSTIC_ID,
            title: s_title,
            messageFormat: s_messageFormat,
            category: CategoryConstant.ALL_RULES,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_description,
            helpLinkUri: "https://github.com/kurnakovv/kuker/wiki/KUK0004"
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
                AnalyzeType,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.StructDeclaration
            );
        }

        private static void AnalyzeType(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is TypeDeclarationSyntax typeDeclaration))
            {
                return;
            }

            if (typeDeclaration.ParameterList == null)
            {
                return;
            }

            string typeName = typeDeclaration.Identifier.Text;

            Diagnostic diagnostic = Diagnostic.Create(
                s_rule,
                typeDeclaration.Identifier.GetLocation(),
                typeName
            );

            context.ReportDiagnostic(diagnostic);
        }
    }
}
