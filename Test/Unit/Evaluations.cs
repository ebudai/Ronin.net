// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;
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
        Assert.Equal(["base price", "tax rate"], graph["total"].Dependencies.Order());
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
            (_, bound) => Builtin.Operators["*"](bound["amount"], 2d)));

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
    }

    [Fact(DisplayName = "every operator the resolver knows has an implementation")]
    public void EveryOperatorTheResolverKnowsHasAnImplementation()
    {
        // two tables, one set of symbols: the resolver gives them binding power
        // and the runtime gives them meaning. Drift and a statement resolves to
        // something nothing can run.
        Assert.Equal(new SymbolTable().Operators.Keys.Order(), Builtin.Operators.Keys.Order());
    }

    [Fact(DisplayName = "an operator with no implementation is an error")]
    public void AnOperatorWithNoImplementationIsAnError()
    {
        // unreachable through the resolver while the tables agree, and the guard
        // is what makes the drift survivable rather than a crash
        Tree.Operation operation = new(new Tree.Literal("1"), "%", new Tree.Literal("2"));

        var error = Assert.IsType<Error>(new Evaluator(new Scope()).Evaluate(new Graph(), operation, insideLet: false));
        Assert.Contains("no implementation", error.Message);
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
