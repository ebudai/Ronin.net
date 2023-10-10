// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Symbol : Token
{
    public static Symbol Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (char.IsSymbol(lexer[0]) is false && char.IsPunctuation(lexer[0]) is false) return null;
        return new Symbol { Memory = lexer.AdvanceBy(1) };
    }

    internal class Special : Symbol
    {
        public static new Special Lex(ref Lexer lexer)
            => Elipsis.Lex(ref lexer)
            ?? Interval.Lex(ref lexer)
            ?? LessThanOrEqual.Lex(ref lexer)
            ?? GreaterThanOrEqual.Lex(ref lexer) as Special;

        protected static T Lex<T>(ref Lexer lexer, string symbol) where T : Special, new()
        {
            if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
            return new() { Memory = lexer.AdvanceBy(symbol.Length) };
        }

        internal class Elipsis : Special
        {
            internal const string symbol = "...";

            public static new Elipsis Lex(ref Lexer lexer) => Lex<Elipsis>(ref lexer, symbol);
        }

        internal class Interval : Special
        {
            internal const string symbol = "..";

            public static new Interval Lex(ref Lexer lexer) => Lex<Interval>(ref lexer, symbol);
        }

        internal class LessThanOrEqual : Special
        {
            internal const string symbol = "<=";

            public static new LessThanOrEqual Lex(ref Lexer lexer) => Lex<LessThanOrEqual>(ref lexer, symbol);
        }

        internal class GreaterThanOrEqual : Special
        {
            internal const string symbol = ">=";

            public static new GreaterThanOrEqual Lex(ref Lexer lexer) => Lex<GreaterThanOrEqual>(ref lexer, symbol);
        }
    }
}


