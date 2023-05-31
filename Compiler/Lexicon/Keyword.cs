// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Keyword : Word
{
    public static new Word Lex(ref Lexer lexer)
        => Keywords.Compiled.Lex(ref lexer)
        ?? Keywords.Constant.Lex(ref lexer)
        ?? Keywords.Datatype.Lex(ref lexer)
        ?? Keywords.ForEach.Lex(ref lexer)
        ?? Keywords.Function.Lex(ref lexer)
        ?? Keywords.Import.Lex(ref lexer)
        ?? Keywords.Reactive.Lex(ref lexer)
        ?? Keywords.PartOf.Lex(ref lexer)
        ?? Keywords.Persistent.Lex(ref lexer)
        ?? Keywords.Shared.Lex(ref lexer)
        ?? Keywords.Optional.Lex(ref lexer)
        ?? Keywords.Variable.Lex(ref lexer)
        ?? Keywords.Extends.Lex(ref lexer)
        ?? null;
}
