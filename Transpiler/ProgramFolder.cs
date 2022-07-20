using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ronin.Transpiler;

public class ProgramFolder
{
    public ProgramFolder[] Folders { get; } = Array.Empty<ProgramFolder>();
    public ProgramFile[] Files { get; } = Array.Empty<ProgramFile>();
}
