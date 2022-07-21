namespace Ronin.Transpiler;

[System.Diagnostics.DebuggerDisplay("{Value}")]
internal class Token
{
    public string Value { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
}
