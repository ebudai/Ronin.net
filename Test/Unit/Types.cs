using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Type = Ronin.Grammar.Type;

namespace Unit;

[Trait(nameof(Parser), null)]
public class Types : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // datatype Test { }

        List<Token> tokens = new()
        {
            Keyword.Datatype(),
            Word("Test"),
            StartScope(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var datatype = Type.Parse(ref parser);

        Assert.Single(datatype?.Identifier);
        var name = datatype.Identifier.Components[0].AsT0;
        Assert.Single(name?.Tokens.ToArray());
    }

    [Fact(DisplayName = "with algebra and members")]
    public void Algebra()
    {
        // datatype Algebra Example = number or { var cash => money; var debt => money; }

        List<Token> tokens = new()
        {
            Keyword.Datatype(),
            Word("Algebra"),
            Word("Example"),
            Assign(),
            Word("number"),
            Word("or"),
            StartScope(),
            Keyword.Variable(),
            Word("cash"),
            Returns(),
            Word("money"),
            Terminal(),
            Keyword.Variable(),
            Word("debt"),
            Returns(),
            Word("money"),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var type = Type.Parse(ref parser);

        Assert.Single(type?.Identifier);
        var algebra = type.Algebra as Algebra.Unresolved;
        Assert.Single(algebra.Reference);
        Assert.Equal(2, type.Members.Count);

        {
            var cash = type.Members[0] as Datum;
            Assert.IsType<Variable>(cash?.Mutability);
            Assert.Single(cash.Identifier);
            var unresolved = cash.Type as Type.Unresolved;
            Assert.Single(unresolved?.Reference);
            var name = unresolved.Reference.Components[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var debt = type.Members[1] as Datum;
            Assert.IsType<Variable>(debt?.Mutability);
            Assert.Single(debt.Identifier);
            var unresolved = debt.Type as Type.Unresolved;
            Assert.Single(unresolved?.Reference);
            var name = unresolved.Reference.Components[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
        }
    }

    /*[Trait(nameof(Analyzer), nameof(Declaration))]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "basic")]
        public void Basic()
        {
            const string Big = nameof(Big);
            const string text = nameof(text);
            const string or = nameof(or);
            const string x = nameof(x);
            const string number = nameof(number);

            // datatype Big = text or { var x => number; }

            Context module = new()
            {
                new Datatype.Declaration
                {
                    Identifier = Words(Big),
                    Algebra = Reference(text, or),
                    Definition = new()
                    {
                        new Datum.Declaration
                        {
                            Mutability = new Variable(),
                            Identifier = Words(x),
                            Datatype = Reference(number)
                        }
                    }
                }
            };

            Analyzer analyzer = new();
            module.Parent = analyzer.Global;
            analyzer.Define(module);
            Assert.Empty(analyzer.Errors);

            Assert.Single(module);

            var entry = module.Members.First();
            var identifier = entry.Key;
            var datatype = entry.Value as Datatype;

            Assert.Single(identifier.Source.ToArray());
            Assert.Equal(Big, identifier.Source.Span[0].Memory.ToString());

            var algebra = datatype.Algebra as Algebra.Unresolved;
            Assert.Equal(2, algebra?.Reference.Components.Count);
            Assert.Equal(1, algebra.Reference.Components[0].value.Source.Length);
            Assert.Equal(text, algebra.Reference.Components[0].value.Source.Span[0].Memory.ToString());
            Assert.Equal(1, algebra.Reference.Components[1].value.Source.Length);
            Assert.Equal(or, algebra.Reference.Components[1].value.Source.Span[0].Memory.ToString());

            Assert.Single(datatype.Definition);

            var datumentry = datatype.Definition.Members.First();
            var name = datumentry.Key;
            var datum = datumentry.Value as Datum;

            Assert.Single(name.Source.ToArray());
            Assert.Equal(x, name.Source.Span[0].Memory.ToString());

            Assert.IsType<Variable>(datum?.Mutability);
            var unresolved = datum.Datatype as Datatype.Unresolved;
            Assert.Single(unresolved?.Reference.Components);
            var unresolvedname = unresolved.Reference.Components[0].value as Name;
            Assert.Single(unresolvedname.Source.ToArray());
            Assert.Equal(number, unresolvedname.Source.Span[0].Memory.ToString());
        }
    }

    [Trait(nameof(Analyzer), nameof(Resolution))]
    public class Resolution : AnalysisTests
    {
        [Fact(DisplayName = "overloaded")]
        public void Overloaded()
        {
            const string Car = nameof(Car);
            const string car = nameof(car);
            const string number = nameof(number);
            const string money = nameof(money);
            const string x = nameof(x);

            // datatype Car(x => number) { }
            // datatype Car(x => money) { }
            // var car => Car(3);

            Analyzer analyzer = new();
                        
            analyzer.Global.Add(Identifier(number), new Datatype());
            analyzer.Global.Add(Identifier(money), new Datatype());

            {
                Parameters param = new();
                Identifier id = Identifier(x);
                //param.Data.Add(id, new Datum { Datatype = analyzer.Global.Contexts[0].Members[Identifier(number)] as Datatype });
                var error = analyzer.Global.Add(Identifier(Name(Car), param), new Datatype());
                Assert.Null(error);
            }

            {
                Parameters param = new();
                Identifier id = Identifier(x);
                //param.Data.Add(id, new Datum { Datatype = analyzer.Global.Contexts[0].Members[Identifier(money)] as Datatype });
                var error = analyzer.Global.Add(Identifier(Name(Car), param), new Datatype());
                Assert.Null(error);
            }

            Reference.Component parameter = new() { value = new Inline { Source = new[] { Number(3) } }, Source = new[] { Number(3) } };
            var datatype = Reference(Name(Car), parameter);

            Datum datum = new() 
            {
                Datatype = new Datatype.Unresolved { Reference = datatype } 
            };

            analyzer.Global.Add(Identifier(car), datum);

            analyzer.Resolve();
            Assert.Empty(analyzer.Errors);

            var overloaded = datum.Datatype as Datatype.Overloaded;
            Assert.Equal(2, overloaded?.Overloads.Count);
        }

        [Fact(DisplayName = "algebra")]
        public void Algebra()
        {
            const string Car = nameof(Car);
            const string car = nameof(car);
            const string number = nameof(number);
            const string and = nameof(and);

            // datatype Car = number and { }
            // var car => Car;

            Analyzer analyzer = new();
            Datatype.Unresolved datatype = new()
            {
                Algebra = new Algebra.Unresolved { Reference = Reference(number, and) },
                Reference = Reference(Car)
            };
            analyzer.Global.Add(Identifier(Car), datatype);

            Datum datum = new() { Datatype = datatype };

            analyzer.Errors.Add(analyzer.Global.Add(Identifier(car), datum));
            Assert.Empty(analyzer.Errors);

            analyzer.Resolve(analyzer.Global);
            Assert.Empty(analyzer.Errors);

            var overloaded = datum.Datatype.Algebra as Algebra.Overloaded;
            Assert.Single(overloaded?.Overloads);
        }
    }*/
}
