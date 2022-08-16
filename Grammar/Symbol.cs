namespace Ronin.Grammar;

public abstract class Symbol : Syntax
{
    public string Value { get; init; }
}

public class Terminal : Symbol
{
    
}

public class Separator : Symbol
{
    
}

public class OpeningParenthesis : Symbol
{
    
}

public class OpeningSquareBracket : Symbol
{
    
}

public class OpeningBrace : Symbol
{
    
}

public class ClosingParenthesis : Symbol
{
    
}

public class ClosingSquareBracket : Symbol
{
    
}

public class ClosingBrace : Symbol
{
    
}