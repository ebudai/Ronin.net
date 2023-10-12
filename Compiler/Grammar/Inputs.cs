// Copyright © 2023 Eric Budai

using OneOf;
using Ronin.Compiler;
using Ronin.Lexicon;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Ronin.Grammar;

/// <summary>
///     Aggregate of <see cref="Input"/>s intended for setting <see cref="Parameters"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="Input"/> values between <see cref="OpenParenthesis"/> and <see cref="CloseParenthesis"/>
/// </remarks>
/// 
/// <example>
///     var x = pack(a, 8.2, "first name");
///                 ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Inputs : Aggregate<Inputs, Open.Parenthesis, Inputs.Input, Separator, Close.Parenthesis>
{
    public override void ResolveTypes(Scope context)
    {
        foreach (var input in this)
        {
            input.Switch
            (
                value => value.ResolveTypes(context), 
                association => association.ResolveTypes(context)
            );
        }
    }

    public class Input : OneOfBase<Association, Value>, IParsable<Input>
    {
        protected Input(OneOf<Association, Value> _) : base(_) { }

        public static implicit operator Input(Value value) => new(value);
        public static implicit operator Input(Association association) => new(association);

        public static Input Parse(ref Parser current)
        {
            if (Association.Parse(ref current) is Association association) return association;
            if (Grammar.Value.Parse(ref current) is Value value) return value;            
            return null;
        }
    }
}