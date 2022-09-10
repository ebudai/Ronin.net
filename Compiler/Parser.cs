using Ronin.Grammar;

namespace Ronin.Compiler;

internal interface IParsable<T> where T : Syntax
{
    public static abstract T Parse(ReadOnlyMemory<char> sourcecode);
    public static abstract string Write(T syntax);
}

internal class Parser
{
    internal class Exception : System.Exception
    {
        internal Exception(string message) : base(message) { }
    }
}
