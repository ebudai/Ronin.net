// Copyright © 2026 Eric Budai

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Scratch;

[ExcludeFromCodeCoverage]
internal sealed class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Workbench();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

[ExcludeFromCodeCoverage]
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
        => AppBuilder.Configure<App>()
                     .UsePlatformDetect()
                     .StartWithClassicDesktopLifetime(args);
}
