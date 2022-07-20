namespace Ronin.Transpiler.Grammar.Tokens;

[Flags]
internal enum LexicalScope : uint
{
    Global,
    TypeDefinition,
    Function,
    All = uint.MaxValue
}
