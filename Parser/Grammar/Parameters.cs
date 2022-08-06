namespace Ronin.Parser.Grammar;

internal class Parameters : Syntax
{
    private List<Identifier> Variables { get; } = new();

    public override string ToString() => "(" + string.Join(',', Variables) + ")";

    internal bool TryAdd(Syntax syntax, ref int cursor)
    {
        if (syntax is ClosingParenthesis) return false;

        if (syntax is Identifier identifier)
        {
            if (Variables.Count is not 0)
            {
                return Variables[^1].TryAdd(identifier, ref cursor);
            }
            Variables.Add(identifier);
        }
        else if (syntax is Separator)
        {
            Variables.Add(new());
        }
        else if (syntax is not Symbol)
        {
            if (Variables.Count is 0) Variables.Add(new());
            Variables[^1].TryAdd(syntax, ref cursor);
        }
        return true;
    }
}
