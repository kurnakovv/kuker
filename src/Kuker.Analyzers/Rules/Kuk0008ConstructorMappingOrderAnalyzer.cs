// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Kuker.Analyzers.Constants;
using Kuker.Core.Contants;
using Kuker.Core.Formatting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            INamedTypeSymbol namedType = (INamedTypeSymbol)context.Symbol;

            if (namedType.TypeKind != TypeKind.Class && namedType.TypeKind != TypeKind.Struct)
            {
                return;
            }

            List<IFieldSymbol> instanceFields = GetOrderedInstanceFields(namedType);

            if (instanceFields.Count == 0)
            {
                return;
            }

            foreach (IMethodSymbol constructor in namedType.InstanceConstructors)
            {
                if (constructor.IsImplicitlyDeclared || constructor.IsStatic)
                {
                    continue;
                }

                AnalyzeConstructor(context, constructor, instanceFields);
            }
        }

        private static List<IFieldSymbol> GetOrderedInstanceFields(INamedTypeSymbol namedType)
        {
            List<(IFieldSymbol Field, int Order)> orderedFields = new List<(IFieldSymbol Field, int Order)>();

            foreach (ISymbol member in namedType.GetMembers())
            {
                if (!(member is IFieldSymbol field))
                {
                    continue;
                }

                if (field.IsStatic || field.IsImplicitlyDeclared)
                {
                    continue;
                }

                SyntaxNode declaringSyntax = field.DeclaringSyntaxReferences.Length > 0
                    ? field.DeclaringSyntaxReferences[0].GetSyntax()
                    : null;

                if (!(declaringSyntax is VariableDeclaratorSyntax variableDeclarator))
                {
                    continue;
                }

                orderedFields.Add((field, variableDeclarator.SpanStart));
            }

            return orderedFields
                .OrderBy(x => x.Order)
                .Select(x => x.Field)
                .ToList();
        }

        private static void AnalyzeConstructor(
            SymbolAnalysisContext context,
            IMethodSymbol constructor,
            List<IFieldSymbol> instanceFields
        )
        {
            if (constructor.DeclaringSyntaxReferences.Length == 0)
            {
                return;
            }

            if (!(constructor.DeclaringSyntaxReferences[0].GetSyntax() is ConstructorDeclarationSyntax constructorDeclaration))
            {
                return;
            }

            BlockSyntax body = constructorDeclaration.Body;
            if (body == null)
            {
                return;
            }

            HashSet<string> fieldNames = new HashSet<string>(instanceFields.Select(x => x.Name), StringComparer.Ordinal);
            List<string> parameterNames = constructor.Parameters.Select(x => x.Name).ToList();
            Dictionary<string, int> parameterIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < parameterNames.Count; i++)
            {
                parameterIndexByName[parameterNames[i]] = i;
            }

            List<SimpleFieldAssignment> directAssignments = new List<SimpleFieldAssignment>();

            foreach (SimpleFieldAssignment simpleAssignment in ConstructorAssignmentHelper.GetSimpleFieldAssignments(body, parameterIndexByName))
            {
                if (!fieldNames.Contains(simpleAssignment.FieldName))
                {
                    continue;
                }

                directAssignments.RemoveAll(x => string.Equals(x.FieldName, simpleAssignment.FieldName, StringComparison.Ordinal));
                directAssignments.Add(simpleAssignment);
            }

            if (directAssignments.Count < 2)
            {
                return;
            }

            Dictionary<string, int> fieldDeclarationIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < instanceFields.Count; i++)
            {
                fieldDeclarationIndexByName[instanceFields[i].Name] = i;
            }

            List<SimpleFieldAssignment> orderedByFieldDeclaration = directAssignments
                .OrderBy(x => fieldDeclarationIndexByName[x.FieldName])
                .ToList();

            ExpressionStatementSyntax firstMismatchedStatement = FindFirstMismatchedStatement(
                directAssignments,
                orderedByFieldDeclaration,
                fieldDeclarationIndexByName,
                parameterIndexByName
            );

            if (firstMismatchedStatement == null)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(s_rule, firstMismatchedStatement.GetLocation()));
        }

        private static ExpressionStatementSyntax FindFirstMismatchedStatement(
            List<SimpleFieldAssignment> directAssignments,
            List<SimpleFieldAssignment> orderedByFieldDeclaration,
            Dictionary<string, int> fieldDeclarationIndexByName,
            Dictionary<string, int> parameterIndexByName
        )
        {
            int previousFieldDeclarationIndex = -1;

            foreach (SimpleFieldAssignment directAssignment in directAssignments)
            {
                int currentFieldDeclarationIndex = fieldDeclarationIndexByName[directAssignment.FieldName];

                if (currentFieldDeclarationIndex < previousFieldDeclarationIndex)
                {
                    return directAssignment.Statement;
                }

                previousFieldDeclarationIndex = currentFieldDeclarationIndex;
            }

            int previousParameterIndex = -1;

            foreach (SimpleFieldAssignment orderedAssignment in orderedByFieldDeclaration)
            {
                int currentParameterIndex = parameterIndexByName[orderedAssignment.ParameterName];

                if (currentParameterIndex < previousParameterIndex)
                {
                    return orderedAssignment.Statement;
                }

                previousParameterIndex = currentParameterIndex;
            }

            return null;
        }
    }
}
