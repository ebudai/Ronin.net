using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Value : Statement, IParsableSyntax<Value>
{
    public new static Value Parse(ref Parser current) 
        => Anonymous.Parse(ref current) 
        ?? Reference.Parse(ref current) as Value;
}