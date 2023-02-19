using Ronin.Compiler;
using Ronin.Lexicon.Keywords;

namespace Ronin.Lexicon;

internal abstract class Keyword : Word
{
    internal static new Keyword Lex(ref Lexer lexer)
        => CanLex(lexer, Compiled.keyword) ? new Compiled { sourcecode = lexer.Commit(Compiled.keyword.Length) }
        : CanLex(lexer, Constant.keyword) ? new Constant { sourcecode = lexer.Commit(Constant.keyword.Length) }
        : CanLex(lexer, Datatype.keyword) ? new Datatype { sourcecode = lexer.Commit(Datatype.keyword.Length) }
        : CanLex(lexer, ForEach.keyword) ? new ForEach { sourcecode = lexer.Commit(ForEach.keyword.Length) }
        : CanLex(lexer, Function.keyword) ? new Function { sourcecode = lexer.Commit(Function.keyword.Length) }
        : CanLex(lexer, Import.keyword) ? new Import { sourcecode = lexer.Commit(Import.keyword.Length) }
        : CanLex(lexer, In.keyword) ? new In { sourcecode = lexer.Commit(In.keyword.Length) }
        : CanLex(lexer, Reactive.keyword) ? new Reactive { sourcecode = lexer.Commit(Reactive.keyword.Length) }
        : CanLex(lexer, PartOf.keyword) ? new PartOf { sourcecode = lexer.Commit(PartOf.keyword.Length) }
        : CanLex(lexer, Persistent.keyword) ? new Persistent { sourcecode = lexer.Commit(Persistent.keyword.Length) }
        : CanLex(lexer, Shared.keyword) ? new Shared { sourcecode = lexer.Commit(Shared.keyword.Length) }
        : CanLex(lexer, Optional.keyword) ? new Optional { sourcecode = lexer.Commit(Optional.keyword.Length) }
        : CanLex(lexer, Variable.keyword) ? new Variable { sourcecode = lexer.Commit(Variable.keyword.Length) }
        : null;

    private static bool CanLex(Lexer lexer, string keyword) => lexer.StartsWith(keyword) && char.IsWhiteSpace(lexer[keyword.Length]);    
}
