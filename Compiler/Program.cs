// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

namespace Ronin;

[ExcludeFromCodeCoverage]
internal static class Program
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

        ConcurrentBag<Module> modules = [];
        Parse(folder, modules);
    }
    

    private static void Parse(DirectoryInfo folder, ConcurrentBag<Module> modules)
    {
        var infos = folder.EnumerateFileSystemInfos();
        foreach (var info in infos)
        {
            if (info is DirectoryInfo subfolder)
            {
                Parse(subfolder, modules);
                continue;
            }

            var file = info as FileInfo;
            ThreadPool.UnsafeQueueUserWorkItem(static state => state.modules.Add(Parse(state.file)), (file, modules), preferLocal: true);
        }
    }

    private static Module Parse(FileInfo file)
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