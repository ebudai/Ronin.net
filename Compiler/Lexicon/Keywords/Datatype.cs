using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class Datatype : Keyword
{
    internal const string keyword = "datatype";
    
    internal Datatype(Lexer lexer) : base(lexer, keyword.Length) { }
}
