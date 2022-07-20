namespace Ronin.Transpiler.Grammar.Tokens;

[Flags]
public enum Declaration : uint
{
    Type = 1,
    Member = 2,
    Local = 4,
    Parameter = 8,
    All = uint.MaxValue
}
