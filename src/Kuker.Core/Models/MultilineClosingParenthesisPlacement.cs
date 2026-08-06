// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

namespace Kuker.Core.Models
{
    /// <summary>
    /// Placement details for a multiline closing parenthesis.
    /// </summary>
    public readonly struct MultilineClosingParenthesisPlacement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultilineClosingParenthesisPlacement"/> struct.
        /// </summary>
        /// <param name="anchorColumn">Zero-based anchor column.</param>
        /// <param name="hasCodeOnTheLeft">Whether non-whitespace code appears to the left of the closing parenthesis.</param>
        /// <param name="expectedLineNumber">Expected one-based line number for diagnostics.</param>
        /// <param name="expectedCharacter">Expected one-based character index for diagnostics.</param>
        public MultilineClosingParenthesisPlacement(int anchorColumn, bool hasCodeOnTheLeft, int expectedLineNumber, int expectedCharacter)
        {
            AnchorColumn = anchorColumn;
            HasCodeOnTheLeft = hasCodeOnTheLeft;
            ExpectedLineNumber = expectedLineNumber;
            ExpectedCharacter = expectedCharacter;
        }

        /// <summary>
        /// Gets the zero-based anchor column derived from the opening line.
        /// </summary>
        public int AnchorColumn { get; }

        /// <summary>
        /// Gets a value indicating whether non-whitespace code appears to the left of the closing parenthesis.
        /// </summary>
        public bool HasCodeOnTheLeft { get; }

        /// <summary>
        /// Gets the expected one-based line number for the closing parenthesis.
        /// </summary>
        public int ExpectedLineNumber { get; }

        /// <summary>
        /// Gets the expected one-based character index for the closing parenthesis.
        /// </summary>
        public int ExpectedCharacter { get; }

        /// <summary>
        /// Gets indentation text based on the anchor column.
        /// </summary>
        public string Indentation => new string(' ', AnchorColumn);
    }
}
