using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class PartOf : Syntax, IParsable
{
    internal PartOf(Parser parser, int length) : base(parser, length) { }

    internal string[] Name { get; init; }

    public static Syntax Parse(Parser parser)
    {
        if (parser[0] is not Keyword keyword || keyword.ToString() is not Keyword.part_of) return null;

        var (hierarchy, length) = parser.ParseHierarchy();

        return hierarchy is null ? new Expected<Name>(parser) : new PartOf(parser, length) { Name = hierarchy };
    }
}
