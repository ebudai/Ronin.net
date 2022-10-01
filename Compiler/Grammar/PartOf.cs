using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class PartOf : Syntax, IParsable<PartOf>
{
    internal PartOf(Parser parser, int length) : base(parser, length) { }

    internal string[] Name { get; init; }

    public static Syntax Parse(Parser parser)
    {
        if (parser.IsEmpty 
            || parser[0] is not Keyword keyword 
            || keyword.Type is not Keyword.Word.part_of) return null;

        var (hierarchy, length) = Hierarchy.Parse(parser);

        return hierarchy is null ? new Expected<Name>(parser) : new PartOf(parser, length) { Name = hierarchy };
    }

    public string Transpile() => string.Empty;
}
