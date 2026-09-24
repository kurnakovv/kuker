// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Kuker.Analyzers.Constants;
using Kuker.Core.Contants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Kuker.Analyzers.Rules
{
    /// <summary>
    /// KUK0008 rule - Enforce consistent order between field declarations, constructor parameters, and constructor assignments.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Kuk0008ConstructorMappingOrderAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_title = "Inconsistent order between field declarations, constructor parameters and assignments";

        private static readonly LocalizableString s_messageFormat =
            "The order of field declarations, constructor parameters and assignments should be consistent";

        private static readonly LocalizableString s_description =
            "Field declaration order, constructor parameter order and constructor assignment order should match to make the class easier to read and less error-prone.";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            id: DiagnosticIdContant.KUK0008,
            title: s_title,
            messageFormat: s_messageFormat,
            category: CategoryConstant.ALL_RULES,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_description,
            helpLinkUri: "https://github.com/kurnakovv/kuker/wiki/KUK0008"
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
        }
    }
}
