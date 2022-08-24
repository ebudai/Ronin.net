using Ronin.Grammar;
using Ronin.Parser;

using static Ronin.Parser.Parser;

namespace Ronin.Program;

public class Program
{
    public Program(DirectoryInfo folder)
    {
        var files = folder.EnumerateFiles("*.ronin", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            Context context = new(file);
            var scope = Parse(context) as Scope;

            if (scope.Name.Names.Count is 0)
            {
                //Scope.Global.Add(scope);
            }
        }
    }
}