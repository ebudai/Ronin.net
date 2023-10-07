using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

using Delegate = Ronin.Grammar.Delegate;

namespace Failure;

[Trait("Parser", null)]
public class Delegates : ParsingTests
{
    [Fact(DisplayName = "missing returns symbol")]
    public void MissingReturns()
    {
        // (things, stuff, others) { return 3; }

        List<Token> tokens = new()
        {
            StartValues(),
            Word("things"),
            Separator(),
            Word("stuff"),
            Separator(),
            Word("others"),
            EndValues(),
            StartScope(),
            Word("return"),
            Number(3),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens.AsLinkedList());
        var @delegate = Delegate.Parse(ref parser);
        
        Assert.Null(@delegate);
    }

    [Fact(DisplayName = "no body")]
    public void NoBody()
    {
        // billy => ;
        List<Token> tokens = new()
        {
            Word("billy"),
            Returns(),
            Terminal(),
        };

        Parser parser = new(tokens.AsLinkedList());
        var @delegate = Delegate.Parse(ref parser);

        Assert.Null(@delegate);
    }
}
