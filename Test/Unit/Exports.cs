using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Hierarchy;
using Ronin.Lexicon;
using Test;

using Function = Ronin.Grammar.Function;

namespace Unit;

[Trait(nameof(Parser), null)]
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
        var export = Export.Parse(ref parser);

        Assert.Equal(1, export.Identifier?.Source.Length);     
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
        var export = Export.Parse(ref parser);

        Assert.Equal(3, export.Identifier?.Source.Length);
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
        var export = Export.Parse(ref parser);

        Assert.Equal(6, export.Identifier?.Source.Length);
    }

    [Trait(nameof(Analyzer), nameof(Declaration))]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "basic")]
        public void Basic()
        {
            const string widgets = nameof(widgets);
            const string with = nameof(with);
            const string stuff = nameof(stuff);

            // { part of thing with stuff; }

            AnonymousScope scope = new()
            {
                Definition = new()
                {
                    new Export
                    {
                        Keyword = new PartOf(),
                        Identifier = Words(widgets, with, stuff)
                    }
                }
            };

            Analyzer analyzer = new();
            analyzer.DefineScope(analyzer.Global, scope);
            Assert.Empty(analyzer.Errors);

            Assert.Single(analyzer.Global.Modules);
            var module = analyzer.Global.Modules.FirstOrDefault().Value;
            Assert.Single(analyzer.Global.Modules);
            module = module.Modules.FirstOrDefault().Value;
            Assert.Single(module.Modules);
            module = module.Modules.FirstOrDefault().Value;
            Assert.Single(module.Contexts);
            var context = module.Contexts[0];
            Assert.Equal(scope.Definition, context);
        }

        [Fact(DisplayName = "join existing")]
        public void JoinExisting()
        {
            const string what = nameof(what);
            const string the = nameof(the);
            const string correct = nameof(correct);
            const string horse = nameof(horse);
            const string battery = nameof(battery);
            const string money = nameof(money);
            const string whole = nameof(whole);
            const string number = nameof(number);
            const string @return = nameof(@return);
            const string Bag = nameof(Bag);

            // {
            //      part of what the what;
            //      var what what;
            //      function correct horse battery(horse => number) => whole number { return 24; }
            //      function (correct => number) horse battery => number { return 8.2; }
            // }
            //
            // {
            //      part of what the what;
            //      var the the;
            //      function correct horse battery(horse => money) => whole number { return 72; }
            //      function (correct => Bag(15)) horse battery => number { return 12; }
            // }

            AnonymousScope scope = new()
            {
                Definition = new()
                {
                    new AnonymousScope
                    {
                        Definition = new()
                        {
                            new Export { Keyword = new PartOf(), Identifier = Words(what, the, what) },
                            new Datum.Declaration { Identifier = Words(what, what) },
                            new Function.Declaration
                            {
                                Identifier = new()
                                {
                                    Components = new List<Identifier.Component>
                                    {
                                        Name(correct, horse, battery),
                                        new Parameters
                                        {
                                            new()
                                            {
                                                Datatype = Reference(number),
                                                Mutability = new Constant(),
                                                Identifier = Words(horse),
                                                Source = new Token[] { Word(horse), Returns(), Word(number) }
                                            },
                                            //Source = new Token[] { StartValues(), Word(horse), Returns(), Word(number), EndValues() }
                                        }
                                    },
                                    Source = new Token[] { Word(correct), Word(horse), Word(battery), StartValues(), Word(horse), Returns(), Word(number), EndValues() }
                                },
                                Returns = Reference(whole, number),
                                Definition = new()
                                {
                                    UnresolvedReference(Name(@return), new Inline { Source = new Token[] { Number(24) } })
                                }
                            },
                            new Function.Declaration
                            {
                                Identifier = new()
                                {
                                    Components = new List<Identifier.Component>
                                    {
                                        new Parameters
                                        {
                                            new()
                                            {
                                                Datatype = Reference(new Name { Source = new[] { Word(Bag) } }, new Inline { Source = new Token[] { StartValues(), Number(15), EndValues() } } ),
                                                Mutability = new Constant(),
                                                Identifier = Words(correct),
                                                Source = new Token[] { StartValues(), Word(correct), Returns(), Word(number), EndValues() }
                                            }
                                        },
                                        Name(horse, battery)
                                    },
                                    Source = new Token[] { StartValues(), Word(correct), Returns(), Word(Bag), StartValues(), Number(15), EndValues(), EndValues(), Word(horse), Word(battery) }
                                },
                                Returns = Reference(number),
                                Definition = new()
                                {
                                    UnresolvedReference(Name(@return), new Inline { Source = new Token[] { Number(8.2) } })
                                }
                            }
                        }
                    },
                    new AnonymousScope
                    {
                        Definition = new()
                        {
                            new Export { Keyword = new PartOf(), Identifier = Words(what, the, what) },
                            new Datum.Declaration { Identifier = Words(the, the) },
                            new Function.Declaration
                            {
                                Identifier = new()
                                {
                                    Components = new List<Identifier.Component>
                                    {
                                        Name(correct, horse, battery),
                                        new Parameters
                                        {
                                            new()
                                            {
                                                Datatype = Reference(money),
                                                Mutability = new Variable(),
                                                Identifier = Words(horse),
                                                Source = new Token[] { Word(horse), Returns(), Word(money) }
                                            },
                                            //Source = new Token[] { StartValues(), Word(horse), Returns(), Word(money), EndValues() }
                                        }
                                    },
                                    Source = new Token[] { Word(correct), Word(horse), Word(battery), StartValues(), Word(horse), Returns(), Word(money), EndValues() }
                                },
                                Returns = Reference(whole, number),
                                Definition = new()
                                {
                                    UnresolvedReference(Name(@return), new Inline { Source = new[] { Number(72) } })
                                }
                            },
                            new Function.Declaration
                            {
                                Identifier = new()
                                {
                                    Components = new List<Identifier.Component>
                                    {
                                        new Parameters
                                        {
                                            new()
                                            {
                                                Datatype = Reference(money),
                                                Mutability = new Constant(),
                                                Identifier = Words(correct),
                                                Source = new Token[] { StartValues(), Word(correct), Returns(), Word(money), EndValues() }
                                            }
                                        },
                                        Name(horse, battery),
                                    },
                                    Source = new Token[] { StartValues(), Word(correct), Returns(), Word(money), EndValues(), Word(horse), Word(battery) }
                                },
                                Returns = Reference(number),
                                Definition = new()
                                {
                                    UnresolvedReference(Name(@return), new Inline { Source = new Token[] { Number(12) } })
                                }
                            }
                        }
                    }
                }
            };

            Analyzer analyzer = new();
            analyzer.DefineScope(analyzer.Global, scope);
            Assert.Empty(analyzer.Errors);

            Assert.Single(analyzer.Global.Modules);
            var module = analyzer.Global.Modules.FirstOrDefault().Value;
            Assert.Single(module.Modules);
            module = module.Modules.FirstOrDefault().Value;
            Assert.Single(module.Modules);
            module = module.Modules.FirstOrDefault().Value;
            Assert.Equal(2, module.Contexts.Count);
        }
    }
}