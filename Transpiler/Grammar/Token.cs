using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar;

internal abstract class Token
{
    internal int Line = 0;
    internal int Column = 0;
}
