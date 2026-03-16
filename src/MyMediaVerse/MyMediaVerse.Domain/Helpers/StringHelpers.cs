using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMediaVerse.Domain.Helpers
{
    public static class StringHelpers
    {
        public static string[] ParseCommaSeparatedValues(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return Array.Empty<string>();

            return input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToArray();
        }

        public static string JoinValues(IEnumerable<string> values)
        {
            return string.Join(", ", values);
        }
    }
}

