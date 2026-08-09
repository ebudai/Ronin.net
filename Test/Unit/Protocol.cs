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

    /// <summary>A full-document «didChange», as a full-sync client sends one.</summary>
    private static string Changing(string uri, string text)
        => "{\"jsonrpc\":\"2.0\",\"method\":\"textDocument/didChange\",\"params\":{\"textDocument\":{\"uri\":\""
         + uri + "\"},\"contentChanges\":[{\"text\":\"" + text.Replace("\n", "\\n") + "\"}]}}";

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

    [Fact(DisplayName = "and exit is obeyed only as a valid notification, not on the method text")]
    public void AndExitIsObeyedOnlyAsAValidNotificationNotOnTheMethodText()
    {
        // «exit» stops the server, but only as the notification the specification
        // says it is. The loop broke on the method text alone, so a wrong-version
        // or id-carrying «exit» terminated on a message that was no valid exit —
        // and left a request after it unread and unanswered. Each is handled
        // without stopping, and the shutdown after it is read and answered.
        var (_, said) = Session(
            """{"jsonrpc":"1.0","method":"exit"}""",             // wrong version — a dropped notification
            """{"jsonrpc":"2.0","id":7,"method":"exit"}""",      // an id — a request, refused not obeyed
            """{"jsonrpc":"2.0","id":8,"method":"shutdown"}""");

        Assert.Contains("\"id\":7", said, StringComparison.Ordinal);
        Assert.Contains("\"code\":-32601", said, StringComparison.Ordinal);
        Assert.Contains("\"id\":8", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and an explicit null id is a request, while a Boolean id is not")]
    public void AndAnExplicitNullIdIsARequestWhileABooleanIdIsNot()
    {
        // Presence, not value. An initialize with «\"id\":null» is a request — the
        // id member is there and null is an id JSON-RPC allows — so it
        // initializes, which the shutdown answered after it proves; reading the
        // value for the presence dropped it as a notification and left the server
        // uninitialized.
        var (_, accepted) = Serving(
            """{"jsonrpc":"2.0","id":null,"method":"initialize","params":{}}""",
            """{"jsonrpc":"2.0","id":9,"method":"shutdown"}""");

        Assert.Contains("hoverProvider", accepted, StringComparison.Ordinal);
        Assert.Contains("\"id\":9", accepted, StringComparison.Ordinal);

        // A Boolean is no id JSON-RPC allows, so the request is refused with a null
        // id rather than answered with capabilities named «true».
        var (_, refused) = Serving("""{"jsonrpc":"2.0","id":true,"method":"initialize","params":{}}""");

        Assert.Contains("\"code\":-32600", refused, StringComparison.Ordinal);
        Assert.DoesNotContain("\"id\":true", refused, StringComparison.Ordinal);
        Assert.DoesNotContain("hoverProvider", refused, StringComparison.Ordinal);
    }

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

    [Fact(DisplayName = "and shutting down without an id is dropped, not answered")]
    public void AndShuttingDownWithoutAnIdIsDroppedNotAnswered()
    {
        // «shutdown» is a request; without an id it is a notification, and a
        // notification is never answered — the server used to reply with a null
        // id and, worse, transition to closing on it. Dropped, it does neither:
        // no reply, and the server is still running when the input ends, so the
        // end is the unshut failure it is.
        var (status, said) = Session("""{"jsonrpc":"2.0","method":"shutdown"}""");

        Assert.Equal(1, status);
        Assert.DoesNotContain("\"id\":null", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and a request method sent as a notification is dropped, not crashed on")]
    public void AndARequestMethodSentAsANotificationIsDroppedNotCrashedOn()
    {
        // «hover», «codeAction», and a second «initialize» without an id are
        // notifications of request methods. Refusing them called «Fail», which
        // cloned the id they do not have and threw a NullReferenceException out of
        // the host. They are dropped now — no reply, no crash — and the request
        // after them being answered is the proof the loop lived.
        var (_, said) = Session(
            """{"jsonrpc":"2.0","method":"textDocument/hover","params":{}}""",
            """{"jsonrpc":"2.0","method":"textDocument/codeAction","params":{}}""",
            """{"jsonrpc":"2.0","method":"initialize","params":{}}""",
            """{"jsonrpc":"2.0","id":5,"method":"shutdown"}""");

        Assert.Contains("\"id\":5", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and a valid notification of a request method is not answered")]
    public void AndAValidNotificationOfARequestMethodIsNotAnswered()
    {
        // A well-formed «hover» without an id is still a notification, and a
        // notification is never answered — not with a result, not with an error.
        // The handshake is the only reply, so its is the only frame on the wire.
        var (_, said) = Session(
            """{"jsonrpc":"2.0","method":"textDocument/hover","params":{"textDocument":{"uri":"file:///p.ron"},"position":{"line":0,"character":1}}}""");

        Assert.Equal(1, Occurrences(said, "Content-Length"));
    }

    /// <summary>How many times a marker appears in the traffic.</summary>
    private static int Occurrences(string said, string marker)
    {
        var count = 0;

        for (var at = said.IndexOf(marker, StringComparison.Ordinal); at >= 0;
             at = said.IndexOf(marker, at + 1, StringComparison.Ordinal))
            ++count;

        return count;
    }

    [Fact(DisplayName = "and initialize without an id does not initialize the server")]
    public void AndInitializeWithoutAnIdDoesNotInitializeTheServer()
    {
        // An initialize notification cannot complete a handshake a client is
        // waiting on, so it is dropped and the server stays uninitialized — which
        // the request after it, refused as not initialized, proves.
        var (_, said) = Serving(
            """{"jsonrpc":"2.0","method":"initialize","params":{}}""",
            """{"jsonrpc":"2.0","id":6,"method":"textDocument/hover","params":{}}""");

        Assert.Contains("\"id\":6", said, StringComparison.Ordinal);
        Assert.Contains("\"code\":-32600", said, StringComparison.Ordinal);
        Assert.DoesNotContain("hoverProvider", said, StringComparison.Ordinal);
    }

    [Theory(DisplayName = "and a message that is not JSON-RPC 2.0 is refused or dropped")]
    [InlineData("""{"jsonrpc":"1.0","id":7,"method":"shutdown"}""")]     // wrong version, a request
    [InlineData("""{"id":7,"method":"shutdown"}""")]                     // no version, a request
    public void AndAMessageThatIsNotJsonRpcTwoIsRefusedOrDropped(string request)
    {
        // The version is part of what makes it a message the server speaks.
        // Missing or wrong, it was processed as though it were «2.0»; now a
        // request saying so is refused, and — the notification case — a version-
        // less notification is dropped rather than acted on.
        var (_, refused) = Session(request);

        Assert.Contains("\"id\":7", refused, StringComparison.Ordinal);
        Assert.Contains("\"code\":-32600", refused, StringComparison.Ordinal);

        Assert.Empty(Serving("""{"jsonrpc":"1.0","method":"textDocument/didOpen","params":{"textDocument":{"uri":"file:///p.ron","text":"var a => Number;\n"}}}""").Said);
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

        // FULL, not incremental. «2» promised ranged fragments the server did not
        // apply — it replaced the whole document with the first one — so a
        // conforming client's first edit left every query reading a scrap. This
        // server recompiles the whole file on every change, so full is what it
        // does and «1» is what it may honestly advertise.
        Assert.Contains("\"textDocumentSync\":1", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and an unknown method is a method-not-found error, or a dropped notification")]
    public void AndAnUnknownMethodIsAMethodNotFoundErrorOrADroppedNotification()
    {
        // A request names a method the server does not have. The client is
        // waiting, so it is told, with the code the protocol names for exactly
        // this — a successful null said "here is your result, and it is nothing",
        // which is not true and not an answer to «who/knows».
        var (_, answered) = Session("""{"jsonrpc":"2.0","id":7,"method":"who/knows"}""");

        Assert.Contains("\"id\":7", answered, StringComparison.Ordinal);
        Assert.Contains("\"code\":-32601", answered, StringComparison.Ordinal);

        // A notification names one too, but nobody is waiting on it, so it is
        // dropped rather than answered — inventing a reply nobody expects.
        Assert.DoesNotContain("\"id\":7", Session("""{"jsonrpc":"2.0","method":"who/knows"}""").Said,
                              StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and a message that names no method is an invalid request, not a crash")]
    public void AndAMessageThatNamesNoMethodIsAnInvalidRequestNotACrash()
    {
        // Well framed and valid JSON, but not a request the server can route: it
        // names no method. With an id a client is waiting, and the invalid-request
        // error is its answer — reaching for a method that is not there ended the
        // session over a client's slip.
        var (_, said) = Session("""{"jsonrpc":"2.0","id":9}""");

        Assert.Contains("\"id\":9", said, StringComparison.Ordinal);
        Assert.Contains("\"code\":-32600", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and a method that is not a string does not escape the host")]
    public void AndAMethodThatIsNotAStringDoesNotEscapeTheHost()
    {
        // «"method": 7» is valid JSON and well framed, so the framing guard never
        // saw it — and reading it as a string threw an InvalidOperationException
        // straight out of the loop. A method that is not a string is no method the
        // server can route; with an id, the client is told so and the loop lives.
        var (_, said) = Session("""{"jsonrpc":"2.0","id":1,"method":7}""",
                                """{"jsonrpc":"2.0","id":2,"method":"shutdown"}""");

        Assert.Contains("\"id\":1", said, StringComparison.Ordinal);
        Assert.Contains("\"code\":-32600", said, StringComparison.Ordinal);

        // the loop survived it — the shutdown after was still answered
        Assert.Contains("\"id\":2", said, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "and a notification missing its params is dropped, not crashed on")]
    public void AndANotificationMissingItsParamsIsDroppedNotCrashedOn()
    {
        // «textDocument/didOpen» with no «params» reached into an absent object
        // and threw a NullReferenceException out of the host — a well-framed body
        // that parsed. A notification the server cannot read has nothing to
        // publish and nobody waiting, so it is dropped, and the request after it
        // being answered is the proof the conversation went on.
        var (_, said) = Session("""{"jsonrpc":"2.0","method":"textDocument/didOpen"}""",
                                """{"jsonrpc":"2.0","id":3,"method":"shutdown"}""");

        Assert.Contains("\"id\":3", said, StringComparison.Ordinal);
    }

    [Theory(DisplayName = "and a request whose params cannot be read is an invalid-params error")]
    // a hover missing its position, and one missing its document
    [InlineData("""{"jsonrpc":"2.0","id":3,"method":"textDocument/hover","params":{"textDocument":{"uri":"file:///p.ron"}}}""")]
    [InlineData("""{"jsonrpc":"2.0","id":3,"method":"textDocument/hover","params":{"position":{"line":0,"character":0}}}""")]
    // a code action missing its range, missing the range's end, and missing its document
    [InlineData("""{"jsonrpc":"2.0","id":3,"method":"textDocument/codeAction","params":{"textDocument":{"uri":"file:///p.ron"}}}""")]
    [InlineData("""{"jsonrpc":"2.0","id":3,"method":"textDocument/codeAction","params":{"textDocument":{"uri":"file:///p.ron"},"range":{"start":{"line":0,"character":0}}}}""")]
    [InlineData("""{"jsonrpc":"2.0","id":3,"method":"textDocument/codeAction","params":{"range":{"start":{"line":0,"character":0},"end":{"line":0,"character":1}}}}""")]
    public void AndARequestWhoseParamsCannotBeReadIsAnInvalidParamsError(string request)
    {
        // A hover or a code action whose target the server cannot read — no
        // position, no range, half a range, or no document. The client is waiting
        // either way, and reaching for the absent field threw; the invalid-params
        // error is the answer. Every field is validated on the way in, so a
        // request short one is refused rather than dereferenced.
        var (_, said) = Session(Opening("file:///p.ron", "var a => Number;\n"), request);

        Assert.Contains("\"id\":3", said, StringComparison.Ordinal);
        Assert.Contains("\"code\":-32602", said, StringComparison.Ordinal);
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

    [Fact(DisplayName = "and after a change, a query reads the changed text, not the opened one")]
    public void AndAfterAChangeAQueryReadsTheChangedTextNotTheOpenedOne()
    {
        // The point of synchronisation, and the one finding 2 broke: an edit lands
        // and the next query reads the edited file. The server advertised
        // incremental sync and then replaced the document with the first fragment,
        // so a conforming client's first edit left hover reading a scrap. Full
        // sync sends the whole document, and a hover after it must see it.
        var (_, said) = Session(
            Opening("file:///p.ron", "var a => Number;\n"),
            Changing("file:///p.ron", "function send (x => Number) to (y => Number) { return x; }\n"
                                    + "var a => Number;\nvar r = send a to a;\n"),
            """
            {"jsonrpc":"2.0","id":2,"method":"textDocument/hover","params":{"textDocument":{
            "uri":"file:///p.ron"},"position":{"line":2,"character":9}}}
            """.ReplaceLineEndings(string.Empty));

        // the reading from the CHANGED document — a server still holding the
        // opened one-line file would resolve nothing here. Escaped on the wire,
        // the guillemets going out as ««» and «»».
        Assert.Contains(@"send \u00ABa\u00BB to \u00ABa\u00BB", said, StringComparison.Ordinal);
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
