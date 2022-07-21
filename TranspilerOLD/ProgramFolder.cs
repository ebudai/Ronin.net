namespace Ronin.Transpiler;

public class ProgramFolder
{
    public ProgramFolder[] Folders { get; init; } = Array.Empty<ProgramFolder>();
    public ProgramFile[] Files { get; init; } = Array.Empty<ProgramFile>();
}
