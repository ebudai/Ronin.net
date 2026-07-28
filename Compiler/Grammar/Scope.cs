using Ronin.Compiler;
using Ronin.Lexicon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

internal class Scope : Statement
{
    public Modifiers Modifiers { get; init; }
    public List<Import> Imports { get; } = [];
    public List<Statement> Statements { get; } = [];

    public Scope() { }
    private Scope(Scope scope) => Statements = scope.Statements;

    // Reactive before ConditionalReactive: both open with «when», and the
    // conditional form reports a missing condition with an error node rather
    // than a null, which commits the alternation. Tried the other way round,
    // «when changing x» is read as a «when» whose condition is missing and the
    // reactive form is never reached at all.
    public static new Scope Parse(ref Parser current)
        => Basic.Parse(ref current)
        ?? Applicative.Parse(ref current)
        ?? Conditional.Parse(ref current)
        ?? Repeating.Parse(ref current)
        ?? Reactive.Parse(ref current)
        ?? ConditionalReactive.Parse(ref current)
        ?? Iterating.Parse(ref current) as Scope;

    public class Conditional : Conditional<If> { }
    
    public class Repeating : Conditional<While> { }
    
    public class ConditionalReactive : Conditional<When> { }

    public class Applicative : Scope
    {
        private Applicative(Scope scope) : base(scope) { }

        public static new Applicative Parse(ref Parser current)
        {
            Parser parser = current;

            if (Modifiers.Parse(ref parser) is not Modifiers modifiers) return null;

            if (Basic.Parse(ref parser) is not Scope scope) return null;

            current = parser;
            return new Applicative(scope) { Modifiers = modifiers };
        }
    }

    public class Iterating : Scope
    {
        public Datum Iterable { get; init; }
        public Identifier Current { get; init; }

        private Iterating() { }
        private Iterating(Scope scope) : base(scope) { }

        public static new Iterating Parse(ref Parser current)
        {
            Parser parser = current;

            var modifiers = Modifiers.Parse(ref parser);

            if (parser.TryAdvance<Iterate>() is false) return null;

            if (Datum.Unresolved.Parse(ref parser) is not Datum datum)
            {
                return new ExpectedIterableError { Tokens = Parser.Recover(ref current, parser) };
            }

            if (parser.TryAdvance<Returns>() is false)
            {
                return new ExpectedReturnsSymbolError { Tokens = Parser.Recover(ref current, parser) };
            }

            if (Name.Parse(ref parser) is not Name name)
            {
                return new ExpectedNameError { Tokens = Parser.Recover(ref current, parser) };
            }

            if (Definition.Parse(ref parser) is not Scope definition) return null;

            current = parser;
            return new Iterating(definition)
            {
                Modifiers = modifiers,
                Iterable = datum,
                Current = [name]
            };
        }

        public class ExpectedIterableError : Iterating, IError
        {
            public string Reason { get; } = "expected list";
            public ReadOnlyMemory<Token> Tokens { get; init; }
        }

        public class ExpectedReturnsSymbolError : Iterating, IError
        {
            public string Reason { get; } = $"expected '{Returns.symbol}'";
            public ReadOnlyMemory<Token> Tokens { get; init; }
        }

        public class ExpectedNameError : Iterating, IError
        {
            public string Reason { get; } = "expected name";
            public ReadOnlyMemory<Token> Tokens { get; init; }
        }
    }

    public class Reactive : Scope
    {
        public Datum Target { get; init; }

        /// <inheritdoc cref="Conditional{T}.Trigger"/>
        public string Trigger { get; init; }

        private Reactive() { }
        private Reactive(Scope scope) : base(scope) { }

        public static new Reactive Parse(ref Parser current)
        {
            Parser parser = current;

            var modifiers = Modifiers.Parse(ref parser);

            var keyword = parser.Token;
            if (parser.TryAdvance<When>() is false || parser.TryAdvance<Changing>() is false) return null;

            if (Datum.Unresolved.Parse(ref parser) is not Datum datum)
            {
                return new ExpectedTargetError { Tokens = Parser.Recover(ref current, parser) };
            }

            var trigger = keyword.ToLexemes(parser.Token).Render();

            if (Definition.Parse(ref parser) is not Scope definition)
            {
                return new ExpectedDefinitionError { Tokens = Parser.Recover(ref current, parser) };
            }

            current = parser;
            return new Reactive(definition)
            {
                Modifiers = modifiers,
                Target = datum,
                Trigger = trigger
            };
        }

        public class ExpectedTargetError : Reactive, IError
        {
            public string Reason { get; } = "expected reactive variable";
            public ReadOnlyMemory<Token> Tokens { get; init; }
        }

        public class ExpectedDefinitionError : Reactive, IError
        {
            public string Reason { get; } = "expected definition";
            public ReadOnlyMemory<Token> Tokens { get; init; }
        }
    }

    public class Definition : Scope
    {
        private Definition(Scope scope) : base(scope) { }

        public static new Definition Parse(ref Parser current)
        {
            Parser parser = current;
            Statement definition = null;

            if (parser.TryAdvance<Returns>())
            {
                // A consumed «=>» commits to a value. Falling through to the
                // block form when none followed made «function f => {}» a
                // function whose body is «{}» and whose «=>» simply evaporated.
                //
                // The error goes where the value should have been rather than in
                // place of the definition itself: a scope is folded into its
                // parent by its STATEMENTS, so a definition that is an error
                // hands up an empty list and takes the error with it.
                if (Value.Parse(ref parser) is not Value value)
                {
                    var missing = new ExpectedValueError { Tokens = Parser.Recover(ref current, parser) };
                    return new Definition(new Scope { Statements = { missing } });
                }

                definition = value;
            }

            definition ??= Basic.Parse(ref parser);

            if (definition is null) return null;

            current = parser;
            return new(definition as Scope ?? new Scope { Statements = { definition } });
        }

        public class ExpectedValueError : Statement, IError
        {
            public string Reason { get; } = $"expected a value after '{Returns.symbol}'";
            public ReadOnlyMemory<Token> Tokens { get; init; }
        }
    }

    internal class Basic : Scope, IList<Statement>
    {
        public static new Basic Parse(ref Parser current) => Aggregate<Basic, Open.Brace, Statement, Terminal, Close.Brace>.Parse(ref current);

        [ExcludeFromCodeCoverage]
        public Statement this[int index] 
        {
            get => Statements[index];
            set => Statements[index] = value; 
        }

        [ExcludeFromCodeCoverage] public int Count => Statements.Count;
        [ExcludeFromCodeCoverage] public bool IsReadOnly => false;

        [ExcludeFromCodeCoverage] public void Add(Statement item) => Statements.Add(item);
        [ExcludeFromCodeCoverage] public void Clear() => Statements.Clear();
        [ExcludeFromCodeCoverage] public bool Contains(Statement item) => Statements.Contains(item);
        [ExcludeFromCodeCoverage] public void CopyTo(Statement[] array, int arrayIndex) => Statements.CopyTo(array, arrayIndex);
        [ExcludeFromCodeCoverage] public IEnumerator<Statement> GetEnumerator() => Statements.GetEnumerator();
        [ExcludeFromCodeCoverage] public int IndexOf(Statement item) => Statements.IndexOf(item);
        [ExcludeFromCodeCoverage] public void Insert(int index, Statement item) => Statements.Insert(index, item);
        [ExcludeFromCodeCoverage] public bool Remove(Statement item) => Statements.Remove(item);
        [ExcludeFromCodeCoverage] public void RemoveAt(int index) => Statements.RemoveAt(index);
        [ExcludeFromCodeCoverage] IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Statements).GetEnumerator();
    }

    internal class Conditional<T> : Scope where T : Keyword
    {
        public Member Condition { get; init; }

        /// <summary>
        ///     The clause as written, from the keyword to the block, rendered
        ///     canonically. For a <c>when</c> this is its name — see
        ///     <see cref="Triggers"/> for why that needs no syntax.
        /// </summary>
        public string Trigger { get; init; }

        protected Conditional() { }
        protected Conditional(Scope scope) : base(scope) { }

        public static new Conditional<T> Parse(ref Parser current)
        {
            Parser parser = current;

            var modifiers = Modifiers.Parse(ref parser);

            var keyword = parser.Token;
            if (parser.TryAdvance<T>() is false) return null;

            if (Member.Unresolved.Parse(ref parser) is not Member condition)
            {
                return new ExpectedConditionError { Tokens = Parser.Recover(ref current, parser) };
            }

            var trigger = keyword.ToLexemes(parser.Token).Render();

            if (Definition.Parse(ref parser) is not Scope definition) return null;

            current = parser;
            return new Conditional<T>(definition)
            {
                Modifiers = modifiers,
                Condition = condition,
                Trigger = trigger
            };
        }

        public class ExpectedConditionError : Conditional<T>, IError
        {
            public string Reason { get; } = "expected condition";
            public ReadOnlyMemory<Token> Tokens { get; init; }
        }
    }
}