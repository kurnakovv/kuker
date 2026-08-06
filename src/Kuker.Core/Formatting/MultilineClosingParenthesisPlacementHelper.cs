// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Kuker.Core.Formatting
{
    /// <summary>
    /// Shared placement calculations for multiline closing parenthesis rules.
    /// </summary>
    public static class MultilineClosingParenthesisPlacementHelper
    {
        /// <summary>
        /// Calculates a complete placement model used by diagnostics and code fixes.
        /// </summary>
        /// <param name="text">Source text containing the invocation.</param>
        /// <param name="openParen">Opening parenthesis token.</param>
        /// <param name="closeParen">Closing parenthesis token.</param>
        /// <returns>Calculated placement details.</returns>
        public static MultilineClosingParenthesisPlacement GetPlacement(SourceText text, SyntaxToken openParen, SyntaxToken closeParen)
        {
            int anchorColumn = GetAnchorColumn(text, openParen);
            bool hasCodeOnTheLeft = IsCodeOnTheLeft(text, closeParen);
            TextLine closeLine = text.Lines.GetLineFromPosition(closeParen.SpanStart);

            int expectedLineNumber = hasCodeOnTheLeft
                ? closeLine.LineNumber + 2
                : closeLine.LineNumber + 1;

            int expectedCharacter = anchorColumn + 1;

            return new MultilineClosingParenthesisPlacement(anchorColumn, hasCodeOnTheLeft, expectedLineNumber, expectedCharacter);
        }

        /// <summary>
        /// Returns the first non-whitespace column of the line containing the opening parenthesis.
        /// </summary>
        /// <param name="text">Source text containing the invocation.</param>
        /// <param name="openParen">Opening parenthesis token.</param>
        /// <returns>Zero-based anchor column.</returns>
        private static int GetAnchorColumn(SourceText text, SyntaxToken openParen)
        {
            TextLine openLine = text.Lines.GetLineFromPosition(openParen.SpanStart);
            string openLineText = openLine.ToString();

            for (int index = 0; index < openLineText.Length; index++)
            {
                if (!char.IsWhiteSpace(openLineText[index]))
                {
                    return index;
                }
            }

            return 0;
        }

        /// <summary>
        /// Determines whether there is non-whitespace code to the left of the closing parenthesis on its line.
        /// </summary>
        /// <param name="text">Source text containing the invocation.</param>
        /// <param name="closeParen">Closing parenthesis token.</param>
        /// <returns><see langword="true"/> if there is code on the left; otherwise <see langword="false"/>.</returns>
        private static bool IsCodeOnTheLeft(SourceText text, SyntaxToken closeParen)
        {
            TextLine closeLine = text.Lines.GetLineFromPosition(closeParen.SpanStart);
            string closeLineText = closeLine.ToString();
            int closeColumnInLine = closeParen.SpanStart - closeLine.Start;

            if (closeColumnInLine <= 0)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(closeLineText.Substring(0, closeColumnInLine));
        }
    }
}
