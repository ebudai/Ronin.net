// Copyright © 2026 Eric Budai

using Ronin.Server;
using System.IO;
using System.Text;

namespace Unit;

/// <summary>
///     The language server's conversation, at the byte boundary.
/// </summary>
///
/// <remarks>
///     <para>
///     These are the tests an exclusion was standing in for. «Everything here
///     reads bytes» was true of the two lines that open a console and a cover
///     story for the rest — the loop answered «shutdown» and went back to
///     waiting, so a conforming client could not end the process it had just
///     been told was finished, and nothing was watching because nothing could
///     be.
///     </para>
///     <para>
///     Streams rather than a process, so every case here is deterministic and
///     none of them needs a timeout to decide it.
///     </para>
/// </remarks>
[Trait(nameof(Host), null)]
public class Protocol
{
    /// <summary>One framed message, as a client would send it.</summary>
    private static string Framed(string body)
        => $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}";

    /// <summary>What the server said, and how it ended.</summary>
    private static (int Status, string Said) Serving(params string[] messages)
    {
        using MemoryStream input = new(Encoding.UTF8.GetBytes(string.Concat(messages.Select(Framed))));
        using MemoryStream output = new();

        var status = new Host().Serve(input, output);

        return (status, Encoding.UTF8.GetString(output.ToArray()));
    }

    /// <summary>
    ///     A conversation that has completed the initialize handshake first.
    /// </summary>
    ///
    /// <remarks>
    ///     Most requests are only meaningful after initialize, and the server now
    ///     says so — so the functional cases send it first and read the answer
    ///     that follows the capability reply. The lifecycle cases do not, because
    ///     their subject IS the handshake.
    /// </remarks>
    private static (int Status, string Said) Session(params string[] messages)
        => Serving([Handshake, .. messages]);

    private const string Handshake = """{"jsonrpc":"2.0","id":0,"method":"initialize","params":{}}""";

    /// <summary>A «didOpen» notification for a file with some text.</summary>
    private static string Opening(string uri, string text)
        => "{\"jsonrpc\":\"2.0\",\"method\":\"textDocument/didOpen\",\"params\":{\"textDocument\":{"
         + $"\"uri\":\"{uri}\",\"text\":\"{text.Replace("\n", "\\n")}\"}}}}}}";

    private const string Colliding =
        "function send (x => Number) { return x; }\n"
      + "function send (x => Number) to (y => Number) { return x; }\n"
      + "var a to b => Number;\nvar a => Number;\nvar b => Number;\n"
      + "var result = send a to b;\n";

    // ---- lifecycle ---------------------------------------------------------

    [Fact(DisplayName = "shutdown then exit ends the server, successfully")]
    public void ShutdownThenExitEndsTheServerSuccessfully()
    {
        // «shutdown» prepares, «exit» ends. Before this, «exit» fell through the
        // default path, wrote nothing because it carries no id, and the server
        // went back to blocking on a client that had already gone. A message
        // after the exit is the whole test: without it the stream ends where the
        // exit does, and "the server stopped" cannot be told from "the input ran
        // out".
        var (status, said) = Session("""{"jsonrpc":"2.0","id":1,"method":"shutdown"}""",
                                     """{"jsonrpc":"2.0","method":"exit"}""",
                                     """{"jsonrpc":"2.0","id":9,"method":"shutdown"}""");

        Assert.Equal(0, status);

        // it answered the first shutdown, which tells a client the server agreed
        Assert.Contains("\"id\":1", said, StringComparison.Ordinal);

        // and never saw the message after exit, because it had already gone
        Assert.DoesNotContain("\"id\":9", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and exiting without shutting down is a failure")]
    public void AndExitingWithoutShuttingDownIsAFailure()
        // A client that exits unprepared has lost track of the conversation.
        // Saying so is the difference between a clean stop and a crash nobody can
        // tell from one — the status is the only place it can be said, since by
        // then there is nobody to send a message to.
        => Assert.Equal(1, Session("""{"jsonrpc":"2.0","method":"exit"}""").Status);

    [Fact(DisplayName = "and so is the client simply going away")]
    public void AndSoIsTheClientSimplyGoingAway()
        // End of input with no shutdown: a client that died takes its half of the
        // pipe with it, and a server left blocking on a closed stream is a
        // process nobody owns.
        => Assert.Equal(1, Serving().Status);

    [Fact(DisplayName = "a request before initialize is refused, a notification dropped")]
    public void ARequestBeforeInitializeIsRefusedANotificationDropped()
    {
        // Nothing the client sends means anything until the handshake it is
        // waiting on has happened. A request gets the error the specification
        // names; a notification, with nobody waiting, is dropped.
        var (_, refused) = Serving("""{"jsonrpc":"2.0","id":3,"method":"textDocument/hover","params":{}}""");

        Assert.Contains("\"id\":3", refused, StringComparison.Ordinal);
        Assert.Contains("\"code\":-32600", refused, StringComparison.Ordinal);

        Assert.Equal(string.Empty, Serving("""{"jsonrpc":"2.0","method":"textDocument/didOpen","params":{}}""").Said);
    }

    [Fact(DisplayName = "and initialize a second time is refused")]
    public void AndInitializeASecondTimeIsRefused()
    {
        // A second initialize is a client that lost track of the handshake, and
        // answering it as though it were the first would let two of them disagree
        // about what was negotiated.
        var (_, said) = Session("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""");

        Assert.Contains("\"id\":1", said, StringComparison.Ordinal);
        Assert.Contains("\"code\":-32600", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and after shutdown a request is refused and a notification dropped")]
    public void AndAfterShutdownARequestIsRefusedAndANotificationDropped()
    {
        // After shutdown only «exit» is allowed. A request is answered with the
        // error, because the client is still waiting on one; a notification, with
        // nobody waiting, is dropped in this state just as before initialize.
        var (_, said) = Session("""{"jsonrpc":"2.0","id":1,"method":"shutdown"}""",
                                """{"jsonrpc":"2.0","method":"textDocument/didClose","params":{"textDocument":{"uri":"file:///p.ron"}}}""",
                                """{"jsonrpc":"2.0","id":2,"method":"textDocument/hover","params":{}}""");

        Assert.Contains("\"id\":2", said, StringComparison.Ordinal);
        Assert.Contains("\"code\":-32600", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and shutting down without an id is still answered")]
    public void AndShuttingDownWithoutAnIdIsStillAnswered()
    {
        // «shutdown» is a request and carries one, but a client may send anything.
        // The reply names a null id rather than skipping the write, and having
        // shut down, the server ends cleanly when the input runs out.
        var (status, said) = Session("""{"jsonrpc":"2.0","method":"shutdown"}""");

        Assert.Equal(0, status);
        Assert.Contains("\"id\":null", said, StringComparison.Ordinal);
    }

    // ---- framing -----------------------------------------------------------

    [Fact(DisplayName = "a body that is not JSON ends it rather than being skipped")]
    public void ABodyThatIsNotJsonEndsItRatherThanBeingSkipped()
    {
        // No id to answer to and no way back: a stream whose meaning is in doubt
        // cannot be re-entered, because the next byte is in the middle of
        // something. Ending is honest; throwing would take the process out over a
        // client's mistake.
        var (status, said) = Session("""{"jsonrpc":"2.0","id":1,"method":"shutdown"}""", "{ not json");

        Assert.Equal(0, status);
        Assert.Contains("\"id\":1", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and a header block that stops early ends the conversation")]
    public void AndAHeaderBlockThatStopsEarlyEndsTheConversation()
    {
        // Framing lost mid-header. Nothing to resynchronise to, so the only safe
        // move is to stop — and to say the client did not end properly.
        using MemoryStream input = new(Encoding.UTF8.GetBytes("Content-Length: 40\r\n"));
        using MemoryStream output = new();

        Assert.Equal(1, new Host().Serve(input, output));
    }

    [Fact(DisplayName = "and a body shorter than its header ends it too")]
    public void AndABodyShorterThanItsHeaderEndsItToo()
    {
        // The header promised more than the stream had. Reading on would block
        // forever on a client that has nothing left to send.
        using MemoryStream input = new(Encoding.UTF8.GetBytes("Content-Length: 400\r\n\r\n{\"a\":1}"));
        using MemoryStream output = new();

        Assert.Equal(1, new Host().Serve(input, output));
    }

    [Theory(DisplayName = "and a length that is not a usable number ends it rather than crashing")]
    [InlineData("nope")]                    // not a number — was a FormatException
    [InlineData("-1")]                      // negative — a byte array cannot have it
    [InlineData("99999999999999999999")]    // past Int32 — was an OverflowException
    [InlineData("104857600")]               // in range but past the frame ceiling
    public void AndALengthThatIsNotAUsableNumberEndsItRatherThanCrashing(string length)
    {
        // The length is the client's, and a client may send anything. Parsing it
        // with «Parse» threw and took the process out — a framing the server
        // cannot trust is a stream it cannot re-enter, so it ends deliberately
        // with a status, and never allocates a body sized from an untrusted
        // number.
        using MemoryStream input = new(Encoding.UTF8.GetBytes($"Content-Length: {length}\r\n\r\nx"));
        using MemoryStream output = new();

        Assert.Equal(1, new Host().Serve(input, output));
    }

    // ---- functional --------------------------------------------------------

    [Fact(DisplayName = "initialize says what the server can do")]
    public void InitializeSaysWhatTheServerCanDo()
    {
        var (_, said) = Serving(Handshake);

        Assert.Contains("\"hoverProvider\":true", said, StringComparison.Ordinal);
        Assert.Contains("\"codeActionProvider\":true", said, StringComparison.Ordinal);
        Assert.Contains("Content-Length:", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and an unknown request is answered rather than ignored")]
    public void AndAnUnknownRequestIsAnsweredRatherThanIgnored()
    {
        // A request has an id and a client waiting on it, so an unknown one is
        // answered with a null result. A notification does not, and answering it
        // would invent a message nobody expects.
        Assert.Contains("\"id\":7", Session("""{"jsonrpc":"2.0","id":7,"method":"who/knows"}""").Said,
                        StringComparison.Ordinal);

        Assert.DoesNotContain("\"id\":7", Session("""{"jsonrpc":"2.0","method":"who/knows"}""").Said,
                              StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and a message with no method at all is ignored, not crashed on")]
    public void AndAMessageWithNoMethodAtAllIsIgnoredNotCrashedOn()
    {
        // Well framed, valid JSON, and not a request or a notification. Reaching
        // into it for a method that is not there would end the session over a
        // client's slip.
        var (_, said) = Session("""{"jsonrpc":"2.0","id":9}""");

        Assert.Contains("\"id\":9", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "opening a document publishes what is wrong with it")]
    public void OpeningADocumentPublishesWhatIsWrongWithIt()
    {
        var (_, said) = Session(Opening("file:///p.ron", "var true => Number;\n"));

        Assert.Contains("publishDiagnostics", said, StringComparison.Ordinal);
        Assert.Contains("\"code\":\"Supplied\"", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and changing it republishes against the new text")]
    public void AndChangingItRepublishesAgainstTheNewText()
    {
        // The second publish must have nothing in it. A server that answered from
        // the text it was opened with would look right until someone fixed
        // something.
        var (_, said) = Session(Opening("file:///p.ron", "var true => Number;\n"),
                                """
            {"jsonrpc":"2.0","method":"textDocument/didChange","params":{"textDocument":{
            "uri":"file:///p.ron"},"contentChanges":[{"text":"var truth be told => Number;\n"}]}}
            """.ReplaceLineEndings(string.Empty));

        Assert.EndsWith("\"diagnostics\":[]}}", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and closing it forgets it, so a query afterwards has nothing to answer from")]
    public void AndClosingItForgetsItSoAQueryAfterwardsHasNothingToAnswerFrom()
    {
        // A closed document is not the server's to keep. Before this, «open»
        // retained the text forever, so an action over a file the editor had
        // closed still answered from a stale copy.
        var (_, said) = Session(Opening("file:///p.ron", Colliding),
                                """{"jsonrpc":"2.0","method":"textDocument/didClose","params":{"textDocument":{"uri":"file:///p.ron"}}}""",
                                """
            {"jsonrpc":"2.0","id":8,"method":"textDocument/codeAction","params":{"textDocument":{
            "uri":"file:///p.ron"},"range":{"start":{"line":5,"character":13},"end":{"line":5,"character":24}}}}
            """.ReplaceLineEndings(string.Empty));

        Assert.Contains("\"id\":8,\"result\":[]", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "hovering shows the reading, and says nothing about a file it has not seen")]
    public void HoveringShowsTheReadingAndSaysNothingAboutAFileItHasNotSeen()
    {
        const string Hover = """
            {"jsonrpc":"2.0","id":2,"method":"textDocument/hover","params":{"textDocument":{
            "uri":"file:///p.ron"},"position":{"line":2,"character":14}}}
            """;

        var (_, said) = Session(
            Opening("file:///p.ron",
                    "function send (x => Number) to (y => Number) { return x; }\nvar a => Number;\nvar r = send a to a;\n"),
            Hover.ReplaceLineEndings(string.Empty));

        // ESCAPED on the wire: the guillemets that mark a name go out as «\u00AB»
        // and «\u00BB», because that is what the encoder does with anything
        // outside ASCII. An editor decodes them; a person grepping the traffic
        // will not find the character they were looking for.
        Assert.Contains(@"send \u00ABa\u00BB to \u00ABa\u00BB", said, StringComparison.Ordinal);

        // Nothing open, so nothing to say — «null» rather than an empty box over
        // every space, which is worse than saying nothing at all.
        Assert.Contains("\"result\":null", Session(Hover.ReplaceLineEndings(string.Empty)).Said,
                        StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and hovering where there is no reading says so")]
    public void AndHoveringWhereThereIsNoReadingSaysSo()
        // An open document, a position inside it, and nothing resolved there —
        // «var» is a declaration and has no expression to read. Null rather than
        // an empty box.
        => Assert.Contains("\"result\":null",
                           Session(Opening("file:///p.ron", "var a => Number;\n"),
                                   """
            {"jsonrpc":"2.0","id":3,"method":"textDocument/hover","params":{"textDocument":{
            "uri":"file:///p.ron"},"position":{"line":0,"character":1}}}
            """.ReplaceLineEndings(string.Empty)).Said,
                           StringComparison.Ordinal);

    [Fact(DisplayName = "a code action turns a repair into a workspace edit an editor can apply")]
    public void ACodeActionTurnsARepairIntoAWorkspaceEditAnEditorCanApply()
    {
        // The whole promise made selectable at the wire: open an ambiguous file,
        // ask for the actions over the ambiguous statement, and get bracketings
        // as concrete edits. This is «codeActionProvider» made good.
        var (_, said) = Session(Opening("file:///p.ron", Colliding),
                                """
            {"jsonrpc":"2.0","id":4,"method":"textDocument/codeAction","params":{"textDocument":{
            "uri":"file:///p.ron"},"range":{"start":{"line":5,"character":13},"end":{"line":5,"character":24}}}}
            """.ReplaceLineEndings(string.Empty));

        Assert.Contains("\"id\":4", said, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"quickfix\"", said, StringComparison.Ordinal);
        Assert.Contains("\"newText\":\"(\"", said, StringComparison.Ordinal);
        Assert.Contains("\"changes\"", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and a code action for a document it has not opened is empty")]
    public void AndACodeActionForADocumentItHasNotOpenedIsEmpty()
        // Nothing open, nothing to recompute, no actions — «result»: an empty
        // array, not an error, because asking about a file the server never saw
        // is a race, not a fault.
        => Assert.Contains("\"result\":[]",
                           Session("""
            {"jsonrpc":"2.0","id":5,"method":"textDocument/codeAction","params":{"textDocument":{
            "uri":"file:///gone.ron"},"range":{"start":{"line":0,"character":0},"end":{"line":0,"character":1}}}}
            """.ReplaceLineEndings(string.Empty)).Said,
                           StringComparison.Ordinal);
}
