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
    ///     The declarations of one scope. Nested scopes are not descended into:
    ///     what an inner scope can see of an outer one is a scoping question that
    ///     has not been settled, and guessing at it here would bake in an answer.
    /// </summary>
    public static Declarations Of(IEnumerable<Statement> statements)
    {
        Declarations declarations = new();

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

        if (member is Datum { Mutability: Constant }) Symbols.Constants(name);
        else if (member is Datum) Symbols.Declaring(name);
        else Symbols.WithNames(name);
    }

    /// <summary>The parameter names of each declared pattern, by hole.</summary>
    public Dictionary<Compiler.Pattern, IReadOnlyList<IReadOnlyList<string>>> Blocks { get; } = [];

    private readonly List<string> problems = [];
}
