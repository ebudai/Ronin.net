// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Reserved : Word
{
    public static new Word Lex(ref Lexer lexer)
        => Keyword.Compiled.Lex(ref lexer)
        ?? Keyword.Constant.Lex(ref lexer)
        ?? Keyword.Datatype.Lex(ref lexer)
        ?? Keyword.ForEach.Lex(ref lexer)
        ?? Keyword.Function.Lex(ref lexer)
        ?? Keyword.Import.Lex(ref lexer)
        ?? Keyword.Reactive.Lex(ref lexer)
        ?? Keyword.PartOf.Lex(ref lexer)
        ?? Keyword.Persistent.Lex(ref lexer)
        ?? Keyword.Shared.Lex(ref lexer)
        ?? Keyword.Optional.Lex(ref lexer)
        ?? Keyword.Variable.Lex(ref lexer)
        ?? null;
}
