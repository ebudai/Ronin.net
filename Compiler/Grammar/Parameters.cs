using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Object : Syntax, IParsable<Object>
{
    internal Reference[] Parameters { get; init; }

    internal Object(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(Parser parser)
    {
        throw new NotImplementedException();
    }

    public string Transpile() => '(' + string.Join(',', Parameters.Select(Transpiled).ToArray()) + ')';

    private static string Transpiled(Reference reference) => reference.Transpile();
}
