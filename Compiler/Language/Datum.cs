using Ronin.Grammar;
using Ronin.Token;

namespace Ronin.Language;

/*internal class Datum : Syntax
{
    internal bool IsReadonly { get; set; }
    internal bool IsReactive { get; set; }
    internal bool IsCompiled { get; set; }
    internal bool IsPersistent { get; set; }
    internal bool IsShared { get; set; }
    internal bool IsOptional { get; set; }

    internal string Name { get; set; }
    internal Datatype Datatype { get; set; }
    internal Function Initializer { get; set; }

    private bool NotTyped => Datatype is null && Initializer is null;

    internal override Result Add(Keyword keyword)
    {
        if (Name is null)
        {
            switch (keyword.Type)
            {
                case var:
                case constant:
                    Name = string.Empty;
                    IsReadonly = keyword.Type is constant;
                    break;
                case shared:
                    if (IsShared) Name = nameof(shared);
                    else IsShared = true;
                    break;
                case reactive:
                    if (IsReactive) Name = nameof(reactive);
                    else IsReactive = true;
                    break;
                case compiled:
                    if (IsCompiled) Name = nameof(compiled);
                    else IsCompiled = true;
                    break;
                case persistent:
                    if (IsPersistent) Name = nameof(persistent);
                    else IsPersistent = true;
                    break;
                case optional:
                    if (IsOptional) Name = nameof(optional);
                    else IsOptional = true;
                    break;
                default:
                    Name = Enum.GetName(keyword.Type);
                    break;
            }
        }
        else if (NotTyped)
        {
            if (Name.Length is not 0) Name += ' ';
            Name += keyword.Sourcecode.ToString();
        }
        else if (Initializer is null)
        {
            return Datatype.Add(keyword);
        }
        else
        {
            return Initializer.Add(keyword);
        }
        Incorporate(keyword);
        return Result.Applied;
    }

    internal override Result Add(Name name)
    {
        if (Name is null)
        {
            Name = name.Sourcecode.ToString();
        }
        else if (NotTyped)
        {
            if (Name.Length is not 0) Name += ' ';
            Name += name.Sourcecode.ToString();
        }
        else if (Initializer is null)
        {
            return Datatype.Add(name);
        }
        else
        {
            return Initializer.Add(name);
        }
        Incorporate(name);
        return Result.Applied;
    }

    internal override Result Add(Symbol symbol)
    {
        if (Name is null) return Result.DoesNotApply;

        if (symbol.IsTerminal || symbol.IsSeparator || symbol.IsClose)
        {            
            if (Datatype is null && Initializer is null) return Result.DoesNotApply;
            Incorporate(symbol);
            return Result.Completed;
        }
        
        if (symbol.IsReturns)
        {
            if (Datatype is null)
            {
                Datatype = new();
                Incorporate(symbol);
                return Result.Applied;
            }
            
            return Datatype.Add(symbol);
        }

        if (symbol.IsAssign)
        {
            Initializer?.Name.Add(symbol); // if we have already begun initializing, then this symbol is part of an identifier
            Initializer ??= new(); // else begin initializing
            Incorporate(symbol);
            return Result.Applied;
        }

        if (symbol.IsOpen)
        {
            if (Datatype is null && Initializer is null) return Result.DoesNotApply;
            return Result.Completed;
        }

        return Result.DoesNotApply;
    }
}*/

/*

variable x => integer;
constant y => money;
shared z => 44.2;

datatype list of (type => datatype) { ... }
alias list => list of integer;

constant b => list of money;

datatype I`m a (magnitude of thing => number, compiled count => int64) bussssss { ... }

reactive c => I`m a (7.2, 60978293720987) bussssss;

*/