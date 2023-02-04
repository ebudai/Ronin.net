// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     The part of an <see cref="Identifier"/> or <see cref="Reference"/> which is not being used for parameters     
/// </summary>
internal class Name : Syntax, Compiler.IParsable<Name>
{
    internal List<string> Words { get; init; }

    public static Name Parse(ref Parser context)
    {
        if (context.CurrentToken is Keyword or Punctuation) return null;

        List<string> words = new(64);
        Parser parser = context;

        while (parser.IsNotFinished)
        {
            var name = parser.CurrentToken;
            
            if (name is Word or Symbol and not Punctuation)
            {
                words.Add(name);
            }
            else
            {
                break;
            }

            parser.Advance();
        }

        if (words.Count is 0) return null;

        return new Name { Words = words, Source = parser.Commit(ref context) };
    }
}