using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Analyzer", "declare")]
public class Identifier : AnalysisTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // thing with (a => number, b => money) stuff;

        Ronin.Grammar.Identifier name = new() { Components = new() };
        List<Token> parts = new();
        Word thing = new();
        thing.SetMemory("thing");
        Word with = new();
        with.SetMemory("with");

        Ronin.Grammar.Compound.Parameters parameters = new();
        
    }
}

