using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar.Errors;

/// <summary>
///     Thrown when parser's current <see cref="Token"/> is not <typeparamref name="TSeparator"/> or <typeparamref name="TClose"/>
/// </summary>
/// 
/// <typeparam name="T">
///     <see cref="Token"/> expected
/// </typeparam>
internal class ExpectedSyntaxError<T> : Error
    where T : Token
{
    public ExpectedSyntaxError(ref Parser parser) : base(ref parser) { }
}

/// <summary>
///     Thrown when parser's current <see cref="Token"/> is not <typeparamref name="T0"/> or <typeparamref name="T1"/>
/// </summary>
/// 
/// <typeparam name="T0">
///     <see cref="Token"/> expected
/// </typeparam>
/// 
/// <typeparam name="T1">
///     <see cref="Token"/> expected
/// </typeparam>
internal class ExpectedSyntaxError<T0, T1> : Error 
    where T0 : Token
    where T1 : Token
{
    public ExpectedSyntaxError(ref Parser parser) : base(ref parser) { }
}

/// <summary>
///     Thrown when parser's current <see cref="Token"/> is not <typeparamref name="T0"/>, <typeparamref name="T1"/>, or <typeparamref name="T2"/>
/// </summary>
/// 
/// <typeparam name="T0">
///     <see cref="Token"/> expected
/// </typeparam>
/// 
/// <typeparam name="T1">
///     <see cref="Token"/> expected
/// </typeparam>
/// 
/// <typeparam name="T2">
///     <see cref="Token"/> expected
/// </typeparam>
internal class ExpectedSyntaxError<T0, T1, T2> : Error
    where T0 : Token
    where T1 : Token
    where T2 : Token
{
    public ExpectedSyntaxError(ref Parser parser) : base(ref parser) { }
}
