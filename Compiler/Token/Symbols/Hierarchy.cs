using Ronin.Compiler;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Token.Symbols;

[ExcludeFromCodeCoverage]
internal class Hierarchy : Symbol
{
    public const char character = '/';

    public Hierarchy(Lexer lexer) : base(lexer, 1) { }

    public static new Hierarchy Lex(Lexer lexer) => !lexer.IsEmpty && lexer[0] is character ? new Hierarchy(lexer) : null;

    internal override bool CanBeUsedInNames => true;
}
