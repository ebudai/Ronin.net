using Ronin.Compiler;
using Ronin.Token;
using Ronin.Token.Delimiter;

namespace Ronin.Grammar;

internal class Import : Syntax, IParsable
{
    internal Import(Parser parser, int length) : base(parser, length) { }

    internal string[] Name { get; init; }

    public static Syntax Parse(Parser parser)
    {
        if (parser[0] is not Keyword keyword || keyword.ToString() is not Keyword.import) return null;

        var (hierarchy, length) = parser.ParseHierarchy();

        return hierarchy is null ? new Expected<Name, Hierarchy>(parser) : new Import(parser, length) { Name = hierarchy };
    }
}
