
using System.CommandLine;
using System.IO;

// Output option
var outputOption = new Option<FileInfo>(
    name: "--output",
    description: "File path and name",
    getDefaultValue: () => new FileInfo("file.txt")
);
outputOption.AddAlias("-o");

// Language option
var langOption = new Option<string>(
    name: "--language",
    description: "Programming language ('c#', 'python', or 'all')."
)
{ IsRequired = true };
langOption.AddAlias("-l");

//note
var noteOption = new Option<bool>(
    name: "--note",
    description: "Include source code",
    getDefaultValue: () => false
);
noteOption.AddAlias("-n");

//sort
var sortOption = new Option<string>(
    name: "--sort",
    description: "Sort by: AB - for name files, TP - for extetion files",
    getDefaultValue: () => "NO"
);
sortOption.AddAlias("-s");
sortOption.Arity = ArgumentArity.ZeroOrOne;

//remove
var removeEmptyLines = new Option<bool>(
    name: "--remove-empty-lines",
    description: "Remove empty lines",
    getDefaultValue: () => false
);
removeEmptyLines.AddAlias("-r");

//author
var authorOption = new Option<string>(
    name: "--author",
    description: "Include provider auther",
    getDefaultValue: () => ""
);
authorOption.AddAlias("-a");

// Command definition
var bundleCommand = new Command("bundle", "Bundle code files into a single file");
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(langOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLines);
bundleCommand.AddOption(authorOption);

static List<string> SortFiles(List<string> files, string sortOption)
{
    // אם המשתמש לא הזין שום ערך, נמיין לפי שם הקובץ
    if (sortOption == "AB" || string.IsNullOrEmpty(sortOption))
        return files = files.OrderBy(f => Path.GetFileName(f)).ToList();
    // אם המשתמש בחר "TYPE", נמיין לפי סוג הקובץ
    else if (sortOption == "TP")
        return files = files.OrderBy(f => Path.GetExtension(f)).ToList();
    else if (sortOption != "NO")
    {
        Console.WriteLine("Invalid sort option. Defaulting to 'NAME'.");
        // מיון ברירת מחדל אם המשתמש הזין ערך לא תקני
        return files.OrderBy(f => Path.GetFileName(f)).ToList();  // מיון לפי שם הקובץ
    }
    else
        return files;
}

bundleCommand.SetHandler((FileInfo output, string language, bool note, string sort, bool remove, string auther) =>
{
    // Directories to exclude
    var excludedDirs = new[] { "bin", "debug", "obj", ".git" };

    // Supported languages and file extensions

    var langExtensions = new Dictionary<string, string>
    {
        { "c#", ".cs" },
        { "java", ".java" },
        { "python",".py"},
        { "SQL",  ".sql"},
        { "javascript", ".js"},
        { "javaScript",".js" }

    };

    try
    {
        // Get all files, excluding unwanted directories
        var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
                             .Where(file => !excludedDirs.Any(dir => file.Contains(dir, StringComparison.OrdinalIgnoreCase)))
                             .ToList();

        // Filter files by language
        if (language.ToLower() != "all")
        {
            if (!langExtensions.ContainsKey(language.ToLower()))
            {
                Console.WriteLine($"Unsupported language: {language}");
                return;
            }
            string extension = langExtensions[language.ToLower()];
            files = files.Where(file => file.EndsWith(extension, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        else
        {
            // Include all supported language files
            files = files.Where(file => langExtensions.Values.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).ToList();
        }
        files = SortFiles(files, sort);
        // Create output file
        using (var writer = new StreamWriter(output.FullName))
        {
            if (auther != "")
                writer.WriteLine(auther);
            foreach (var file in files.Distinct()) // Avoid duplicates
            {
                if (note)
                    writer.WriteLine(Path.Combine(Path.GetDirectoryName(file), Path.GetFileName(file)));
                var fileContent = File.ReadAllText(file);

                // Remove empty lines if the option is set
                if (remove)
                {
                    fileContent = string.Join(Environment.NewLine,
                        fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
                }

                writer.WriteLine(fileContent);
                writer.WriteLine();
            }
        }

        Console.WriteLine($"Successfully bundled {files.Count} file(s) into {output.FullName}.");
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine("Error: Access denied to the specified output path.");
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine("Error: The specified directory does not exist.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}, outputOption, langOption, noteOption, sortOption, removeEmptyLines, authorOption);


var createRspCommand = new Command("create-rsp", "Create RSP");
createRspCommand.SetHandler(async () =>
{
    // בקשת ערכים מהמשתמש
    Console.WriteLine("Enter output file path:");
    string outputPath = Console.ReadLine();

    Console.WriteLine("Enter language (e.g., c#, python, or all):");
    string language = Console.ReadLine();

    Console.WriteLine("Include source code? (yes/no):");
    bool note = Console.ReadLine()?.ToLower() == "yes";

    Console.WriteLine("Sort by (AB/TP):");
    string sort = Console.ReadLine();

    Console.WriteLine("Remove empty lines? (yes/no):");
    bool remove = Console.ReadLine()?.ToLower() == "yes";

    Console.WriteLine("Enter author name:");
    string author = Console.ReadLine();
    var command = $"bundle " +
               $"{(string.IsNullOrEmpty(outputPath) ? "" : $"--output \"{outputPath}\" ")}" +
               $"--language \"{language}\" " +
               $"{(note ? "--note " : "")}" +
               $"{(string.IsNullOrEmpty(sort) ? "" : $"--sort \"{sort} \"")}" +
               $"{(remove ? "--remove-empty-lines " : "")}" +
               $"{(string.IsNullOrEmpty(author) ? "" : $"--author \"{author}\"")}";

    // שמירת הפקודה לקובץ תגובה
    string rspFileName = "command.rsp"; // שם קובץ התגובה
    await File.WriteAllTextAsync(rspFileName, command);

    Console.WriteLine($"Response file created: {rspFileName}");
});

// Root command
var rootCommand = new RootCommand("Root command for File Bundler CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);

// Run command
await rootCommand.InvokeAsync(args);