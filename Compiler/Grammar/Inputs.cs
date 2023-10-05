// Copyright © 2023 Eric Budai

using OneOf;
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
internal class Inputs : Aggregate<Inputs, OpenParenthesis, Inputs.Input, Separator, CloseParenthesis>
{
    public class Input : Grammar<Input, Value>, IGrammar<Input>
    {
        public Input(OneOf<Value, Association> _) : base(_)
        {
        }

        public static new Input Parse(ref Parser current)
        {
            return Grammar<Value, Association>.Parse(ref current).Match(
                value => value,
                association => association
            );
        }
    }
}