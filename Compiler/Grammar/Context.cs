// Copyright © 2023 Eric Budai

namespace Ronin.Grammar;

internal interface IContext
{
    IContext Parent { get; }
    Resolution Resolve(Reference reference);
}