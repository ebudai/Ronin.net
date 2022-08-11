using System.Text.RegularExpressions;

namespace Ronin.Parser;

internal class Context
{
    private readonly string sourcecode;
    private int cursor = 0;
    private int line = 0;
    private readonly Stack<int> bookmarks = new();

    internal Context(FileInfo file) => sourcecode = Syntax.scopeopen + File.ReadAllText(file.FullName) + Syntax.scopeclose;

    internal string Lex(Regex regex)
    {
        var match = regex.Match(sourcecode, cursor);
        if (!match.Success || match.Index != cursor) return null;
        cursor += match.Length;
        line += match.Value.Count(static c => c is '\n');
        return match.Value;
    }

    internal void Retreat(int amount) => cursor -= amount;

    internal void AddBookmark() => bookmarks.Push(cursor);

    internal void RetreatToLastBookmark() => cursor = bookmarks.Pop();

    internal void RemoveBookmark() => bookmarks.Pop();

    internal bool IsAtEnd => cursor == sourcecode.Length;

    /*internal class ParseException : Exception
    {
        public ParseException(Context context, Exception inner = null)
                : base(CreateMessage(parser), inner) { }

        public ParseException(string message, Context context, Exception inner = null)
            : base(message + Environment.NewLine + CreateMessage(parser), inner) { }

        private static string CreateMessage(Context context, Exception inner = null)
        {
            var message = $"line {parser.line}: {parser.sourcecode[parser.cursor..][..30]}";
            if (inner is not null) message += $" caused by {inner.Message}";
            return message;
        }
    }*/
}