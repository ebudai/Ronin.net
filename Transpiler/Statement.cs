using Ronin.Transpiler.Grammar;
using Ronin.Transpiler.Grammar.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ronin.Transpiler;

internal abstract class Statement
{
    internal Statement()
    {

    }

    public Token[] Tokens;

    public override string ToString() => string.Join<Token>(' ', Tokens);
}
