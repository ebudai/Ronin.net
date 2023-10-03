// Copyright © 2023 Eric Budai

using Ronin.Lexicon;
using System.Collections.Generic;

namespace Ronin.Grammar;

/// <summary>
///     Aggregate of <see cref="Datum.Declaration"/> used to declare variables to enter into a <see cref="Function.Declaration"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="Datum"/>s between <see cref="StartValues"/> and <see cref="EndValues"/>
/// </remarks>
/// 
/// <example>
///     function thing (x => number, y => money) with stuff { return 8; }
///                    ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Parameters : Aggregate<Parameters, StartValues, Datum.Declaration, Separator, EndValues>
{
    public Dictionary<Identifier, Datum> Data { get; } = new();

    public int MandatoryInputsCount
    {
        get
        {
            var mandatory = 0;
            foreach (var parameter in Data.Values)
            {
                if (parameter.Modifiers?.Is<Optional>() ?? false) continue;
                if (parameter.Initializer is not null) continue;
                ++mandatory;
            }
            return mandatory;
        }
    }

    public bool Bind(Inputs inputs)
    {
        var mandatory = MandatoryInputsCount;
        foreach (var input in inputs)
        {
            
        }
        return mandatory is >= 0;
    }
}