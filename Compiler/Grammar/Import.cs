using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Import : Syntax, IParsable
{
    internal Import(Parser parser, int length) : base(parser, length) { }

    internal string[] Name { get; init; }

    public static Syntax Parse(ref Parser parser)
    {
        if (parser[0] is not Keyword keyword || keyword.ToString() is not Keyword.import) return null;

        var (hierarchy, length) = parser.ParseHierarchy();

        return hierarchy is null ? new Expected<Name>(parser) : new Import(parser, length) { Name = hierarchy };
    }

    public string Transpile() => string.Empty;
}
