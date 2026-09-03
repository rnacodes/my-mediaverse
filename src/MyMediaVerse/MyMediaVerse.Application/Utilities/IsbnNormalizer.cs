namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Canonicalizes ISBN values so every write path stores the same shape:
    /// ISBN-13 digits only, no separators. Handles the raw formats seen across
    /// import sources — hyphenated/spaced ISBNs, the Goodreads CSV Excel wrapper
    /// (="0596007124"), and ISBN-10 values (converted to ISBN-13).
    /// </summary>
    public static class IsbnNormalizer
    {
        /// <summary>
        /// Returns the canonical ISBN-13 form of the input, or null when the input
        /// is not a plausible ISBN (wrong length/characters after cleaning).
        /// ISBN-10 inputs are converted to ISBN-13 (978 prefix + recomputed check digit).
        /// </summary>
        public static string? Normalize(string? isbn)
        {
            var cleaned = Clean(isbn);
            if (cleaned == null)
                return null;

            if (cleaned.Length == 13 && cleaned.All(char.IsDigit))
                return cleaned;

            if (IsValidIsbn10Shape(cleaned))
                return ConvertIsbn10To13(cleaned);

            return null;
        }

        /// <summary>
        /// Returns every stored form an existing row might hold for this ISBN:
        /// the canonical ISBN-13 plus, when derivable, the equivalent ISBN-10.
        /// Used by <see cref="BookDuplicateFinder"/> so legacy rows that still
        /// store ISBN-10 values are matched. Empty when the input is not an ISBN.
        /// </summary>
        public static IReadOnlyList<string> GetSearchVariants(string? isbn)
        {
            var isbn13 = Normalize(isbn);
            if (isbn13 == null)
                return Array.Empty<string>();

            var variants = new List<string> { isbn13 };

            var isbn10 = ConvertIsbn13To10(isbn13);
            if (isbn10 != null)
                variants.Add(isbn10);

            return variants;
        }

        /// <summary>
        /// Strips separators (hyphens, spaces), the Goodreads/Excel ="…" wrapper and
        /// stray quotes, and upper-cases a trailing x. Returns null for empty input.
        /// </summary>
        private static string? Clean(string? isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return null;

            var chars = isbn
                .Where(c => c != '-' && c != ' ' && c != '=' && c != '"' && c != '\'')
                .ToArray();

            var cleaned = new string(chars).Trim().ToUpperInvariant();
            return cleaned.Length == 0 ? null : cleaned;
        }

        private static bool IsValidIsbn10Shape(string cleaned)
        {
            return cleaned.Length == 10
                && cleaned.Take(9).All(char.IsDigit)
                && (char.IsDigit(cleaned[9]) || cleaned[9] == 'X');
        }

        private static string ConvertIsbn10To13(string isbn10)
        {
            var core = "978" + isbn10.Substring(0, 9);
            return core + ComputeIsbn13CheckDigit(core);
        }

        private static string? ConvertIsbn13To10(string isbn13)
        {
            // Only 978-prefixed ISBN-13s have an ISBN-10 equivalent.
            if (!isbn13.StartsWith("978"))
                return null;

            var core = isbn13.Substring(3, 9);
            var sum = 0;
            for (var i = 0; i < 9; i++)
            {
                sum += (core[i] - '0') * (10 - i);
            }

            var remainder = (11 - sum % 11) % 11;
            var checkDigit = remainder == 10 ? "X" : remainder.ToString();
            return core + checkDigit;
        }

        private static char ComputeIsbn13CheckDigit(string first12)
        {
            var sum = 0;
            for (var i = 0; i < 12; i++)
            {
                var digit = first12[i] - '0';
                sum += i % 2 == 0 ? digit : digit * 3;
            }

            return (char)('0' + (10 - sum % 10) % 10);
        }
    }
}
