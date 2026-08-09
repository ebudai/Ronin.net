// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ronin.Server;

/// <summary>
///     The language server, over standard input and output.
/// </summary>
///
/// <remarks>
///     <para>
///     STREAMS rather than the console, which is the whole of what makes this
///     testable. It was excluded from coverage as "everything here reads bytes"
///     — true of the two lines that open a console, and a cover story for the
///     rest: an unconditional read loop that answered «shutdown» and then went
///     back to waiting, so a conforming client could not end the process it had
///     just been told was finished. Nothing was watching, because nothing could
///     be.
///     </para>
///     <para>
///     Hand written rather than a library, so «restore --locked-mode» keeps
///     meaning what it means. The protocol's framing is a «Content-Length»
///     header and a JSON body; the rest is a dictionary of method names. A
///     dependency to avoid forty lines is a dependency to update forever.
///     </para>
///     <para>
///     ONE EDITOR, MANY EDITORS. A plugin for one IDE would have to talk to a
///     compiler in another process anyway, which is this boundary without the
///     standard — so this is the plugin, for every editor that speaks it.
///     </para>
/// </remarks>
internal sealed class Host
{
    /// <summary>
    ///     Serves until the client says to stop, and answers with its status.
    /// </summary>
    ///
    /// <remarks>
    ///     The lifecycle the specification asks for: «shutdown» prepares, «exit»
    ///     ends, and ending WITHOUT having been shut down is a failure — a
    ///     client that exits unprepared has lost track of the conversation, and
    ///     saying so is the difference between a clean stop and a crash nobody
    ///     can tell from one.
    ///     <para>
    ///     End of input ends it too. A client that dies takes its half of the
    ///     pipe with it, and a server left blocking on a closed stream is a
    ///     process nobody owns.
    ///     </para>
    /// </remarks>
    public int Serve(Stream input, Stream output)
    {
        while (exiting is false && Read(input) is JsonObject message)
        {
            Handle(message, output);
        }

        return closing ? 0 : 1;
    }

    /// <summary>
    ///     A message's method, or nothing when it is absent or not a string.
    /// </summary>
    ///
    /// <remarks>
    ///     Read once, safely. «GetValue&lt;string&gt;()» on «"method": 7» threw an
    ///     «InvalidOperationException» straight out of the loop and took the
    ///     process out — a well-framed body that parsed as JSON, so the framing
    ///     guard never saw it. A method that is not a string is not a method the
    ///     server can route, and a client may send anything.
    /// </remarks>
    private static string Method(JsonObject message)
        => message["method"] is JsonValue value && value.TryGetValue(out string method) ? method : null;

    /// <summary>Whether an id, present, is a kind JSON-RPC allows for one.</summary>
    ///
    /// <remarks>
    ///     A string, a number, or null — a Boolean or a structure is none of
    ///     those. A message carrying one of those for its id is a request in shape
    ///     the server cannot answer to, not a request it may serve.
    /// </remarks>
    private static bool Named(JsonNode id)
        => id is null || id.GetValueKind() is JsonValueKind.String or JsonValueKind.Number;

    /// <summary>One message, framed by its length.</summary>
    private static JsonObject Read(Stream input)
    {
        var length = -1;

        for (var header = Line(input); header?.Length is not 0; header = Line(input))
        {
            if (header is null) return null;

            // TRY, not «Parse», because the number is the client's and a client
            // may send anything. «Content-Length: nope» threw a FormatException
            // and took the process out; «-1» and a value past «Int32» threw
            // OverflowException. A framing the server cannot trust is a stream it
            // cannot re-enter, so it ends the conversation — deliberately, with a
            // status, rather than as an unhandled exception.
            if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(header["Content-Length:".Length..].Trim(),
                                 NumberStyles.Integer,
                                 CultureInfo.InvariantCulture,
                                 out length) is false)
                    return null;
            }
        }

        // A LENGTH the server will act on: present, not negative, and under a
        // ceiling. A large in-range value would otherwise size a single
        // allocation from the wire, and the language's own limit is on lexemes,
        // which do not exist until after these bytes are read.
        if (length < 0 || length > MaxFrame) return null;

        var body = new byte[length];

        for (var read = 0; read < length;)
        {
            var taken = input.Read(body, read, length - read);

            if (taken is 0) return null;

            read += taken;
        }

        return Parsed(Encoding.UTF8.GetString(body));
    }

    /// <summary>The largest message body the server will allocate for.</summary>
    ///
    /// <remarks>
    ///     A megabyte is far past any real request and far short of a size a
    ///     client could use to make the server allocate its way out of memory.
    ///     The bound is on the frame because it is the first thing the wire
    ///     controls, before there is JSON or source to bound instead.
    /// </remarks>
    private const int MaxFrame = 1 << 20;

    /// <summary>A body, or nothing when it is not JSON at all.</summary>
    ///
    /// <remarks>
    ///     A malformed body is the client's fault and not this process's to die
    ///     of, but it is also not something to answer — there is no id to answer
    ///     to. Ending is the honest response: the framing held and the meaning
    ///     did not, and a stream whose meaning is in doubt cannot be re-entered.
    /// </remarks>
    private static JsonObject Parsed(string body)
    {
        try
        {
            return JsonNode.Parse(body) as JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static string Line(Stream input)
    {
        StringBuilder line = new();

        for (var read = input.ReadByte(); read >= 0; read = input.ReadByte())
        {
            if (read is '\n') return line.ToString().TrimEnd('\r');

            line.Append((char)read);
        }

        return null;
    }

    // The JSON-RPC error codes the server answers with. A request carries an id
    // and a client waiting on it, so every one it cannot serve is answered with
    // the code the protocol names for why — a null result said "here is your
    // answer, and it is nothing", which is a different and false thing.
    private const int InvalidRequest = -32600;
    private const int MethodNotFound = -32601;
    private const int InvalidParams = -32602;

    private void Handle(JsonObject message, Stream output)
    {
        var method = Method(message);

        // THE ENVELOPE, classified once, before any lifecycle or dispatch. A
        // REQUEST carries an id member — a string, a number, or null — and a
        // client waiting on an answer; a NOTIFICATION carries no id member and is
        // never answered. Reading the id's VALUE for its presence made an explicit
        // null look like an absent one, so a null-id request was dropped as a
        // notification; and an id of any other kind, a Boolean say, was taken for a
        // request and answered. Presence and kind are read apart now.
        var present = message.ContainsKey("id");
        var id = message["id"];
        var request = present && Named(id);

        // The version is part of what makes it a message at all. «1.0» or none was
        // processed as though it were «2.0», answering a client speaking a protocol
        // this one does not.
        if (Text(message, "jsonrpc") is not "2.0")
        {
            if (request) Fail(output, id, InvalidRequest, "not a JSON-RPC 2.0 message");

            return;
        }

        // An id present but of no allowed kind is a request in shape the server
        // cannot answer TO — the error carries a null id, because the one it was
        // sent is not one a response may echo.
        if (present && request is false)
        {
            Fail(output, null, InvalidRequest, "the id is not a string, a number, or null");

            return;
        }

        // «exit» stops the server in any state, but only as the notification the
        // specification says it is — checking the method text in the loop honoured
        // a wrong-version or id-carrying «exit» and terminated on a message that
        // was no valid exit at all. A request named «exit» is answered, not obeyed.
        if (method is "exit")
        {
            if (request) Fail(output, id, MethodNotFound, "«exit» is a notification, not a request");
            else exiting = true;

            return;
        }

        // AFTER shutdown, only «exit» is allowed. A request in this state is
        // refused; a notification is dropped. Processing either would act on a
        // conversation the client has said is over.
        if (closing)
        {
            if (request) Fail(output, id, InvalidRequest, "the server is shutting down");

            return;
        }

        // BEFORE initialize nothing else is meaningful, and «initialize» is the one
        // method that passes here to be handled below — as a request, since a
        // notification cannot complete a handshake a client is waiting on.
        if (initialized is false && method is not "initialize")
        {
            if (request) Fail(output, id, InvalidRequest, "the server is not initialized");

            return;
        }

        switch (method)
        {
            case "initialize" when request:
                // ONCE. A second initialize is a client that lost track of the
                // handshake, and answering it as the first would let two of them
                // disagree about what was negotiated.
                if (initialized) Fail(output, id, InvalidRequest, "already initialized");
                else Reply(output, id, Capabilities());

                initialized = true;
                break;

            case "textDocument/didOpen" when request is false:
            case "textDocument/didChange" when request is false:
                Publish(message, output);
                break;

            case "textDocument/didClose" when request is false:
                // The document is gone, so its text is not the server's to keep —
                // and a hover or an action over it afterwards has nothing to
                // recompute from, which is the point of removing it.
                if (Uri(message) is string closed) open.Remove(closed);
                break;

            case "textDocument/hover" when request:
                if (Uri(message) is not string hovered || AsPlace(Param(message, "position")) is not Place at)
                    Fail(output, id, InvalidParams, "the hover request names no document or position");
                else Reply(output, id, Hovered(hovered, at));
                break;

            case "textDocument/codeAction" when request:
                if (Uri(message) is not string acted || AsExtent(Param(message, "range")) is not Extent range)
                    Fail(output, id, InvalidParams, "the code-action request names no document or range");
                else Reply(output, id, Actioned(acted, range));
                break;

            case "shutdown" when request:
                closing = true;
                Reply(output, id, null);
                break;

            default:
                // A request the server cannot route — no method at all, or a method
                // that is not one of its requests — is refused with the code for
                // why; a notification it cannot route is dropped, because nobody is
                // waiting on it.
                if (request)
                    Fail(output, id,
                         method is null ? InvalidRequest : MethodNotFound,
                         method is null ? "the request names no method" : $"the method «{method}» is not supported");

                break;
        }
    }

    private static JsonObject Capabilities()
        => new()
        {
            ["capabilities"] = new JsonObject
            {
                // FULL, not incremental. Advertising «2» promised that later edits
                // arrive as ranged fragments, and the server replaced the whole
                // document with the first fragment — so a conforming client's
                // first edit left every hover, diagnostic, and action reading a
                // one-character file. This server recompiles the whole text on
                // every change regardless, so a delta would buy nothing and cost
                // a synchronisation to keep; «1» is what it actually does.
                ["textDocumentSync"] = 1,
                ["hoverProvider"] = true,
                ["codeActionProvider"] = true,
            },
        };

    private void Publish(JsonObject message, Stream output)
    {
        var uri = Uri(message);

        // «didOpen» carries the whole document under «textDocument.text»; a Full
        // «didChange» carries it as its one content change. A notification with
        // neither a uri to key by nor text to store has nothing to publish and
        // nobody waiting on it — indexing an absent «params» threw a
        // NullReferenceException out of the host, which a dropped notification
        // does not.
        var text = Text(First(Param(message, "contentChanges")), "text")
                ?? Text(Param(message, "textDocument"), "text");

        if (uri is null || text is null) return;

        open[uri] = text;

        JsonArray reported = [];

        foreach (var finding in Language.Diagnostics(new SourceText(open[uri], uri)))
        {
            reported.Add(new JsonObject
            {
                ["range"] = Ranged(finding.Extent),
                ["severity"] = 1,
                ["code"] = finding.Code,
                ["source"] = "ronin",
                ["message"] = finding.Message,
            });
        }

        Send(output, new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = "textDocument/publishDiagnostics",
            ["params"] = new JsonObject { ["uri"] = uri, ["diagnostics"] = reported },
        });
    }

    private JsonObject Hovered(string uri, Place at)
    {
        if (open.TryGetValue(uri, out var text) is false) return null;

        if (Language.Hover(new SourceText(text, uri), at) is not string reading) return null;

        return new JsonObject
        {
            ["contents"] = new JsonObject { ["kind"] = "plaintext", ["value"] = reading },
        };
    }

    private JsonArray Actioned(string uri, Extent range)
    {
        if (open.TryGetValue(uri, out var text) is false) return [];

        JsonArray actions = [];

        foreach (var action in Language.Actions(new SourceText(text, uri), range))
        {
            JsonArray edits = [];

            foreach (var edit in action.Edits)
            {
                edits.Add(new JsonObject
                {
                    // an insertion is an empty range at one place, with text
                    ["range"] = Ranged(new Extent(edit.At, edit.At)),
                    ["newText"] = edit.Text,
                });
            }

            actions.Add(new JsonObject
            {
                ["title"] = action.Title,
                ["kind"] = "quickfix",
                ["edit"] = new JsonObject
                {
                    ["changes"] = new JsonObject { [uri] = edits },
                },
            });
        }

        return actions;
    }

    private static JsonObject Ranged(Extent extent)
        => new()
        {
            ["start"] = new JsonObject { ["line"] = extent.From.Line, ["character"] = extent.From.Character },
            ["end"] = new JsonObject { ["line"] = extent.To.Line, ["character"] = extent.To.Character },
        };

    // ---- reading a client's message, which may be anything ----------------

    /// <summary>A field of a node, or nothing when the node is not an object.</summary>
    ///
    /// <remarks>
    ///     Every step into a client's message goes through here. «params»
    ///     «["textDocument"]["uri"]» indexed straight into whatever the client
    ///     sent, so a request missing «params» threw a NullReferenceException out
    ///     of the host — a well-framed body that parsed, so the framing guard did
    ///     not catch it. An absent field is nothing, not a fault to die of.
    /// </remarks>
    private static JsonNode At(JsonNode node, string key) => (node as JsonObject)?[key];

    /// <summary>The «params» member's field, since almost everything hangs off it.</summary>
    private static JsonNode Param(JsonObject message, string key) => At(At(message, "params"), key);

    private static string Text(JsonNode node, string key)
        => At(node, key) is JsonValue value && value.TryGetValue(out string text) ? text : null;

    private static int? Number(JsonNode node, string key)
        => At(node, key) is JsonValue value && value.TryGetValue(out int number) ? number : null;

    private static JsonNode First(JsonNode node) => node is JsonArray array && array.Count > 0 ? array[0] : null;

    /// <summary>The document uri a request or notification names, if it names one.</summary>
    private static string Uri(JsonObject message) => Text(Param(message, "textDocument"), "uri");

    private static Place? AsPlace(JsonNode position)
        => Number(position, "line") is int line && Number(position, "character") is int character
           ? new Place(line, character)
           : null;

    private static Extent? AsExtent(JsonNode range)
        => AsPlace(At(range, "start")) is Place start && AsPlace(At(range, "end")) is Place end
           ? new Extent(start, end)
           : null;

    /// <summary>Answers a request the server cannot serve, with the reason's code.</summary>
    ///
    /// <remarks>
    ///     The client is waiting on an answer; the error is the answer, and the
    ///     code says why — the lifecycle did not permit the request, or it named no
    ///     method, or a method the server does not have, or parameters it could not
    ///     read. The id is echoed, or null where the request carried an explicit
    ///     null or an id no response may name.
    /// </remarks>
    private static void Fail(Stream output, JsonNode id, int code, string reason)
        => Send(output, new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["error"] = new JsonObject { ["code"] = code, ["message"] = reason },
        });

    private static void Reply(Stream output, JsonNode id, JsonNode result)
        => Send(output, new JsonObject
        {
            // A reply answers a request, and echoes the id it carried — which the
            // specification permits to be null, though a client is unwise to send
            // one, so «null» rather than an assumption there is always a value.
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["result"] = result,
        });

    private static void Send(Stream output, JsonObject message)
    {
        var body = Encoding.UTF8.GetBytes(message.ToJsonString());

        output.Write(Encoding.UTF8.GetBytes($"Content-Length: {body.Length}\r\n\r\n"));
        output.Write(body);
        output.Flush();
    }

    /// <summary>
    ///     The documents the client has open, by uri.
    /// </summary>
    ///
    /// <remarks>
    ///     Per host rather than per process. It was static, which is invisible
    ///     with one server to a process and is what makes two of them in one test
    ///     run answer for each other.
    /// </remarks>
    private readonly Dictionary<string, string> open = [];

    /// <summary>Whether the client has completed the initialize handshake.</summary>
    private bool initialized;

    /// <summary>Whether the client has said it is finished.</summary>
    private bool closing;

    /// <summary>Whether the client has said to stop — «exit», which the loop obeys.</summary>
    private bool exiting;
}

/// <summary>
///     The process, which is the two lines that cannot be tested.
/// </summary>
///
/// <remarks>
///     Everything above takes streams and returns a status. What is left is
///     opening the console's pair and handing the status to the operating
///     system, which is the whole of what an exclusion should ever cover.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class Serving
{
    private static int Main()
    {
        using var input = Console.OpenStandardInput();
        using var output = Console.OpenStandardOutput();

        return new Host().Serve(input, output);
    }
}
