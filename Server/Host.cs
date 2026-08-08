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
        while (Read(input) is JsonObject message)
        {
            if (message["method"]?.GetValue<string>() is "exit") break;

            Handle(message, output);
        }

        return closing ? 0 : 1;
    }

    /// <summary>One message, framed by its length.</summary>
    private static JsonObject Read(Stream input)
    {
        var length = 0;

        for (var header = Line(input); header?.Length is not 0; header = Line(input))
        {
            if (header is null) return null;

            if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                length = int.Parse(header["Content-Length:".Length..].Trim(), CultureInfo.InvariantCulture);
        }

        var body = new byte[length];

        for (var read = 0; read < length;)
        {
            var taken = input.Read(body, read, length - read);

            if (taken is 0) return null;

            read += taken;
        }

        return Parsed(Encoding.UTF8.GetString(body));
    }

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

    private void Handle(JsonObject message, Stream output)
    {
        var id = message["id"];

        switch (message["method"]?.GetValue<string>())
        {
            case "initialize":
                Reply(output, id, new JsonObject
                {
                    ["capabilities"] = new JsonObject
                    {
                        ["textDocumentSync"] = 1,
                        ["hoverProvider"] = true,
                        ["codeActionProvider"] = true,
                    },
                });
                break;

            case "textDocument/didOpen":
            case "textDocument/didChange":
                Publish(message, output);
                break;

            case "textDocument/hover":
                Reply(output, id, Hovered(message));
                break;

            case "textDocument/codeAction":
                Reply(output, id, Actioned(message));
                break;

            case "shutdown":
                closing = true;
                Reply(output, id, null);
                break;

            default:
                if (id is not null) Reply(output, id, null);
                break;
        }
    }

    private void Publish(JsonObject message, Stream output)
    {
        var document = message["params"]["textDocument"];
        var uri = document["uri"].GetValue<string>();

        open[uri] = message["params"]["contentChanges"] is JsonArray changes
                  ? changes[0]["text"].GetValue<string>()
                  : document["text"].GetValue<string>();

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

    private JsonObject Hovered(JsonObject message)
    {
        var uri = message["params"]["textDocument"]["uri"].GetValue<string>();

        if (open.TryGetValue(uri, out var text) is false) return null;

        var position = message["params"]["position"];
        var at = new Place(position["line"].GetValue<int>(), position["character"].GetValue<int>());

        if (Language.Hover(new SourceText(text, uri), at) is not string reading) return null;

        return new JsonObject
        {
            ["contents"] = new JsonObject { ["kind"] = "plaintext", ["value"] = reading },
        };
    }

    private JsonArray Actioned(JsonObject message)
    {
        var uri = message["params"]["textDocument"]["uri"].GetValue<string>();

        if (open.TryGetValue(uri, out var text) is false) return [];

        var range = message["params"]["range"];
        Extent asked = new(Placed(range["start"]), Placed(range["end"]));

        JsonArray actions = [];

        foreach (var action in Language.Actions(new SourceText(text, uri), asked))
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

    private static Place Placed(JsonNode position)
        => new(position["line"].GetValue<int>(), position["character"].GetValue<int>());

    private static JsonObject Ranged(Extent extent)
        => new()
        {
            ["start"] = new JsonObject { ["line"] = extent.From.Line, ["character"] = extent.From.Character },
            ["end"] = new JsonObject { ["line"] = extent.To.Line, ["character"] = extent.To.Character },
        };

    private static void Reply(Stream output, JsonNode id, JsonNode result)
        => Send(output, new JsonObject
        {
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

    /// <summary>Whether the client has said it is finished.</summary>
    private bool closing;
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
