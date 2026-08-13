// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using Test;
using Finder = Ronin.Compiler.Sources;

namespace Unit;

/// <summary>
///     The runtime's boundary in both directions: what it accepts, and what it
///     hands back.
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

    [Theory(DisplayName = "and admitting a value that is not an array costs nothing")]
    [InlineData("scalar")]
    [InlineData("text")]
    [InlineData("error")]
    [InlineData("list")]
    public void AndAdmittingAValueThatIsNotAnArrayCostsNothing(string kind)
    {
        // Found by audit. Making the call universal is the right shape — it is
        // what stopped each API having to know whether it might be handed a list
        // — but it put a set and a dictionary in front of every scalar
        // recompute, write and declaration crossing, for machinery none of them
        // reach. 144 bytes each, on the path cutoff runs every settle.
        //
        // PRE-BOXED, so the measurement is admission and not boxing.
        object value = kind switch
        {
            "scalar" => 1d,
            "text" => "hello",
            "error" => new Error("gone wrong"),
            _ => List.Admit(new object[] { 1d }),
        };

        List.Admit(value);

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var at = 0; at < 1_000; ++at) List.Admit(value);

        // Zero, not "less than". There is nothing for this to allocate.
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
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

    [Fact(DisplayName = "and nothing the graph hands back can be written through")]
    public void AndNothingTheGraphHandsBackCanBeWrittenThrough()
    {
        // Found by audit, and the census above is what missed it: it asked about
        // PARAMETERS, so it reported every door closed while «Var», «Let»,
        // «When», «Shadow» and the indexer handed back the live node. Its value
        // setter changed a source without advancing the clock or dirtying a
        // reader — «x» became 2 while a cell that read it stayed 1, with nothing
        // able to repair it — and it installed raw arrays past the admission
        // boundary. Its clocks and its edge sets were writable too.
        //
        // There is no regression for those four mutations because there is no
        // longer a way to spell them: «Node» is nested and private, so the probe
        // that found this does not compile. That is the strongest form the
        // assertion can take, and this test is what keeps it that way.
        static IEnumerable<Type> Ours(Type type)
        {
            if (type.IsGenericType)
            {
                foreach (var inner in type.GetGenericArguments().SelectMany(Ours)) yield return inner;
            }

            if (type.IsArray)
            {
                foreach (var inner in Ours(type.GetElementType())) yield return inner;
            }

            if (type.Assembly == typeof(Graph).Assembly && type.IsEnum is false) yield return type;
        }

        static bool Constructing(PropertyInfo property)
            => property.SetMethod
                       .ReturnParameter
                       .GetRequiredCustomModifiers()
                       .Any(modifier => modifier == typeof(System.Runtime.CompilerServices.IsExternalInit));

        static bool Mutable(Type type)
            => type.IsArray
            || type.IsGenericType
            && type.GetGenericTypeDefinition() is var kind
            && (kind == typeof(HashSet<>) || kind == typeof(List<>) || kind == typeof(Dictionary<,>));

        var returned =
            (from owner in new[] { typeof(Graph), typeof(Scope) }
             from method in owner.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
             select (Owner: owner, Method: method)).ToArray();

        // The RETURN TYPE itself, and not only the types it is made of. Handing
        // back the node's «HashSet» directly is a mutable collection from the
        // framework rather than from here, so a check that only descends into
        // our own types walks straight past it — which is half of what was
        // wrong: «Dependencies» and «Dependents» were writable sets a caller
        // could simply empty.
        var handed =
            from door in returned
            where Mutable(door.Method.ReturnType)
            select $"{door.Owner.Name}.{door.Method.Name}";

        Assert.Empty(handed);

        var writable =
            from type in returned.SelectMany(door => Ours(door.Method.ReturnType)).Distinct()
            from member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            // «init» is not a setter for this purpose: it runs during
            // construction and in a «with», and cannot reach a value somebody
            // already holds. «Instance» is a readonly record struct and would
            // otherwise be reported for saying what it is made of.
            let settable = member is PropertyInfo property && property.CanWrite && Constructing(property) is false
            let assignable = member is FieldInfo field && field.IsInitOnly is false
            let holds = member is PropertyInfo held && Mutable(held.PropertyType)
                     || member is FieldInfo kept && Mutable(kept.FieldType)
            where settable || assignable || holds
            select $"{type.Name}.{member.Name}";

        Assert.Empty(writable);
    }

    /// <summary>
    ///     Everything the compiler actually builds, and what each object hands
    ///     back — returning the promises it opened, and naming those that were
    ///     writable.
    /// </summary>
    ///
    /// <remarks>
    ///     A hand-kept list of places to look was wrong twice. So this runs a
    ///     compilation and a graph, reaches everything from them and from every
    ///     static in the assembly, and asks each object. Nothing has to be
    ///     remembered for a new type to be covered — but a walk only calls
    ///     members that take no arguments, which is why what it opened is
    ///     returned rather than assumed.
    /// </remarks>
    private static SortedSet<string> Walk(out SortedSet<string> writable, out SortedSet<string> failed)
    {
        Graph graph = new();
        graph.Var("armed", false);
        graph.Let("copy", scope => scope.Read("armed"));
        graph.When("on armed", scope => scope.Read("armed"), _ => throw new InvalidOperationException("bug"));
        graph.Chain("counting", (_ => true, _ => { }));
        graph.Prime();
        graph.Write("armed", true);
        graph.Step();
        graph.Read("copy");

        var compilation = Compilation.Of(new SourceText("""
            var x = 1;
            var x = 2;
            let y = x + 1;
            function add (left => number) to (right => number) { return left; }
            when y { }
            """, "rich.ron"));

        List<object> roots = [graph, compilation, Finder.Under(new DirectoryInfo("."))];

        foreach (var still in
                 from owner in typeof(Graph).Assembly.GetTypes()
                 from field in owner.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                                             | BindingFlags.Static | BindingFlags.DeclaredOnly)
                 select Held(() => field.GetValue(null)))
        {
            roots.Add(still);
        }

        HashSet<object> seen = new(ReferenceEqualityComparer.Instance);
        Queue<object> pending = new(roots);
        SortedSet<string> opened = [];

        writable = [];
        failed = [];

        while (pending.Count is not 0)
        {
            var node = pending.Dequeue();

            if (node is null || node is string || node.GetType().IsPrimitive) continue;
            if (seen.Add(node) is false) continue;

            var type = node.GetType();

            if (type.Assembly == typeof(Graph).Assembly)
            {
                foreach (var member in type.GetMethods(BindingFlags.Public | BindingFlags.Instance
                                                     | BindingFlags.DeclaredOnly))
                {
                    if (member.GetParameters().Length is not 0 || Promises(member.ReturnType) is false) continue;

                    var name = $"{type.Name}.{member.Name.Replace("get_", string.Empty)}";
                    object handed;

                    // Invoked BEFORE the name is recorded, and a failure is kept
                    // rather than dropped. Recording first meant a getter that
                    // threw counted as opened and checked.
                    try
                    {
                        handed = member.Invoke(node, null);
                    }
                    catch (Exception refused)
                    {
                        failed.Add($"{name}: {refused.InnerException?.GetType().Name ?? refused.GetType().Name}");
                        continue;
                    }

                    opened.Add(name);

                    if (Writable(Deeply(handed))) writable.Add(name);
                }

                for (var walk = type; walk?.Assembly == typeof(Graph).Assembly; walk = walk.BaseType)
                {
                    foreach (var field in walk.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                                                       | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        pending.Enqueue(Held(() => field.GetValue(node)));
                    }
                }
            }

            // A dictionary's VALUES, separately. Enumerating one yields
            // «KeyValuePair», a framework struct whose fields this does not walk
            // — so everything the graph keeps in a dictionary was invisible,
            // and that is most of what the graph keeps.
            if (node is System.Collections.IDictionary keyed)
            {
                foreach (var value in keyed.Values) pending.Enqueue(value);
            }

            if (node is System.Collections.IEnumerable inside)
            {
                foreach (var item in inside) pending.Enqueue(item);
            }
        }

        return opened;
    }

    [Fact(DisplayName = "and every read-only promise is one something actually opened")]
    public void AndEveryReadOnlyPromiseIsOneSomethingActuallyOpened()
    {
        // Found by audit. The ledger this replaces listed all the promises and
        // proved nothing about most of them: the walk only calls members that
        // take no arguments, so a NAME in the list recorded that somebody had
        // seen a declaration. Five members were writable while it was green.
        //
        // Discovery is joined to execution here. Every promise must be one the
        // walk reached or one a probe below opens, and the two together must be
        // the whole reflection result — so a member cannot be acknowledged
        // without being opened.
        var opened = Walk(out var writable, out var failed);

        // Nothing the walk asked for may refuse to answer. A member that throws
        // is one it stopped exercising, and that is the difference this test
        // exists to keep.
        Assert.Empty(failed);

        foreach (var (member, probe) in Probes)
        {
            // Called DIRECTLY, so a probe that throws fails the test — and its
            // result is asserted before anything else looks at it, because null
            // is not writable either. Run through a swallowing helper and then
            // checked, an opener whose setup stopped reaching the branch it was
            // written for went on reporting the promise safe, by both routes.
            var handed = probe();

            Assert.NotNull(handed);


            if (Writable(Deeply(handed))) writable.Add(member);

            opened.Add(member);
        }

        Assert.Empty(writable);

        // «Deconstruct» is not discovered, and it is the one exclusion. A
        // record's deconstructor hands out its positional properties, each of
        // which is discovered on its own — measured rather than assumed, on the
        // two records whose properties SHADOW their parameters, since those are
        // the ones where it could have handed back the raw input instead.
        var discovered =
            from owner in typeof(Graph).Assembly.GetTypes()
            from member in owner.GetMethods(BindingFlags.Public | BindingFlags.Instance
                                          | BindingFlags.Static | BindingFlags.DeclaredOnly)
            where member.Name is not "Deconstruct"

               // «out» too. «TryOrder» promises a read-only order through one
               // and returns a bool, so nothing that looked at return types
               // ever saw it.
            where Promises(member.ReturnType)
               || member.GetParameters()
                        .Any(parameter => parameter.ParameterType.HasElementType
                                       && Promises(parameter.ParameterType.GetElementType()))
            select $"{owner.Name}.{member.Name.Replace("get_", string.Empty)}";

        // «Compilation.Body» is one exclusion, and it is structural rather than a
        // decision to skip something: a private record struct used as a local
        // inside «Declare», never stored anywhere, and not a type anything outside
        // «Compilation» can name. There is no instance to reach and no holder to
        // keep one, so the promise it makes cannot be broken from outside the file
        // that makes it.
        //
        // «Resolver.Filling» and «Repairs.Search» are the same kind of exclusion:
        // private nested things one file builds and consumes, never handed to
        // anything past it. «Filling.Arguments» owns its list at construction;
        // «Search.Selecting» hands its result straight into an owned «Repair»,
        // and the walk cannot reach either to check — but the code they cannot
        // escape is the code that would misuse them. «Fillings.Tuples» IS reached
        // — an empty one is left in the memo — so it is opened, not excluded.
        Assert.Equal(discovered.Distinct().Order(),
                     opened.Concat(["Body.Parameters", "Body.Statements", "Filling.Arguments",
                                    "Search.Selecting"]).Order());
    }

    [Theory(DisplayName = "and what a type keeps is what it made, because nothing else can be trusted")]
    [InlineData("list")]
    [InlineData("collection")]
    [InlineData("array")]
    [InlineData("wrapped")]
    [InlineData("segment")]
    [InlineData("opaque")]
    [InlineData("owned")]
    public void AndWhatATypeKeepsIsWhatItMadeBecauseNothingElseCanBeTrusted(string built)
    {
        // Found by audit, and it is the third rule for one question. Two named
        // concrete types. Then «ICollection.IsReadOnly» — which says mutation is
        // unavailable THROUGH THAT INTERFACE and nothing about who else holds
        // the storage:
        //
        //     ReadOnlyCollection over a list the caller kept   -> changed
        //     ArraySegment over an array the caller kept       -> changed
        //
        // Both answer "read-only" and both change underneath. The previous
        // version of this test built its wrapper inline, kept no reference to
        // the backing, and then asserted the wrapper was RETAINED — so it proved
        // writes through the view were unavailable and called that ownership.
        //
        // Ownership is not asked now, it is established: «Owned» keeps only what
        // «Owned» made.
        var backing = new List<string> { "one", "two" };
        var array = new[] { "one", "two" };

        IReadOnlyList<string> given = built switch
        {
            "list" => backing,
            "collection" => new Collection<string>(backing),
            "array" => array,
            "wrapped" => new ReadOnlyCollection<string>(backing),
            "segment" => new ArraySegment<string>(array),
            "opaque" => new Opaque(array),
            _ => Owned.Copy<string>(backing),
        };

        var declared = new Declared("print job", default) { Words = given };

        // Through whatever the caller still holds, which is the half a type test
        // cannot show.
        backing[0] = "changed";
        array[0] = "changed";

        Assert.Equal("one", declared.Words[0]);
        // Only the owned value is kept. Everything else is copied, including the
        // two that call themselves read-only.
        Assert.Equal(built is "owned", ReferenceEquals(given, declared.Words));
    }

    /// <summary>A list that answers «IReadOnlyList» and nothing else.</summary>
    private sealed class Opaque(string[] values) : IReadOnlyList<string>
    {
        public int Count => values.Length;

        public string this[int index] => values[index];

        public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)values).GetEnumerator();

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => values.GetEnumerator();
    }

    [Fact(DisplayName = "and the old pattern and runtime name cannot become two definitions again")]
    public void AndTheOldPatternAndRuntimeNameCannotBecomeTwoDefinitionsAgain()
    {
        // Found by audit. The words were the caller's «params» array, and the
        // descriptor is read two ways: «Words» dynamically by the built-in
        // pattern, «Prefix» by the runtime graph. Writing
        // an element split them —
        //
        //     SymbolTable.Old   prior      the resolver recognises this
        //     Of(["x"])         prior x    a word view says this
        //     Of("x")           old x      while the graph name stays this
        //     Prefix            «old »     and still says this
        //
        // — which is the two-independent-definitions failure the descriptor
        // exists to prevent, reintroduced from inside.
        Assert.Throws<InvalidCastException>(() => (string[])Injection.Shadow.Words);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)Injection.Shadow.Words)[0] = "prior");

        Assert.Equal(Injection.Shadow.Words[0], SymbolTable.Old);
        Assert.Equal(Injection.Shadow.Prefix, Injection.Shadow.Words[0] + " ");
        Assert.Equal(Injection.Shadow.Of("x"), string.Join(" ", Injection.Shadow.Of(["x"])));
    }

    [Fact(DisplayName = "and reading a finding's labels twice allocates nothing the second time")]
    public void AndReadingAFindingsLabelsTwiceAllocatesNothingTheSecondTime()
    {
        // «AsReadOnly» built a fresh wrapper on every read for an object that
        // never needs to change, where the graph and the compilation each cache
        // one. Small, and the kind of difference that is only ever noticed by
        // someone measuring something else.
        var finding = Compilation.Of(new SourceText("var x = 1;\nvar x = 2;\n", "twice.ron")).Findings[0];

        Assert.NotEmpty(finding.Related);

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var at = 0; at < 1_000; ++at) _ = finding.Related;

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>
    ///     One opener for every promise the walk cannot reach by itself.
    /// </summary>
    ///
    /// <remarks>
    ///     What LEAVES this table is as informative as what is in it. «Owned.Of»,
    ///     «Owned.Copy», «Best.Pair», «Best.Either» and «Best.Readings» were
    ///     here and are not, because each returns «Owned.Kept» now rather than
    ///     an «IReadOnlyList» — a concrete type nothing can write to makes no
    ///     promise, so there is nothing here to check. The compiler took over
    ///     the assertion.
    ///
    ///     Parameterised members, and types the pipeline builds only as locals.
    ///     Each is here because the test above will not pass without it: a
    ///     promise that is neither walked nor opened here fails, so this table
    ///     cannot quietly fall behind the code the way a list of names did.
    /// </remarks>
    /// <summary>A table with a tie in it, for the repair search to have something to search.</summary>
    private static readonly SymbolTable Ambiguity =
        new SymbolTable().WithNames("a", "b", "a to b").WithPatterns("send _", "send _ to _");

    /// <summary>One ambiguity, for the three promises it hands out.</summary>
    private static readonly Ambiguous Tie =
        new(default,
            new List<string> { "one" },
            new List<Repair> { new("one", 0, new List<Insertion> { new(0, "(") }) },
            2,
            false);

    private static readonly (string Member, Func<object> Open)[] Probes =
    [
        ("Ambiguous.Readings", () => Tie.Readings),
        ("Ambiguous.Repairs", () => Tie.Repairs),
        ("Repair.Insertions", () => Tie.Repairs[0].Insertions),
        ("Repairs.For", () => Repairs.For(new Resolver(Ambiguity), Lexemes.Lex("send a to b"),
                                          new Resolver(Ambiguity).Resolve("send a to b"))),
        ("Builtin.Operators", () => Builtin.Operators),
        ("Descriptor.Forms", () => SymbolTable.Supplies[0].Forms),
        ("Descriptor.SeeAlso", () => SymbolTable.Supplies.First(s => s.SeeAlso.Count is not 0).SeeAlso),
        ("SymbolTable.Supplies", () => SymbolTable.Supplies),
        ("SymbolTable.Truths", () => SymbolTable.Truths),
        ("Resolution.Alternatives", () => new Resolver(Ambiguity).Resolve("send a to b").Alternatives),
        ("SymbolTable.Whole", () => SymbolTable.Whole),
        ("Call.Arguments", () => new Node.Call(Pattern.Parse("print _"), new List<Node> { new Node.Name("x") }).Arguments),
        ("Cascades.Cycles", () => Cascades.Cycles(Ringed)),
        ("Completion.After", () => new Completion(new SymbolTable().WithNames("total")).After(Lexemes.Lex("tot"))),
        ("Declaration.Blocks", () => new Declaration(Pattern.Parse("twice _"), [["a"]], (_, _) => 1d).Blocks),
        ("Declared.Words", () => new Declared("print job", default).Words),
        ("Effects.Reads", () => Ringed["a"].Reads),
        ("Effects.Writes", () => Ringed["a"].Writes),
        ("Finding.Related", () => Twice.Findings[0].Related),
        ("Glue.Reserved", () => Glue.Reserved([Pattern.Parse("send _ to _")])),
        ("Glue.Shapes", () => Glue.Shapes),
        ("Graph.Dependencies", () => Reading.Dependencies("copy")),
        ("Group.Parts", () => new Node.Group(new List<Node.Entry> { new(null, new Node.Name("x")) }).Parts),
        ("Identifier.TryPattern", () => Blocks()),
        ("Initialisation.Cycles", () => Initialisation.Cycles(Reads)),
        ("Initialisation.TryOrder", () => Ordered()),
        ("Injection.All", () => Injection.All),
        ("Injection.Of", () => Injection.Shadow.Of(["x"])),
        ("ManyWriters.Writers", () => new ManyWriters(default, "cash", new List<string> { "a" }).Writers),
        ("Pattern.Reads", () => Pattern.Reads(["print", null])),
        ("Rules.Infix", () => Rules.Infix),
        ("Rules.Injected", () => Rules.Injected),
        ("SymbolTable.Builtins", () => SymbolTable.Builtins),
        ("Triggers.Distinct", () => Triggers.Distinct(["a", "a"])),
    ];

    /// <summary>Two whens that write what each other reads, which is a ring.</summary>
    private static Dictionary<string, Effects> Ringed { get; } = new()
    {
        ["a"] = new Effects(new HashSet<string> { "b" }, new HashSet<string> { "b" }),
        ["b"] = new Effects(new HashSet<string> { "a" }, new HashSet<string> { "a" }),
    };

    private static Dictionary<string, IReadOnlySet<string>> Reads { get; } = new()
    {
        ["a"] = new HashSet<string>(),
        ["b"] = new HashSet<string> { "a" },
    };

    private static Compilation Twice { get; }
        = Compilation.Of(new SourceText("var x = 1;\nvar x = 2;\n", "twice.ron"));

    private static Graph Reading { get; } = Watching();

    private static Graph Watching()
    {
        Graph graph = new();
        graph.Var("source", 1d);
        graph.Let("copy", scope => scope.Read("source"));
        graph.Read("copy");

        return graph;
    }

    private static object Ordered()
    {
        Initialisation.TryOrder(Reads, out var order);

        return order;
    }

    /// <remarks>
    ///     A real declaration, because «print job» is a NAME. It has no hole, so
    ///     «TryPattern» said false and this opener returned null — for as long as
    ///     it has existed. Null was not writable and the name was recorded as
    ///     opened, so the promise went unexercised and the test stayed green,
    ///     which is what the null check in the opener loop is for.
    /// </remarks>
    private static object Blocks()
    {
        var module = Compilation.Of(new SourceText("function add (left => number) to (right => number) { return left; }\n", "add.ron")).Module;
        var declaration = module.Scopes[0].Statements.OfType<Ronin.Grammar.Function>().Single();

        return declaration.Identifier.TryPattern(out _, out var blocks) ? blocks : null;
    }

    /// <summary>The published thing, or the first thing inside it that is writable.</summary>
    ///
    /// <remarks>
    ///     A result is published all the way down. The rings inside a cycle
    ///     report were each the mutable list that built them while the
    ///     collection holding them was read-only, so a check that looked only at
    ///     the outer object said the answer was safe.
    /// </remarks>
    private static object Deeply(object handed)
    {
        if (Writable(handed) || handed is not System.Collections.IEnumerable inside || handed is string)
            return handed;

        foreach (var item in inside)
        {
            if (Deeply(item) is object found && Writable(found)) return found;
        }

        return handed;
    }

    /// <summary>Whether a member promises a collection nobody may write to.</summary>
    ///
    /// <remarks>
    ///     «IEnumerable» is deliberately not here. It promises a sequence and
    ///     says nothing about storage, so a method returning one is not claiming
    ///     what these are claiming.
    /// </remarks>
    private static bool Promises(Type type)
        => type.IsGenericType
        && new[]
           {
               typeof(IReadOnlyList<>), typeof(IReadOnlyCollection<>),
               typeof(IReadOnlyDictionary<,>), typeof(IReadOnlySet<>),
           }.Contains(type.GetGenericTypeDefinition());

    /// <summary>Whether the thing actually handed back can be written to.</summary>
    ///
    /// <remarks>
    ///     An ARRAY is asked about separately, and it is the case that matters:
    ///     «string[]» reports «IsReadOnly» as TRUE through «ICollection», while
    ///     one cast assigns an element. A check that believed the collection
    ///     about itself walked straight past the finding it was written for.
    /// </remarks>
    private static bool Writable(object handed)
        => handed is Array
        || handed is not null
        && handed.GetType()
                 .GetInterfaces()
                 .Any(face => face.IsGenericType
                           && face.GetGenericTypeDefinition() == typeof(ICollection<>)
                           && (bool)face.GetProperty("IsReadOnly").GetValue(handed) is false);

    /// <summary>Whatever it holds, where asking is allowed to fail.</summary>
    private static object Held(Func<object> ask)
    {
        try { return ask(); }
        catch (Exception) { return null; }
    }

    [Fact(DisplayName = "and a forged dependency cannot outlive a source that changed")]
    public void AndAForgedDependencyCannotOutliveASourceThatChanged()
    {
        // The failure the writable edge set produced, kept as the thing that
        // must stay true. Rewriting «copy»'s dependencies left the reverse edge
        // intact, so it was still dirtied — and then cutoff consulted the forged
        // set, found the name it now pointed at had not changed, cleared the
        // dirty bit, and kept a cached answer that was wrong with nothing able
        // to repair it.
        Graph graph = new();
        graph.Var("source", 1d);
        graph.Var("stable", 0d);
        graph.Let("copy", scope => scope.Read("source"));

        Assert.Equal(1d, graph.Read("copy"));

        var seen = graph.Dependencies("copy");

        Assert.Throws<InvalidCastException>(() => (HashSet<string>)seen);
        Assert.Throws<NotSupportedException>(() => ((ICollection<string>)seen).Clear());
        Assert.Throws<NotSupportedException>(() => ((ICollection<string>)seen).Add("stable"));

        graph.Write("source", 2d);
        graph.Step();

        Assert.Equal(2d, graph.Read("copy"));
        Assert.Equal(["source"], graph.Dependencies("copy"));
    }

    [Fact(DisplayName = "and the runtime's account of its own failures cannot be erased")]
    public void AndTheRuntimesAccountOfItsOwnFailuresCannotBeErased()
    {
        Graph graph = new();
        graph.Var("armed", false);
        graph.When("on armed", scope => scope.Read("armed"), _ => throw new InvalidOperationException("bug"));
        graph.Prime();
        graph.Write("armed", true);
        graph.Step();

        Assert.Single(graph.Faults);
        Assert.NotEmpty(graph.Trace);

        Assert.Throws<NotSupportedException>(() => ((ICollection<Fault>)graph.Faults).Clear());
        Assert.Throws<NotSupportedException>(() => ((ICollection<string>)graph.Trace).Clear());
        Assert.Throws<NotSupportedException>(() => ((ICollection<string>)graph.Fired).Clear());

        Assert.Single(graph.Faults);
    }

    [Fact(DisplayName = "and a malformed file cannot be made to compile clean")]
    public void AndAMalformedFileCannotBeMadeToCompileClean()
    {
        // «Program» chooses success or failure from this count, so the
        // collection is not cosmetic output.
        var compilation = Compilation.Of(new SourceText("var x = 1;\nvar x = 2;\n", "twice.ron"));

        Assert.NotEmpty(compilation.Findings);
        Assert.NotEmpty(compilation.Findings[0].Related);

        Assert.Throws<NotSupportedException>(() => ((ICollection<Finding>)compilation.Findings).Clear());
        Assert.Throws<NotSupportedException>(
            () => ((ICollection<Labelled>)compilation.Findings[0].Related).Clear());

        Assert.NotEmpty(compilation.Findings);
    }

    [Fact(DisplayName = "and a scope may extend the language for itself and never for everyone")]
    public void AndAScopeMayExtendTheLanguageForItselfAndNeverForEveryone()
    {
        // The one fixed table was a mutable dictionary behind a read-only type,
        // so one cast removed «+» for every resolver built afterwards — which
        // is exactly what the comments say a scope must not be able to do.
        Assert.Throws<InvalidCastException>(() => (Dictionary<string, Operator>)Builtin.Operators);
        Assert.Throws<NotSupportedException>(
            () => ((IDictionary<string, Operator>)Builtin.Operators).Remove("+"));

        // And the deliberate half still works: a table extends itself, and the
        // next one is untouched.
        SymbolTable extended = new();
        extended.Operators["~"] = Builtin.Operators["+"];

        Assert.True(new SymbolTable().Operators.ContainsKey("+"));
        Assert.False(new SymbolTable().Operators.ContainsKey("~"));
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
