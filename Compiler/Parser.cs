using Ronin.Grammar;

namespace Ronin.Compiler;

internal interface IParsable<T> where T : Syntax
{
    public static abstract T Parse(ReadOnlyMemory<char> sourcecode);
    public static abstract string Write(T syntax);
}

internal class Parser
{
}
