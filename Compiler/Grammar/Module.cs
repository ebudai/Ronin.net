using Ronin.Compiler;
using Ronin.Lexicon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

internal class Module : IContext
{
    public IContext Parent { get; set; }
    private readonly Dictionary<Token, Module> children = new();
    private readonly List<Scope> scopes = new();

    public static Module Parse(ref Parser current)
    {
        Parser parser = current;

        Module values = new();

        while (Statement.Parse(ref parser) is Statement statement)
        {
            values.Add(statement);
            parser.TryAdvance<Terminal>();
        }

        current = parser;
        return values;
    }

    public void ResolveTypes(IContext context)
    {
        Parent = context;
        foreach (var scope in scopes)
        {
            scope.ResolveTypes(this);
        }
        foreach (var child in children.Values)
        {
            child.ResolveTypes(this);
        }
    }

    public Module GetOrAdd(Name name)
    {
        var module = this;

        foreach (var token in name.Tokens.Span)
        {
            if (module.children.TryGetValue(token, out module) is false)
            {
                module = new();
            }
        }

        return module;
    }

    public Module this[Name name]
    {
        get
        {
            var module = this;

            foreach (var token in name.Tokens.Span)
            {
                if (module.children.TryGetValue(token, out module) is false) return null;
            }

            return module;
        }
    }

    public void Add(Scope scope) => scopes.Add(scope);

    public Resolution Resolve(Reference reference)
    {
        List<Resolution> resolutions = new();
        foreach (var scope in scopes)
        {
            if (scope.Resolve(reference) is Resolution resolution)
            {
                resolutions.Add(resolution);
            }
        }
        return Resolution.From(resolutions);
    }

    private (Scope, int) GetRealIndex(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), "index must be positive");

        int remaining = index;
        foreach (var scope in scopes)
        {
            if (remaining < scope.Count)
            {
                return (scope, remaining);
            }
            remaining -= scope.Count;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    public class Unresolved : Module
    {
        public Name Name { get; init; }
    }

    public sealed class Statements : IEnumerator<Statement>
    {
        public Statements(Module module)
        {
            this.module = module;
            Reset();
        }

        public Statement Current => module.scopes[scope][statement];

        object IEnumerator.Current => Current;

        public void Dispose() { /* do nothing */ }

        public bool MoveNext()
        {
            if (IsLastStatement)
            {
                statement = 0;
                if (IsLastScope) return false;
                ++scope;
            }
            else
            {
                ++statement;
            }
            return true;
        }

        public void Reset()
        {
            scope = 0;
            statement = -1;
        }

        private bool IsLastScope => scope == module.scopes.Count - 1;
        private bool IsLastStatement => statement == module.scopes[scope].Count - 1;

        private readonly Module module;
        private int scope;
        private int statement;
    }

    #region list implementation

    [ExcludeFromCodeCoverage] public int Count
    {
        get
        {
            int count = 0;
            foreach (var scope in scopes)
            {
                count += scope.Count;
            }
            return count;
        }
    }

    [ExcludeFromCodeCoverage] public bool IsReadOnly => false;

    [ExcludeFromCodeCoverage] public Statement this[int index]
    {
        get
        {
            var (scope, realindex) = GetRealIndex(index);
            return scope[realindex];
        }

        set
        {
            var (scope, realindex) = GetRealIndex(index);
            scope[realindex] = value;
        }
    }

    [ExcludeFromCodeCoverage] public int IndexOf(Statement statement)
    {
        var total = 0;
        foreach (var scope in scopes)
        {
            var index = scope.IndexOf(statement);
            if (index is not -1) return total + index;
            total += scope.Count;
        }
        return -1;
    }

    [ExcludeFromCodeCoverage] public void Insert(int index, Statement statement)
    {
        var (scope, realindex) = GetRealIndex(index);
        scope.Insert(realindex, statement);
    }

    [ExcludeFromCodeCoverage] public void RemoveAt(int index)
    {
        var (scope, realindex) = GetRealIndex(index);
        scope.RemoveAt(realindex);
    }

    [ExcludeFromCodeCoverage] public void Add(Statement statement)
    {
        if (scopes.Count is 0)
        {
            Scope scope = new() { statement };
            scopes.Add(scope);
        }
        else
        {
            scopes[^1].Add(statement);
        }
    }

    [ExcludeFromCodeCoverage] public void Clear() { scopes.Clear(); }

    [ExcludeFromCodeCoverage] public bool Contains(Statement statement)
    {
        foreach (var scope in scopes)
        {
            if (scope.Contains(statement)) return true;
        }
        return false;
    }

    [ExcludeFromCodeCoverage] public void CopyTo(Statement[] array, int arrayIndex)
    {
        foreach (var scope in scopes)
        {
            scope.CopyTo(array, arrayIndex);
            arrayIndex += scope.Count;
        }
    }

    [ExcludeFromCodeCoverage] public bool Remove(Statement statement)
    {
        foreach (var scope in scopes)
        {
            if (scope.Remove(statement)) return true;
        }
        return false;
    }

    [ExcludeFromCodeCoverage] public IEnumerator<Statement> GetEnumerator() => new Statements(this);

    [ExcludeFromCodeCoverage] IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion
}
