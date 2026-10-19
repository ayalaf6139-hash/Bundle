using System;
using System.CommandLine;
using System.Linq;
using System.Text;

namespace Bundle
{
    public class Program
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var rootCommand = new RootCommand(
                "Bundle - אריזת קובצי קוד");

            var languageOption = new Option<string[]>(
                "--language", "-l")
            {
                Description = "שפות הקוד לאריזה או all",
                Required = true,
                AllowMultipleArgumentsPerToken = true
            };

            languageOption.Validators.Add(result =>
            {
                var values = result.Tokens
                    .Select(token => token.Value)
                    .ToList();

                var error = Languages.Validate(values);

                if (error != null)
                    result.AddError(error);
            });

            var outputOption = new Option<string>(
                "--output", "-o")
            {
                Description = "נתיב קובץ ה-bundle",
                Required = true
            };

            var noteOption = new Option<bool>(
                "--note", "-n")
            {
                Description = "הוספת נתיב מקור כהערה"
            };

            var sortOption = new Option<string>(
                "--sort", "-s")
            {
                Description = "מיון: file-name או type",
                DefaultValueFactory = _ => "file-name"
            };

            sortOption.AcceptOnlyFromAmong(
                "file-name", "type");

            var removeEmptyLinesOption = new Option<bool>(
                "--remove-empty-lines", "-r")
            {
                Description = "הסרת שורות ריקות"
            };

            var authorOption = new Option<string>(
                "--author", "-a")
            {
                Description = "שם יוצר הקובץ"
            };

            var bundleCommand = new Command(
                "bundle", "אריזת קובצי קוד");

            bundleCommand.Options.Add(languageOption);
            bundleCommand.Options.Add(outputOption);
            bundleCommand.Options.Add(noteOption);
            bundleCommand.Options.Add(sortOption);
            bundleCommand.Options.Add(removeEmptyLinesOption);
            bundleCommand.Options.Add(authorOption);

            bundleCommand.SetAction(parseResult =>
                Bundler.Run(
                    parseResult.GetValue(languageOption),
                    parseResult.GetValue(outputOption),
                    parseResult.GetValue(noteOption),
                    parseResult.GetValue(sortOption),
                    parseResult.GetValue(removeEmptyLinesOption),
                    parseResult.GetValue(authorOption)));

            var createRspCommand = new Command(
                "create-rsp",
                "יצירת קובץ תגובה עם פקודת bundle מוכנה");

            createRspCommand.SetAction(
                _ => ResponseFileCreator.Run());

            rootCommand.Subcommands.Add(bundleCommand);
            rootCommand.Subcommands.Add(createRspCommand);

            return rootCommand.Parse(args).Invoke();
        }
    }
}
