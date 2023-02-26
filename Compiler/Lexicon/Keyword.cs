// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal abstract class Keyword : Word
{
    internal static new Keyword Lex(ref Lexer lexer)
        => CanLex(lexer, CompiledKeyword.keyword) ? new CompiledKeyword { sourcecode = lexer.Commit(CompiledKeyword.keyword.Length) }
        : CanLex(lexer, ConstantKeyword.keyword) ? new ConstantKeyword { sourcecode = lexer.Commit(ConstantKeyword.keyword.Length) }
        : CanLex(lexer, DatatypeKeyword.keyword) ? new DatatypeKeyword { sourcecode = lexer.Commit(DatatypeKeyword.keyword.Length) }
        : CanLex(lexer, ForEachKeyword.keyword) ? new ForEachKeyword { sourcecode = lexer.Commit(ForEachKeyword.keyword.Length) }
        : CanLex(lexer, FunctionKeyword.keyword) ? new FunctionKeyword { sourcecode = lexer.Commit(FunctionKeyword.keyword.Length) }
        : CanLex(lexer, ImportKeyword.keyword) ? new ImportKeyword { sourcecode = lexer.Commit(ImportKeyword.keyword.Length) }
        : CanLex(lexer, ReactiveKeyword.keyword) ? new ReactiveKeyword { sourcecode = lexer.Commit(ReactiveKeyword.keyword.Length) }
        : CanLex(lexer, PartOfKeyword.keyword) ? new PartOfKeyword { sourcecode = lexer.Commit(PartOfKeyword.keyword.Length) }
        : CanLex(lexer, PersistentKeyword.keyword) ? new PersistentKeyword { sourcecode = lexer.Commit(PersistentKeyword.keyword.Length) }
        : CanLex(lexer, SharedKeyword.keyword) ? new SharedKeyword { sourcecode = lexer.Commit(SharedKeyword.keyword.Length) }
        : CanLex(lexer, OptionalKeyword.keyword) ? new OptionalKeyword { sourcecode = lexer.Commit(OptionalKeyword.keyword.Length) }
        : CanLex(lexer, VariableKeyword.keyword) ? new VariableKeyword { sourcecode = lexer.Commit(VariableKeyword.keyword.Length) }
        : null;

    private static bool CanLex(Lexer lexer, string keyword) => lexer.StartsWith(keyword) && char.IsWhiteSpace(lexer[keyword.Length]);    
}
