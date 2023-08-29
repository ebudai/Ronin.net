using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Function = Ronin.Grammar.Function;

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

    internal static Currency Currency(double value)
    {
        Currency currency = new();
        currency.SetMemory(value.ToString());
        return currency;
    }

    internal static Assign Assign()
    {
        Assign assign = new();
        assign.SetMemory(Ronin.Lexicon.Assign.symbol.ToString());
        return assign;
    }

    internal static AddAssign AddAssign()
    {
        AddAssign assign = new();
        assign.SetMemory(Ronin.Lexicon.AddAssign.symbol);
        return assign;
    }

    internal static AndAssign AndAssign()
    {
        AndAssign assign = new();
        assign.SetMemory(Ronin.Lexicon.AndAssign.symbol);
        return assign;
    }

    internal static DivideAssign DivideAssign()
    {
        DivideAssign assign = new();
        assign.SetMemory(Ronin.Lexicon.DivideAssign.symbol);
        return assign;
    }

    internal static MultiplyAssign MultiplyAssign()
    {
        MultiplyAssign assign = new();
        assign.SetMemory(Ronin.Lexicon.MultiplyAssign.symbol);
        return assign;
    }

    internal static OrAssign OrAssign()
    {
        OrAssign assign = new();
        assign.SetMemory(Ronin.Lexicon.OrAssign.symbol);
        return assign;
    }

    internal static SubtractAssign SubtractAssign()
    {
        SubtractAssign assign = new();
        assign.SetMemory(Ronin.Lexicon.SubtractAssign.symbol);
        return assign;
    }

    internal static Terminal Terminal()
    {
        Terminal terminal = new();
        terminal.SetMemory(Ronin.Lexicon.Terminal.symbol.ToString());
        return terminal;
    }

    internal static Separator Separator()
    {
        Separator separator = new();
        separator.SetMemory(Ronin.Lexicon.Separator.symbol.ToString());
        return separator;
    }

    internal static StartValues StartValues()
    {
        StartValues startValues = new();
        startValues.SetMemory(Ronin.Lexicon.StartValues.symbol.ToString());
        return startValues;
    }

    internal static EndValues EndValues()
    {
        EndValues endValues = new();
        endValues.SetMemory(Ronin.Lexicon.EndValues.symbol.ToString());
        return endValues;
    }

    internal static StartIndexer StartIndexer()
    {
        StartIndexer startIndexer = new();
        startIndexer.SetMemory(Ronin.Lexicon.StartIndexer.symbol.ToString());
        return startIndexer;
    }

    internal static EndIndexer EndIndexer()
    {
        EndIndexer endIndexer = new();
        endIndexer.SetMemory(Ronin.Lexicon.EndIndexer.symbol.ToString());
        return endIndexer;
    }

    internal static StartScope StartScope()
    {
        StartScope startScope = new();
        startScope.SetMemory(Ronin.Lexicon.StartScope.symbol.ToString());
        return startScope;
    }

    internal static EndScope EndScope()
    {
        EndScope endScope = new();
        endScope.SetMemory(Ronin.Lexicon.EndScope.symbol.ToString());
        return endScope;
    }

    internal static TextDelimiter TextDelimiter()
    {
        TextDelimiter textDelimiter = new();
        textDelimiter.SetMemory(Ronin.Lexicon.TextDelimiter.symbol.ToString());
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
        returns.SetMemory(Ronin.Lexicon.Returns.symbol);
        return returns;
    }

    internal static class Keyword
    {
        internal static Ronin.Lexicon.Function Function()
        {
            Ronin.Lexicon.Function function = new();
            function.SetMemory(Ronin.Lexicon.Function.keyword);
            return function;
        }

        internal static Ronin.Lexicon.Datatype Datatype()
        {
            Ronin.Lexicon.Datatype datatype = new();
            datatype.SetMemory(Ronin.Lexicon.Datatype.keyword);
            return datatype;
        }

        internal static PartOf PartOf()
        {
            PartOf export = new();
            export.SetMemory(Ronin.Lexicon.PartOf.keyword);
            return export;
        }

        internal static Variable Variable()
        {
            Variable variable = new();
            variable.SetMemory(Ronin.Lexicon.Variable.keyword);
            return variable;
        }

        internal static Constant Constant()
        {
            Constant constant = new();
            constant.SetMemory(Ronin.Lexicon.Constant.keyword);
            return constant;
        }

        internal static Reactive Reactive()
        {
            Reactive reactive = new();
            reactive.SetMemory(Ronin.Lexicon.Reactive.keyword);
            return reactive;
        }

        internal static Let Let()
        {
            Let let = new();
            let.SetMemory(Ronin.Lexicon.Let.keyword);
            return let;
        }

        internal static Compiled Compiled()
        {
            Compiled compiled = new();
            compiled.SetMemory(Ronin.Lexicon.Compiled.keyword);
            return compiled;
        }

        internal static Persistent Persistent()
        {
            Persistent persistent = new();
            persistent.SetMemory(Ronin.Lexicon.Persistent.keyword);
            return persistent;
        }

        internal static Shared Shared()
        {
            Shared shared = new();
            shared.SetMemory(Ronin.Lexicon.Shared.keyword);
            return shared;
        }

        internal static Optional Optional()
        {
            Optional optional = new();
            optional.SetMemory(Ronin.Lexicon.Optional.keyword);
            return optional;
        }

        internal static ForEach ForEach()
        {
            ForEach @foreach = new();
            @foreach.SetMemory(Ronin.Lexicon.ForEach.keyword);
            return @foreach;
        }

        internal static If If()
        {
            If @if = new();
            @if.SetMemory(Ronin.Lexicon.If.keyword);
            return @if;
        }

        internal static While While()
        {
            While @while = new();
            @while.SetMemory(Ronin.Lexicon.While.keyword);
            return @while;
        }

        internal static Ronin.Lexicon.Import Import()
        {
            Ronin.Lexicon.Import import = new();
            import.SetMemory(Ronin.Lexicon.Import.keyword);
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

public class AnalysisTests : ParsingTests
{
    internal static Identifier Identifier(params string[] names)
    {
        List<Word> words = new();
        foreach (var name in names)
        {
            Word word = new();
            word.SetMemory(name);
            words.Add(word);
        }
        
        return new Identifier
        { 
            Components = names.Select(name => new Identifier.Component { value = Name(name) }).ToList(),
            Source = words.ToArray() 
        };
    }

    internal static Identifier Identifier(params Identifier.Component[] components)
    {
        List<Token> tokens = new();
        foreach (var component in components)
        {
            foreach (var token in component.Source.Span) tokens.Add(token);
        }
        return new() { Components = components.ToList(), Source = tokens.ToArray() };
    }

    internal static Name Name(string name)
    {
        Word word = new();
        word.SetMemory(name);
        return new() { Source = new[] { word } };
    }

    internal static Identifier Words(string name) => Words(name.Split(new[] { ' ' }));

    internal static Identifier Words(params string[] names)
    {
        List<Word> words = new();
        foreach (var part in names)
        {
            Word word = new();
            word.SetMemory(part);
            words.Add(word);
        }
        List<Identifier.Component> components = new();
        foreach (var word in words)
        {
            components.Add(new Name { Source = new[] { word } });
        }
        return new Identifier
        {
            Components = components,
            Source = words.ToArray()
        };
    }

    internal static Reference Reference(params string[] words)
    {
        List<Reference.Component> components = new();
        foreach (var word in words)
        {
            Word token = new();
            token.SetMemory(word);
            Name name = new() { Source = new[] { token } };
            components.Add(name);
        }
        return Reference(components.ToArray());
    }

    internal static Reference Reference(params Reference.Component[] components) => new() { Components = components.ToList(), Source = components.SelectMany(component => component.value.Source.Span.ToArray()).ToList().AsMemory() };

    internal static Function.Call FunctionCall(params string[] words)
    {
        List<Reference.Component> components = new();
        foreach (var word in words)
        {
            Word token = new();
            token.SetMemory(word);
            Name name = new() { Source = new[] { token } };
            components.Add(name);
        }
        return FunctionCall(components.ToArray());
    }

    internal static Function.Call FunctionCall(params Reference.Component[] components) => new() 
    {
        Function = new Function.Unresolved 
        { 
            Reference = new() { Components = components.ToList() } 
        } 
    };
}