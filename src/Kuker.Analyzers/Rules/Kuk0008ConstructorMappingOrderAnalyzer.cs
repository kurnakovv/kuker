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

            context.RegisterSyntaxNodeAction(AnalyzeConstructorDeclaration, SyntaxKind.ConstructorDeclaration);
        }

        private static void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context)
        {
            ConstructorDeclarationSyntax constructorDeclaration = (ConstructorDeclarationSyntax)context.Node;

            if (!(context.SemanticModel.GetDeclaredSymbol(constructorDeclaration, context.CancellationToken) is IMethodSymbol constructor) ||
                constructor.IsImplicitlyDeclared ||
                constructor.IsStatic)
            {
                return;
            }

            INamedTypeSymbol namedType = constructor.ContainingType;
            if (namedType == null)
            {
                return;
            }

            if (IsLayoutSensitiveType(namedType))
            {
                return;
            }

            List<IFieldSymbol> instanceFields = GetInstanceFields(namedType);

            if (instanceFields.Count == 0)
            {
                return;
            }

            AnalyzeConstructor(context, constructorDeclaration, constructor, instanceFields);
        }

        private static bool IsLayoutSensitiveType(INamedTypeSymbol namedType)
        {
            if (namedType.TypeKind == TypeKind.Struct)
            {
                return true;
            }

            foreach (AttributeData attribute in namedType.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != "System.Runtime.InteropServices.StructLayoutAttribute")
                {
                    continue;
                }

                if (attribute.ConstructorArguments.Length == 0)
                {
                    continue;
                }

                object layoutKindValue = attribute.ConstructorArguments[0].Value;
                if (layoutKindValue is int layoutKind &&
                    (layoutKind == (int)System.Runtime.InteropServices.LayoutKind.Sequential ||
                     layoutKind == (int)System.Runtime.InteropServices.LayoutKind.Explicit))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<IFieldSymbol> GetInstanceFields(INamedTypeSymbol namedType)
        {
            List<IFieldSymbol> fields = new List<IFieldSymbol>();

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

                fields.Add(field);
            }

            return fields;
        }

        private static List<IFieldSymbol> GetOrderedInstanceFieldsForConstructor(
            List<IFieldSymbol> instanceFields,
            SyntaxNode constructorTypeDeclaration
        )
        {
            List<(IFieldSymbol Field, int Order)> orderedFields = new List<(IFieldSymbol Field, int Order)>();

            foreach (IFieldSymbol field in instanceFields)
            {
                SyntaxReference declaringSyntaxReference = field.DeclaringSyntaxReferences.Length > 0
                    ? field.DeclaringSyntaxReferences[0]
                    : null;

                if (declaringSyntaxReference == null)
                {
                    continue;
                }

                if (!(declaringSyntaxReference.GetSyntax() is VariableDeclaratorSyntax variableDeclarator))
                {
                    continue;
                }

                if (!(variableDeclarator.FirstAncestorOrSelf<FieldDeclarationSyntax>()?.Parent is SyntaxNode fieldTypeDeclaration) ||
                    fieldTypeDeclaration != constructorTypeDeclaration)
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
            SyntaxNodeAnalysisContext context,
            ConstructorDeclarationSyntax constructorDeclaration,
            IMethodSymbol constructor,
            List<IFieldSymbol> instanceFields
        )
        {
            BlockSyntax body = constructorDeclaration.Body;
            if (body == null)
            {
                return;
            }

            List<IFieldSymbol> orderedFields = GetOrderedInstanceFieldsForConstructor(
                instanceFields,
                constructorDeclaration.Parent
            );

            if (orderedFields.Count == 0)
            {
                return;
            }

            HashSet<string> fieldNames = new HashSet<string>(orderedFields.Select(x => x.Name), StringComparer.Ordinal);
            List<string> parameterNames = constructor.Parameters.Select(x => x.Name).ToList();
            Dictionary<string, int> parameterIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < parameterNames.Count; i++)
            {
                parameterIndexByName[parameterNames[i]] = i;
            }

            List<SimpleFieldAssignment> directAssignments = new List<SimpleFieldAssignment>();
            HashSet<string> assignedFieldNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (SimpleFieldAssignment item in ConstructorAssignmentHelper.GetSimpleFieldAssignments(body, parameterIndexByName, context.SemanticModel))
            {
                if (!fieldNames.Contains(item.FieldName))
                {
                    continue;
                }

                if (!assignedFieldNames.Add(item.FieldName))
                {
                    return;
                }

                directAssignments.Add(item);
            }

            if (directAssignments.Count < 2)
            {
                return;
            }

            Dictionary<string, int> fieldDeclarationIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < orderedFields.Count; i++)
            {
                fieldDeclarationIndexByName[orderedFields[i].Name] = i;
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

            foreach (SimpleFieldAssignment item in directAssignments)
            {
                int currentFieldDeclarationIndex = fieldDeclarationIndexByName[item.FieldName];

                if (currentFieldDeclarationIndex < previousFieldDeclarationIndex)
                {
                    return item.Statement;
                }

                previousFieldDeclarationIndex = currentFieldDeclarationIndex;
            }

            int previousParameterIndex = -1;

            foreach (SimpleFieldAssignment item in orderedByFieldDeclaration)
            {
                int currentParameterIndex = parameterIndexByName[item.ParameterName];

                if (currentParameterIndex < previousParameterIndex)
                {
                    return item.Statement;
                }

                previousParameterIndex = currentParameterIndex;
            }

            return null;
        }
    }
}
