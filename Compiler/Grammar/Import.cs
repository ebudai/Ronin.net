using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Import : Syntax, IParsable<Import>
{
    public Import(Parser parser, int length) : base(parser, length) { }

    internal string[] Name { get; init; }

    public static Syntax Parse(Parser parser)
    {
        if (parser.IsEmpty
            || parser[0] is not Keyword keyword
            || keyword.Type is not Keyword.Word.import) return null;

        var (hierarchy, length) = Hierarchy.Parse(parser);

        return hierarchy is null ? new Expected<Name>(parser) : new Import(parser, length) { Name = hierarchy };
    }

    public string Transpile() => string.Empty;
}
