using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar.Errors;

/// <summary>
///     Thrown when parser's current <see cref="Token"/> is not <typeparamref name="TSeparator"/> or <typeparamref name="TClose"/>
/// </summary>
/// 
/// <typeparam name="TSeparator">
///     <see cref="Symbol"/> separating each element of the Aggregate
/// </typeparam>
/// 
/// <typeparam name="TClose">
///     <see cref="Symbol"/> completing the Aggregate
/// </typeparam>
internal class ExpectedSyntaxError<T> : Error where T : Symbol
{
    public ExpectedSyntaxError(ref Parser parser) : base(ref parser) { }
}

/// <summary>
///     Thrown when parser's current <see cref="Token"/> is not <typeparamref name="T0"/> or <typeparamref name="T1"/>
/// </summary>
/// 
/// <typeparam name="T0">
///     <see cref="Symbol"/> separating each element of the Aggregate
/// </typeparam>
/// 
/// <typeparam name="T1">
///     <see cref="Symbol"/> completing the Aggregate
/// </typeparam>
internal class ExpectedSyntaxError<T0, T1> : Error 
    where T0 : Symbol
    where T1 : Symbol
{
    public ExpectedSyntaxError(ref Parser parser) : base(ref parser) { }
}
