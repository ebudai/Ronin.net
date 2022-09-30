using Ronin.Grammar;
using Ronin.Token;
using static Ronin.Token.Keyword.Word;

namespace Ronin.Language;

/*internal class Datatype : Syntax
{
    internal bool IsCompiled { get; set; }
    internal bool IsReactive { get; set; }
    internal bool IsOptional { get; set; }

    internal Identifier Name { get; } = new();
    internal List<Function> Parameters { get; } = new(); 
    internal List<Datatype> MemberVariables { get; } = new();

    private bool _isDeclaration = false;

    internal override Result Add(Keyword keyword)
    {
        var result = Result.Applied;

        if (keyword.Type is datatype)
        {            
            if (!_isDeclaration) _isDeclaration = true;
            else result = Name.Add(keyword);
            if (result is not Result.DoesNotApply) Incorporate(keyword);
            return result;
        }

        if (!_isDeclaration && keyword.Type is function or shared or part_of or import or @return)
        {
            result = Name.Add(keyword);
            if (result is not Result.DoesNotApply) Incorporate(keyword);
            return result;
        }

        return result;
    }

    internal override Result Add(Name name)
    {
        var result = Name.Add(name);
        if (result is not Result.DoesNotApply) Incorporate(name);
        return result;
    }
    
}
*/