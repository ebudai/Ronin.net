// Copyright © 2023 Eric Budai

using OneOf;
using Ronin.Compiler;
using Ronin.Lexicon;
using static Ronin.Grammar.Lookup;

namespace Ronin.Grammar;

/// <summary>
///     Aggregate of <see cref="Datum"/> used to declare variables to enter into a <see cref="Function"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="Datum"/>s between <see cref="OpenParenthesis"/> and <see cref="CloseParenthesis"/>
/// </remarks>
/// 
/// <example>
///     function thing (x => number, y => money) with stuff { return 8; }
///                    ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Parameters : Aggregate<Parameters, OpenParenthesis, Parameters.Parameter, Separator, CloseParenthesis>
{
    public class Parameter : OneOfBase<Datum, Association>, IGrammar<Parameter>
    {
        protected Parameter(OneOf<Datum, Association> _) : base(_) { }

        public static implicit operator Parameter(Datum datum) => datum;
        public static implicit operator Parameter(Association association) => association;

        public static Parameter Parse(ref Parser current)
            => Datum.Parse(ref current) is Datum datum
                ? datum
                : Association.Parse(ref current);
    }
}