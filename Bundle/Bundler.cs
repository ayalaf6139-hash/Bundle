using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace Bundle
{
    public static class Bundler
    {
        private static readonly HashSet<string> ExcludedDirectories =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bin", "obj", "debug", "release",
                "node_modules", "packages",
                ".git", ".vs", ".vscode", ".idea",
                "x64", "x86", "dist", "build", "out"
            };

        public static int Run(
            string[] languages,
            string output,
            bool note,
            string sort,
            bool removeEmptyLines,
            string author)
        {
            if (languages == null)
                return Error("רשימת השפות חסרה.");

            var languageError = Languages.Validate(languages);

            if (languageError != null)
                return Error(languageError);

            if (string.IsNullOrWhiteSpace(output))
                return Error("יש לציין נתיב לקובץ הפלט.");

            if (!string.Equals(
                    sort, "file-name",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    sort, "type",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Error(
                    "ערך sort חייב להיות file-name או type.");
            }

            sort = sort.ToLowerInvariant();

            string outputPath;

            try
            {
                outputPath = Path.GetFullPath(output);
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is NotSupportedException ||
                ex is PathTooLongException ||
                ex is IOException ||
                ex is SecurityException)
            {
                return Error($"נתיב הקובץ '{output}' אינו תקין.");
            }

            var outputDirectory = Path.GetDirectoryName(outputPath);

            if (string.IsNullOrEmpty(outputDirectory) ||
                !Directory.Exists(outputDirectory))
            {
                return Error(
                    $"תיקיית הפלט '{outputDirectory}' אינה קיימת.");
            }

            if (Directory.Exists(outputPath))
                return Error("נתיב הפלט מצביע על תיקייה ולא על קובץ.");

            var root = Path.GetFullPath(
                Directory.GetCurrentDirectory());

            HashSet<string> extensions;

            try
            {
                extensions =
                    Languages.Resolve(languages) ??
                    Languages.AllExtensions;
            }
            catch (ArgumentException ex)
            {
                return Error(ex.Message);
            }

            List<string> fileList;

            try
            {
                fileList = CollectFiles(root)
                    .Where(file =>
                        extensions.Contains(Path.GetExtension(file)))
                    .Where(file =>
                        !string.Equals(
                            Path.GetFullPath(file),
                            outputPath,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (sort == "type")
                {
                    fileList = fileList
                        .OrderBy(
                            file => Path.GetExtension(file),
                            StringComparer.OrdinalIgnoreCase)
                        .ThenBy(
                            file => Path.GetFileName(file),
                            StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                else
                {
                    fileList = fileList
                        .OrderBy(
                            file => Path.GetFileName(file),
                            StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException)
            {
                return Error(
                    $"שגיאה באיסוף קובצי המקור: {ex.Message}");
            }

            if (fileList.Count == 0)
            {
                return Error(
                    "לא נמצאו קובצי קוד מתאימים בתיקייה הנוכחית.");
            }

            var bundle = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(author))
            {
                bundle.AppendLine($"// Author: {author.Trim()}");
                bundle.AppendLine();
            }

            foreach (var file in fileList)
            {
                string[] lines;

                try
                {
                    lines = File.ReadAllLines(file);
                }
                catch (Exception ex) when (
                    ex is IOException ||
                    ex is UnauthorizedAccessException ||
                    ex is SecurityException)
                {
                    return Error(
                        $"לא ניתן לקרוא את קובץ המקור '{file}': {ex.Message}");
                }

                if (note)
                {
                    string relativePath = GetRelativePath(root, file);

                    bundle.AppendLine(
                        $"// Source: {relativePath}");
                }

                foreach (var line in lines)
                {
                    if (removeEmptyLines &&
                        string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    bundle.AppendLine(line);
                }

                bundle.AppendLine();
            }

            try
            {
                File.WriteAllText(
                    outputPath,
                    bundle.ToString(),
                    new UTF8Encoding(
                        encoderShouldEmitUTF8Identifier: false));
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException)
            {
                return Error(
                    $"שמירת קובץ הפלט נכשלה: {ex.Message}");
            }

            Console.WriteLine(
                $"נארזו {fileList.Count} קבצים לתוך {outputPath}");

            return 0;
        }

        private static string GetRelativePath(
            string root,
            string file)
        {
            string fullRoot = Path.GetFullPath(root)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

            string fullFile = Path.GetFullPath(file);

            string rootWithSeparator =
                fullRoot + Path.DirectorySeparatorChar;

            if (fullFile.StartsWith(
                rootWithSeparator,
                StringComparison.OrdinalIgnoreCase))
            {
                return fullFile.Substring(rootWithSeparator.Length);
            }

            return fullFile;
        }

        private static List<string> CollectFiles(string root)
        {
            var result = new List<string>();

            CollectFilesRecursive(root, result);

            return result;
        }

        private static void CollectFilesRecursive(
            string directory,
            List<string> result)
        {
            string[] files;
            string[] subDirectories;

            try
            {
                files = Directory.GetFiles(directory);
                subDirectories = Directory.GetDirectories(directory);
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (IOException)
            {
                return;
            }
            catch (SecurityException)
            {
                return;
            }

            result.AddRange(files);

            foreach (var subDirectory in subDirectories)
            {
                var directoryInfo = new DirectoryInfo(subDirectory);

                if (ExcludedDirectories.Contains(directoryInfo.Name))
                    continue;

                try
                {
                    if ((directoryInfo.Attributes &
                         FileAttributes.ReparsePoint) != 0)
                    {
                        continue;
                    }
                }
                catch (Exception ex) when (
                    ex is IOException ||
                    ex is UnauthorizedAccessException ||
                    ex is SecurityException)
                {
                    continue;
                }

                CollectFilesRecursive(subDirectory, result);
            }
        }

        private static int Error(string message)
        {
            Console.Error.WriteLine($"שגיאה: {message}");

            return 1;
        }
    }
}
