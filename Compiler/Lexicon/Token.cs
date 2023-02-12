using Ronin.Compiler;

namespace Ronin.Lexicon;

internal abstract class Token
{
    protected internal ReadOnlyMemory<char> Sourcecode { get; init; }
}