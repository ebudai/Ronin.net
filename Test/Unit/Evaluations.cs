// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

// Grammar is not imported wholesale: Scope means one thing to the parser and
// another to the runtime, and this file wants the runtime's.
using Member = Ronin.Grammar.Member;
using Reference = Ronin.Grammar.Reference;
using Tree = Ronin.Compiler.Node;

namespace Unit;

/// <summary>
///     Source to value: lex, resolve, walk the tree, read the graph.
/// </summary>
[Trait(nameof(Evaluator), null)]
public class Evaluations
{
    private static Tree Resolve(SymbolTable symbols, string source)
    {
        Assert.True(new Resolver(symbols).Resolve(Lexemes.Lex(source)).TryTree(out var tree), source);
        return tree;
    }

    [Fact(DisplayName = "a parsed statement resolves and runs")]
    public void AParsedStatementResolvesAndRuns()
    {
        // The whole frontend, continuous: characters to a value, with the parser
        // deciding where the expression is and the resolver deciding what it
        // means. Nothing here hands the resolver a string.
        const string source = "compute total for base price + tax;";

        Lexer lexer = new(source);
        Parser parser = new(lexer.Lex());
        var module = parser.Parse();

        var statement = Assert.IsType<Member.Unresolved>(Assert.Single(module.Scopes[0].Statements));

        // the parser committed to no shape — «compute total for base price» is
        // still one greedy run of words at this point
        Assert.Equal(3, statement.Reference.Span.Length);

        SymbolTable symbols = new();
        symbols.WithNames("base price", "tax").WithPatterns("compute total for _");

        var resolution = new Resolver(symbols).Resolve(statement.Reference.ToLexemes());
        Assert.Equal("compute total for («base price» + «tax»)", resolution.Reading);
        Assert.True(resolution.TryTree(out var tree));

        Scope scope = new();
        scope.Declare(new Declaration(
            Pattern.Parse("compute total for _"),
            [["amount"]],
            (_, bound) => Builtin.Operators["*"].Apply(bound["amount"], 2d)));

        Graph graph = new();
        graph.Var("base price", 100d);
        graph.Var("tax", 20d);

        Assert.Equal(240d, new Evaluator(scope).Evaluate(graph, tree, insideLet: false));
    }

    [Fact(DisplayName = "a reference span stops at its punctuation")]
    public void AReferenceSpanStopsAtItsPunctuation()
    {
        // the terminator bounds the span and is not part of it, which is what
        // lets the parser hand over a run the resolver can score whole
        Lexer lexer = new("x > 3; y");
        Parser parser = new(lexer.Lex());

        var reference = Reference.Parse(ref parser);

        Assert.Equal(["x", ">", "3"], reference.ToLexemes().Select(lexeme => lexeme.Text));
    }

    [Fact(DisplayName = "a resolved statement evaluates against the graph")]
    public void AResolvedStatementEvaluatesAgainstTheGraph()
    {
        // the whole pipeline: characters in, a number out
        SymbolTable symbols = new();
        symbols.WithNames("base price", "tax");

        Graph graph = new();
        graph.Var("base price", 100d);
        graph.Var("tax", 20d);

        Evaluator evaluator = new(new Scope());

        Assert.Equal(120d, evaluator.Evaluate(graph, Resolve(symbols, "base price + tax"), insideLet: false));
    }

    [Fact(DisplayName = "a declared name is not read from the graph")]
    public void ADeclaredNameIsNotReadFromTheGraph()
    {
        // The end of the argument that started in the resolver. It works out
        // that «bank» is being DECLARED — which is what lets the loop resolve
        // against a scope without it — and used to hand back a node meaning "a
        // name in scope, one lookup". Evaluating that read the name the loop was
        // about to introduce and reported it undeclared.
        //
        // The graph here holds «banks» and not «bank», exactly as the enclosing
        // scope would, so a read is observable rather than merely wrong.
        SymbolTable symbols = new();
        symbols.WithNames("banks");

        foreach (var builtin in SymbolTable.Builtins) symbols.Patterns.Add((builtin, SymbolKind.Value));

        Graph graph = new();
        graph.Var("banks", 3d);

        var arguments = Assert.IsType<Tree.Call>(Resolve(symbols, "for each bank in banks")).Arguments;

        // the argument ALONE, which is the case with no declaring call around it
        // — inside one it is handed over as a name rather than evaluated, which
        // is the test above
        var evaluated = new Evaluator(new Scope()).Evaluate(graph, arguments[0], insideLet: false);

        // whatever the loop's runtime turns out to be, it is not a read of a
        // name nothing has bound — which is what «no declaration for «bank»»
        // would have been
        Assert.Contains("«bank» is being declared here", evaluated.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("no declaration", evaluated.ToString(), StringComparison.Ordinal);

        // and the collection beside it is an ordinary read
        Assert.Equal(3d, new Evaluator(new Scope()).Evaluate(graph, arguments[1], insideLet: false));
    }

    [Fact(DisplayName = "a declaring call receives the name, and runs")]
    public void ADeclaringCallReceivesTheNameAndRuns()
    {
        // The other half. Preserving the binding in the tree was not enough,
        // because the only generic call boundary evaluated every argument — so
        // the binding arrived as the error saying nothing had given it a value,
        // and a body never runs on an error input. The construct that DECLARES
        // the name never got to say what it introduces.
        SymbolTable symbols = new();
        symbols.WithNames("banks");

        foreach (var builtin in SymbolTable.Builtins) symbols.Patterns.Add((builtin, SymbolKind.Value));

        Graph graph = new();
        graph.Var("banks", 3d);

        List<object> received = [];

        Scope scope = new();
        scope.Declare(new Declaration(SymbolTable.Builtins[0],
                                      [["variable"], ["collection"]],
                                      (_, bound) =>
                                      {
                                          received.Add(bound["variable"]);
                                          received.Add(bound["collection"]);
                                          return "the body ran";
                                      }));

        Assert.Equal("the body ran",
                     new Evaluator(scope).Evaluate(graph, Resolve(symbols, "for each bank in banks"),
                                                   insideLet: false));

        // the name unevaluated, and the collection beside it read as usual
        Assert.Equal("bank", Assert.IsType<Evaluator.Binding>(received[0]).Name);
        Assert.Equal(3d, received[1]);
    }

    [Fact(DisplayName = "a declaration may not name a parameter twice")]
    public void ADeclarationMayNotNameAParameterTwice()
    {
        // The last line of defence for the source rule. Binding writes parameter
        // names into a dictionary, so a repeat is not an error there — the
        // second value silently replaces the first, and the body reads one
        // argument where two were passed.
        Pattern shape = new(["compare", null]);

        Assert.Throws<ArgumentException>(() => new Declaration(shape, [["a", "a"]], (_, _) => null));

        // A hole that binds nothing passes a duplicate check vacuously, which is
        // how «ping (_)» survived: a pattern with a hole and a block no ordinary
        // argument can fill.
        Assert.Throws<ArgumentException>(() => new Declaration(shape, [[]], (_, _) => null));
        Assert.Throws<ArgumentException>(() => new Declaration(shape, [null], (_, _) => null));
        Assert.Throws<ArgumentException>(() => new Declaration(shape, [[null]], (_, _) => null));
        Assert.Throws<ArgumentException>(() => new Declaration(shape, [[" "]], (_, _) => null));
    }

    [Fact(DisplayName = "precedence survives into evaluation")]
    public void PrecedenceSurvivesIntoEvaluation()
    {
        SymbolTable symbols = new();
        symbols.WithNames("a", "b", "c");

        Graph graph = new();
        graph.Var("a", 2d);
        graph.Var("b", 3d);
        graph.Var("c", 4d);

        Evaluator evaluator = new(new Scope());

        Assert.Equal(14d, evaluator.Evaluate(graph, Resolve(symbols, "a + b * c"), insideLet: false));
        Assert.Equal(20d, evaluator.Evaluate(graph, Resolve(symbols, "(a + b) * c"), insideLet: false));
        Assert.Equal(1d, evaluator.Evaluate(graph, Resolve(symbols, "c - b + a - a"), insideLet: false));

        // left associative, so this is (c / a) / a and not c / (a / a)
        Assert.Equal(1d, evaluator.Evaluate(graph, Resolve(symbols, "c / a / a"), insideLet: false));
    }

    [Fact(DisplayName = "dividing by zero is an error, not an infinity")]
    public void DividingByZeroIsAnErrorNotAnInfinity()
    {
        SymbolTable symbols = new();
        symbols.WithNames("a", "nothing at all");

        Graph graph = new();
        graph.Var("a", 2d);
        graph.Var("nothing at all", 0d);

        Evaluator evaluator = new(new Scope());

        var error = Assert.IsType<Error>(
            evaluator.Evaluate(graph, Resolve(symbols, "a / nothing at all"), insideLet: false));

        Assert.Contains("cannot divide by zero", error.Message);

        // the message proposes «otherwise», so the edit it proposes has to work
        Assert.Equal(0d, Builtin.Otherwise(error, 0d));
    }

    [Fact(DisplayName = "a resolved tree can be a let body")]
    public void AResolvedTreeCanBeALetBody()
    {
        // the payoff: the resolver decides the meaning once, and the graph
        // re-runs it whenever a source it happened to read changes
        SymbolTable symbols = new();
        symbols.WithNames("base price", "tax rate");

        Evaluator evaluator = new(new Scope());

        Graph graph = new();
        graph.Var("base price", 100d);
        graph.Var("tax rate", 0.2d);
        graph.Let("total", evaluator.Body(Resolve(symbols, "base price + base price * tax rate")));

        Assert.Equal(120d, graph.Read("total"));

        graph.Write("base price", 200d);
        graph.Step();
        Assert.Equal(240d, graph.Read("total"));

        // and the edges came from the walk, not from anyone declaring them
        Assert.Equal(["base price", "tax rate"], graph.Dependencies("total").Order());
    }

    [Fact(DisplayName = "a resolved call invokes its declaration")]
    public void AResolvedCallInvokesItsDeclaration()
    {
        SymbolTable symbols = new();
        symbols.WithNames("order").WithPatterns("compute total for _");

        Scope scope = new();
        scope.Declare(new Declaration(
            Pattern.Parse("compute total for _"),
            [["amount"]],
            (_, bound) => Builtin.Operators["*"].Apply(bound["amount"], 2d)));

        Graph graph = new();
        graph.Var("order", 21d);

        Evaluator evaluator = new(scope);

        Assert.Equal(42d, evaluator.Evaluate(graph, Resolve(symbols, "compute total for order"), insideLet: false));

        // the trailing argument absorbs the operator, so the call sees 25
        Assert.Equal(50d, evaluator.Evaluate(graph, Resolve(symbols, "compute total for order + 4"), insideLet: false));
    }

    [Fact(DisplayName = "a group of two binds a parameter block of two")]
    public void AGroupOfTwoBindsAParameterBlockOfTwo()
    {
        // the whole reason a hole is a block and not a parameter: the resolver
        // hands over one argument per hole and never learns about arity
        SymbolTable symbols = new();
        symbols.WithNames("circle", "here").WithPatterns("draw _ at _");

        Scope scope = new();
        scope.Declare(new Declaration(
            Pattern.Parse("draw _ at _"),
            [["shape"], ["x", "y"]],
            (_, bound) => $"{bound["shape"]}@{bound["x"]},{bound["y"]}"));

        Graph graph = new();
        graph.Var("circle", "circle");
        graph.Var("here", 9d);

        Evaluator evaluator = new(scope);

        Assert.Equal("circle@3,4",
                     evaluator.Evaluate(graph, Resolve(symbols, "draw circle at (3, 4)"), insideLet: false));

        // the parts are expressions, not literals
        Assert.Equal("circle@10,9",
                     evaluator.Evaluate(graph, Resolve(symbols, "draw circle at (here + 1, here)"), insideLet: false));

        // brackets may be dropped for one parameter and never for two
        var unbracketed = Assert.IsType<Error>(
            evaluator.Evaluate(graph, Resolve(symbols, "draw circle at here"), insideLet: false));
        Assert.Contains("a single argument", unbracketed.Message);

        var wrong = Assert.IsType<Error>(
            evaluator.Evaluate(graph, Resolve(symbols, "draw circle at (here, here, here)"), insideLet: false));
        Assert.Contains("was given 3", wrong.Message);
    }

    [Fact(DisplayName = "an effectful call is refused inside a let body")]
    public void AnEffectfulCallIsRefusedInsideALetBody()
    {
        SymbolTable symbols = new();
        symbols.WithNames("order").WithPatterns("save _");

        Scope scope = new();
        scope.Declare(new Declaration(
            Pattern.Parse("save _"),
            [["data"]],
            (_, bound) => $"wrote {bound["data"]}",
            pure: false));

        Graph graph = new();
        graph.Var("order", 21d);

        Evaluator evaluator = new(scope);
        var tree = Resolve(symbols, "save order");

        Assert.Equal("wrote 21", evaluator.Evaluate(graph, tree, insideLet: false));

        // purity travels down the walk, so it holds wherever the call sits
        graph.Let("bad", evaluator.Body(tree));
        var error = Assert.IsType<Error>(graph.Read("bad"));
        Assert.Contains("cannot appear in a let body", error.Message);
    }

    [Fact(DisplayName = "literals evaluate, and unreadable ones say so")]
    public void LiteralsEvaluateAndUnreadableOnesSaySo()
    {
        SymbolTable symbols = new();
        symbols.WithPatterns("print _");

        Scope scope = new();
        scope.Declare(new Declaration(Pattern.Parse("print _"), [["it"]], (_, bound) => bound["it"]));

        Graph graph = new();
        Evaluator evaluator = new(scope);

        Assert.Equal(42d, evaluator.Evaluate(graph, Resolve(symbols, "print 42"), insideLet: false));
        Assert.Equal(7000876d, evaluator.Evaluate(graph, Resolve(symbols, "print 7,000,876"), insideLet: false));
        Assert.Equal("stuff", evaluator.Evaluate(graph, Resolve(symbols, "print \"stuff\""), insideLet: false));

        // a date lexes and resolves but has no runtime value yet
        var unread = Assert.IsType<Error>(evaluator.Evaluate(graph, Resolve(symbols, "print 2023-11-16"), insideLet: false));
        Assert.Contains("does not read yet", unread.Message);

        // «١» (U+0661) is a char.IsDigit, so the lexer calls it a Numeric — but invariant
        // «double» reads only «0-9», so evaluating it is a number this interpreter cannot take
        // yet, an Error, rather than the exception a throwing parse would escape as (REAUDIT64
        // finding 4).
        var unreadable = Assert.IsType<Error>(evaluator.Evaluate(graph, Resolve(symbols, "print ١"), insideLet: false));
        Assert.Contains("cannot read yet", unreadable.Message);
    }

    [Fact(DisplayName = "an undeclared name is an error value")]
    public void AnUndeclaredNameIsAnErrorValue()
    {
        SymbolTable symbols = new();
        symbols.WithNames("missing");

        Evaluator evaluator = new(new Scope());

        var error = Assert.IsType<Error>(
            evaluator.Evaluate(new Graph(), Resolve(symbols, "missing"), insideLet: false));
        Assert.Contains("is not declared", error.Message);
    }

    [Fact(DisplayName = "arithmetic needs numbers")]
    public void ArithmeticNeedsNumbers()
    {
        SymbolTable symbols = new();
        symbols.WithNames("label", "count");

        Graph graph = new();
        graph.Var("label", "words");
        graph.Var("count", 1d);

        Evaluator evaluator = new(new Scope());

        var error = Assert.IsType<Error>(
            evaluator.Evaluate(graph, Resolve(symbols, "label + count"), insideLet: false));
        Assert.Contains("needs two numbers", error.Message);

        // division says the same thing, though it checks for zero first
        var divided = Assert.IsType<Error>(
            evaluator.Evaluate(graph, Resolve(symbols, "label / count"), insideLet: false));
        Assert.Contains("needs two numbers", divided.Message);
    }

    [Fact(DisplayName = "every operator the resolver knows has an implementation")]
    public void EveryOperatorTheResolverKnowsHasAnImplementation()
    {
        // two tables, one set of symbols: the resolver gives them binding power
        // and the runtime gives them meaning. Drift and a statement resolves to
        // something nothing can run.
        // One table, so drift is not a thing that can happen rather than a
        // thing a test notices afterwards. The old assertion compared two key
        // sets, which would have caught a symbol added to one side and never a
        // precedence or a meaning changed on the other — the drifts that would
        // actually mislead. A scope may still ADD an operator, and what it may
        // not do is add one without saying what it means.
        SymbolTable symbols = new();

        Assert.Equal(new SymbolTable().Operators.Keys.Order(), Builtin.Operators.Keys.Order());

        foreach (var (symbol, op) in Builtin.Operators)
        {
            Assert.Same(op, symbols.Operators[symbol]);
        }
    }

    [Fact(DisplayName = "evaluation uses the operator resolution chose")]
    public void EvaluationUsesTheOperatorResolutionChose()
    {
        // The two halves used to be looked up in different registries: the
        // resolver read the scope's table, the evaluator read the global one. So
        // an operator a scope added resolved and then had "no implementation",
        // and an implementation a scope replaced was ignored in favour of the
        // built-in — both silent, and both invisible to a test that compared the
        // two tables' initial contents.
        SymbolTable symbols = new();
        symbols.WithNames("a", "b");
        symbols.Operators["^"] = new Operator(25, Builtin.Lift((left, right) => Math.Pow((double)left, (double)right)));
        symbols.Operators["+"] = new Operator(10, (_, _) => 999d);

        Graph graph = new();
        graph.Var("a", 2d);
        graph.Var("b", 3d);

        Assert.Equal(8d, Evaluated(symbols, graph, "a ^ b"));

        // and the scope's «+» wins, because it is the one that resolved
        Assert.Equal(999d, Evaluated(symbols, graph, "a + b"));

        // while an untouched scope still means what the language means
        Assert.Equal(5d, Evaluated(new SymbolTable().WithNames("a", "b"), graph, "a + b"));
    }

    private static object Evaluated(SymbolTable symbols, Graph graph, string source)
    {
        Assert.True(new Resolver(symbols).Resolve(source).TryTree(out var tree), source);

        return new Evaluator(new Scope()).Evaluate(graph, tree, insideLet: false);
    }

    [Fact(DisplayName = "the evaluator rejects nonsense")]
    public void TheEvaluatorRejectsNonsense()
    {
        Evaluator evaluator = new(new Scope());

        Assert.Throws<ArgumentNullException>(() => evaluator.Evaluate(null, new Tree.Literal("1"), insideLet: false));
        Assert.Throws<ArgumentNullException>(() => evaluator.Evaluate(new Graph(), null, insideLet: false));
        Assert.Throws<ArgumentNullException>(() => evaluator.Body(null));
    }
}
