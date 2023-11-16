using Ronin.Semantics;

namespace Unit;

[Trait(nameof(Analyzer), "Type Resolution")]
public class CalculatedData
{
    [Fact(DisplayName = "calculated type")]
    public void Calculated()
    {
        // function test (thing => stuff) => if stuff is not finished { }
    }
}
