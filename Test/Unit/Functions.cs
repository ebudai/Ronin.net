using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Datatype = Ronin.Grammar.Type;
using Function = Ronin.Grammar.Function;
using Literal = Ronin.Grammar.Literal;
using Type = Ronin.Grammar.Type;

namespace Unit;

[Trait(nameof(Parser), null)]
public class Functions : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // function test(x => number) { return 7; }

        List<Token> tokens = new()
        {
            Keyword.Function(),
            Word("test"),
            StartValues(),
            Word("x"),
            Arrow(),
            Word("number"),
            EndValues(),
            StartScope(),
            Word("return"),
            Number(7),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var function = Function.Parse(ref parser);

        Assert.Equal(2, function?.Identifier?.Count);
        
        var parameters = function.Identifier[1].AsParameters;

        Assert.Single(parameters);
        var parameter = parameters[0].AsDatum;
        Assert.Single(parameter?.Identifier);

        var unresolved = parameter.Type as Type.Unresolved;
        Assert.Single(unresolved.Reference);
        var type = unresolved.Reference.Span[0].AsName;
        Assert.Single(type?.Tokens.ToArray());
        
        Assert.Single(function.Definition.Statements);
        var member = function.Definition.Statements[0] as Member.Unresolved;
        Assert.Equal(2, member?.Reference.Span.Length);

        var @return = member.Reference.Span[0].AsName;
        Assert.Single(@return?.Tokens.ToArray());

        var scalar = member.Reference.Span[1].AsTemporary as Literal;
        Assert.Single(scalar?.Tokens.ToArray());
    }

    [Fact(DisplayName = "specifies return datatype")]
    public void ReturnsSymbol()
    {
        // function test(x => text) => number { return x as number; }

        List<Token> tokens = new()
        {
            Keyword.Function(),
            Word("test"),
            StartValues(),
            Word("x"),
            Arrow(),
            Word("text"),
            EndValues(),
            Arrow(),
            Word("number"),
            StartScope(),
            Word("return"),
            Word("x"),
            Word("as"),
            Word("number"),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var function = Function.Parse(ref parser);

        Assert.Equal(2, function?.Identifier?.Count);

        Assert.Single(function.Identifier[0].AsName.Tokens.ToArray());

        var parameters = function.Identifier[1].AsParameters;
        Assert.Single(parameters);
        var parameter = parameters[0].AsDatum;
        Assert.Single(parameter.Identifier);

        {
            var unresolved = parameter.Type as Type.Unresolved;
            Assert.Single(unresolved.Reference);
            var type = unresolved.Reference.Span[0].AsName;
            Assert.Single(type?.Tokens.ToArray());
        }

        {
            var unresolved = function.Returns as Type.Unresolved;
            Assert.Single(unresolved.Reference);
            var returns = unresolved.Reference.Span[0].AsName;
            Assert.Single(returns?.Tokens.ToArray());
        }

        Assert.Single(function.Definition.Statements);
        var member = function.Definition.Statements[0] as Member.Unresolved;
        Assert.Single(member?.Reference);
    }

    [Fact(DisplayName = "default parameter")]
    public void DefaultParameter()
    {
        // function test(x = "3") => number { return x as number; }

        List<Token> tokens = new()
        {
            Keyword.Function(),
            Word("test"),
            StartValues(),
            Word("x"),
            Assign(),
            Text("3"),
            EndValues(),
            Arrow(),
            Word("number"),
            StartScope(),
            Word("return"),
            Word("x"),
            Word("as"),
            Word("number"),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var function = Function.Parse(ref parser);

        Assert.Equal(2, function?.Identifier?.Count());
    }

    /*[Trait(nameof(Analyzer), nameof(Declaration))]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "basic")]
        public void Basic()
        {
            const string run = nameof(run);
            const string home = nameof(home);
            const string whole = nameof(whole);
            const string number = nameof(number);
            const string @return = nameof(@return);
            const string cash = nameof(cash);
            const string money = nameof(money);

            // function run home (cash => money) => whole number { return 72; }

            AnonymousScope scope = new()
            {
                Definition = new()
                {
                    new Function.Declaration
                    {
                        Identifier = new()
                        {
                            Components = new List<Identifier.Component>
                            {
                                Name(run, home),
                                new Parameters
                                {
                                    new()
                                    {
                                        Datatype = Reference(money),
                                        Mutability = new Constant(),
                                        Identifier = Words(cash),
                                        Source = new Token[] { Word(cash), Arrow(), Word(money) }
                                    },
                                    //Source = new Token[] { StartValues(), Word(cash), Arrow(), Word(money), EndValues() }
                                }
                            }
                        },
                        Returns = Reference(whole, number),
                        Definition = new()
                        {
                            UnresolvedReference(@return),
                            new Inline { Source = new[] { Number(72) } }
                        }
                    }
                }
            };

            Analyzer analyzer = new();
            analyzer.DefineScope(analyzer.Global, scope);
            Assert.Empty(analyzer.Errors);

            Assert.Single(analyzer.Global.Contexts);
            var context = analyzer.Global.Contexts.FirstOrDefault()?.Children.FirstOrDefault().Value;
            Assert.Single(context?.Members);

            var entry = context.Members.First();
            var identifier = entry.Key;
            var function = entry.Value as Function;

            Parameters parameters = identifier;
            Assert.Single(parameters);
            Assert.Single(parameters[0].Identifier.Components);
            Name name = parameters[0].Identifier.Components[0];
            Assert.Single(name?.Source.ToArray());
            Assert.Equal(cash, name.Source.Span[0].Memory.ToString());
            Assert.Single(parameters[0].Datatype.Components);
            name = parameters[0].Datatype.Components[0];
            Assert.Single(name?.Source.ToArray());
            Assert.Equal(money, name.Source.Span[0].Memory.ToString());
            Assert.Empty(function.Modifiers.Source.ToArray());

            Assert.IsType<Datatype.Unresolved>(function.Returns);
        }
    }

    [Trait(nameof(Analyzer), nameof(Resolution))]
    public class Resolution : AnalysisTests
    {
        [Fact(DisplayName = "overloaded")]
        public void Overloaded()
        {
            const string test = nameof(test);
            const string x = nameof(x);

            // function test(x => number) { }
            // function test(x => money) { }
            // test 3;

            Analyzer analyzer = new();

            {
                Parameters param = new();
                Identifier id = Identifier(x);
                param.Data.Add(id, new Datum { Datatype = new() });
                analyzer.Global.Add(Identifier(Name(test), param), new Function());
            }

            {
                Parameters param = new();
                Identifier id = Identifier(x);
                param.Data.Add(id, new Datum { Datatype = new() });
                analyzer.Global.Add(Identifier(Name(test), param), new Function());
            }

            Reference.Component parameter = new() { value = new Inline { Source = new[] { Number(3) } }, Source = new[] { Number(3) } };
            var reference = Reference(Name(test), parameter);

            Context.Member.Unresolved unresolved = new() { Reference = reference };
            analyzer.Global.Add(unresolved);

            analyzer.Resolve();

            Assert.Single(analyzer.Global.Contexts);
            var context = analyzer.Global.Contexts[0];
            Assert.Equal(2, context.Members.Count);
            Assert.Single(analyzer.Global);
            var overloaded = analyzer.Global.First();// as Function.Overloaded;
            //Assert.Equal(2, overloaded?.Overloads.Length);
        }
    }*/
}
