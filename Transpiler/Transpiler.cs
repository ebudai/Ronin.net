namespace Ronin.Transpiler;

public static class Transpiler
{
    public static ProgramFolder Transpile(DirectoryInfo folder) => new()
    {
        Folders = folder.EnumerateDirectories().Select(Transpile).ToArray(),
        Files = folder.EnumerateFiles().Select(Transpile).ToArray()
    };

    private static ProgramFile Transpile(FileInfo file) => new(File.ReadAllLines(file.FullName));
}
