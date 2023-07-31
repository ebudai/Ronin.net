using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Function = Ronin.Grammar.Function;

namespace Unit;

[Trait("Parser", null)]
public class Exports : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // part of things;

        List<Token> tokens = new()
        {
            Keyword.PartOf(),
            Word("things"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var export = Join.Parse(ref parser);

        Assert.Equal(1, export.Name?.Source.Length);     
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void WithExport()
    {
        // part of standard funstuff websockets;

        List<Token> tokens = new()
        {
            Keyword.PartOf(),
            Word("standard"),
            Word("funstuff"),
            Word("websockets"),
            Terminal(),
            Sentinel.Instance
        };
                
        Parser parser = new(tokens);
        var export = Join.Parse(ref parser);

        Assert.Equal(3, export.Name?.Source.Length);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        // part of thing compiled to whatever secret stuff;

        List<Token> tokens = new()
        {
            Keyword.PartOf(),
            Word("thing"),
            Keyword.Compiled(),
            Word("to"),
            Word("whatever"),
            Word("secret"),
            Word("stuff"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var export = Join.Parse(ref parser);

        Assert.Equal(6, export.Name?.Source.Length);
    }

    [Trait("Analyzer", "declaration")]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "basic")]
        public void Basic()
        {
            const string widgets = nameof(widgets);
            const string with = nameof(with);
            const string stuff = nameof(stuff);

            // { part of thing with stuff; }

            AnonymousScope module = new()
            {
                Definition = new()
                {
                    Values = new List<Statement>
                    {
                        new Join
                        {
                            Keyword = new PartOf(),
                            Name = Words(widgets, with, stuff)
                        }
                    }
                }
            };

            Global.Scope.Children.Clear();
            Global.Scope.Members.Clear();

            List<Error> errors = new();
            Analyzer.Define(Global.Scope, module, errors);
            Assert.Empty(errors);

            Assert.Single(Global.Scope.Children);

            Name name = Global.Scope.Children.First().Key;

            Assert.Equal(3, name.Source.Length);
            Assert.Equal(widgets, name.Source.Span[0].Memory.ToArray());
            Assert.Equal(with, name.Source.Span[1].Memory.ToArray());
            Assert.Equal(stuff, name.Source.Span[2].Memory.ToArray());
        }

        [Fact(DisplayName = "join existing")]
        public void JoinExisting()
        {
            const string what = nameof(what);
            const string the = nameof(the);
            const string correct = nameof(correct);
            const string horse = nameof(horse);
            const string battery = nameof(battery);
            const string cash = nameof(cash);
            const string money = nameof(money);
            const string whole = nameof(whole);
            const string number = nameof(number);
            const string @return = nameof(@return);

            // {
            //      part of what the what;
            //      var what what;
            //      function correct horse battery(horse => number) => whole number { return 24; }
            // }
            //
            // {
            //      part of what the what;
            //      var the the;
            //      function correct horse battery(cash => money) => whole number { return 72; }
            // }

            AnonymousScope module = new()
            {
                Definition = new()
                {
                    Values = new List<Statement>
                    {
                        new AnonymousScope
                        {
                            Definition = new()
                            {
                                Values = new List<Statement>
                                {
                                    new Join { Keyword = new PartOf(), Name = Words(what, the, what) },
                                    new Datum.Declaration { Name = Words(what, what) },
                                    new Function.Declaration
                                    {
                                        Identifier = new()
                                        {
                                            Components = new List<Identifier.Component>
                                            {
                                                new() { value = Words(correct, horse, battery) },
                                                new()
                                                {
                                                    value = new Parameters
                                                    {
                                                        Values = new List<Datum.Declaration>
                                                        {
                                                            new()
                                                            {
                                                                Datatype = new Reference
                                                                {
                                                                    Components = new List<Reference.Component> { new() { value = Words(number) } },
                                                                    Source = new[] { Word(money) }
                                                                },
                                                                Mutability = new Variable(),
                                                                Name = new() { Source = new[] { Word(horse) } },
                                                                Source = new Token[] { Word(horse), Returns(), Word(number) }
                                                            }
                                                        },
                                                        Source = new Token[] { StartValues(), Word(horse), Returns(), Word(number), EndValues() }
                                                    }
                                                }
                                            }
                                        },
                                        Returns = new Reference
                                        {
                                            Components = new List<Reference.Component>
                                            {
                                                new() { value = Words(whole, number) },
                                            }
                                        },
                                        Definition = new()
                                        {
                                            Values = new List<Statement>
                                            {
                                                new Reference
                                                {
                                                    Components = new List<Reference.Component>
                                                    {
                                                        new() { value = Words(@return) },
                                                        new() { value = new Inline { Source = new[] { Number(24) } } },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new AnonymousScope
                        {
                            Definition = new()
                            {
                                Values = new List<Statement>
                                {
                                    new Join { Keyword = new PartOf(), Name = Words(what, the, what) },
                                    new Datum.Declaration { Name = Words(the, the) },
                                    new Function.Declaration
                                    {
                                        Identifier = new()
                                        {
                                            Components = new List<Identifier.Component>
                                            {
                                                new() { value = Words(correct, horse, battery) },
                                                new()
                                                {
                                                    value = new Parameters
                                                    {
                                                        Values = new List<Datum.Declaration>
                                                        {
                                                            new()
                                                            {
                                                                Datatype = new Reference
                                                                {
                                                                    Components = new List<Reference.Component> { new() { value = Words(money) } },
                                                                    Source = new[] { Word(money) }
                                                                },
                                                                Mutability = new Variable(),
                                                                Name = new() { Source = new[] { Word(cash) } },
                                                                Source = new Token[] { Word(cash), Returns(), Word(money) }
                                                            }
                                                        },
                                                        Source = new Token[] { StartValues(), Word(cash), Returns(), Word(money), EndValues() }
                                                    }
                                                }
                                            }
                                        },
                                        Returns = new Reference
                                        {
                                            Components = new List<Reference.Component>
                                            {
                                                new() { value = new Name { Source = new[] { Word(whole), Word(number) } } },
                                            }
                                        },
                                        Definition = new()
                                        {
                                            Values = new List<Statement>
                                            {
                                                new Reference
                                                {
                                                    Components = new List<Reference.Component>
                                                    {
                                                        new() { value = new Name { Source = new[] { Word(@return) } } },
                                                        new() { value = new Inline { Source = new[] { Number(72) } } },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            Global.Scope.Children.Clear();
            List<Error> errors = new();
            Analyzer.Define(Global.Scope, module, errors);
            Assert.Empty(errors);

            Assert.Single(Global.Scope.Children);

            var child = Global.Scope.Children.First().Value;

            Assert.Single(child.Children);
            Assert.Equal(3, child.Members.Count);
        }
    }
}