using System;
using System.Collections.Generic;
using System.Linq;

namespace Bundle
{
    public static class Languages
    {
        public const string All = "all";

        public static readonly Dictionary<string, string[]> Extensions =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "csharp", new[] { ".cs" } },
                { "cs", new[] { ".cs" } },
                { "java", new[] { ".java" } },
                { "python", new[] { ".py" } },
                { "py", new[] { ".py" } },
                { "javascript", new[] { ".js", ".jsx" } },
                { "js", new[] { ".js", ".jsx" } },
                { "typescript", new[] { ".ts", ".tsx" } },
                { "ts", new[] { ".ts", ".tsx" } },
                { "html", new[] { ".html", ".htm" } },
                { "css", new[] { ".css", ".scss", ".sass", ".less" } },
                { "c", new[] { ".c", ".h" } },
                { "cpp", new[] { ".cpp", ".cc", ".cxx", ".hpp", ".h" } },
                { "sql", new[] { ".sql" } },
                { "go", new[] { ".go" } },
                { "kotlin", new[] { ".kt", ".kts" } },
                { "php", new[] { ".php" } },
                { "ruby", new[] { ".rb" } },
                { "swift", new[] { ".swift" } },
                { "rust", new[] { ".rs" } }
            };

        public static IEnumerable<string> Names
        {
            get
            {
                return Extensions.Keys
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(
                        name => name,
                        StringComparer.OrdinalIgnoreCase);
            }
        }

        public static HashSet<string> AllExtensions
        {
            get
            {
                return new HashSet<string>(
                    Extensions.Values.SelectMany(
                        extensions => extensions),
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        public static HashSet<string> Resolve(
            IEnumerable<string> languages)
        {
            if (languages == null)
                throw new ArgumentNullException(nameof(languages));

            var list = languages
                .Where(language =>
                    !string.IsNullOrWhiteSpace(language))
                .Select(language => language.Trim())
                .ToList();

            var error = Validate(list);

            if (error != null)
                throw new ArgumentException(
                    error,
                    nameof(languages));

            if (list.Any(language =>
                language.Equals(
                    All,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            return new HashSet<string>(
                list.SelectMany(language => Extensions[language]),
                StringComparer.OrdinalIgnoreCase);
        }

        public static string Validate(
            IReadOnlyCollection<string> languages)
        {
            if (languages == null)
                return "רשימת השפות חסרה.";

            if (languages.Count == 0)
                return "יש לציין לפחות שפה אחת, או all.";

            if (languages.Any(string.IsNullOrWhiteSpace))
                return "שם שפה אינו יכול להיות ריק.";

            var hasAll = languages.Any(language =>
                language.Equals(
                    All,
                    StringComparison.OrdinalIgnoreCase));

            if (hasAll && languages.Count > 1)
                return "לא ניתן לשלב את all עם שפות נוספות.";

            var unknown = languages
                .Where(language =>
                    !language.Equals(
                        All,
                        StringComparison.OrdinalIgnoreCase)
                    && !Extensions.ContainsKey(language))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (unknown.Count > 0)
            {
                return
                    $"שפה לא מוכרת: {string.Join(", ", unknown)}. " +
                    $"שפות נתמכות: all, {string.Join(", ", Names)}";
            }

            return null;
        }
    }
}
