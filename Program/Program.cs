using Ronin.Parser.Grammar;

namespace Ronin.Program;

public class Program
{
    public Program(DirectoryInfo folder)
    {
        modules = folder.EnumerateDirectories().ToDictionary(subfolder => subfolder.Name, )
    }

    private readonly Dictionary<Identifier, Module> modules;
}