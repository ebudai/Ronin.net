using Ronin.Grammar;
using Ronin.Lexicon;

namespace Test;

internal static class Utility
{
    internal static void SetMemory<T>(this T value, string args) where T : Token => typeof(T).GetProperty(nameof(Token.Memory)).SetMethod.Invoke(value, new object[] { args.AsMemory() });

    internal static Token AsLinkedList(this List<Token> tokens)
    {
        if (tokens.Count is 0) return null;
        var list = tokens[0];
        foreach (var token in tokens.Skip(1))
        {
            list = list.Append(token);
        }
        return tokens[0];
    }

    internal static Token[] ToArray(this Token token)
    {
        if (token is null) return null;
        List<Token> tokens = new();
        while (token is not null)
        {
            tokens.Add(token);
            token = token.Next as Token;
        }
        return tokens.ToArray();
    }
}

public class ParsingTests
{
    protected ParsingTests() { }

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

    internal static Open.Parenthesis StartValues()
    {
        Open.Parenthesis startValues = new();
        startValues.SetMemory(Open.Parenthesis.symbol.ToString());
        return startValues;
    }

    internal static Close.Parenthesis EndValues()
    {
        Close.Parenthesis endValues = new();
        endValues.SetMemory(Close.Parenthesis.symbol.ToString());
        return endValues;
    }

    internal static Open.SquareBracket StartIndexer()
    {
        Open.SquareBracket startIndexer = new();
        startIndexer.SetMemory(Open.SquareBracket.symbol.ToString());
        return startIndexer;
    }

    internal static Close.SquareBracket EndIndexer()
    {
        Close.SquareBracket endIndexer = new();
        endIndexer.SetMemory(Close.SquareBracket.symbol.ToString());
        return endIndexer;
    }

    internal static Open.Brace StartScope()
    {
        Open.Brace startScope = new();
        startScope.SetMemory(Open.Brace.symbol.ToString());
        return startScope;
    }

    internal static Close.Brace EndScope()
    {
        Close.Brace endScope = new();
        endScope.SetMemory(Close.Brace.symbol.ToString());
        return endScope;
    }

    internal static TextDelimiter TextDelimiter()
    {
        TextDelimiter textDelimiter = new();
        textDelimiter.SetMemory(Ronin.Lexicon.TextDelimiter.symbol.ToString());
        return textDelimiter;
    }

    internal static Symbol.Special.Interval Range()
    {
        Symbol.Special.Interval range = new();
        range.SetMemory(Ronin.Lexicon.Symbol.Special.Interval.symbol);
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

        internal static Ronin.Lexicon.Type Type()
        {
            Ronin.Lexicon.Type datatype = new();
            datatype.SetMemory(Ronin.Lexicon.Type.keyword);
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

        internal static Hidden Hidden()
        {
            Hidden hidden = new();
            hidden.SetMemory(Ronin.Lexicon.Hidden.keyword);
            return hidden;
        }

        internal static Global Shared()
        {
            Global shared = new();
            shared.SetMemory(Ronin.Lexicon.Global.keyword);
            return shared;
        }

        internal static Optional Optional()
        {
            Optional optional = new();
            optional.SetMemory(Ronin.Lexicon.Optional.keyword);
            return optional;
        }

        internal static Iterate Iterate()
        {
            Iterate iterate = new();
            iterate.SetMemory(Ronin.Lexicon.Iterate.keyword);
            return iterate;
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

        internal static When When()
        {
            When when = new();
            when.SetMemory(Ronin.Lexicon.When.keyword);
            return when;
        }

        internal static Changing Changing()
        {
            Changing changing = new();
            changing.SetMemory(Ronin.Lexicon.Changing.keyword);
            return changing;
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

        List<Identifier.Component> components = new();
        foreach (var name in names) components.Add(Name(name));
        return new Identifier { components };
    }

    internal static Identifier Identifier(params Identifier.Component[] components) => new() { components };

    internal static Name Name(params string[] names)
    {
        List<Word> words = new();
        foreach (var name in names)
        {
            Word word = new();
            word.SetMemory(name);
            words.Add(word);
        }
        return new() { Tokens = words.ToArray() };
    }

    internal static Identifier Words(string name) => Words(name.Split(' '));

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
            components.Add(new Name { Tokens = new[] { word } });
        }
        return new Identifier { components };
    }

    internal static Reference Reference(params string[] words)
    {
        List<Reference.Component> components = new();
        foreach (var word in words)
        {
            Word token = new();
            token.SetMemory(word);
            Name name = new() { Tokens = new[] { token } };
            components.Add(name);
        }
        return Reference(components.ToArray());
    }

    internal static Reference Reference(params Reference.Component[] components) => new() { components };

    internal static Member UnresolvedReference(params string[] words)
    {
        List<Reference.Component> components = new();
        foreach (var word in words)
        {
            Word token = new();
            token.SetMemory(word);
            Name name = new() { Tokens = new[] { token } };
            components.Add(name);
        }
        return UnresolvedReference(components.ToArray());
    }

    internal static Member UnresolvedReference(params Reference.Component[] components) => new Member.Unresolved { Reference = new() { components } };
}