// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Grammar;

/// <summary>
///     What a parsed scope declares, as the resolver's scope.
/// </summary>
///
/// <remarks>
///     <para>
///     The join the resolver has been waiting for. Until now a
///     <see cref="SymbolTable"/> was built by hand in a test; this builds one from
///     what the parser found, so a statement can be resolved against the names and
///     patterns its own file declares.
///     </para>
///     <para>
///     Which of the two a declaration produces is structural rather than a matter
///     of interpretation: <c>var base price =&gt; Number</c> is all name words and
///     so is a name, while <c>function compute total for (order)</c> contains a
///     parameter block and so is a pattern with a hole where the block sits.
///     </para>
/// </remarks>
internal sealed class Declarations
{
    private Declarations() { }

    public SymbolTable Symbols { get; } = new();

    /// <summary>What could not be declared, in the order it was found.</summary>
    public IReadOnlyList<string> Problems => problems;

    /// <summary>
    ///     The declarations of one scope, folded into everything the enclosing
    ///     scopes declared.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Inward yes, outward no. An inner scope sees every enclosing
    ///     declaration at any position — the pre-pass already makes order
    ///     irrelevant within a scope and nesting inherits that — while nothing an
    ///     inner scope declares is visible to a sibling or a parent. That
    ///     direction matters more here than in most languages, because a pattern
    ///     declaration is a grammar production: escaping ones would let a nested
    ///     function change the grammar of its siblings' bodies, and scopes would
    ///     have to resolve inside-out.
    ///     </para>
    ///     <para>
    ///     The result is flat. Shadowing being an error is what allows that, and
    ///     it is why a lookup stays one probe rather than a walk up N levels.
    ///     </para>
    /// </remarks>
    public static Declarations Of(IEnumerable<Statement> statements, Declarations enclosing = null)
    {
        Declarations declarations = new();

        if (enclosing is not null)
        {
            declarations.Symbols.Merging(enclosing.Symbols);
            declarations.inherited.UnionWith(enclosing.Symbols.Names);

            foreach (var (pattern, blocks) in enclosing.Blocks) declarations.Blocks[pattern] = blocks;
        }

        foreach (var statement in statements) declarations.Declare(statement);

        return declarations;
    }

    private void Declare(Statement statement)
    {
        // Member.Unresolved is a statement that mentions names rather than
        // declaring any, and it is the shape an ordinary expression takes
        if (statement is not Member member || member.Identifier is null) return;

        if (member.Identifier.TryPattern(out var pattern, out var blocks) is false)
        {
            Cell(member);
            return;
        }

        var unnamed = blocks.SelectMany(block => block).Count(name => name is null);
        if (unnamed is not 0)
        {
            problems.Add(
                $"«{pattern}» has {unnamed} parameter(s) this pass cannot name. A defaulted " +
                "parameter is written as an assignment rather than a declaration, and reading " +
                "its name is not implemented — give it a type for now.");
            return;
        }

        Symbols.Patterns.Add(pattern);
        Blocks[pattern] = blocks;
    }

    /// <summary>
    ///     A value-holding declaration. Only these inject a shadow, and a constant
    ///     does not even do that — its previous value is provably its current one.
    /// </summary>
    private void Cell(Member member)
    {
        var name = member.Identifier.Words;

        if (name.StartsWith(SymbolTable.Shadowed, System.StringComparison.Ordinal))
        {
            problems.Add($"«{name}» begins with the reserved word «{SymbolTable.Old}», which is " +
                         "injected rather than declared. Respell it.");
            return;
        }

        if (Symbols.Names.Contains(name))
        {
            problems.Add(
                $"«{name}» is already declared {Where(name)}. Shadowing is not allowed, because " +
                "reading a value has to tell you where it came from, and the compiler cannot flag " +
                "the ambiguity when both readings are legal. Rename this one.");
            return;
        }

        if (member is Datum { Mutability: Constant }) Symbols.Constants(name);
        else if (member is Datum) Symbols.Declaring(name);
        else Symbols.WithNames(name);
    }

    private string Where(string name) => inherited.Contains(name) ? "in an enclosing scope" : "in this scope";

    /// <summary>The parameter names of each declared pattern, by hole.</summary>
    public Dictionary<Compiler.Pattern, IReadOnlyList<IReadOnlyList<string>>> Blocks { get; } = [];

    private readonly List<string> problems = [];
    private readonly HashSet<string> inherited = [];
}
