namespace Ronin.Builder;

internal class ParseException : Exception
{
    protected internal readonly int line_;

    public ParseException(int line) => line_ = line;
    public ParseException(string message, int line) : base(message) => line_ = line;
}

internal class MalformedLiteralException : ParseException
{
    public MalformedLiteralException(string badliteral, int line) : base($"form of literal is wrong: {badliteral}", line)
    {
        //nothing to do
    }
}
