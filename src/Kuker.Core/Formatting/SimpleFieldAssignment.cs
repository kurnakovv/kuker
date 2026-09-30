// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Kuker.Core.Formatting
{
    /// <summary>
    /// Represents a simple "field = parameter" assignment found in a constructor body.
    /// </summary>
    public sealed class SimpleFieldAssignment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleFieldAssignment"/> class.
        /// </summary>
        /// <param name="statement">The statement containing the assignment.</param>
        /// <param name="fieldName">The assigned field name.</param>
        /// <param name="parameterName">The constructor parameter name used as the assignment source.</param>
        /// <param name="parameterIndex">The declaration index of the constructor parameter.</param>
        public SimpleFieldAssignment(
            ExpressionStatementSyntax statement,
            string fieldName,
            string parameterName,
            int parameterIndex
        )
        {
            Statement = statement;
            FieldName = fieldName;
            ParameterName = parameterName;
            ParameterIndex = parameterIndex;
        }

        /// <summary>
        /// Gets the statement containing the assignment.
        /// </summary>
        public ExpressionStatementSyntax Statement { get; }

        /// <summary>
        /// Gets the assigned field name.
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// Gets the constructor parameter name used as the assignment source.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// Gets the declaration index of the constructor parameter.
        /// </summary>
        public int ParameterIndex { get; }
    }
}
