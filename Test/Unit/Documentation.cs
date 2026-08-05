// Copyright © 2026 Eric Budai

using System.IO;
using System.Xml.Linq;

namespace Unit;

/// <summary>
///     What the generated documentation says each member is.
/// </summary>
///
/// <remarks>
///     Found by audit. Two summary blocks written one above the other both
///     attach to whatever declaration follows — so inserting a class between an
///     existing comment and its own declaration silently moves that comment onto
///     the new one, and leaves the old declaration with none.
///
///     XML permits repeated elements, so the malformed-XML gate cannot see this:
///     the file is well formed and describes the wrong member. Nine places in
///     the compiler had it, two of them mine and three the audit's own sweep did
///     not reach.
/// </remarks>
public class Documentation
{
    private static readonly string Generated = Path.Combine(AppContext.BaseDirectory, "Ronin.xml");

    [Fact(DisplayName = "no member is described twice, or as something it is not")]
    public void NoMemberIsDescribedTwiceOrAsSomethingItIsNot()
    {
        Assert.True(File.Exists(Generated), $"{Generated} is missing — the build must generate documentation");

        var doubled = from member in XDocument.Load(Generated).Descendants("member")
                      where member.Elements("summary").Count() > 1
                      select member.Attribute("name").Value;

        Assert.Empty(doubled);
    }
}
