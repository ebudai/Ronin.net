// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Ronin;

/// <summary>
///     Lex and parse every source file under a folder, deterministically.
/// </summary>
///
/// <remarks>
///     It queued its work on the thread pool and never waited, so the process
///     could exit before a single file finished; it recursed into every
///     directory and handed every filesystem entry to File.ReadAllText,
///     including .git, bin and obj; and it dropped what it parsed into a bag
///     nothing read. The phases beyond parsing — declaration building, name
///     resolution, type-directed overload selection, imports, lowering — are not
///     connected yet, and this says so rather than implying otherwise.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class Program
{
    private const string Extension = ".ron";

    private static readonly string[] Skipped = [".git", "bin", "obj", "node_modules"];

    private static int Main(string[] args)
    {
        var folder = new DirectoryInfo(args is null or { Length: 0 } ? "." : args[0]);

        if (folder.Exists is false)
        {
            Console.Error.WriteLine($"{folder.FullName} does not exist");
            return 1;
        }

        var sources = Sources(folder).ToArray();
        if (sources.Length is 0)
        {
            Console.Error.WriteLine($"no {Extension} files under {folder.FullName}");
            return 1;
        }

        var failed = 0;

        // Ordered, and awaited by virtue of being a loop. Parallelism is worth
        // having here, but not before the pipeline it would parallelise exists.
        foreach (var source in sources)
        {
            failed += Report(source);
        }

        Console.WriteLine($"{sources.Length} file(s), {failed} with problems");
        return failed is 0 ? 0 : 1;
    }

    /// <summary>Source files only, in a stable order, skipping what is not source.</summary>
    private static IEnumerable<FileInfo> Sources(DirectoryInfo folder)
    {
        if (Skipped.Contains(folder.Name, StringComparer.OrdinalIgnoreCase)) yield break;

        foreach (var file in folder.EnumerateFiles($"*{Extension}").OrderBy(file => file.FullName, StringComparer.Ordinal))
        {
            yield return file;
        }

        foreach (var nested in folder.EnumerateDirectories().OrderBy(directory => directory.Name, StringComparer.Ordinal))
        {
            foreach (var file in Sources(nested)) yield return file;
        }
    }

    private static int Report(FileInfo file)
    {
        string text;
        try
        {
            text = File.ReadAllText(file.FullName);
        }
        catch (IOException unreadable)
        {
            Console.Error.WriteLine($"{file.FullName}: {unreadable.Message}");
            return 1;
        }

        Lexer lexer = new(text);
        Parser parser = new(lexer.Lex());
        var module = parser.Parse();

        if (module is Module.UnexpectedInputError unexpected)
        {
            Console.Error.WriteLine($"{file.FullName}: {unexpected.Reason} at «{unexpected.Tokens.Span[0].ToLexemes().Render()}»");
            return 1;
        }

        var declared = Declarations.Of(module.Scopes[0].Statements);

        foreach (var problem in declared.Problems.Concat(declared.Symbols.Validate()))
        {
            Console.Error.WriteLine($"{file.FullName}: {problem}");
        }

        Console.WriteLine($"{file.FullName}: {module.Scopes[0].Statements.Count} statement(s), " +
                          $"{declared.Symbols.Names.Count} name(s), {declared.Symbols.Patterns.Count} pattern(s)");

        return declared.Problems.Count is 0 ? 0 : 1;
    }
}
