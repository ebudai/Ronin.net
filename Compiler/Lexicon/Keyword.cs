using Ronin.Compiler;
using Ronin.Lexicon.Keywords;

namespace Ronin.Lexicon;

internal abstract class Keyword : Word
{
    internal static new Keyword Lex(ref Lexer lexer)
        => CanLex(lexer, Compiled.keyword) ? new Compiled { Sourcecode = lexer.Commit(Compiled.keyword.Length) }
        : CanLex(lexer, Constant.keyword) ? new Constant { Sourcecode = lexer.Commit(Constant.keyword.Length) }
        : CanLex(lexer, Datatype.keyword) ? new Datatype { Sourcecode = lexer.Commit(Datatype.keyword.Length) }
        : CanLex(lexer, ForEach.keyword) ? new ForEach { Sourcecode = lexer.Commit(ForEach.keyword.Length) }
        : CanLex(lexer, Function.keyword) ? new Function { Sourcecode = lexer.Commit(Function.keyword.Length) }
        : CanLex(lexer, Import.keyword) ? new Import { Sourcecode = lexer.Commit(Import.keyword.Length) }
        : CanLex(lexer, In.keyword) ? new In { Sourcecode = lexer.Commit(In.keyword.Length) }
        : CanLex(lexer, Reactive.keyword) ? new Reactive { Sourcecode = lexer.Commit(Reactive.keyword.Length) }
        : CanLex(lexer, PartOf.keyword) ? new PartOf { Sourcecode = lexer.Commit(PartOf.keyword.Length) }
        : CanLex(lexer, Persistent.keyword) ? new Persistent { Sourcecode = lexer.Commit(Persistent.keyword.Length) }
        : CanLex(lexer, Shared.keyword) ? new Shared { Sourcecode = lexer.Commit(Shared.keyword.Length) }
        : CanLex(lexer, Optional.keyword) ? new Optional { Sourcecode = lexer.Commit(Optional.keyword.Length) }
        : CanLex(lexer, Variable.keyword) ? new Variable { Sourcecode = lexer.Commit(Variable.keyword.Length) }
        : null;

    private static bool CanLex(Lexer lexer, string keyword) => lexer.StartsWith(keyword) && char.IsWhiteSpace(lexer[keyword.Length]);    
}
