using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Hierarchy;
using Ronin.Lexicon;
using Test;

using Datatype = Ronin.Grammar.Datatype;

namespace Unit;

[Trait(nameof(Parser), null)]
public class Datatypes : ParsingTests
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
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datatype = Datatype.Declaration.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Identifier.Component name = datatype.Identifier.Components[0];
        Assert.Equal(1, name?.Source.Length);
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
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datatype = Datatype.Declaration.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Name algebra = datatype.Algebra.Components[0];
        Assert.Equal(2, algebra?.Source.Length);

        Assert.Equal(2, datatype.Definition.Count);

        {
            var cash = datatype.Definition[0] as Datum.Declaration;
            Assert.IsType<Variable>(cash?.Mutability);
            Assert.Equal(1, cash.Identifier?.Source.Length);
            Assert.Single(cash.Datatype?.Components);
            Name type = cash.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        {
            var debt = datatype.Definition[1] as Datum.Declaration;
            Assert.IsType<Variable>(debt?.Mutability);
            Assert.Equal(1, debt.Identifier?.Source.Length);
            Assert.Single(debt.Datatype?.Components);
            Name type = debt.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }
    }

    [Trait(nameof(Analyzer), nameof(Declaration))]
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

            var entry = module.GetMembers().First();
            var identifier = entry.Key;
            var datatype = entry.Value as Datatype;

            Assert.Single(identifier.Components);
            Assert.Single(identifier.Components[0].Source.ToArray());
            Assert.Equal(Big, identifier.Components[0].Source.Span[0].Memory.ToArray());

            var algebra = datatype.Algebra as Algebra.Unresolved;
            Assert.Equal(2, algebra?.Reference.Components.Count);
            Assert.Equal(1, algebra.Reference.Components[0].value.Source.Length);
            Assert.Equal(text, algebra.Reference.Components[0].value.Source.Span[0].Memory.ToArray());
            Assert.Equal(1, algebra.Reference.Components[1].value.Source.Length);
            Assert.Equal(or, algebra.Reference.Components[1].value.Source.Span[0].Memory.ToArray());

            Assert.Single(datatype.Definition);

            var datumentry = datatype.Definition.GetMembers().First();
            var name = datumentry.Key;
            var datum = datumentry.Value as Datum;

            Assert.Single(name.Components);
            Assert.Single(name.Components[0].Source.ToArray());
            Assert.Equal(x, name.Components[0].Source.Span[0].Memory.ToArray());

            Assert.IsType<Variable>(datum?.Mutability);
            var unresolved = datum.Datatype as Datatype.Unresolved;
            Assert.Single(unresolved?.Reference.Components);
            var unresolvedname = unresolved.Reference.Components[0].value as Name;
            Assert.Single(unresolvedname.Source.ToArray());
            Assert.Equal(number, unresolvedname.Source.Span[0].Memory.ToArray());
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
            const string x = nameof(x);

            // datatype Car(x => number) { }
            // datatype Car(x => money) { }
            // var car => Car(3);

            Analyzer analyzer = new();

            {
                Parameters param = new();
                Identifier id = Identifier(x);
                param.Data.Add(id, new Datum { Datatype = new() });
                analyzer.Global.Add(Identifier(Name(Car), param), new Datatype());
            }

            {
                Parameters param = new();
                Identifier id = Identifier(x);
                param.Data.Add(id, new Datum { Datatype = new() });
                analyzer.Global.Add(Identifier(Name(Car), param), new Datatype());
            }

            Reference.Component parameter = new() { value = new Inline { Source = new[] { Number(3) } }, Source = new[] { Number(3) } };
            var datatype = Reference(Name(Car), parameter);

            Datum datum = new() 
            {
                Datatype = new Datatype.Unresolved { Reference = datatype } 
            };

            analyzer.Global.Add(Identifier(car), datum);
            Assert.Empty(analyzer.Errors);

            //analyzer.Resolve(Module.Global, errors);
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

            analyzer.Global.Add(Identifier(car), datum);
            Assert.Empty(analyzer.Errors);

            //analyzer.Resolve(Module.Global, errors);
            Assert.Empty(analyzer.Errors);

            var overloaded = datum.Datatype.Algebra as Algebra.Overloaded;
            Assert.Single(overloaded?.Overloads);
        }
    }
}
