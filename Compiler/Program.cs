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
    private const string Extension = Compiler.Sources.Extension;

    /// <summary>Files discovered but refused, which is not the same as files with problems.</summary>
    private static int unreadable;

    private static int Main(string[] args)
    {
        var folder = new DirectoryInfo(args is null or { Length: 0 } ? "." : args[0]);

        if (folder.Exists is false)
        {
            Console.Error.WriteLine($"{folder.FullName} does not exist");
            return 1;
        }

        var discovered = Compiler.Sources.Under(folder);

        foreach (var unreadable in discovered.Unreadable) Console.Error.WriteLine(unreadable);

        if (discovered.Files.Count is 0)
        {
            Console.Error.WriteLine($"no {Extension} files under {folder.FullName}");
            return 1;
        }

        var failed = 0;

        // Ordered, and awaited by virtue of being a loop. Parallelism is worth
        // having here, but not before the pipeline it would parallelise exists.
        foreach (var source in discovered.Files)
        {
            failed += Report(source);
        }

        // Counted apart from files with problems, which is not the same thing:
        // adding them together could report more files with problems than files
        // discovered, since a directory nobody can read holds no files at all.
        var refused = discovered.Unreadable.Count + unreadable;

        Console.WriteLine($"{discovered.Files.Count} file(s), {failed} with problems" +
                          (refused is 0 ? string.Empty : $", {refused} unreadable"));

        return failed is 0 && refused is 0 ? 0 : 1;
    }

    private static int Report(FileInfo file)
    {
        string text;
        try
        {
            text = File.ReadAllText(file.FullName);
        }
        // The same refusal set the discovery walk uses. Only IOException was
        // caught here, and a file whose permissions forbid reading raises
        // UnauthorizedAccessException, which is not one — so a single file
        // nobody could open ended the whole project scan.
        catch (Exception refused) when (refused is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"{file.FullName}: {refused.Message}");
            ++unreadable;
            return 0;
        }

        var compilation = Compilation.Of(new SourceText(text, file.FullName));

        foreach (var problem in compilation.Findings)
        {
            Console.Error.WriteLine(Diagnostics.Report(problem));
        }

        Console.WriteLine($"{file.FullName}: {compilation.Module.Scopes[0].Statements.Count} statement(s), " +
                          $"{compilation.Declarations.Symbols.Names.Count} name(s), " +
                          $"{compilation.Declarations.Symbols.Patterns.Count} pattern(s)");

        return compilation.Findings.Count is 0 ? 0 : 1;
    }
}
