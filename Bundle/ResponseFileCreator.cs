using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;

namespace Bundle
{
    public static class ResponseFileCreator
    {
        public static int Run()
        {
            var languages = Ask(
                "אילו שפות לארוז? הפרידי ברווח או בפסיק, או הקלידי all.\n" +
                "שפות נתמכות: " + string.Join(", ", Languages.Names),
                input => Languages.Validate(SplitWords(input)));

            var output = Ask(
                "מה שם קובץ ה-bundle? אפשר לציין שם או נתיב מלא.",
                input =>
                {
                    if (string.IsNullOrWhiteSpace(input))
                        return "יש להזין שם קובץ.";

                    try
                    {
                        Path.GetFullPath(input);
                        return null;
                    }
                    catch (Exception ex) when (
                        ex is ArgumentException ||
                        ex is NotSupportedException ||
                        ex is PathTooLongException ||
                        ex is IOException ||
                        ex is SecurityException)
                    {
                        return "יש להזין נתיב קובץ תקין.";
                    }
                });

            var note = AskYesNo(
                "לרשום את מקור הקוד כהערה? (y/n)");

            var sort = Ask(
                "סדר מיון: file-name או type? (Enter = file-name)",
                input =>
                    input == "" ||
                    input.Equals(
                        "file-name",
                        StringComparison.OrdinalIgnoreCase) ||
                    input.Equals(
                        "type",
                        StringComparison.OrdinalIgnoreCase)
                        ? null
                        : "יש להזין file-name או type.");

            if (sort == "")
                sort = "file-name";

            sort = sort.ToLowerInvariant();

            var removeEmptyLines = AskYesNo(
                "למחוק שורות ריקות? (y/n)");

            var author = Ask(
                "שם יוצר הקובץ? (Enter לדילוג)",
                input => null);

            var rspName = Ask(
                "שם קובץ התגובה? (Enter = bundle.rsp)",
                input =>
                {
                    if (string.IsNullOrWhiteSpace(input))
                        return null;

                    if (input.IndexOfAny(
                        Path.GetInvalidFileNameChars()) >= 0 ||
                        input.Contains(
                            Path.DirectorySeparatorChar.ToString()) ||
                        input.Contains(
                            Path.AltDirectorySeparatorChar.ToString()))
                    {
                        return "יש להזין שם קובץ בלבד, ללא נתיב.";
                    }

                    return null;
                });

            if (rspName == "")
                rspName = "bundle.rsp";

            if (!rspName.EndsWith(
                ".rsp",
                StringComparison.OrdinalIgnoreCase))
            {
                rspName += ".rsp";
            }

            string assemblyPath;

            try
            {
                var entryAssembly = Assembly.GetEntryAssembly();

                assemblyPath = entryAssembly == null
                    ? ""
                    : entryAssembly.Location;

                if (string.IsNullOrWhiteSpace(assemblyPath) ||
                    !File.Exists(assemblyPath))
                {
                    Console.Error.WriteLine(
                        "שגיאה: לא ניתן לזהות את קובץ ה-DLL של התוכנית.");
                    return 1;
                }

                assemblyPath = Path.GetFullPath(assemblyPath);
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException ||
                ex is NotSupportedException)
            {
                Console.Error.WriteLine(
                    "שגיאה בזיהוי קובץ התוכנית: " + ex.Message);
                return 1;
            }

            var lines = new List<string>
            {
                Quote(assemblyPath),
                "bundle",
                "--language"
            };

            lines.AddRange(
                SplitWords(languages).Select(Quote));

            lines.Add("--output");
            lines.Add(Quote(output));

            lines.Add("--sort");
            lines.Add(sort);

            if (note)
                lines.Add("--note");

            if (removeEmptyLines)
                lines.Add("--remove-empty-lines");

            if (author.Length > 0)
            {
                lines.Add("--author");
                lines.Add(Quote(author));
            }

            try
            {
                File.WriteAllLines(
                    rspName,
                    lines,
                    new UTF8Encoding(false));
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException ||
                ex is ArgumentException ||
                ex is NotSupportedException)
            {
                Console.Error.WriteLine(
                    "שגיאה בשמירת קובץ התגובה: " + ex.Message);
                return 1;
            }

            Console.WriteLine(
                "קובץ התגובה נוצר: " +
                Path.GetFullPath(rspName));

            Console.WriteLine(
                "להרצה: dotnet @" + rspName);

            return 0;
        }

        private static string Ask(
            string question,
            Func<string, string> validate)
        {
            while (true)
            {
                Console.WriteLine(question);

                var input = (Console.ReadLine() ?? "").Trim();
                var error = validate(input);

                if (error == null)
                    return input;

                Console.WriteLine(
                    "קלט לא תקין: " + error);
            }
        }

        private static bool AskYesNo(string question)
        {
            var answer = Ask(
                question,
                input =>
                    input.Equals(
                        "y",
                        StringComparison.OrdinalIgnoreCase) ||
                    input.Equals(
                        "n",
                        StringComparison.OrdinalIgnoreCase)
                        ? null
                        : "יש להזין y או n.");

            return answer.Equals(
                "y",
                StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> SplitWords(string input)
        {
            return input.Split(
                new[] { ' ', ',' },
                StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
