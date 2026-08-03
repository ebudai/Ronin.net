// Copyright © 2026 Eric Budai

using System.Reflection;
using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     The value-admission boundary: what the runtime accepts, and every door
///     that leads to it.
/// </summary>
///
/// <remarks>
///     Eight successive sweeps found eight doors, one at a time — «Var», two
///     writes, body results, evaluator groups, type seeds, declaration input and
///     output, and constants. Each was a public API taking an «object» from a
///     caller and remembering, or not remembering, one call. That is the shape
///     these tests exist to stop: the last one is a census, so the next door has
///     to be answered for rather than noticed.
/// </remarks>
public class Admission
{
    [Fact(DisplayName = "a constant is a value the runtime admitted, not the caller's array")]
    public void AConstantIsAValueTheRuntimeAdmittedNotTheCallersArray()
    {
        // Found by audit, and a constant is the worst place for it. A read
        // creates no edge on purpose, so a derived cell that cached an element
        // has no write and no clock advance that could ever wake it — the direct
        // and cached readings disagree for the life of the program rather than
        // until the next settle.
        var nested = new object[] { 1d };
        var xs = new object[] { nested };

        Graph graph = new();
        graph.Constant("xs", xs);

        var held = Assert.IsType<List>(graph.Read("xs"));

        xs[0] = 99d;
        nested[0] = 99d;

        Assert.Equal(1d, Assert.IsType<List>(Assert.IsType<List>(graph.Read("xs"))[0])[0]);
        Assert.Equal(1d, Assert.IsType<List>(held[0])[0]);
    }

    [Fact(DisplayName = "and a constant list is one «@» will index")]
    public void AndAConstantListIsOneAtWillIndex()
    {
        // The other half of the same defect: indexing accepts only the
        // representation the runtime holds, so a door that skipped admission
        // supplied a value no operator would take.
        Graph graph = new();
        graph.Constant("xs", new object[] { 10d, 20d });

        Assert.Equal(20d, Builtin.Operators["@"].Apply(graph.Read("xs"), 2d));
    }

    [Fact(DisplayName = "and a constant that cannot be admitted stops the program, not the read")]
    public void AndAConstantThatCannotBeAdmittedStopsTheProgramNotTheRead()
    {
        // Admission runs BEFORE the failure is looked for, so a refusal is one
        // of the failures a constant refuses. Stored verbatim, the cyclic array
        // was installed without the check ever seeing it.
        var looping = new object[1];
        looping[0] = looping;

        Graph graph = new();

        Assert.Contains("cannot contain itself",
                        Assert.Throws<InitialisationFailure>(() => graph.Constant("xs", looping)).Message);
    }

    [Fact(DisplayName = "and an element that failed does not stop a constant, because it is a value")]
    public void AndAnElementThatFailedDoesNotStopAConstantBecauseItIsAValue()
    {
        // The refusal is not an error and an error is not a refusal. A constant
        // refuses the first and holds the second, which is what keeps «a
        // constant's initialiser failed» about the initialiser.
        Graph graph = new();
        graph.Constant("xs", new object[] { new Error("gone wrong"), 2d });

        Assert.Equal(2d, Assert.IsType<List>(graph.Read("xs"))[1]);
    }

    [Fact(DisplayName = "a value mentioned twice is admitted once, not copied twice")]
    public void AValueMentionedTwiceIsAdmittedOnceNotCopiedTwice()
    {
        // Found by audit. The recursion-path set answers "is this a cycle" and
        // drops a child as soon as it completes, so a second mention of the same
        // acyclic array rebuilt its whole subtree — a host DAG with one array
        // per level expanded into a tree.
        //
        //     depth  8      35,504 bytes
        //     depth 12     557,744 bytes
        //     depth 16   8,913,584 bytes
        //
        // Two dozen levels reach gigabytes, all of it acyclic and far inside a
        // limit of 256. Reusing the completed child is invisible: a list is a
        // value, identity is not its equality, and nothing can mutate it to make
        // the sharing observable.
        static long Work(int levels)
        {
            object value = new object[] { 1d };

            for (var at = 0; at < levels; ++at) value = new object[] { value, value };

            var before = GC.GetAllocatedBytesForCurrentThread();

            List.Admit(value);

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        Work(4);

        var shallow = Work(8);
        var deep = Work(16);

        // Twice the input, under twice the work. Exponential is 256 times.
        Assert.True(deep < shallow * 2, $"8 levels allocated {shallow} bytes and 16 allocated {deep}");
    }

    [Fact(DisplayName = "and two shared values are compared once per pair, not once per path")]
    public void AndTwoSharedValuesAreComparedOncePerPairNotOncePerPath()
    {
        // Keeping the sharing at admission moves the same exponential into the
        // comparison the moment two INDEPENDENTLY admitted values meet: no
        // reference is shared between them, so every equal subtree would be
        // re-proved once per path that reaches it.
        //
        // A counted leaf and not a stopwatch. The count is exact, machine-
        // independent, and it is the thing that grows — 2^levels against
        // levels.
        static object Shared(int levels)
        {
            object value = new object[] { new Counted() };

            for (var at = 0; at < levels; ++at) value = new object[] { value, value };

            return List.Admit(value);
        }

        var left = Shared(20);
        var right = Shared(20);

        Counted.Comparisons = 0;

        Assert.True(Builtin.Same(left, right));

        // One per shared pair. Without it, 2^20 — a million — and the walk that
        // produces them is what makes it slow rather than the count itself.
        Assert.True(Counted.Comparisons <= 21,
                    $"comparing two 20-level shared values took {Counted.Comparisons} leaf comparisons");
    }

    [Theory(DisplayName = "and nothing admitted is deeper than the limit, whatever the leaf is")]
    [InlineData("scalar")]
    [InlineData("empty")]
    [InlineData("list")]
    public void AndNothingAdmittedIsDeeperThanTheLimitWhateverTheLeafIs(string leaf)
    {
        // Found by audit: «[]» returned before the depth was checked, so a nest
        // of exactly «Deep» wrappers around an empty list was admitted at depth
        // 257 — past the limit that is supposed to define what the runtime
        // accepts. A limit with a way around it is not one.
        object Leaf() => leaf switch
        {
            "scalar" => 1d,
            "empty" => Array.Empty<object>(),
            _ => List.Admit(new object[] { 1d }),
        };

        object Wrapped(int levels)
        {
            var built = Leaf();

            for (var at = 0; at < levels; ++at) built = new object[] { built };

            return List.Admit(built);
        }

        // The invariant is about the VALUE and not about the wrappers: nothing
        // admitted has a depth past the limit. How many wrappers reach it
        // differs, because a scalar is not a level and a list is — which is
        // exactly the distinction the empty case had lost.
        var deepest = leaf is "scalar" ? List.Deep : List.Deep - 1;

        Assert.Equal(List.Deep, Assert.IsType<List>(Wrapped(deepest)).Depth);
        Assert.Contains("deeper", Assert.IsType<Error>(Wrapped(deepest + 1)).Message);
    }

    [Theory(DisplayName = "and an argument the runtime refuses says so, rather than being called the wrong shape")]
    [InlineData("cyclic", "cannot contain itself")]
    [InlineData("deep", "deeper")]
    public void AndAnArgumentTheRuntimeRefusesSaysSoRatherThanBeingCalledTheWrongShape(string kind, string says)
    {
        // Found by audit. A refusal is not an «IReadOnlyList», so a block
        // binding two names asked about the shape first and reported "given a
        // single argument" — the real failure lost, and the message
        // recommending a repair for a mistake nobody made.
        object Refused()
        {
            if (kind is "cyclic")
            {
                var looping = new object[1];
                looping[0] = looping;

                return looping;
            }

            object built = 1d;

            for (var at = 0; at <= List.Deep; ++at) built = new object[] { built };

            return built;
        }

        Scope scope = new();
        scope.Declare(new Declaration(Pattern.Parse("use _"), [["a", "b"]], (_, _) => 1d));

        var answer = scope.Invoke(new Graph(), Pattern.Parse("use _"), [Refused()], insideLet: true);

        Assert.Contains(says, Assert.IsType<Error>(answer).Message);
    }

    [Theory(DisplayName = "and a group that is simply the wrong size still says that")]
    [InlineData(2, null)]
    [InlineData(3, "was given 3")]
    [InlineData(1, "was given a single argument")]
    public void AndAGroupThatIsSimplyTheWrongSizeStillSaysThat(int given, string says)
    {
        // The other side of the same edge: propagating a refusal first must not
        // swallow the arity diagnostics, which are the right answer when the
        // argument was admitted and is merely the wrong shape.
        object argument = given is 1 ? 1d : Enumerable.Range(1, given).Select(at => (object)(double)at).ToArray();

        Scope scope = new();
        scope.Declare(new Declaration(Pattern.Parse("use _"), [["a", "b"]], (_, bound) => bound["a"]));

        var answer = scope.Invoke(new Graph(), Pattern.Parse("use _"), [argument], insideLet: true);

        if (says is null) Assert.Equal(1d, answer);
        else Assert.Contains(says, Assert.IsType<Error>(answer).Message);
    }

    [Fact(DisplayName = "and an element of a group that failed still keeps the body from running")]
    public void AndAnElementOfAGroupThatFailedStillKeepsTheBodyFromRunning()
    {
        // The per-argument refusal above catches the whole argument. An element
        // INSIDE an admitted group is a different failure — the group is a
        // perfectly good list of the right size, and one of its values is an
        // error — and "bodies never run on error inputs" has to hold for it too.
        var ran = 0;

        Scope scope = new();
        scope.Declare(new Declaration(Pattern.Parse("use _"), [["a", "b"]], (_, _) => ++ran));

        var answer = scope.Invoke(new Graph(), Pattern.Parse("use _"),
                                  [new object[] { new Error("gone wrong"), 2d }], insideLet: true);

        Assert.Equal("gone wrong", Assert.IsType<Error>(answer).Message);
        Assert.Equal(0, ran);
    }

    [Fact(DisplayName = "every public API that carries a caller's value is one of the known doors")]
    public void EveryPublicApiThatCarriesACallersValueIsOneOfTheKnownDoors()
    {
        // A CENSUS, and it is the only part of this that guards the next defect
        // rather than the last eight. Admission is one named call, and a call is
        // something an API can forget — so the set of APIs that have to make it
        // is enumerated here, and a new one fails this until somebody answers
        // for it with a regression above.
        //
        // Reflective for the same reason the diagnostic walk is: a hand-kept
        // list of doors is the thing that was already wrong eight times.
        static bool Carries(Type type)
            => typeof(Delegate).IsAssignableFrom(type) is false
            && (type == typeof(object)
                || type.IsArray && Carries(type.GetElementType())
                || type.IsGenericType && type.GetGenericArguments().Any(Carries));

        var doors =
            from type in new[] { typeof(Graph), typeof(Scope) }
            from method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            where method.GetParameters().Any(parameter => Carries(parameter.ParameterType))
            orderby type.Name, method.Name, method.GetParameters().Length
            select $"{type.Name}.{method.Name}/{method.GetParameters().Length}";

        Assert.Equal(
            [
                "Graph.Constant/2",     // AConstantIsAValueTheRuntimeAdmittedNotTheCallersArray
                "Graph.Type/2",         // Instances.AndAMemberSeedIsAValueTheRuntimeAdmitted…
                "Graph.Var/2",          // Indexing.ACycleIsRefusedWhereTheValueStillHasACaller
                "Graph.Write/2",        // Indexing.AListThatRecomputesToTheSameListWakesNobody
                "Graph.Write/3",        // Instances tests write members
                "Scope.Invoke/4",       // Reactions.AndAnArgumentIsAdmittedOnTheWayIn…
            ],
            doors);
    }

    /// <summary>A leaf that says how often it was asked.</summary>
    private sealed class Counted
    {
        public static int Comparisons { get; set; }

        public override bool Equals(object obj)
        {
            ++Comparisons;

            return obj is Counted;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public override int GetHashCode() => 0;
    }
}
