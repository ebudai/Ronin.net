// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Language;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage] //remove later
internal class Program
{
    private const string debug = nameof(debug);

    static void Main(string[] args)
    {
        string foldername = args is null or { Length: 0 } ? "." : args[0];
        
        DirectoryInfo folder = new(foldername);
        if (folder.Exists is false)
        {
            Console.WriteLine($"{folder.FullName} does not exist");
            return;
        }

        ConcurrentBag<Scope> scopes = new();
        Parse(folder, scopes);
        var semantics = Analyze(scopes);
        //bool isDebug = args.Length is > 1 && args[1] is debug;
        
        
    }
    
    private static Semantics Analyze(ConcurrentBag<Scope> scopes)
    {
        foreach (var scope in scopes)
        {

        }

        return null;
    }

    private static void Parse(DirectoryInfo folder, ConcurrentBag<Scope> scopes)
    {
        var infos = folder.EnumerateFileSystemInfos();
        foreach (var info in infos)
        {
            if (info is DirectoryInfo subfolder)
            {
                Parse(subfolder, scopes);
                continue;
            }

            var file = info as FileInfo;
            ThreadPool.UnsafeQueueUserWorkItem(static state => state.scopes.Add(Parse(state.file)), (file, scopes), preferLocal: true);
        }
    }

    private static Scope Parse(FileInfo file)
    {
        string sourcecode = File.ReadAllText(file.FullName);
        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        return parser.Parse();
    }
}

[Flags]
internal enum ProgramOptions
{
    None = 0,
    Debug = 1 << 0,
}