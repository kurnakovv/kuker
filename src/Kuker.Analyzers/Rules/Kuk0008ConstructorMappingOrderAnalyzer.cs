// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Kuker.Analyzers.Constants;
using Kuker.Core.Contants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
            List<string> parameterOrder = constructor.Parameters.Select(x => x.Name).ToList();
            Dictionary<string, int> parameterIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < parameterOrder.Count; i++)
            {
                parameterIndexByName[parameterOrder[i]] = i;
            }

            List<(string FieldName, string ParameterName, AssignmentExpressionSyntax Assignment)> directAssignments =
                new List<(string FieldName, string ParameterName, AssignmentExpressionSyntax Assignment)>();

            foreach (StatementSyntax statement in body.Statements)
            {
                if (!(statement is ExpressionStatementSyntax expressionStatement))
                {
                    continue;
                }

                if (!(expressionStatement.Expression is AssignmentExpressionSyntax assignment) ||
                    !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    continue;
                }

                if (!TryGetFieldName(assignment.Left, out string fieldName) || !fieldNames.Contains(fieldName))
                {
                    continue;
                }

                if (!TryGetParameterName(assignment.Right, out string parameterName) ||
                    !parameterIndexByName.ContainsKey(parameterName))
                {
                    continue;
                }

                directAssignments.RemoveAll(x => string.Equals(x.FieldName, fieldName, StringComparison.Ordinal));
                directAssignments.Add((fieldName, parameterName, assignment));
            }

            if (directAssignments.Count < 2)
            {
                return;
            }

            Dictionary<string, int> fieldDeclarationIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < instanceFields.Count; i++)
            {
                fieldDeclarationIndex[instanceFields[i].Name] = i;
            }

            List<(string FieldName, string ParameterName, AssignmentExpressionSyntax Assignment)> orderedByFieldDeclaration =
                directAssignments
                    .OrderBy(x => fieldDeclarationIndex[x.FieldName])
                    .ToList();

            bool assignmentOrderMatches = true;
            bool parameterOrderMatches = true;

            for (int i = 0; i < orderedByFieldDeclaration.Count; i++)
            {
                if (!string.Equals(orderedByFieldDeclaration[i].FieldName, directAssignments[i].FieldName, StringComparison.Ordinal))
                {
                    assignmentOrderMatches = false;
                }
            }

            List<string> parameterNamesInFieldDeclarationOrder = orderedByFieldDeclaration
                .Select(x => x.ParameterName)
                .ToList();
            List<int> parameterIndexesInFieldDeclarationOrder = parameterNamesInFieldDeclarationOrder
                .Select(x => parameterIndexByName[x])
                .ToList();

            for (int i = 1; i < parameterIndexesInFieldDeclarationOrder.Count; i++)
            {
                if (parameterIndexesInFieldDeclarationOrder[i] < parameterIndexesInFieldDeclarationOrder[i - 1])
                {
                    parameterOrderMatches = false;
                    break;
                }
            }

            if (assignmentOrderMatches && parameterOrderMatches)
            {
                return;
            }

            AssignmentExpressionSyntax firstMismatchedAssignment = FindFirstMismatchedAssignment(
                directAssignments,
                orderedByFieldDeclaration,
                fieldDeclarationIndex,
                parameterIndexByName
            );

            if (firstMismatchedAssignment == null)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(s_rule, firstMismatchedAssignment.Left.GetLocation()));
        }

        private static AssignmentExpressionSyntax FindFirstMismatchedAssignment(
            List<(string FieldName, string ParameterName, AssignmentExpressionSyntax Assignment)> directAssignments,
            List<(string FieldName, string ParameterName, AssignmentExpressionSyntax Assignment)> orderedByFieldDeclaration,
            Dictionary<string, int> fieldDeclarationIndex,
            Dictionary<string, int> parameterIndexByName
        )
        {
            int previousFieldDeclarationIndex = -1;

            foreach ((string fieldName, string _, AssignmentExpressionSyntax assignment) in directAssignments)
            {
                int currentFieldDeclarationIndex = fieldDeclarationIndex[fieldName];

                if (currentFieldDeclarationIndex < previousFieldDeclarationIndex)
                {
                    return assignment;
                }

                previousFieldDeclarationIndex = currentFieldDeclarationIndex;
            }

            int previousParameterIndex = -1;

            foreach ((string _, string parameterName, AssignmentExpressionSyntax assignment) in orderedByFieldDeclaration)
            {
                int currentParameterIndex = parameterIndexByName[parameterName];

                if (currentParameterIndex < previousParameterIndex)
                {
                    return assignment;
                }

                previousParameterIndex = currentParameterIndex;
            }

            return null;
        }

        private static bool TryGetFieldName(ExpressionSyntax expression, out string fieldName)
        {
            fieldName = null;

            if (expression is IdentifierNameSyntax identifierName)
            {
                fieldName = identifierName.Identifier.ValueText;
                return true;
            }

            if (expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression.IsKind(SyntaxKind.ThisExpression) &&
                memberAccess.Name is IdentifierNameSyntax memberIdentifierName)
            {
                fieldName = memberIdentifierName.Identifier.ValueText;
                return true;
            }

            return false;
        }

        private static bool TryGetParameterName(ExpressionSyntax expression, out string parameterName)
        {
            parameterName = null;

            if (expression is IdentifierNameSyntax identifierName)
            {
                parameterName = identifierName.Identifier.ValueText;
                return true;
            }

            return false;
        }
    }
}
