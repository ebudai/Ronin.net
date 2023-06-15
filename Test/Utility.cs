using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Test;

internal static class Utility
{
    internal static void SetMemory<T>(this T value, char[] args) where T : Token => typeof(T).GetProperty("Memory").SetMethod.Invoke(value, new object[] { (ReadOnlyMemory<char>)args.AsMemory() });
}

public class ParsingTests
{
    internal static Word Word(string text)
    {
        Word word = new();
        word.SetMemory(text.ToCharArray());
        return word;
    }

    internal static Symbol Symbol(string text)
    {
        Symbol symbol = new();
        symbol.SetMemory(text.ToCharArray());
        return symbol;
    }

    internal static Text Text(string value)
    {
        Text text = new();
        text.SetMemory(("\"" + value + "\"").ToCharArray());
        return text;
    }

    internal static Number Number(double value)
    {
        Number number = new();
        number.SetMemory(value.ToString().ToCharArray());
        return number;
    }

    internal static Number Number(long value)
    {
        Number number = new();
        number.SetMemory(value.ToString().ToCharArray());
        return number;
    }

    internal static Number Number(ulong value)
    {
        Number number = new();
        number.SetMemory(value.ToString().ToCharArray());
        return number;
    }

    internal static Assign Assign()
    {
        Assign assign = new();
        assign.SetMemory(new[] { Ronin.Lexicon.Symbols.Assign.symbol });
        return assign;
    }

    internal static Terminal Terminal()
    {
        Terminal terminal = new();
        terminal.SetMemory(new[] { Ronin.Lexicon.Symbols.Terminal.symbol });
        return terminal;
    }

    internal static Separator Separator()
    {
        Separator separator = new();
        separator.SetMemory(new[] { Ronin.Lexicon.Symbols.Separator.symbol });
        return separator;
    }

    internal static StartValues StartValues()
    {
        StartValues startValues = new();
        startValues.SetMemory(new[] { Ronin.Lexicon.Symbols.StartValues.symbol });
        return startValues;
    }

    internal static EndValues EndValues()
    {
        EndValues endValues = new();
        endValues.SetMemory(new[] { Ronin.Lexicon.Symbols.EndValues.symbol });
        return endValues;
    }

    internal static StartOrdinal StartOrdinal()
    {
        StartOrdinal startOrdinal = new();
        startOrdinal.SetMemory(new[] { Ronin.Lexicon.Symbols.StartOrdinal.symbol });
        return startOrdinal;
    }

    internal static EndOrdinal EndOrdinal()
    {
        EndOrdinal endOrdinal = new();
        endOrdinal.SetMemory(new[] { Ronin.Lexicon.Symbols.EndOrdinal.symbol });
        return endOrdinal;
    }

    internal static StartScope StartScope()
    {
        StartScope startScope = new();
        startScope.SetMemory(new[] { Ronin.Lexicon.Symbols.StartScope.symbol });
        return startScope;
    }

    internal static EndScope EndScope()
    {
        EndScope endScope = new();
        endScope.SetMemory(new[] { Ronin.Lexicon.Symbols.EndScope.symbol });
        return endScope;
    }

    internal static TextDelimiter TextDelimiter()
    {
        TextDelimiter textDelimiter = new();
        textDelimiter.SetMemory(new[] { Ronin.Lexicon.Symbols.TextDelimiter.symbol });
        return textDelimiter;
    }

    internal static Ronin.Lexicon.Symbols.Interval Range()
    {
        Ronin.Lexicon.Symbols.Interval range = new();
        range.SetMemory(Ronin.Lexicon.Symbols.Interval.symbol.ToCharArray());
        return range;
    }

    internal static Returns Returns()
    {
        Returns returns = new();
        returns.SetMemory(Ronin.Lexicon.Symbols.Returns.symbol.ToCharArray());
        return returns;
    }

    internal static PartOf PartOf()
    {
        PartOf export = new();
        export.SetMemory(Ronin.Lexicon.Keywords.PartOf.keyword.ToCharArray());
        return export;
    }

    internal static Variable Variable()
    {
        Variable variable = new();
        variable.SetMemory(Ronin.Lexicon.Keywords.Variable.keyword.ToCharArray());
        return variable;
    }

    internal static Function Function()
    {
        Function function = new();
        function.SetMemory(Ronin.Lexicon.Keywords.Function.keyword.ToCharArray());
        return function;
    }

    internal static Datatype Datatype()
    {
        Datatype datatype = new();
        datatype.SetMemory(Ronin.Lexicon.Keywords.Datatype.keyword.ToCharArray());
        return datatype;
    }

    internal static Constant Constant()
    {
        Constant constant = new();
        constant.SetMemory(Ronin.Lexicon.Keywords.Constant.keyword.ToCharArray());
        return constant;
    }

    internal static Reactive Reactive()
    {
        Reactive reactive = new();
        reactive.SetMemory(Ronin.Lexicon.Keywords.Reactive.keyword.ToCharArray());
        return reactive;
    }

    internal static Compiled Compiled()
    {
        Compiled compiled = new();
        compiled.SetMemory(Ronin.Lexicon.Keywords.Compiled.keyword.ToCharArray());
        return compiled;
    }

    internal static Persistent Persistent()
    {
        Persistent persistent = new();
        persistent.SetMemory(Ronin.Lexicon.Keywords.Persistent.keyword.ToCharArray());
        return persistent;
    }

    internal static Shared Shared()
    {
        Shared shared = new();
        shared.SetMemory(Ronin.Lexicon.Keywords.Shared.keyword.ToCharArray());
        return shared;
    }

    internal static Optional Optional()
    {
        Optional optional = new();
        optional.SetMemory(Ronin.Lexicon.Keywords.Optional.keyword.ToCharArray());
        return optional;
    }

    internal static ForEach ForEach()
    {
        ForEach @foreach = new();
        @foreach.SetMemory(Ronin.Lexicon.Keywords.ForEach.keyword.ToCharArray());
        return @foreach;
    }

    internal static Import Import()
    {
        Import import = new();
        import.SetMemory(Ronin.Lexicon.Keywords.Import.keyword.ToCharArray());
        return import;
    }

    internal static Whitespace Whitespace()
    {
        Whitespace whitespace = new();
        whitespace.SetMemory(new[] { ' ' });
        return whitespace;
    }
}
