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
using Microsoft.CodeAnalysis.Text;

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
        private const string MEMBER_ONLY_EQUIVALENCE_KEY = "KUK0007_MemberOnly";
        private const string CONSTRUCTOR_CHAIN_EQUIVALENCE_KEY = "KUK0007_ConstructorChain";

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
            if (TryGetParameterFixContext(
                node,
                semanticModel,
                context.CancellationToken,
                out _,
                out ConstructorDeclarationSyntax constructorSyntax,
                out INamedTypeSymbol constructorContainingType))
            {
                string containingTypeDisplayName = constructorContainingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                string title = constructorSyntax != null
                    ? string.Format(CONSTRUCTOR_CHAIN_TITLE_FORMAT, containingTypeDisplayName)
                    : string.Format(MEMBER_ONLY_TITLE_FORMAT, containingTypeDisplayName);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: token => Task.FromResult(UpdateLoggerType(context.Document, diagnostic, root, semanticModel, token)),
                        equivalenceKey: CONSTRUCTOR_CHAIN_EQUIVALENCE_KEY
                    ),
                    diagnostic
                );

                return;
            }

            if (TryGetMemberFixContext(
                node,
                semanticModel,
                context.CancellationToken,
                out _,
                out INamedTypeSymbol memberContainingType))
            {
                string containingTypeDisplayName = memberContainingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                string title = string.Format(MEMBER_ONLY_TITLE_FORMAT, containingTypeDisplayName);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: token => Task.FromResult(UpdateLoggerType(context.Document, diagnostic, root, semanticModel, token)),
                        equivalenceKey: MEMBER_ONLY_EQUIVALENCE_KEY
                    ),
                    diagnostic
                );
            }
        }

        private static Document UpdateLoggerType(
            Document document,
            Diagnostic diagnostic,
            SyntaxNode root,
            SemanticModel semanticModel,
            CancellationToken cancellationToken
        )
        {
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
                return ApplyMemberFix(document, root, semanticModel, memberDeclaration, memberContainingType, cancellationToken);
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

            List<(TypeSyntax Node, bool ReplaceWholeType)> targetNodes = new List<(TypeSyntax Node, bool ReplaceWholeType)>();
            if (TryGetLoggerReplacementTarget(parameterSyntax.Type, semanticModel, cancellationToken, out TypeSyntax parameterReplacementNode, out bool parameterReplaceWholeType))
            {
                targetNodes.Add((parameterReplacementNode, parameterReplaceWholeType));
            }

            if (constructorSyntax != null)
            {
                IEnumerable<(TypeSyntax Node, bool ReplaceWholeType)> memberTypeArguments = GetDirectlyAssignedMembers(constructorSyntax, parameterSymbol, semanticModel, cancellationToken)
                    .Select(memberSymbol =>
                    {
                        return TryGetDeclaredLoggerReplacementTargetSyntax(
                            memberSymbol,
                            root.SyntaxTree,
                            semanticModel,
                            cancellationToken,
                            out TypeSyntax memberReplacementNode,
                            out bool memberReplaceWholeType
                        )
                            ? (memberReplacementNode, memberReplaceWholeType)
                            : ((TypeSyntax Node, bool ReplaceWholeType)?)null;
                    })
                    .Where(x => x.HasValue)
                    .Select(x => x.GetValueOrDefault());

                targetNodes.AddRange(memberTypeArguments);
            }

            SyntaxNode newRoot = ReplaceLoggerTypeArguments(root, semanticModel, containingType, targetNodes);
            return document.WithSyntaxRoot(newRoot);
        }

        private static Document ApplyMemberFix(
            Document document,
            SyntaxNode root,
            SemanticModel semanticModel,
            MemberDeclarationSyntax memberDeclaration,
            INamedTypeSymbol containingType,
            CancellationToken cancellationToken)
        {
            TypeSyntax memberTypeSyntax = GetMemberTypeSyntax(memberDeclaration);
            if (memberTypeSyntax == null || !TryGetLoggerReplacementTarget(memberTypeSyntax, semanticModel, cancellationToken, out TypeSyntax memberReplacementNode, out bool memberReplaceWholeType))
            {
                return document;
            }

            SyntaxNode newRoot = ReplaceLoggerTypeArguments(root, semanticModel, containingType, new[] { (memberReplacementNode, memberReplaceWholeType) });
            return document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxNode ReplaceLoggerTypeArguments(
            SyntaxNode root,
            SemanticModel semanticModel,
            INamedTypeSymbol containingType,
            IEnumerable<(TypeSyntax Node, bool ReplaceWholeType)> targetNodes)
        {
            Dictionary<TextSpan, (TypeSyntax Node, bool ReplaceWholeType)> uniqueTargetNodes = new Dictionary<TextSpan, (TypeSyntax Node, bool ReplaceWholeType)>();
            foreach ((TypeSyntax Node, bool ReplaceWholeType) targetNode in targetNodes)
            {
                TextSpan span = targetNode.Node.Span;
                if (!uniqueTargetNodes.ContainsKey(span))
                {
                    uniqueTargetNodes.Add(span, targetNode);
                }
            }

            return root.ReplaceNodes(
                uniqueTargetNodes.Values.Select(x => x.Node),
                (originalNode, _) =>
                {
                    bool replaceWholeType = uniqueTargetNodes[originalNode.Span].ReplaceWholeType;
                    string replacementText = replaceWholeType
                        ? $"ILogger<{containingType.ToMinimalDisplayString(semanticModel, originalNode.SpanStart)}>"
                        : containingType.ToMinimalDisplayString(semanticModel, originalNode.SpanStart);

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
            constructorSyntax = null;
            containingType = null;

            if (parameterSyntax?.Type == null || !IsILoggerType(parameterSyntax.Type, semanticModel, cancellationToken))
            {
                return false;
            }

            constructorSyntax = parameterSyntax.Parent?.Parent as ConstructorDeclarationSyntax;
            if (constructorSyntax != null)
            {
                IMethodSymbol constructorSymbol = semanticModel.GetDeclaredSymbol(constructorSyntax, cancellationToken);
                containingType = constructorSymbol?.ContainingType;
                return containingType != null;
            }

            TypeDeclarationSyntax typeDeclarationSyntax = parameterSyntax.Parent?.Parent as TypeDeclarationSyntax;
            if (typeDeclarationSyntax?.ParameterList == parameterSyntax.Parent)
            {
                containingType = semanticModel.GetDeclaredSymbol(typeDeclarationSyntax, cancellationToken);
                return containingType != null;
            }

            return false;
        }

        private static bool TryGetMemberFixContext(
            SyntaxNode node,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out MemberDeclarationSyntax memberDeclaration,
            out INamedTypeSymbol containingType)
        {
            memberDeclaration = node.FirstAncestorOrSelf<FieldDeclarationSyntax>();
            if (memberDeclaration != null)
            {
                TypeSyntax fieldType = GetMemberTypeSyntax(memberDeclaration);
                if (fieldType != null && IsILoggerType(fieldType, semanticModel, cancellationToken))
                {
                    containingType = GetContainingType(memberDeclaration, semanticModel, cancellationToken);
                    return containingType != null;
                }
            }

            memberDeclaration = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (memberDeclaration != null)
            {
                TypeSyntax propertyType = GetMemberTypeSyntax(memberDeclaration);
                if (propertyType != null && IsILoggerType(propertyType, semanticModel, cancellationToken))
                {
                    containingType = GetContainingType(memberDeclaration, semanticModel, cancellationToken);
                    return containingType != null;
                }
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
                VariableDeclaratorSyntax variable = fieldDeclaration.Declaration.Variables.FirstOrDefault();
                return semanticModel.GetDeclaredSymbol(variable, cancellationToken)?.ContainingType;
            }

            if (memberDeclaration is PropertyDeclarationSyntax propertyDeclaration)
            {
                return semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken)?.ContainingType;
            }

            return null;
        }

        private static bool TryGetDeclaredLoggerReplacementTargetSyntax(
            ISymbol symbol,
            SyntaxTree currentSyntaxTree,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out TypeSyntax replacementTargetNode,
            out bool replaceWholeType)
        {
            replacementTargetNode = null;
            replaceWholeType = false;

            SyntaxReference syntaxReference = symbol.DeclaringSyntaxReferences.FirstOrDefault(x => x.SyntaxTree == currentSyntaxTree);
            if (syntaxReference == null)
            {
                return false;
            }

            SyntaxNode syntaxNode = syntaxReference.GetSyntax(cancellationToken);
            if (syntaxNode is VariableDeclaratorSyntax variableDeclarator
                && variableDeclarator.Parent is VariableDeclarationSyntax variableDeclaration)
            {
                return TryGetLoggerReplacementTarget(variableDeclaration.Type, semanticModel, cancellationToken, out replacementTargetNode, out replaceWholeType);
            }

            if (syntaxNode is PropertyDeclarationSyntax propertyDeclaration)
            {
                return TryGetLoggerReplacementTarget(propertyDeclaration.Type, semanticModel, cancellationToken, out replacementTargetNode, out replaceWholeType);
            }

            return false;
        }

        private static TypeSyntax GetMemberTypeSyntax(MemberDeclarationSyntax memberDeclaration)
        {
            if (memberDeclaration is FieldDeclarationSyntax fieldDeclaration)
            {
                return fieldDeclaration.Declaration.Type;
            }

            if (memberDeclaration is PropertyDeclarationSyntax propertyDeclaration)
            {
                return propertyDeclaration.Type;
            }

            return null;
        }

        private static bool TryGetLoggerReplacementTarget(
            TypeSyntax loggerTypeSyntax,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out TypeSyntax replacementTargetNode,
            out bool replaceWholeType)
        {
            replacementTargetNode = null;
            replaceWholeType = false;

            if (loggerTypeSyntax == null)
            {
                return false;
            }

            if (TryGetLoggerTypeArgumentSyntax(loggerTypeSyntax, out TypeSyntax loggerTypeArgumentSyntax))
            {
                replacementTargetNode = loggerTypeArgumentSyntax;
                return true;
            }

            ITypeSymbol loggerTypeSymbol = semanticModel.GetTypeInfo(loggerTypeSyntax, cancellationToken).Type;
            if (IsILoggerTypeSymbol(loggerTypeSymbol))
            {
                replacementTargetNode = loggerTypeSyntax;
                replaceWholeType = true;
                return true;
            }

            return false;
        }

        private static bool IsILoggerType(TypeSyntax typeSyntax, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (TryGetLoggerTypeArgumentSyntax(typeSyntax, out _))
            {
                return true;
            }

            ITypeSymbol loggerTypeSymbol = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type;
            return IsILoggerTypeSymbol(loggerTypeSymbol);
        }

        private static bool IsILoggerTypeSymbol(ITypeSymbol loggerTypeSymbol)
        {
            return loggerTypeSymbol is INamedTypeSymbol namedTypeSymbol
                && namedTypeSymbol.Name == "ILogger"
                && namedTypeSymbol.Arity == 1;
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
