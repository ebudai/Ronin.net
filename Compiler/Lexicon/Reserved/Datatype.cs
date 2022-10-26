using Ronin.Compiler;

namespace Ronin.Lexicon.Reserved;

internal class Datatype : Keyword
{
    internal const string keyword = "datatype";
    
    internal Datatype(Lexer lexer) : base(lexer, keyword.Length) { }
}
