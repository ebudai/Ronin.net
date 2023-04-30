// Copyright © 2023 Eric Budai

using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage] //remove later
internal class Program
{
    static void Main()
    {
        
    }
    
    private static void Parse(DirectoryInfo folder)
    {

    }

    private static void Parse(FileInfo file)
    {
        string sourcecode = File.ReadAllText(file.FullName);
        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var scope = parser.Parse();
        var module = SemanticAnalyzer.Analyze(scope);
    }
}