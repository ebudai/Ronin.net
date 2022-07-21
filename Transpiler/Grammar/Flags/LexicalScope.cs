namespace Ronin.Transpiler.Grammar.Flags;

[Flags]
internal enum LexicalScope : uint
{
    Global,
    TypeDefinition,
    Function,
    All = uint.MaxValue
}
