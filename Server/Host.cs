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
///     EXCLUDED from coverage, like the compiler's own entry point and for the
///     same reason: everything here reads bytes, writes bytes, or looks a method
///     name up in a switch. What it does with a file is <see cref="Language"/>,
///     which is a function from source text to an answer and is tested as one.
///     Testing this would mean testing «Console.OpenStandardInput».
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
[ExcludeFromCodeCoverage]
internal static class Host
{
    private static readonly Dictionary<string, string> Open = [];

    private static void Main()
    {
        using var input = Console.OpenStandardInput();
        using var output = Console.OpenStandardOutput();

        while (Read(input) is JsonObject message) Handle(message, output);
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

        return JsonNode.Parse(Encoding.UTF8.GetString(body)) as JsonObject;
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

    private static void Handle(JsonObject message, Stream output)
    {
        var method = message["method"]?.GetValue<string>();
        var id = message["id"];

        switch (method)
        {
            case "initialize":
                Reply(output, id, new JsonObject
                {
                    ["capabilities"] = new JsonObject
                    {
                        ["textDocumentSync"] = 1,
                        ["hoverProvider"] = true,
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

            case "shutdown":
                Reply(output, id, null);
                break;

            default:
                if (id is not null) Reply(output, id, null);
                break;
        }
    }

    private static void Publish(JsonObject message, Stream output)
    {
        var document = message["params"]["textDocument"];
        var uri = document["uri"].GetValue<string>();

        Open[uri] = message["params"]["contentChanges"] is JsonArray changes
                  ? changes[0]["text"].GetValue<string>()
                  : document["text"].GetValue<string>();

        JsonArray reported = [];

        foreach (var finding in Language.Diagnostics(new SourceText(Open[uri], uri)))
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

    private static JsonObject Hovered(JsonObject message)
    {
        var uri = message["params"]["textDocument"]["uri"].GetValue<string>();

        if (Open.TryGetValue(uri, out var text) is false) return null;

        var position = message["params"]["position"];
        var at = new Place(position["line"].GetValue<int>(), position["character"].GetValue<int>());

        if (Language.Hover(new SourceText(text, uri), at) is not string reading) return null;

        return new JsonObject
        {
            ["contents"] = new JsonObject { ["kind"] = "plaintext", ["value"] = reading },
        };
    }

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
}
