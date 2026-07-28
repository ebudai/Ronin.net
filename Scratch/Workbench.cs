// Copyright © 2026 Eric Budai

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Ronin.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ronin.Scratch;

/// <summary>
///     A textarea with the completion list permanently beside it, and every line
///     resolved as you type.
/// </summary>
///
/// <remarks>
///     <para>
///     This is disposable. It exists to answer one question — whether Ronin can
///     be written by a person who does not already hold the whole scope in their
///     head — and the answer is worth having before an interpreter is built on
///     the syntax. Nothing here is meant to survive into the real host.
///     </para>
///     <para>
///     Scope is declared by hand rather than parsed out of the source, because
///     the point is to vary the scope freely and watch what it does to the same
///     statements. A name per line on the left, a pattern per line under it with
///     <c>_</c> for each hole.
///     </para>
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class Workbench : Window
{
    public Workbench()
    {
        Title = "Ronin scratch";
        Width = 1200;
        Height = 800;

        names = Editor(80);
        patterns = Editor(80);
        source = Editor(0);

        names.Text = "base\nbase price\nprice\ntax";
        patterns.Text = "compute total for _";
        source.Text = "base price + tax\ncompute total for base price\ncompute total for base price + tax";

        candidates.FontFamily = monospace;
        readings.FontFamily = monospace;

        problems.TextWrapping = TextWrapping.Wrap;
        problems.Foreground = Brushes.IndianRed;
        problems.Margin = new Thickness(0, 4, 0, 8);

        Content = Layout();

        foreach (var editor in new[] { names, patterns, source })
        {
            editor.TextChanged += (_, _) => Refresh();
        }

        // caret movement changes what is being completed just as much as typing
        source.KeyUp += (_, _) => Refresh();
        source.PointerReleased += (_, _) => Refresh();

        Refresh();
    }

    private Control Layout()
    {
        Grid grid = new()
        {
            Margin = new Thickness(8),
            ColumnDefinitions = new ColumnDefinitions("*,8,380"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,*"),
        };

        Add(grid, Caption("names"), 0, 0);
        Add(grid, names, 1, 0);
        Add(grid, Caption("patterns — one per line, _ for each hole"), 2, 0);
        Add(grid, patterns, 3, 0);
        Add(grid, problems, 4, 0);
        Add(grid, Caption("statements — one per line"), 5, 0);
        Add(grid, source, 6, 0);

        Grid right = new() { RowDefinitions = new RowDefinitions("Auto,*,Auto,260") };
        Add(right, Caption("completes"), 0, 0);
        Add(right, candidates, 1, 0);
        Add(right, Caption("resolves"), 2, 0);
        Add(right, readings, 3, 0);

        Add(grid, right, 0, 2);
        Grid.SetRowSpan(right, 7);

        return grid;
    }

    private static void Add(Grid grid, Control control, int row, int column)
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        grid.Children.Add(control);
    }

    private static TextBlock Caption(string text)
        => new() { Text = text, Margin = new Thickness(0, 8, 0, 2), Opacity = 0.6 };

    private static TextBox Editor(double height) => new()
    {
        AcceptsReturn = true,
        AcceptsTab = false,
        FontFamily = monospace,
        VerticalAlignment = height is 0 ? VerticalAlignment.Stretch : VerticalAlignment.Top,
        Height = height is 0 ? double.NaN : height,
        TextWrapping = TextWrapping.NoWrap,
    };

    private void Refresh()
    {
        try
        {
            var symbols = Symbols();

            problems.Text = string.Join(Environment.NewLine, symbols.Validate());

            Complete(symbols);
            Resolve(symbols);
        }
        catch (Exception error)
        {
            // half-typed input is the normal case here, so a throw is a finding
            // rather than a crash
            problems.Text = error.Message;
        }
    }

    private SymbolTable Symbols()
    {
        SymbolTable symbols = new();
        symbols.WithNames([.. Lines(names)]);

        foreach (var pattern in Lines(patterns))
        {
            symbols.Patterns.Add(Pattern.Parse(pattern));
        }

        return symbols;
    }

    /// <summary>What could continue the line the caret is on, up to the caret.</summary>
    private void Complete(SymbolTable symbols)
    {
        var text = source.Text ?? string.Empty;
        var caret = Math.Clamp(source.CaretIndex, 0, text.Length);
        var typed = text[..caret];
        var line = typed[(typed.LastIndexOf('\n') + 1)..];

        candidates.ItemsSource = new Completion(symbols)
            .After(Lexemes.Lex(line))
            .Select(candidate => $"{candidate.Word,-18} {candidate.Matched}/{candidate.Words}  {candidate.Whole}")
            .ToArray();
    }

    private void Resolve(SymbolTable symbols)
    {
        Resolver resolver = new(symbols);

        List<string> resolved = [];
        var number = 0;

        foreach (var line in (source.Text ?? string.Empty).Split('\n'))
        {
            ++number;
            if (string.IsNullOrWhiteSpace(line)) continue;

            resolved.Add($"{number,3}  {resolver.Resolve(Lexemes.Lex(line))}");
        }

        readings.ItemsSource = resolved;
    }

    private static IEnumerable<string> Lines(TextBox editor)
        => (editor.Text ?? string.Empty).Split('\n')
                                        .Select(line => line.Trim())
                                        .Where(line => line.Length is not 0);

    private static readonly FontFamily monospace = new("monospace");

    private readonly TextBox names;
    private readonly TextBox patterns;
    private readonly TextBox source;
    private readonly ListBox candidates = new();
    private readonly ListBox readings = new();
    private readonly TextBlock problems = new();
}
