// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Kuker.Analyzers.Constants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Kuker.Analyzers.Rules
{
    /// <summary>
    /// KUK0002 rule - File name should match the type name.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Kuk0002FileNameMismatchAnalyzer : DiagnosticAnalyzer
    {
        private const string DIAGNOSTIC_ID = "KUK0002";
        private static readonly LocalizableString s_title = "File name should match the type name";
        private static readonly LocalizableString s_messageFormat = "The file name should match the name of one of the public or internal types: '{0}'";
        private static readonly LocalizableString s_description =
            "The file name must match the name of at least one of the public or internal types (class, interface, struct, enum, etc.) it contains.";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            s_title,
            s_messageFormat,
            CategoryConstant.ALL_RULES,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_description,
            helpLinkUri: "https://github.com/kurnakovv/kuker/wiki/KUK0002"
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

            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            string fileName = Path.GetFileNameWithoutExtension(context.Tree.FilePath);
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);
            IEnumerable<BaseTypeDeclarationSyntax> topLevelTypesQuery = root
                .DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>()
                .Where(t =>
                    t.Parent is CompilationUnitSyntax ||
                    t.Parent is NamespaceDeclarationSyntax ||
                    t.Parent is FileScopedNamespaceDeclarationSyntax
                )
                .Where(t =>
                    t.Modifiers.Any(
                        m =>
                            m.IsKind(SyntaxKind.PublicKeyword)
                    )
                    ||
                    !t.Modifiers.Any(
                        m =>
                            m.IsKind(SyntaxKind.PrivateKeyword) ||
                            m.IsKind(SyntaxKind.ProtectedKeyword)
                    )
                );

            if (topLevelTypesQuery.Any(t => t.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword))))
            {
                return;
            }

            IEnumerable<string> topLevelTypes = topLevelTypesQuery.Select(t => t.Identifier.Text);

            if (topLevelTypes.Any(name => name == fileName))
            {
                return;
            }

            string topLevelTypesNames = string.Join(", ", topLevelTypes);

            if (string.IsNullOrEmpty(topLevelTypesNames))
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(s_rule, root.GetLocation(), topLevelTypesNames)
            );
        }
    }
}
