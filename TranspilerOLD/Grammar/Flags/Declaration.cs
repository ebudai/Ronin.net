namespace Ronin.Transpiler.Grammar.Flags;

[Flags]
public enum Declaration : uint
{
    Type = 1,
    Member = 2,
    Local = 4,
    Parameter = 8,
    All = uint.MaxValue
}
