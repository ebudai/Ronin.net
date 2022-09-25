using Ronin.Token;

using static Ronin.Token.Keyword.Word;

namespace Ronin.Grammar;

internal class Datum : Syntax
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

    private bool NotNamed => Datatype is null && Initializer is null;

    internal override Result Add(Keyword keyword)
    {
        if (Name is null)
        {
            if (keyword.Type is var or constant)
            {
                Name = string.Empty;
                IsReadonly = keyword.Type is constant;
            }
            else if (keyword.Type is shared)
            {
                if (IsShared) Name = nameof(shared);
                else IsShared = true;
            }
            else if (keyword.Type is reactive)
            {
                if (IsReactive) Name = nameof(reactive);
                else IsReactive = true;
            }
            else if (keyword.Type is compiled)
            {
                if (IsCompiled) Name = nameof(compiled);
                else IsCompiled = true;
            }
            else if (keyword.Type is persistent)
            {
                if (IsPersistent) Name = nameof(persistent);
                else IsPersistent = true;
            }
            else if (keyword.Type is optional)
            {
                if (IsOptional) Name = nameof(optional);
                else IsOptional = true;
            }
            else
            {
                Name = Enum.GetName(keyword.Type);
            }
        }
        else if (NotNamed)
        {
            if (Name.Length is not 0) Name += ' ';
            Name += keyword.Sourcecode.ToString();
        }
        else if (Initializer is null)
        {
            return Datatype.Name.Add(keyword);
        }
        else
        {
            return Initializer.Name.Add(keyword);
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
        else if (NotNamed)
        {
            if (Name.Length is not 0) Name += ' ';
            Name += name.Sourcecode.ToString();
        }
        else if (Initializer is null)
        {
            return Datatype.Name.Add(name);
        }
        else
        {
            return Initializer.Name.Add(name);
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
            
            return Initializer?.Name.Add(symbol) ?? Datatype.Add(symbol);            
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
            return Result.Descended;
        }

        return Result.DoesNotApply;
    }
}

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