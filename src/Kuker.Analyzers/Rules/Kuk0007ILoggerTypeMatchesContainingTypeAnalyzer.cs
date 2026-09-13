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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Kuker.Analyzers.Rules
{
    /// <summary>
    /// KUK0007 rule - ILogger type argument should match containing type.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Kuk0007ILoggerTypeMatchesContainingTypeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_title = "ILogger type parameter should match containing type";

        private static readonly LocalizableString s_messageFormat =
            "ILogger<T> type parameter '{0}' does not match the containing type '{1}'";

        private static readonly LocalizableString s_description =
            "The ILogger<T> category type should be the same as the containing type.";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            id: DiagnosticIdContant.KUK0007,
            title: s_title,
            messageFormat: s_messageFormat,
            category: CategoryConstant.ALL_RULES,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_description,
            helpLinkUri: "https://github.com/kurnakovv/kuker/wiki/KUK0007"
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
            INamedTypeSymbol containingType = (INamedTypeSymbol)context.Symbol;

            if (containingType.TypeKind != TypeKind.Class && containingType.TypeKind != TypeKind.Struct)
            {
                return;
            }

            List<(Location Location, ITypeSymbol LoggerCategoryType)> constructorMismatches =
                GetConstructorMismatches(containingType);

            foreach ((Location location, ITypeSymbol loggerCategoryType) in constructorMismatches)
            {
                ReportMismatchDiagnostic(context, location, loggerCategoryType, containingType);
            }

            if (constructorMismatches.Count > 0)
            {
                return;
            }

            foreach ((Location location, ITypeSymbol loggerCategoryType) in GetFieldAndPropertyMismatches(containingType))
            {
                ReportMismatchDiagnostic(context, location, loggerCategoryType, containingType);
            }
        }

        private static List<(Location Location, ITypeSymbol LoggerCategoryType)> GetConstructorMismatches(INamedTypeSymbol containingType)
        {
            List<(Location Location, ITypeSymbol LoggerCategoryType)> mismatches = new List<(Location Location, ITypeSymbol LoggerCategoryType)>();

            foreach (IMethodSymbol constructor in containingType.InstanceConstructors)
            {
                foreach (IParameterSymbol parameter in constructor.Parameters)
                {
                    if (!TryGetLoggerCategoryType(parameter.Type, out ITypeSymbol loggerCategoryType))
                    {
                        continue;
                    }

                    if (IsSameType(containingType, loggerCategoryType))
                    {
                        continue;
                    }

                    Location location = GetParameterTypeLocation(parameter);
                    mismatches.Add((location, loggerCategoryType));
                }
            }

            return mismatches;
        }

        private static IEnumerable<(Location Location, ITypeSymbol LoggerCategoryType)> GetFieldAndPropertyMismatches(INamedTypeSymbol containingType)
        {
            foreach (ISymbol member in containingType.GetMembers())
            {
                if (member is IFieldSymbol field)
                {
                    if (field.IsImplicitlyDeclared)
                    {
                        continue;
                    }

                    if (!TryGetLoggerCategoryType(field.Type, out ITypeSymbol loggerCategoryType))
                    {
                        continue;
                    }

                    if (IsSameType(containingType, loggerCategoryType))
                    {
                        continue;
                    }

                    yield return (GetFieldTypeLocation(field), loggerCategoryType);
                    continue;
                }

                if (!(member is IPropertySymbol property))
                {
                    continue;
                }

                if (!TryGetLoggerCategoryType(property.Type, out ITypeSymbol propertyLoggerCategoryType))
                {
                    continue;
                }

                if (property.SetMethod != null && property.SetMethod.DeclaredAccessibility != Accessibility.Private)
                {
                    continue;
                }

                if (IsSameType(containingType, propertyLoggerCategoryType))
                {
                    continue;
                }

                yield return (GetPropertyTypeLocation(property), propertyLoggerCategoryType);
            }
        }

        private static bool TryGetLoggerCategoryType(ITypeSymbol typeSymbol, out ITypeSymbol loggerCategoryType)
        {
            loggerCategoryType = null;

            if (!(typeSymbol is INamedTypeSymbol namedTypeSymbol))
            {
                return false;
            }

            if (!string.Equals(namedTypeSymbol.Name, "ILogger", StringComparison.Ordinal))
            {
                return false;
            }

            if (namedTypeSymbol.Arity != 1 || namedTypeSymbol.TypeArguments.Length != 1)
            {
                return false;
            }

            loggerCategoryType = namedTypeSymbol.TypeArguments[0];
            return true;
        }

        private static bool IsSameType(INamedTypeSymbol containingType, ITypeSymbol loggerCategoryType)
        {
            if (loggerCategoryType is ITypeParameterSymbol loggerTypeParameter &&
                containingType.TypeParameters.Any(x => SymbolEqualityComparer.Default.Equals(x, loggerTypeParameter)))
            {
                return true;
            }

            ITypeSymbol normalizedContainingType = NormalizeType(containingType);
            ITypeSymbol normalizedLoggerCategoryType = NormalizeType(loggerCategoryType);
            return SymbolEqualityComparer.Default.Equals(normalizedContainingType, normalizedLoggerCategoryType);
        }

        private static ITypeSymbol NormalizeType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                return namedTypeSymbol.WithNullableAnnotation(NullableAnnotation.None);
            }

            return typeSymbol;
        }

        private static Location GetParameterTypeLocation(IParameterSymbol parameter)
        {
            return GetMemberTypeLocation(
                parameter,
                syntaxNode => syntaxNode is ParameterSyntax parameterSyntax ? parameterSyntax.Type : null
            );
        }

        private static Location GetFieldTypeLocation(IFieldSymbol field)
        {
            return GetMemberTypeLocation(
                field,
                syntaxNode =>
                    syntaxNode is VariableDeclaratorSyntax variableDeclarator &&
                    variableDeclarator.Parent is VariableDeclarationSyntax variableDeclaration
                        ? variableDeclaration.Type
                        : null
            );
        }

        private static Location GetPropertyTypeLocation(IPropertySymbol property)
        {
            return GetMemberTypeLocation(
                property,
                syntaxNode => syntaxNode is PropertyDeclarationSyntax propertyDeclarationSyntax ? propertyDeclarationSyntax.Type : null
            );
        }

        private static Location GetMemberTypeLocation(ISymbol symbol, Func<SyntaxNode, TypeSyntax> getTypeSyntax)
        {
            SyntaxReference syntaxReference = symbol.DeclaringSyntaxReferences.Length > 0
                ? symbol.DeclaringSyntaxReferences[0]
                : null;

            if (syntaxReference == null)
            {
                return Location.None;
            }

            TypeSyntax typeSyntax = getTypeSyntax(syntaxReference.GetSyntax());
            if (typeSyntax == null)
            {
                return Location.None;
            }

            if (TryGetLoggerTypeArgumentLocation(typeSyntax, out Location loggerTypeArgumentLocation))
            {
                return loggerTypeArgumentLocation;
            }

            return typeSyntax.GetLocation();
        }

        private static bool TryGetLoggerTypeArgumentLocation(TypeSyntax loggerTypeSyntax, out Location loggerTypeArgumentLocation)
        {
            loggerTypeArgumentLocation = null;

            GenericNameSyntax loggerGenericType = loggerTypeSyntax
                .DescendantNodesAndSelf()
                .OfType<GenericNameSyntax>()
                .FirstOrDefault(x => x.Identifier.ValueText == "ILogger" && x.TypeArgumentList.Arguments.Count == 1);

            if (loggerGenericType == null)
            {
                return false;
            }

            loggerTypeArgumentLocation = loggerGenericType.TypeArgumentList.Arguments[0].GetLocation();
            return true;
        }

        private static void ReportMismatchDiagnostic(
            SymbolAnalysisContext context,
            Location location,
            ITypeSymbol loggerCategoryType,
            INamedTypeSymbol containingType
        )
        {
            string loggerCategoryTypeDisplayName = loggerCategoryType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            string containingTypeDisplayName = containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            context.ReportDiagnostic(
                Diagnostic.Create(
                    s_rule,
                    location,
                    loggerCategoryTypeDisplayName,
                    containingTypeDisplayName
                )
            );
        }
    }
}
