namespace Ronin.Parser;

internal class Parameters : Syntax
{
    private List<Identifier> Variables { get; } = new();

    public override string ToString() => "(" + string.Join(',', Variables) + ")";

    internal bool Add(Syntax syntax, Parser parser, ref int cursor)
    {
        if (syntax is Identifier identifier)
        {
            if (Variables.Count is 0) Variables.Add(identifier);
            else return Variables[^1].Add(identifier, ref cursor);
        }
        else if (syntax is Separator)
        {
            Variables.Add(new());
        }
        else if (syntax is ClosingParenthesis)
        {
            return false;
        }
        else
        {
            if (Variables.Count is 0) throw new Parser.ParseException("bad aggregate", parser);
            Variables[^1].Add(syntax, ref cursor);
        }
        return true;
    }
}
