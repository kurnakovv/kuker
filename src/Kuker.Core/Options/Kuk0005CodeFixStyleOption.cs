// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;

namespace Kuker.Core.Options
{
    /// <summary>
    /// Shared option metadata for <c>dotnet_diagnostic.KUK0005.code_fix_style</c>.
    /// </summary>
    public static class Kuk0005CodeFixStyleOption
    {
        /// <summary>
        /// The .editorconfig / analyzerconfig option key.
        /// </summary>
        public const string KEY = "dotnet_diagnostic.KUK0005.code_fix_style";

        /// <summary>
        /// Inserts <c>.TagWithCallSite()</c> on the same line as the preceding expression.
        /// </summary>
        public const string INLINE = "inline";

        /// <summary>
        /// Inserts <c>.TagWithCallSite()</c> on a new line, following the multiline indentation of the chain.
        /// </summary>
        public const string NEWLINE = "newline";

        /// <summary>
        /// Returns <see langword="true"/> when <paramref name="value"/> is a recognised option value
        /// (empty/null, <c>inline</c>, or <c>newline</c>).
        /// </summary>
        /// <param name="value">The raw string read from the config file.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is valid; otherwise <see langword="false"/>.</returns>
        public static bool IsValid(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            string trimmed = value.Trim();

            return string.Equals(trimmed, INLINE, StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, NEWLINE, StringComparison.OrdinalIgnoreCase);
        }
    }
}
