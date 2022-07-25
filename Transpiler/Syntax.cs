using Ronin.Transpiler.Program.Statements;
using System.Text.RegularExpressions;

namespace Ronin.Transpiler;

internal static class Syntax
{
    public const string Terminal = ".";
    public const string Assign = "=";
    public const string TypeStart = ":";
    public const string Separator = ",";

    public const string TupleStart = "(";
    public const string TupleEnd = ")";

    private const RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;

    //TODO still need lambda operator (-> or => ??)
    private static readonly Regex whitespace =      new(@"^\s+", options);
    private static readonly Regex strings =         new(@"^""[^""\\]*(\\.[^""\\]*)*"""                  , options);
    private static readonly Regex characters =      new(@"^'\\?.'"                                      , options);
    private static readonly Regex unicodes =        new(@"^'\\u[a-f0-9]{4}'"                            , options | RegexOptions.IgnoreCase);
    private static readonly Regex hexadecimals =    new(@"^0x[\d_a-f]+"                                 , options | RegexOptions.IgnoreCase);
    private static readonly Regex binaries =        new(@"^0b[01_]+"                                    , options | RegexOptions.IgnoreCase);
    private static readonly Regex floats =          new(@"^\d[\d_]*[.][\d_]"                            , options);
    private static readonly Regex reals =           new(@"^\d[\d_]*([.][\d_])?[\d_]*r(16|64)"           , options | RegexOptions.IgnoreCase);
    private static readonly Regex money =           new(@"^\$\d[\d_]*([.][\d_])?[\d_]*"                 , options);
    private static readonly Regex integers =        new(@"^\d[\d_]*(i(8|16|32|64)?)?"                   , options | RegexOptions.IgnoreCase); //TODO take care of the suffix using units
    private static readonly Regex terminal =        new(@"^[.]"                                         , options);
    private static readonly Regex brackets =        new(@"^[\[({})\]]"                                  , options);
    private static readonly Regex symbols =         new(@"^[,=:]"                                       , options);
    private static readonly Regex identifiers =     new(@"^[^\d\s\[({})\],=.:'""][^\s\[({})\],=.:'""]*" , options); // no symbols (underscore allowed) or brackets (angle allowed)
                                                                                                                    // can't start with a number
                                                                                                                    // spaces are allowed in identifiers, but we will concat later
    private static readonly Regex keywords = new(@"^(package|var|const|reactive|function|type)", options);

    // statements' ToString() returns a coded string which we can regex on to easily match token arrays to statement types
    // <...> is a keyword
    // I is an indicator
    // L is a literal
    private static readonly Regex package = new(@"^<package>I*[.]", options);
    private static readonly Regex declareVariableExplicit = new(@"^<var>I+:I+(=(L|I+))?[.]", options);
    private static readonly Regex declareVariableImplicit = new(@"^<var>I+=(L|I+)[.]", options);
    private static readonly Regex declareVariableDeconstructed = new(@"^<var>\(I+(,I+)*\)=(I+|\((L|I+)(,(L|I+))+\))[.]", options);
    private static readonly Regex declareVariableExplicitTuple = new(@"^<var>I+:\(I+(,I+)+\)(=(I+|\((L|I+)(,(L|I+))+\)))?[.]", options);
    private static readonly Regex declareVariableImplicitTuple = new(@"^<var>I+=(I+|\((L|I+)(,(L|I+))+\))[.]", options);    

    private static readonly Regex literal = new(@"^L[.,]", options);
    private static readonly Regex identifier = new(@"^I+[.,]", options);
    private static readonly Regex tuple = new(@"^\((L|I+)(,(L|I+))+\)[.,]", options);

    public static readonly Regex[] ParseOrder =
    {
        package,
        declareVariableExplicit,
        declareVariableImplicit,
        declareVariableDeconstructed,
        declareVariableImplicitTuple,
        declareVariableExplicitTuple,
        literal,
        identifier,
        tuple,
    };

    public static readonly Dictionary<Regex, Type> StatementTypes = new()
    {
        { package, typeof(PackageStatement) },
        { declareVariableExplicit, typeof(DeclareVariableStatement) },
        { declareVariableImplicit, typeof(DeclareVariableStatement) },
        { declareVariableDeconstructed, typeof(DeclareTupleStatement) },
        { declareVariableImplicitTuple, typeof(DeclareTupleStatement) },
        { declareVariableExplicitTuple, typeof(DeclareTupleStatement) },
        { literal, typeof(LiteralStatement) },
        { identifier, typeof(IdentifierStatement) },
        { tuple, typeof(TupleStatement) },
    };

    public static readonly Regex[] LexicalOrder =
    {
        whitespace,
        strings,
        characters,
        unicodes,
        hexadecimals,
        binaries,
        reals,
        floats,
        money,
        integers, //TODO support 128 bit?
        terminal,
        brackets,
        symbols,
        keywords,
        identifiers,
    };

    public static readonly Dictionary<Regex, Token.Type> TokenTypes = new(ReferenceEqualityComparer.Instance)
    {
        { strings, Token.Type.Literal },
        { characters, Token.Type.Literal },
        { unicodes, Token.Type.Literal },
        { hexadecimals, Token.Type.Literal },
        { binaries, Token.Type.Literal },
        { reals, Token.Type.Literal },
        { floats, Token.Type.Literal },
        { money, Token.Type.Literal },
        { integers, Token.Type.Literal },
        { terminal, Token.Type.Symbol },
        { brackets, Token.Type.Symbol },
        { symbols, Token.Type.Symbol },
        { keywords, Token.Type.Keyword },
        { identifiers, Token.Type.Identifier },
    };
}
