// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using static Ronin.Language.Identifier;

namespace Ronin.Grammar;

/// <summary>
///     Names a <see cref="Scope"/> via 'part of', or exposes one to the current <see cref="Scope"/> via 'import'
/// </summary>
/// 
/// <example>
///     part of best package for weather lookups
///     
///     import best package for weather lookups
///     import git://github.com/ebudai/Ronin as ronin
/// </example>
/// 
internal class ImportExportSyntax : Syntax, Compiler.IParsable<ImportExportSyntax>
{
    public Keyword Direction { get; init; }
    public List<Component> Components { get; init; }

    public string Name
    {
        get
        {
            var name = string.Empty;
            foreach (var component in Components)
            {
                foreach (var token in component.Source.Span)
                {
                    if (token is TextLiteral) name += $" {token.sourcecode[1..^1]}";
                    else name += $" {token.sourcecode}";
                }
            }
            return name is "" ? name : name[1..];
        }
    }

    public static ImportExportSyntax Parse(ref Parser context)
    {
        var direction = context.CurrentToken is PartOfKeyword or ImportKeyword ? context.CurrentToken as Keyword : null;
        if (direction is null) return null;

        Parser parser = context;
        parser.Advance();

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;

        return new ImportExportSyntax 
        {
            Direction = direction,
            Components = components,
            Source = parser.Commit(ref context) 
        };
    }

    public class Component : CompositeSyntax<Component, Name, LiteralSyntax> { }
}