// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

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
    public class Input : IParsable<Input>
    {
        private Input(Value value) => input = value;
        private Input(Association association) => input = association;

        public static implicit operator Input(Value value) => new(value);
        public static implicit operator Input(Association association) => new(association);

        public static Input Parse(ref Parser current)
        {
            if (Association.Parse(ref current) is Association association) return association;
            if (Value.Parse(ref current) is Value value) return value;
            return null;
        }

        public Value AsValue => input as Value;
        public Association AsAssociation => input as Association;

        private readonly Statement input;
    }
}
