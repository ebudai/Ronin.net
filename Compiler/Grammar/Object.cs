using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Object : Syntax, IParsable//<Object>
{
    internal Reference[] Parameters { get; init; }

    internal Object(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(ref Parser parser)
    {
        if (parser.IsEmpty) return null;

        if (parser[0] is not Symbol symbol || !symbol.IsOpenParenthesis) return null;

        Parser attempt = new(parser, 0);
        var references = parser.Parse();

        return null;
    }

    public string Transpile() => '(' + string.Join(',', Parameters.Select(Transpiled).ToArray()) + ')';

    private static string Transpiled(Reference reference) => reference.Transpile();
}
