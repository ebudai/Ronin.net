using Ronin.Grammar;

namespace Ronin.Hierarchy;

internal class Module : Context
{
    public Module() : base() { Parent = Global.Scope; }

    public List<Context> Contexts { get; init; } = new();

    public override Identifier Existing(Identifier identifier)
    {
        foreach (var context in Contexts)
        {
            if (context.Existing(identifier) is Identifier found) return found;
        }
        return null;
    }

    public override List<Resolution> Resolve(Reference reference)
    {
        List<Resolution> resolutions = new();

        foreach (var context in Contexts)
        {
            resolutions.AddRange(context.Resolve(reference));
        }

        return resolutions;
    }
}
