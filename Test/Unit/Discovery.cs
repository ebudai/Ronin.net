// Copyright © 2026 Eric Budai

using Ronin.Compiler;

using Finder = Ronin.Compiler.Sources;

namespace Unit;

/// <summary>
///     Finding the source files under a folder, on a filesystem that is not a
///     tree.
/// </summary>
///
/// <remarks>
///     This lived inside <c>Program</c>, which is excluded from coverage — so the
///     one part of the executable no test could reach was the part that walks a
///     filesystem it did not create. It is out here for that reason and no other.
/// </remarks>
[Trait(nameof(Finder), null)]
public sealed class Discovery : IDisposable
{
    private readonly DirectoryInfo root
        = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"ronin-discovery-{Guid.NewGuid():N}"));

    public void Dispose()
    {
        // a folder made unreadable has to be given back before it can be deleted
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            foreach (var directory in root.EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                if (directory.LinkTarget is null) File.SetUnixFileMode(directory.FullName, Readable);
            }
        }

        root.Delete(recursive: true);
    }

    private const UnixFileMode Readable
        = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

    private DirectoryInfo Folder(string relative)
        => Directory.CreateDirectory(Path.Combine(root.FullName, relative));

    private void Source(string relative)
        => File.WriteAllText(Path.Combine(root.FullName, relative), "var x => number;\n");

    [Fact(DisplayName = "a link back up the tree is not followed")]
    public void ALinkBackUpTheTreeIsNotFollowed()
    {
        // Where the tree stops being one. Following it compiled the same file 41
        // times through ever longer paths before the filesystem's own loop
        // handling stopped the walk — and it reported 41 files with a straight
        // face and exit zero.
        var nested = Folder("nested");
        Source("nested/a.ron");
        Directory.CreateSymbolicLink(Path.Combine(nested.FullName, "self"), nested.FullName);

        var discovered = Finder.Under(root);

        Assert.Equal(["a.ron"], discovered.Files.Select(file => file.Name));
        Assert.Empty(discovered.Unreadable);
    }

    [Fact(DisplayName = "a link that does not loop is not followed either")]
    public void ALinkThatDoesNotLoopIsNotFollowedEither()
    {
        // Whatever it points at is either already under this folder — in which
        // case following it compiles the same file twice — or is not this
        // project's source. Neither is wanted, which is why refusing links needs
        // no loop detection beside it.
        Folder("real");
        Source("real/a.ron");
        Directory.CreateSymbolicLink(Path.Combine(root.FullName, "alias"), Path.Combine(root.FullName, "real"));

        Assert.Single(Finder.Under(root).Files);
    }

    [Fact(DisplayName = "a directory that will not be read is reported, not thrown")]
    public void ADirectoryThatWillNotBeReadIsReportedNotThrown()
    {
        // Enumeration is lazy, so the refusal arrives while the sequence is being
        // walked and not when it is asked for. Only IOException was caught, and
        // permissions raise UnauthorizedAccessException, which is not one — so
        // the executable died on a folder it merely could not look at.
        // Permissions are the readiest way to make a directory refuse. The
        // behaviour under test is not Unix specific — an enumeration that throws
        // is an enumeration that throws — but the way to provoke it is, and the
        // guard is positive because that is the shape the platform analyser
        // recognises.
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            Source("readable.ron");
            var forbidden = Folder("forbidden");

            File.SetUnixFileMode(forbidden.FullName, UnixFileMode.None);

            var discovered = Finder.Under(root);

            Assert.Equal(["readable.ron"], discovered.Files.Select(file => file.Name));

            var refused = Assert.Single(discovered.Unreadable);
            Assert.Contains(forbidden.FullName, refused);
        }
    }

    [Fact(DisplayName = "build output and history are not source")]
    public void BuildOutputAndHistoryAreNotSource()
    {
        Source("real.ron");

        foreach (var skipped in (string[])[".git", "bin", "obj", "node_modules"])
        {
            Folder(skipped);
            Source($"{skipped}/copy.ron");
        }

        Assert.Equal(["real.ron"], Finder.Under(root).Files.Select(file => file.Name));
    }

    [Fact(DisplayName = "the order does not come from the filesystem")]
    public void TheOrderDoesNotComeFromTheFilesystem()
    {
        // Two runs over one tree have to report the same problems in the same
        // order, or a build is only reproducible by luck.
        Folder("b");
        Folder("a");
        Source("z.ron");
        Source("b/y.ron");
        Source("a/x.ron");

        Assert.Equal(["z.ron", "x.ron", "y.ron"], Finder.Under(root).Files.Select(file => file.Name));
    }
}
