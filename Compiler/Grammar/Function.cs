// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Hierarchy;
using Ronin.Lexicon;
using System.Collections.Generic;

namespace Ronin.Grammar;

internal class Function : Context.Member
{
    public Datatype Returns { get; set; }
    public Context Definition { get; init; }
    
    /// <summary>
    ///     Ordered grouping of instructions to execute when called
    /// </summary>
    /// 
    /// <example>
    ///     function florb (x => number) things with (fast => maybe, fun => maybe) stuff => whole number 
    ///     {
    ///         return 8; 
    ///     }
    /// </example>
    public class Declaration : Scope, IParsableSyntax<Declaration>
    {
        public Identifier Identifier { get; init; }
        public Reference Returns { get; init; }

        public new static Declaration Parse(ref Parser current)
        {
            Parser parser = current;

            if (parser.TryAdvance<Lexicon.Function>() is false) return null;

            if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

            Modifiers modifiers = null;
            Reference returns = null;
            if (parser.Token is Returns)
            {
                parser.Advance();
                modifiers = Modifiers.Parse(ref parser);
                returns = Reference.Parse(ref parser);
            }

            var definition = Context.Parse(ref parser);

            return new Declaration
            {
                Identifier = identifier,
                Modifiers = modifiers,
                Returns = returns,
                Definition = definition,
                Source = parser.Commit(ref current)
            };
        }
    }

    public class Call : Statement, IParsableSyntax<Call>
    {
        public Function Function { get; set; }
        public List<Inputs> Inputs { get; } = new();

        public static new Call Parse(ref Parser current)
        {
            Parser parser = current;

            if (Reference.Parse(ref parser) is not Reference reference) return null;

            return new Call
            {
                Function = new Unresolved { Reference = reference },
                Source = parser.Commit(ref current)
            };
        }
    }

    public class Unresolved : Function
    {
        public Reference Reference { get; init; }
    }

    public class Overloaded : Function
    {
        public List<Resolution> Overloads { get; init; }
    }
}