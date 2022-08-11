using Ronin.Parser;
using Ronin.Parser.Grammar;
using System.Reflection;

namespace Unit;

public class UnitTest
{
    protected internal UnitTest(string name)
    {
        Context context = new(new FileInfo(@$"code\{name}.ronin"));

        scope = Scope.Parse(context);

        Assert.NotNull(scope);
    }

    protected internal static readonly PropertyInfo SyntaxProperty = typeof(Expression).GetProperty("Syntax", BindingFlags.Instance | BindingFlags.NonPublic);

    internal readonly Scope scope;
}
