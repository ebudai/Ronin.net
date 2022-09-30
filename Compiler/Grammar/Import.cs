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

        if (parser.Length is <= 2) return new Expected<Name>(parser);

        List<string> hierarchy = new() { string.Empty };
        int length = 1;
        for (int max = parser.Length; length != max; ++length)
        {
            if (parser[length] is Whitespace) continue;

            if (parser[length] is Symbol symbol && symbol.IsTerminal) break;

            string text;
            if (parser[length] is Name name) text = name.ToString();
            else if (parser[length] is Keyword word) text = word.ToString();
            else return new Expected<Name>(parser);

            var names = text.Split(Symbol.hierarchy);
            if (hierarchy[^1].Length is not 0) hierarchy[^1] += ' ';
            hierarchy[^1] += names[0];
            if (names.Length is > 1) hierarchy.AddRange(names[1..]);
        }

        return hierarchy.Count is 1 ? new Expected<Name>(parser) : new Import(parser, length + 1) { Name = hierarchy.ToArray() };
    }

    public string Transpile() => string.Empty;
}
