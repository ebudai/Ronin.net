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

        var failed = discovered.Unreadable.Count;

        // Ordered, and awaited by virtue of being a loop. Parallelism is worth
        // having here, but not before the pipeline it would parallelise exists.
        foreach (var source in discovered.Files)
        {
            failed += Report(source);
        }

        Console.WriteLine($"{discovered.Files.Count} file(s), {failed} with problems");
        return failed is 0 ? 0 : 1;
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
