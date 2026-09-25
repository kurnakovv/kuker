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
using Microsoft.CodeAnalysis.Editing;

namespace Kuker.CodeFixes.CodeFixProviders
{
    /// <summary>
    /// Code fix for KUK0008 rule.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Kuk0008ConstructorMappingOrderCodeFixProvider)), Shared]
    public class Kuk0008ConstructorMappingOrderCodeFixProvider : CodeFixProvider
    {
        private const string TITLE = "Reorder fields and assignments to match constructor parameter order";

        /// <summary>
        /// FixableDiagnosticIds.
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIdContant.KUK0008);

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
            if (root == null)
            {
                return;
            }

            SyntaxNode diagnosticNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            ConstructorDeclarationSyntax constructorDeclaration = diagnosticNode.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (constructorDeclaration == null)
            {
                return;
            }

            if (!(constructorDeclaration.Parent is ClassDeclarationSyntax) &&
                !(constructorDeclaration.Parent is StructDeclarationSyntax) &&
                !(constructorDeclaration.Parent is RecordDeclarationSyntax))
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: TITLE,
                    createChangedDocument: token => ReorderAsync(context.Document, constructorDeclaration, token),
                    equivalenceKey: TITLE
                ),
                diagnostic
            );
        }

        private static async Task<Document> ReorderAsync(
            Document document,
            ConstructorDeclarationSyntax constructorDeclaration,
            CancellationToken cancellationToken
        )
        {
            SyntaxNode typeDeclaration = constructorDeclaration.Parent;
            if (typeDeclaration == null)
            {
                return document;
            }

            BlockSyntax body = constructorDeclaration.Body;
            if (body == null)
            {
                return document;
            }

            SyntaxList<MemberDeclarationSyntax> members = GetMembers(typeDeclaration);

            List<string> parameterOrder = constructorDeclaration.ParameterList.Parameters
                .Select(x => x.Identifier.ValueText)
                .ToList();
            Dictionary<string, int> parameterIndexByName = new Dictionary<string, int>(System.StringComparer.Ordinal);
            for (int i = 0; i < parameterOrder.Count; i++)
            {
                parameterIndexByName[parameterOrder[i]] = i;
            }

            Dictionary<string, int> fieldParameterIndexByFieldName = new Dictionary<string, int>(System.StringComparer.Ordinal);

            foreach (StatementSyntax statement in body.Statements)
            {
                if (!(statement is ExpressionStatementSyntax expressionStatement) ||
                    !(expressionStatement.Expression is AssignmentExpressionSyntax assignment) ||
                    !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    continue;
                }

                if (!TryGetFieldName(assignment.Left, out string fieldName) ||
                    !TryGetParameterName(assignment.Right, out string parameterName) ||
                    !parameterIndexByName.TryGetValue(parameterName, out int parameterIndex))
                {
                    continue;
                }

                fieldParameterIndexByFieldName[fieldName] = parameterIndex;
            }

            List<FieldDeclarationSyntax> fieldsToReorder = new List<FieldDeclarationSyntax>();
            Dictionary<FieldDeclarationSyntax, int> fieldParameterIndex = new Dictionary<FieldDeclarationSyntax, int>();

            foreach (MemberDeclarationSyntax member in members)
            {
                if (!(member is FieldDeclarationSyntax fieldDeclaration))
                {
                    continue;
                }

                if (fieldDeclaration.Declaration.Variables.Count != 1)
                {
                    continue;
                }

                string fieldName = fieldDeclaration.Declaration.Variables[0].Identifier.ValueText;
                if (!fieldParameterIndexByFieldName.TryGetValue(fieldName, out int parameterIndex))
                {
                    continue;
                }

                fieldsToReorder.Add(fieldDeclaration);
                fieldParameterIndex[fieldDeclaration] = parameterIndex;
            }

            if (fieldsToReorder.Count < 2)
            {
                return await ReorderAssignmentsOnlyAsync(document, constructorDeclaration, parameterIndexByName, cancellationToken)
                    .ConfigureAwait(false);
            }

            List<FieldDeclarationSyntax> reorderedFields = fieldsToReorder
                .OrderBy(x => fieldParameterIndex[x])
                .ToList();

            List<FieldDeclarationSyntax> replacementFields = new List<FieldDeclarationSyntax>();
            for (int i = 0; i < fieldsToReorder.Count; i++)
            {
                FieldDeclarationSyntax originalField = fieldsToReorder[i];
                FieldDeclarationSyntax sourceField = reorderedFields[i];

                FieldDeclarationSyntax replacementField = originalField
                    .WithAttributeLists(sourceField.AttributeLists)
                    .WithDeclaration(sourceField.Declaration)
                    .WithModifiers(sourceField.Modifiers)
                    .WithLeadingTrivia(NormalizeLeadingTrivia(originalField.GetLeadingTrivia(), sourceField.GetLeadingTrivia()))
                    .WithTrailingTrivia(sourceField.GetTrailingTrivia());

                replacementFields.Add(replacementField);
            }

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            for (int i = 0; i < fieldsToReorder.Count; i++)
            {
                editor.ReplaceNode(fieldsToReorder[i], replacementFields[i]);
            }

            ReorderAssignmentsInEditor(editor, constructorDeclaration, parameterIndexByName);

            return editor.GetChangedDocument();
        }

        private static async Task<Document> ReorderAssignmentsOnlyAsync(
            Document document,
            ConstructorDeclarationSyntax constructorDeclaration,
            Dictionary<string, int> parameterIndexByName,
            CancellationToken cancellationToken
        )
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            ReorderAssignmentsInEditor(editor, constructorDeclaration, parameterIndexByName);

            return editor.GetChangedDocument();
        }

        private static void ReorderAssignmentsInEditor(
            DocumentEditor editor,
            ConstructorDeclarationSyntax constructorDeclaration,
            Dictionary<string, int> parameterIndexByName
        )
        {
            BlockSyntax body = constructorDeclaration.Body;
            if (body == null)
            {
                return;
            }

            List<ExpressionStatementSyntax> assignmentStatements = new List<ExpressionStatementSyntax>();
            Dictionary<ExpressionStatementSyntax, int> assignmentParameterIndex = new Dictionary<ExpressionStatementSyntax, int>();

            foreach (StatementSyntax statement in body.Statements)
            {
                if (!(statement is ExpressionStatementSyntax expressionStatement) ||
                    !(expressionStatement.Expression is AssignmentExpressionSyntax assignment) ||
                    !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    return;
                }

                if (!TryGetParameterName(assignment.Right, out string parameterName) ||
                    !parameterIndexByName.TryGetValue(parameterName, out int parameterIndex))
                {
                    return;
                }

                assignmentStatements.Add(expressionStatement);
                assignmentParameterIndex[expressionStatement] = parameterIndex;
            }

            if (assignmentStatements.Count < 2)
            {
                return;
            }

            List<ExpressionStatementSyntax> reorderedStatements = assignmentStatements
                .OrderBy(x => assignmentParameterIndex[x])
                .ToList();

            List<ExpressionStatementSyntax> replacementStatements = new List<ExpressionStatementSyntax>();
            for (int i = 0; i < assignmentStatements.Count; i++)
            {
                ExpressionStatementSyntax originalStatement = assignmentStatements[i];
                ExpressionStatementSyntax sourceStatement = reorderedStatements[i];

                ExpressionStatementSyntax replacementStatement = originalStatement
                    .WithExpression(sourceStatement.Expression)
                    .WithLeadingTrivia(sourceStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(sourceStatement.GetTrailingTrivia());

                replacementStatements.Add(replacementStatement);
            }

            for (int i = 0; i < assignmentStatements.Count; i++)
            {
                editor.ReplaceNode(assignmentStatements[i], replacementStatements[i]);
            }
        }

        private static SyntaxList<MemberDeclarationSyntax> GetMembers(SyntaxNode typeDeclaration)
        {
            switch (typeDeclaration)
            {
                case ClassDeclarationSyntax classDeclaration:
                    return classDeclaration.Members;
                case StructDeclarationSyntax structDeclaration:
                    return structDeclaration.Members;
                case RecordDeclarationSyntax recordDeclaration:
                    return recordDeclaration.Members;
                default:
                    return default;
            }
        }

        private static SyntaxTriviaList NormalizeLeadingTrivia(SyntaxTriviaList originalLeadingTrivia, SyntaxTriviaList sourceLeadingTrivia)
        {
            int originalPrefixLength = CountLeadingBlankTrivia(originalLeadingTrivia);
            int sourcePrefixLength = CountLeadingBlankTrivia(sourceLeadingTrivia);

            IEnumerable<SyntaxTrivia> prefix = originalLeadingTrivia.Take(originalPrefixLength);
            IEnumerable<SyntaxTrivia> remainder = sourceLeadingTrivia.Skip(sourcePrefixLength);

            return SyntaxFactory.TriviaList(prefix.Concat(remainder));
        }

        private static int CountLeadingBlankTrivia(SyntaxTriviaList triviaList)
        {
            int count = 0;

            while (count < triviaList.Count &&
                (triviaList[count].IsKind(SyntaxKind.WhitespaceTrivia) || triviaList[count].IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                count++;
            }

            return count;
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
