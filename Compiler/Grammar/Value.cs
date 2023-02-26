// Copyright © 2023 Eric Budai

using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

internal class Value : CompositeSyntax<Value, LiteralSyntax, Arguments, InlineListSyntax, InlineLookupSyntax, DelegateSyntax, Reference>
{

}