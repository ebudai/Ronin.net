using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Test;

internal static class Utility
{
    internal static void SetMemory<T>(this T value, string args) where T : Token => typeof(T).GetProperty("Memory").SetMethod.Invoke(value, new object[] { args.AsMemory() });
}

public class ParsingTests
{
    internal static Word Word(string text)
    {
        Word word = new();
        word.SetMemory(text);
        return word;
    }

    internal static Symbol Symbol(string text)
    {
        Symbol symbol = new();
        symbol.SetMemory(text);
        return symbol;
    }

    internal static Text Text(string value)
    {
        Text text = new();
        text.SetMemory(("\"" + value + "\""));
        return text;
    }

    internal static Numeric Number(double value)
    {
        Numeric number = new();
        number.SetMemory(value.ToString());
        return number;
    }

    internal static Numeric Number(long value)
    {
        Numeric number = new();
        number.SetMemory(value.ToString());
        return number;
    }

    internal static Numeric Number(ulong value)
    {
        Numeric number = new();
        number.SetMemory(value.ToString());
        return number;
    }

    internal static Assign Assign()
    {
        Assign assign = new();
        assign.SetMemory(Ronin.Lexicon.Symbols.Assign.symbol.ToString());
        return assign;
    }

    internal static AddAssign AddAssign()
    {
        AddAssign assign = new();
        assign.SetMemory(Ronin.Lexicon.Symbols.AddAssign.symbol);
        return assign;
    }

    internal static AndAssign AndAssign()
    {
        AndAssign assign = new();
        assign.SetMemory(Ronin.Lexicon.Symbols.AndAssign.symbol);
        return assign;
    }

    internal static DivideAssign DivideAssign()
    {
        DivideAssign assign = new();
        assign.SetMemory(Ronin.Lexicon.Symbols.DivideAssign.symbol);
        return assign;
    }

    internal static MultiplyAssign MultiplyAssign()
    {
        MultiplyAssign assign = new();
        assign.SetMemory(Ronin.Lexicon.Symbols.MultiplyAssign.symbol);
        return assign;
    }

    internal static OrAssign OrAssign()
    {
        OrAssign assign = new();
        assign.SetMemory(Ronin.Lexicon.Symbols.OrAssign.symbol);
        return assign;
    }

    internal static SubtractAssign SubtractAssign()
    {
        SubtractAssign assign = new();
        assign.SetMemory(Ronin.Lexicon.Symbols.SubtractAssign.symbol);
        return assign;
    }

    internal static Terminal Terminal()
    {
        Terminal terminal = new();
        terminal.SetMemory(Ronin.Lexicon.Symbols.Terminal.symbol.ToString());
        return terminal;
    }

    internal static Separator Separator()
    {
        Separator separator = new();
        separator.SetMemory(Ronin.Lexicon.Symbols.Separator.symbol.ToString());
        return separator;
    }

    internal static StartValues StartValues()
    {
        StartValues startValues = new();
        startValues.SetMemory(Ronin.Lexicon.Symbols.StartValues.symbol.ToString());
        return startValues;
    }

    internal static EndValues EndValues()
    {
        EndValues endValues = new();
        endValues.SetMemory(Ronin.Lexicon.Symbols.EndValues.symbol.ToString());
        return endValues;
    }

    internal static StartIndexer StartIndexer()
    {
        StartIndexer startIndexer = new();
        startIndexer.SetMemory(Ronin.Lexicon.Symbols.StartIndexer.symbol.ToString());
        return startIndexer;
    }

    internal static EndIndexer EndIndexer()
    {
        EndIndexer endIndexer = new();
        endIndexer.SetMemory(Ronin.Lexicon.Symbols.EndIndexer.symbol.ToString());
        return endIndexer;
    }

    internal static StartScope StartScope()
    {
        StartScope startScope = new();
        startScope.SetMemory(Ronin.Lexicon.Symbols.StartScope.symbol.ToString());
        return startScope;
    }

    internal static EndScope EndScope()
    {
        EndScope endScope = new();
        endScope.SetMemory(Ronin.Lexicon.Symbols.EndScope.symbol.ToString());
        return endScope;
    }

    internal static TextDelimiter TextDelimiter()
    {
        TextDelimiter textDelimiter = new();
        textDelimiter.SetMemory(Ronin.Lexicon.Symbols.TextDelimiter.symbol.ToString());
        return textDelimiter;
    }

    internal static Interval Range()
    {
        Interval range = new();
        range.SetMemory(Interval.symbol);
        return range;
    }

    internal static Returns Returns()
    {
        Returns returns = new();
        returns.SetMemory(Ronin.Lexicon.Symbols.Returns.symbol);
        return returns;
    }

    internal static class Keyword
    {
        internal static Ronin.Lexicon.Keywords.Function Function()
        {
            Ronin.Lexicon.Keywords.Function function = new();
            function.SetMemory(Ronin.Lexicon.Keywords.Function.keyword);
            return function;
        }

        internal static Ronin.Lexicon.Keywords.Datatype Datatype()
        {
            Ronin.Lexicon.Keywords.Datatype datatype = new();
            datatype.SetMemory(Ronin.Lexicon.Keywords.Datatype.keyword);
            return datatype;
        }

        internal static PartOf PartOf()
        {
            PartOf export = new();
            export.SetMemory(Ronin.Lexicon.Keywords.PartOf.keyword);
            return export;
        }

        internal static Variable Variable()
        {
            Variable variable = new();
            variable.SetMemory(Ronin.Lexicon.Keywords.Variable.keyword);
            return variable;
        }

        internal static Constant Constant()
        {
            Constant constant = new();
            constant.SetMemory(Ronin.Lexicon.Keywords.Constant.keyword);
            return constant;
        }

        internal static Reactive Reactive()
        {
            Reactive reactive = new();
            reactive.SetMemory(Ronin.Lexicon.Keywords.Reactive.keyword);
            return reactive;
        }

        internal static Let Let()
        {
            Let let = new();
            let.SetMemory(Ronin.Lexicon.Keywords.Let.keyword);
            return let;
        }

        internal static Compiled Compiled()
        {
            Compiled compiled = new();
            compiled.SetMemory(Ronin.Lexicon.Keywords.Compiled.keyword);
            return compiled;
        }

        internal static Persistent Persistent()
        {
            Persistent persistent = new();
            persistent.SetMemory(Ronin.Lexicon.Keywords.Persistent.keyword);
            return persistent;
        }

        internal static Shared Shared()
        {
            Shared shared = new();
            shared.SetMemory(Ronin.Lexicon.Keywords.Shared.keyword);
            return shared;
        }

        internal static Optional Optional()
        {
            Optional optional = new();
            optional.SetMemory(Ronin.Lexicon.Keywords.Optional.keyword);
            return optional;
        }

        internal static ForEach ForEach()
        {
            ForEach @foreach = new();
            @foreach.SetMemory(Ronin.Lexicon.Keywords.ForEach.keyword);
            return @foreach;
        }

        internal static If If()
        {
            If @if = new();
            @if.SetMemory(Ronin.Lexicon.Keywords.If.keyword);
            return @if;
        }

        internal static While While()
        {
            While @while = new();
            @while.SetMemory(Ronin.Lexicon.Keywords.While.keyword);
            return @while;
        }

        internal static Ronin.Lexicon.Keywords.Import Import()
        {
            Ronin.Lexicon.Keywords.Import import = new();
            import.SetMemory(Ronin.Lexicon.Keywords.Import.keyword);
            return import;
        }
    }

    internal static Whitespace Whitespace()
    {
        Whitespace whitespace = new();
        whitespace.SetMemory(" ");
        return whitespace;
    }
}

public class AnalysisTests
{
    internal static Identifier Name(string name)
    {
        Word word = new();
        word.SetMemory(name);
        Name words = new() { Source = new[] { word } };
        return new Identifier()
        {
            Components = new() { new Identifier.Component { value = words } }
        };
    }

    internal static Name Words(string name)
    {
        List<Word> words = new();
        foreach (var word in name.Split(new[] { ' ' })) words.Add(new(word));
        return new Name { Source = words.ToArray() };
    }
}