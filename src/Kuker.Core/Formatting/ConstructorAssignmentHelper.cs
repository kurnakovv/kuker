// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Kuker.Core.Formatting
{
    /// <summary>
    /// Shared helpers for detecting simple "field = parameter" constructor assignments.
    /// </summary>
    public static class ConstructorAssignmentHelper
    {
        /// <summary>
        /// Finds every statement in <paramref name="body"/> that is a simple assignment of the form
        /// <c>field = parameter;</c> (or <c>this.field = parameter;</c>), where the right-hand side refers
        /// to a known constructor parameter.
        /// </summary>
        /// <param name="body">Constructor body to scan.</param>
        /// <param name="parameterIndexByName">Map of constructor parameter name to its declaration index.</param>
        /// <returns>Every matching assignment, in source order.</returns>
        public static List<SimpleFieldAssignment> GetSimpleFieldAssignments(
            BlockSyntax body,
            IReadOnlyDictionary<string, int> parameterIndexByName
        )
        {
            List<SimpleFieldAssignment> assignments = new List<SimpleFieldAssignment>();

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

                assignments.Add(new SimpleFieldAssignment(expressionStatement, assignment, fieldName, parameterName, parameterIndex));
            }

            return assignments;
        }

        /// <summary>
        /// Attempts to extract the field name from the left-hand side of an assignment.
        /// </summary>
        /// <param name="expression">Left-hand side expression.</param>
        /// <param name="fieldName">Resolved field name, if any.</param>
        /// <returns><see langword="true"/> if a field name could be extracted.</returns>
        public static bool TryGetFieldName(ExpressionSyntax expression, out string fieldName)
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

        /// <summary>
        /// Attempts to extract the parameter name from the right-hand side of an assignment.
        /// </summary>
        /// <param name="expression">Right-hand side expression.</param>
        /// <param name="parameterName">Resolved parameter name, if any.</param>
        /// <returns><see langword="true"/> if a parameter name could be extracted.</returns>
        public static bool TryGetParameterName(ExpressionSyntax expression, out string parameterName)
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
