using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

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
internal class ExpectedSyntaxError<TOpen> : Error where TOpen : Open
{
    public ExpectedSyntaxError(ref Parser parser) : base(ref parser) { }
}

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
internal class ExpectedSyntaxError<TSeparator, TClose> : Error 
    where TSeparator : Symbol
    where TClose : Close
{
    public ExpectedSyntaxError(ref Parser parser) : base(ref parser) { }
}
