// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kuker.Core.Contants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Kuker.CodeFixes.CodeFixProviders
{
    /// <summary>
    /// Code fix for KUK0007 rule.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Kuk0007ILoggerTypeMatchesContainingTypeCodeFixProvider)), Shared]
    public class Kuk0007ILoggerTypeMatchesContainingTypeCodeFixProvider : CodeFixProvider
    {
        private const string MEMBER_ONLY_TITLE_FORMAT = "Change ILogger<T> to ILogger<{0}>";
        private const string CONSTRUCTOR_CHAIN_TITLE_FORMAT = "Change ILogger<T> to ILogger<{0}> for constructor and assigned members";

        /// <summary>
        /// FixableDiagnosticIds.
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIdContant.KUK0007);

        /// <summary>
        /// GetFixAllProvider.
        /// </summary>
        /// <returns>A <see cref="FixAllProvider"/> that can fix all occurrences of a diagnostic.</returns>
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <summary>
        /// Register code fixes.
        /// </summary>
        /// <param name="context">The context in which the code fix is being registered.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics.First();
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            SemanticModel semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null || semanticModel == null)
            {
                return;
            }

            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            if (TryGetParameterFixContext(node, semanticModel, context.CancellationToken, out _, out _, out INamedTypeSymbol constructorContainingType))
            {
                string containingTypeDisplayName = constructorContainingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                string title = string.Format(CONSTRUCTOR_CHAIN_TITLE_FORMAT, containingTypeDisplayName);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: token => UpdateLoggerTypeAsync(context.Document, diagnostic, token),
                        equivalenceKey: title
                    ),
                    diagnostic
                );

                return;
            }

            if (TryGetMemberFixContext(node, semanticModel, context.CancellationToken, out _, out INamedTypeSymbol memberContainingType))
            {
                string containingTypeDisplayName = memberContainingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                string title = string.Format(MEMBER_ONLY_TITLE_FORMAT, containingTypeDisplayName);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: token => UpdateLoggerTypeAsync(context.Document, diagnostic, token),
                        equivalenceKey: title
                    ),
                    diagnostic
                );
            }
        }

        private static async Task<Document> UpdateLoggerTypeAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root == null || semanticModel == null)
            {
                return document;
            }

            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            if (TryGetParameterFixContext(
                node,
                semanticModel,
                cancellationToken,
                out ParameterSyntax parameterSyntax,
                out ConstructorDeclarationSyntax constructorSyntax,
                out INamedTypeSymbol containingType))
            {
                return ApplyConstructorChainFix(document, root, semanticModel, parameterSyntax, constructorSyntax, containingType, cancellationToken);
            }

            if (TryGetMemberFixContext(
                node,
                semanticModel,
                cancellationToken,
                out MemberDeclarationSyntax memberDeclaration,
                out INamedTypeSymbol memberContainingType))
            {
                return ApplyMemberFix(document, root, semanticModel, memberDeclaration, memberContainingType);
            }

            return document;
        }

        private static Document ApplyConstructorChainFix(
            Document document,
            SyntaxNode root,
            SemanticModel semanticModel,
            ParameterSyntax parameterSyntax,
            ConstructorDeclarationSyntax constructorSyntax,
            INamedTypeSymbol containingType,
            CancellationToken cancellationToken)
        {
            IParameterSymbol parameterSymbol = semanticModel.GetDeclaredSymbol(parameterSyntax, cancellationToken);
            if (parameterSymbol == null)
            {
                return document;
            }

            List<TypeSyntax> targetNodes = new List<TypeSyntax>();
            if (TryGetLoggerTypeArgumentSyntax(parameterSyntax.Type, out TypeSyntax parameterTypeArgument))
            {
                targetNodes.Add(parameterTypeArgument);
            }

            foreach (ISymbol memberSymbol in GetDirectlyAssignedMembers(constructorSyntax, parameterSymbol, semanticModel, cancellationToken))
            {
                if (TryGetDeclaredLoggerTypeArgumentSyntax(memberSymbol, out TypeSyntax memberTypeArgument))
                {
                    targetNodes.Add(memberTypeArgument);
                }
            }

            SyntaxNode newRoot = ReplaceLoggerTypeArguments(root, semanticModel, containingType, targetNodes);
            return document.WithSyntaxRoot(newRoot);
        }

        private static Document ApplyMemberFix(
            Document document,
            SyntaxNode root,
            SemanticModel semanticModel,
            MemberDeclarationSyntax memberDeclaration,
            INamedTypeSymbol containingType)
        {
            if (!TryGetLoggerTypeArgumentSyntax(memberDeclaration, out TypeSyntax memberTypeArgument))
            {
                return document;
            }

            SyntaxNode newRoot = ReplaceLoggerTypeArguments(root, semanticModel, containingType, new[] { memberTypeArgument });
            return document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxNode ReplaceLoggerTypeArguments(
            SyntaxNode root,
            SemanticModel semanticModel,
            INamedTypeSymbol containingType,
            IEnumerable<TypeSyntax> targetNodes)
        {
            Dictionary<int, TypeSyntax> uniqueTargetNodes = new Dictionary<int, TypeSyntax>();
            foreach (TypeSyntax targetNode in targetNodes)
            {
                int key = (targetNode.SpanStart * 397) ^ targetNode.Span.Length;
                if (!uniqueTargetNodes.ContainsKey(key))
                {
                    uniqueTargetNodes.Add(key, targetNode);
                }
            }

            return root.ReplaceNodes(
                uniqueTargetNodes.Values,
                (originalNode, _) =>
                {
                    string replacementText = containingType.ToMinimalDisplayString(semanticModel, originalNode.SpanStart);
                    return SyntaxFactory.ParseTypeName(replacementText)
                        .WithTriviaFrom(originalNode);
                });
        }

        private static IEnumerable<ISymbol> GetDirectlyAssignedMembers(
            ConstructorDeclarationSyntax constructorSyntax,
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            HashSet<ISymbol> assignedMembers = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            foreach (AssignmentExpressionSyntax assignment in constructorSyntax.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) || IsNestedInsideExecutableScope(assignment, constructorSyntax))
                {
                    continue;
                }

                ISymbol rightSymbol = semanticModel.GetSymbolInfo(assignment.Right, cancellationToken).Symbol;
                if (!SymbolEqualityComparer.Default.Equals(rightSymbol, parameterSymbol))
                {
                    continue;
                }

                ISymbol leftSymbol = semanticModel.GetSymbolInfo(assignment.Left, cancellationToken).Symbol;
                if (!(leftSymbol is IFieldSymbol) && !(leftSymbol is IPropertySymbol))
                {
                    continue;
                }

                assignedMembers.Add(leftSymbol);
            }

            return assignedMembers;
        }

        private static bool IsNestedInsideExecutableScope(SyntaxNode node, ConstructorDeclarationSyntax constructorSyntax)
        {
            for (SyntaxNode current = node.Parent; current != null && current != constructorSyntax; current = current.Parent)
            {
                if (current is AnonymousFunctionExpressionSyntax || current is LocalFunctionStatementSyntax)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetParameterFixContext(
            SyntaxNode node,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ParameterSyntax parameterSyntax,
            out ConstructorDeclarationSyntax constructorSyntax,
            out INamedTypeSymbol containingType)
        {
            parameterSyntax = node.FirstAncestorOrSelf<ParameterSyntax>();
            constructorSyntax = parameterSyntax?.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            containingType = null;

            if (parameterSyntax?.Type == null || constructorSyntax == null)
            {
                return false;
            }

            if (!TryGetLoggerTypeArgumentSyntax(parameterSyntax.Type, out _))
            {
                return false;
            }

            IMethodSymbol constructorSymbol = semanticModel.GetDeclaredSymbol(constructorSyntax, cancellationToken);
            containingType = constructorSymbol?.ContainingType;
            return containingType != null;
        }

        private static bool TryGetMemberFixContext(
            SyntaxNode node,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out MemberDeclarationSyntax memberDeclaration,
            out INamedTypeSymbol containingType)
        {
            memberDeclaration = node.FirstAncestorOrSelf<FieldDeclarationSyntax>();
            if (memberDeclaration != null && TryGetLoggerTypeArgumentSyntax(memberDeclaration, out _))
            {
                containingType = GetContainingType(memberDeclaration, semanticModel, cancellationToken);
                return containingType != null;
            }

            memberDeclaration = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (memberDeclaration != null && TryGetLoggerTypeArgumentSyntax(memberDeclaration, out _))
            {
                containingType = GetContainingType(memberDeclaration, semanticModel, cancellationToken);
                return containingType != null;
            }

            containingType = null;
            return false;
        }

        private static INamedTypeSymbol GetContainingType(
            MemberDeclarationSyntax memberDeclaration,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (memberDeclaration is FieldDeclarationSyntax fieldDeclaration)
            {
                VariableDeclaratorSyntax variable = fieldDeclaration.Declaration?.Variables.FirstOrDefault();
                return semanticModel.GetDeclaredSymbol(variable, cancellationToken)?.ContainingType;
            }

            if (memberDeclaration is PropertyDeclarationSyntax propertyDeclaration)
            {
                return semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken)?.ContainingType;
            }

            return null;
        }

        private static bool TryGetDeclaredLoggerTypeArgumentSyntax(ISymbol symbol, out TypeSyntax loggerTypeArgumentSyntax)
        {
            loggerTypeArgumentSyntax = null;

            SyntaxReference syntaxReference = symbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxReference == null)
            {
                return false;
            }

            SyntaxNode syntaxNode = syntaxReference.GetSyntax();
            if (syntaxNode is VariableDeclaratorSyntax variableDeclarator
                && variableDeclarator.Parent is VariableDeclarationSyntax variableDeclaration)
            {
                return TryGetLoggerTypeArgumentSyntax(variableDeclaration.Type, out loggerTypeArgumentSyntax);
            }

            if (syntaxNode is PropertyDeclarationSyntax propertyDeclaration)
            {
                return TryGetLoggerTypeArgumentSyntax(propertyDeclaration.Type, out loggerTypeArgumentSyntax);
            }

            return false;
        }

        private static bool TryGetLoggerTypeArgumentSyntax(MemberDeclarationSyntax memberDeclaration, out TypeSyntax loggerTypeArgumentSyntax)
        {
            loggerTypeArgumentSyntax = null;

            if (memberDeclaration is FieldDeclarationSyntax fieldDeclaration)
            {
                return fieldDeclaration.Declaration != null
                    && TryGetLoggerTypeArgumentSyntax(fieldDeclaration.Declaration.Type, out loggerTypeArgumentSyntax);
            }

            if (memberDeclaration is PropertyDeclarationSyntax propertyDeclaration)
            {
                return TryGetLoggerTypeArgumentSyntax(propertyDeclaration.Type, out loggerTypeArgumentSyntax);
            }

            return false;
        }

        private static bool TryGetLoggerTypeArgumentSyntax(TypeSyntax loggerTypeSyntax, out TypeSyntax loggerTypeArgumentSyntax)
        {
            loggerTypeArgumentSyntax = loggerTypeSyntax?
                .DescendantNodesAndSelf()
                .OfType<GenericNameSyntax>()
                .FirstOrDefault(x => x.Identifier.ValueText == "ILogger" && x.TypeArgumentList.Arguments.Count == 1)
                ?.TypeArgumentList.Arguments[0];

            return loggerTypeArgumentSyntax != null;
        }
    }
}
