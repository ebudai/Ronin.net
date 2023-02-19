using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;

namespace Ronin.Grammar;

internal class Loop : Syntax, Compiler.IParsable<Loop>
{
    public bool Mutable { get; init; } = false;
    public Name Variable { get; init; }
    public Value List { get; init; }
    public Scope Body { get; init; }

    public static Loop Parse(ref Parser context)
    {
        Parser parser = context;

        if (parser.FailedToConsume<ForEach>()) return null;

        var mutable = parser.CurrentToken is Variable;
        if (mutable) parser.Advance();

        if (RestrictedName.Parse(ref parser) is not Name variable) return null;

        if (parser.FailedToConsume<In>()) return null;

        if (Value.Parse(ref parser) is not Value list) return null;

        if (Scope.Parse(ref parser) is not Scope body) return null;

        return new Loop
        {
            Mutable = mutable,
            Body = body,
            Variable = variable,
            List = list,
            Source = parser.Commit(ref context)
        };
    }

    public class RestrictedName : Name
    {
        public static new Name Parse(ref Parser context)
        {
            if (context.CurrentToken is Keyword or Punctuation) return null;

            List<Token> words = new(64);
            Parser parser = context;

            while (parser.IsNotFinished)
            {
                var name = parser.CurrentToken;

                if (name is Word and not In or Symbol and not Punctuation)
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

            return new RestrictedName { Words = words, Source = parser.Commit(ref context) };
        }
    }
}
