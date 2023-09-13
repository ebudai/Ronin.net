// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Keyword : Word
{
    public static new Word Lex(ref Lexer lexer)
        => Compiled.Lex(ref lexer)
        ?? Constant.Lex(ref lexer)
        ?? Datatype.Lex(ref lexer)
        ?? Extends.Lex(ref lexer)
        ?? ForEach.Lex(ref lexer)
        ?? Function.Lex(ref lexer)
        ?? Import.Lex(ref lexer)
        ?? Reactive.Lex(ref lexer)
        ?? PartOf.Lex(ref lexer)
        ?? Persistent.Lex(ref lexer)
        ?? Shared.Lex(ref lexer)
        ?? Optional.Lex(ref lexer)
        ?? Variable.Lex(ref lexer)
        ?? If.Lex(ref lexer)
        ?? Let.Lex(ref lexer)
        ?? While.Lex(ref lexer)
        ?? Hidden.Lex(ref lexer)
        ?? Set.Lex(ref lexer);
}

internal class Modifier : Keyword { }

internal class Mutability : Keyword { }