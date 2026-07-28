// Copyright © 2026 Eric Budai

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ronin.Compiler;

/// <summary>
///     What was found under a folder, and what could not be looked at.
/// </summary>
internal sealed record Discovered(IReadOnlyList<FileInfo> Files, IReadOnlyList<string> Unreadable);

/// <summary>
///     Finds the source files under a folder, deterministically and finitely.
/// </summary>
///
/// <remarks>
///     <para>
///     Lifted out of <c>Program</c>, which is excluded from coverage — so this
///     was the one part of the executable no test could reach, and it is the part
///     that walks a filesystem it did not create.
///     </para>
///     <para>
///     A directory tree is not a tree. A symlink pointing at an ancestor makes it
///     infinite, and the walk followed one: it compiled the same file 41 times
///     through ever longer paths before the filesystem's own loop handling
///     stopped it, and reported 41 files with a straight face.
///     </para>
/// </remarks>
internal static class Sources
{
    public const string Extension = ".ron";

    /// <summary>Not source, and expensive to walk: build output and history.</summary>
    private static readonly string[] Skipped = [".git", "bin", "obj", "node_modules"];

    public static Discovered Under(DirectoryInfo folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        List<FileInfo> files = [];
        List<string> unreadable = [];

        Walk(folder, files, unreadable);

        return new Discovered(files, unreadable);
    }

    private static void Walk(DirectoryInfo folder, List<FileInfo> files, List<string> unreadable)
    {
        if (Skipped.Contains(folder.Name, StringComparer.OrdinalIgnoreCase)) return;

        // A link is where the tree stops being one. Refusing to follow it is
        // enough on its own — a directory cannot be hard linked — and it is also
        // the right answer for a link that does NOT loop: whatever it points at
        // is either already under this folder, or is not this project's source.
        if (folder.LinkTarget is not null) return;

        FileInfo[] found;
        DirectoryInfo[] nested;

        // Both enumerations in one attempt. They open the same directory with the
        // same permissions, so a second guard around the second one is a branch
        // nothing can reach — and each has to be materialised INSIDE the try,
        // because the refusal arrives when the enumerator opens the directory
        // rather than while it is walked.
        //
        // Only IOException was caught, and permissions raise
        // UnauthorizedAccessException, which is not one — so the executable died
        // on a folder it merely could not look at.
        try
        {
            found = [.. folder.EnumerateFiles($"*{Extension}")];
            nested = [.. folder.EnumerateDirectories()];
        }
        catch (Exception refused) when (refused is IOException or UnauthorizedAccessException)
        {
            unreadable.Add($"{folder.FullName}: {refused.Message}");
            return;
        }

        files.AddRange(found.OrderBy(file => file.FullName, StringComparer.Ordinal));

        foreach (var directory in nested.OrderBy(directory => directory.Name, StringComparer.Ordinal))
        {
            Walk(directory, files, unreadable);
        }
    }
}
