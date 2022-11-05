using Ronin.Compiler;
using Ronin.Lexicon.Reserved;

namespace Ronin.Lexicon;

internal abstract class Keyword : Word
{
    protected internal Keyword(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Keyword Lex(Lexer lexer)
        => CanLex(lexer, Compiled.keyword) ? new Compiled(lexer)
        : CanLex(lexer, Constant.keyword) ? new Constant(lexer)
        : CanLex(lexer, Datatype.keyword) ? new Datatype(lexer)
        : CanLex(lexer, Function.keyword) ? new Function(lexer)
        : CanLex(lexer, Import.keyword) ? new Import(lexer)
        : CanLex(lexer, Reactive.keyword) ? new Reactive(lexer)
        : CanLex(lexer, PartOf.keyword) ? new PartOf(lexer)
        : CanLex(lexer, Persistent.keyword) ? new Persistent(lexer)
        : CanLex(lexer, Shared.keyword) ? new Shared(lexer)
        : CanLex(lexer, Optional.keyword) ? new Optional(lexer)
        : CanLex(lexer, Variable.keyword) ? new Variable(lexer) : null;
        
    private static bool CanLex(Lexer lexer, string keyword)
        => lexer.IsNotEmpty
        && lexer.StartsWith(keyword)
        && (keyword.Length >= lexer.Length || char.IsWhiteSpace(lexer[keyword.Length]));
}
