using System.Text.RegularExpressions;

namespace Ronin.Parser;
//TODO add Error and Warning classes
public class Context
{
    private readonly string sourcecode;
    private int cursor = 0;
    private int line = 0;
    private readonly Stack<int> bookmarks = new();

    public Context(FileInfo file) => sourcecode = Form.scopeopen + File.ReadAllText(file.FullName) + Form.scopeclose;

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

    internal void RemoveBookmark() => bookmarks.TryPop(out var _);

    internal bool IsAtEnd => cursor == sourcecode.Length;
}