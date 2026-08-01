// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Kuker.Core.Options
{
    /// <summary>
    /// Shared option metadata for <c>dotnet_diagnostic.KUK0006.syntax_kinds</c>.
    /// </summary>
    public static class Kuk0006TargetSyntaxOption
    {
        /// <summary>
        /// The .editorconfig / analyzerconfig option key.
        /// </summary>
        public const string KEY = "dotnet_diagnostic.KUK0006.syntax_kinds";

        /// <summary>
        /// Apply KUK0006 to method invocation expressions.
        /// </summary>
        public const string METHOD_INVOCATION = "method_invocation";

        /// <summary>
        /// Apply KUK0006 to object creation expressions.
        /// </summary>
        public const string OBJECT_CREATION = "object_creation";

        /// <summary>
        /// Apply KUK0006 to method declaration parameter lists.
        /// </summary>
        public const string METHOD_DECLARATION = "method_declaration";

        private static readonly string[] s_supportedSyntaxes = new[]
        {
            METHOD_INVOCATION,
            OBJECT_CREATION,
            METHOD_DECLARATION,
        };

        /// <summary>
        /// Parse and validate option value.
        /// </summary>
        /// <param name="value">Raw config value.</param>
        /// <param name="targetSyntaxes">Configured syntaxes to analyze.</param>
        /// <returns>True when value is valid; otherwise false.</returns>
        public static bool TryParse(string value, out HashSet<string> targetSyntaxes)
        {
            targetSyntaxes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(value))
            {
                foreach (string syntax in s_supportedSyntaxes)
                {
                    targetSyntaxes.Add(syntax);
                }

                return true;
            }

            foreach (string token in value.Trim().Split(',').Select(token => token.Trim()))
            {
                if (token.Length == 0)
                {
                    return false;
                }

                if (!IsSupportedSyntax(token) || !targetSyntaxes.Add(token))
                {
                    return false;
                }
            }

            return targetSyntaxes.Count > 0;
        }

        private static bool IsSupportedSyntax(string syntax)
        {
            return string.Equals(syntax, METHOD_INVOCATION, StringComparison.OrdinalIgnoreCase)
                || string.Equals(syntax, OBJECT_CREATION, StringComparison.OrdinalIgnoreCase)
                || string.Equals(syntax, METHOD_DECLARATION, StringComparison.OrdinalIgnoreCase);
        }
    }
}
