using Ronin.Grammar;
using Ronin.Lexicon;
using System.Runtime.CompilerServices;

namespace Ronin.Compiler;

public ref struct Parser
{
    public Parser(ref Token token) => Current = ref token;

    public Syntax[] Parse()
    {
        List<Syntax> statements = new();

        while (IsNotFinished)
        {
            var statement = Statement.Parse(ref this);
            statements.Add(statement);
        }

        return statements.ToArray();
    }

    internal ref readonly Token Current;

    internal ref readonly Token this[int index] => ref Unsafe.Add(ref Unsafe.AsRef(Current), index);

    internal bool IsNotFinished => Current is not Sentinel;

    internal void Advance(int amount = 1) => Current = ref this[amount];

    internal SourceLocation[] Commit(scoped ref Parser context)
    {
        List<SourceLocation> source = new(64);
        ref readonly var token = ref context.Current;
        ref var end = ref Unsafe.AsRef(Current);
        
        while (Unsafe.AreSame(ref Unsafe.AsRef(token), ref end) is false)
        {
            source.AddRange(context.Current.SourceLocations);
            token = ref Unsafe.Add(ref Unsafe.AsRef(Current), 1);
        }
        
        context = ref this;
        return source.ToArray();
    }
}
