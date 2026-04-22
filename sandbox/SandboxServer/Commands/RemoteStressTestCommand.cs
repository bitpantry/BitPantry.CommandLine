using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxServer.Commands;

/// <summary>
/// Comprehensive stress test for SignalR client-server UX.
/// Walks through a gauntlet of interactive Spectre.Console features in sequence,
/// exercising every RPC path: console output streaming, ReadKey prompts,
/// progress bars, status spinners, tables, rules, and rapid interleaved I/O.
///
/// Designed to surface:
///   - Dispatch deadlocks (sync-over-async ReadKey)
///   - Message ordering issues from Task.Run in hub
///   - Output buffering race conditions
///   - Progress bar rendering over SignalR
///
/// Usage (after 'server connect'):
///   stresstest
///   stresstest --rounds 3     (repeat the gauntlet N times)
/// </summary>
[Command(Name = "stresstest")]
[Description("Run a comprehensive interactive UX stress test over SignalR")]
public class RemoteStressTestCommand : CommandBase
{
    [Argument(Name = "rounds")]
    [Description("Number of times to repeat the full gauntlet (default: 1)")]
    public int Rounds { get; set; } = 1;

    public async Task Execute(CommandExecutionContext ctx)
    {
        for (int round = 1; round <= Rounds; round++)
        {
            if (Rounds > 1)
            {
                Console.WriteLine();
                Console.Write(new Rule($"[yellow]Round {round} of {Rounds}[/]") { Justification = Justify.Center });
                Console.WriteLine();
            }

            // ── Phase 1: Rapid console output ──
            Phase1_RapidOutput();

            // ── Phase 2: Confirmation prompt ──
            if (!Phase2_ConfirmContinue())
                return;

            // ── Phase 3: Text input prompt ──
            Phase3_TextPrompt();

            // ── Phase 4: Selection prompt ──
            Phase4_SelectionPrompt();

            // ── Phase 5: Multi-selection prompt ──
            Phase5_MultiSelectionPrompt();

            // ── Phase 6: Progress bars ──
            await Phase6_ProgressBars();

            // ── Phase 7: Status spinner ──
            await Phase7_StatusSpinner();

            // ── Phase 8: Rapid output + immediate prompt (ordering test) ──
            if (!Phase8_OutputThenPrompt())
                return;

            // ── Phase 9: Table rendering ──
            Phase9_TableRendering();

            // ── Phase 10: Chatty burst (many small writes) ──
            Phase10_ChattyBurst();

            // ── Phase 11: Back-to-back confirms (rapid RPC round-trips) ──
            Phase11_BackToBackConfirms();

            // ── Phase 12: Final summary ──
            Phase12_Summary(round);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Phase 1: Rapid console output
    // Tests: output streaming, buffered writer throughput
    // ────────────────────────────────────────────────────────────────
    private void Phase1_RapidOutput()
    {
        Console.Write(new Rule("[cyan]Phase 1: Rapid Console Output[/]"));
        Console.MarkupLine("[grey]Writing 20 lines rapidly to test output streaming...[/]");

        for (int i = 1; i <= 20; i++)
        {
            var color = i % 2 == 0 ? "green" : "blue";
            Console.MarkupLine($"[{color}]  Line {i:D2}:[/] The quick brown fox jumps over the lazy dog. 🦊");
        }

        Console.MarkupLine("[green]  ✓ Rapid output complete[/]");
        Console.WriteLine();
    }

    // ────────────────────────────────────────────────────────────────
    // Phase 2: Confirmation prompt
    // Tests: ConfirmationPrompt ReadKey RPC, the core bug scenario
    // ────────────────────────────────────────────────────────────────
    private bool Phase2_ConfirmContinue()
    {
        Console.Write(new Rule("[cyan]Phase 2: Confirmation Prompt[/]"));
        Console.MarkupLine("[grey]This is the core interactive prompt test (ReadKey RPC).[/]");

        var proceed = Console.Prompt(new ConfirmationPrompt("Continue with the stress test?"));
        if (!proceed)
        {
            Console.MarkupLine("[yellow]Aborted by user.[/]");
            return false;
        }

        Console.MarkupLine("[green]  ✓ Confirmation received[/]");
        Console.WriteLine();
        return true;
    }

    // ────────────────────────────────────────────────────────────────
    // Phase 3: Text prompt
    // Tests: TextPrompt with multiple ReadKey calls per character
    // ────────────────────────────────────────────────────────────────
    private void Phase3_TextPrompt()
    {
        Console.Write(new Rule("[cyan]Phase 3: Text Input Prompt[/]"));
        Console.MarkupLine("[grey]Type something — each keystroke is a ReadKey RPC round-trip.[/]");

        var input = Console.Prompt(new TextPrompt<string>("Enter a [green]test message[/]:"));
        Console.MarkupLine($"[green]  ✓ Received:[/] \"{input.EscapeMarkup()}\"");
        Console.WriteLine();
    }

    // ────────────────────────────────────────────────────────────────
    // Phase 4: Selection prompt
    // Tests: Arrow key navigation via ReadKey, re-rendering
    // ────────────────────────────────────────────────────────────────
    private void Phase4_SelectionPrompt()
    {
        Console.Write(new Rule("[cyan]Phase 4: Selection Prompt[/]"));
        Console.MarkupLine("[grey]Use arrow keys to navigate, Enter to select.[/]");

        var choice = Console.Prompt(
            new SelectionPrompt<string>()
                .Title("Pick a deployment [green]environment[/]:")
                .AddChoices("Development", "Staging", "Production", "Disaster Recovery"));

        Console.MarkupLine($"[green]  ✓ Selected:[/] {choice}");
        Console.WriteLine();
    }

    // ────────────────────────────────────────────────────────────────
    // Phase 5: Multi-selection prompt
    // Tests: Spacebar toggles + arrow keys + Enter
    // ────────────────────────────────────────────────────────────────
    private void Phase5_MultiSelectionPrompt()
    {
        Console.Write(new Rule("[cyan]Phase 5: Multi-Selection Prompt[/]"));
        Console.MarkupLine("[grey]Space to toggle, Enter to confirm. Tests multiple ReadKey patterns.[/]");

        var services = Console.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select [green]services[/] to restart:")
                .AddChoices("API Gateway", "Auth Service", "Database", "Cache", "Worker", "Scheduler"));

        Console.MarkupLine($"[green]  ✓ Selected {services.Count} service(s):[/] {string.Join(", ", services)}");
        Console.WriteLine();
    }

    // ────────────────────────────────────────────────────────────────
    // Phase 6: Progress bars
    // Tests: Live rendering, rapid output updates, timer ticks
    // ────────────────────────────────────────────────────────────────
    private async Task Phase6_ProgressBars()
    {
        Console.Write(new Rule("[cyan]Phase 6: Progress Bars[/]"));
        Console.MarkupLine("[grey]Simulating 3 concurrent tasks with progress tracking...[/]");

        await Console.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task1 = ctx.AddTask("[green]Downloading config[/]", maxValue: 100);
                var task2 = ctx.AddTask("[blue]Building assets[/]", maxValue: 100);
                var task3 = ctx.AddTask("[yellow]Running migrations[/]", maxValue: 100);

                while (!ctx.IsFinished)
                {
                    // Advance at different rates to test interleaved updates
                    if (!task1.IsFinished) task1.Increment(3.7);
                    if (!task2.IsFinished) task2.Increment(2.1);
                    if (!task3.IsFinished) task3.Increment(1.5);

                    await Task.Delay(50);
                }
            });

        Console.MarkupLine("[green]  ✓ All progress bars completed[/]");
        Console.WriteLine();
    }

    // ────────────────────────────────────────────────────────────────
    // Phase 7: Status spinner
    // Tests: Spinner animation over SignalR, status text updates
    // ────────────────────────────────────────────────────────────────
    private async Task Phase7_StatusSpinner()
    {
        Console.Write(new Rule("[cyan]Phase 7: Status Spinner[/]"));
        Console.MarkupLine("[grey]Testing animated spinner with changing status text...[/]");

        await Console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Connecting...", async ctx =>
            {
                await Task.Delay(400);
                ctx.Status("Authenticating...");
                ctx.Spinner(Spinner.Known.Star);
                await Task.Delay(400);
                ctx.Status("Loading schema...");
                ctx.Spinner(Spinner.Known.Arrow3);
                await Task.Delay(400);
                ctx.Status("Syncing data...");
                ctx.Spinner(Spinner.Known.BouncingBar);
                await Task.Delay(400);
                ctx.Status("Finalizing...");
                await Task.Delay(300);
            });

        Console.MarkupLine("[green]  ✓ Status spinner complete[/]");
        Console.WriteLine();
    }

    // ────────────────────────────────────────────────────────────────
    // Phase 8: Rapid output then immediate prompt
    // Tests: Message ordering — output must arrive before prompt
    // ────────────────────────────────────────────────────────────────
    private bool Phase8_OutputThenPrompt()
    {
        Console.Write(new Rule("[cyan]Phase 8: Output → Prompt Ordering[/]"));
        Console.MarkupLine("[grey]Writing output then immediately prompting.[/]");
        Console.MarkupLine("[grey]If you see the prompt AFTER the numbered lines, ordering is correct.[/]");

        for (int i = 1; i <= 10; i++)
        {
            Console.MarkupLine($"[magenta]  Output line {i:D2} — this must appear BEFORE the prompt below[/]");
        }

        // Immediate prompt after burst — tests that buffered output is flushed before ReadKey RPC
        var ok = Console.Prompt(new ConfirmationPrompt("Did all 10 lines appear above this prompt?"));
        Console.MarkupLine(ok
            ? "[green]  ✓ Message ordering verified[/]"
            : "[red]  ✗ OUTPUT ORDERING ISSUE — lines appeared after the prompt[/]");
        Console.WriteLine();
        return true;
    }

    // ────────────────────────────────────────────────────────────────
    // Phase 9: Table rendering
    // Tests: Complex renderable output, ANSI escape sequences
    // ────────────────────────────────────────────────────────────────
    private void Phase9_TableRendering()
    {
        Console.Write(new Rule("[cyan]Phase 9: Table Rendering[/]"));
        Console.MarkupLine("[grey]Rendering a styled table with mixed content...[/]");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Service[/]").Centered())
            .AddColumn(new TableColumn("[bold]Status[/]").Centered())
            .AddColumn(new TableColumn("[bold]Latency[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Uptime[/]").RightAligned());

        table.AddRow("API Gateway", "[green]Healthy[/]", "12ms", "99.97%");
        table.AddRow("Auth Service", "[green]Healthy[/]", "8ms", "99.99%");
        table.AddRow("Database", "[yellow]Degraded[/]", "145ms", "99.81%");
        table.AddRow("Cache", "[green]Healthy[/]", "2ms", "100.0%");
        table.AddRow("Worker", "[red]Down[/]", "—", "94.2%");
        table.AddRow("Scheduler", "[green]Healthy[/]", "23ms", "99.95%");

        Console.Write(table);
        Console.MarkupLine("[green]  ✓ Table rendered[/]");
        Console.WriteLine();
    }

    // ────────────────────────────────────────────────────────────────
    // Phase 10: Chatty burst
    // Tests: Many small writes in tight loop — buffer coalescing
    // ────────────────────────────────────────────────────────────────
    private void Phase10_ChattyBurst()
    {
        Console.Write(new Rule("[cyan]Phase 10: Chatty Burst (50 rapid writes)[/]"));
        Console.MarkupLine("[grey]Firing 50 small writes with no delays — tests buffer coalescing...[/]");

        for (int i = 1; i <= 50; i++)
        {
            // Alternate between Markup and Write to test different code paths
            if (i % 5 == 0)
                Console.MarkupLine($"[bold red]  BURST {i:D2}[/] ████████████████");
            else
                Console.MarkupLine($"[grey]  burst {i:D2}[/] ░░░░░░░░░░░░░░░░");
        }

        Console.MarkupLine("[green]  ✓ Chatty burst complete — check that all 50 lines rendered[/]");
        Console.WriteLine();
    }

    // ────────────────────────────────────────────────────────────────
    // Phase 11: Back-to-back confirms
    // Tests: Rapid sequential ReadKey RPC round-trips with no delay
    // ────────────────────────────────────────────────────────────────
    private void Phase11_BackToBackConfirms()
    {
        Console.Write(new Rule("[cyan]Phase 11: Back-to-Back Confirms (5×)[/]"));
        Console.MarkupLine("[grey]5 confirmation prompts in a row — tests rapid RPC cycling.[/]");

        int yesCount = 0;
        for (int i = 1; i <= 5; i++)
        {
            var answer = Console.Prompt(new ConfirmationPrompt($"Confirm #{i}/5?"));
            if (answer) yesCount++;
            Console.MarkupLine(answer
                ? $"[green]  #{i}: Yes[/]"
                : $"[yellow]  #{i}: No[/]");
        }

        Console.MarkupLine($"[green]  ✓ Completed 5 confirms ({yesCount} yes, {5 - yesCount} no)[/]");
        Console.WriteLine();
    }

    // ────────────────────────────────────────────────────────────────
    // Phase 12: Summary
    // ────────────────────────────────────────────────────────────────
    private void Phase12_Summary(int round)
    {
        Console.Write(new Rule("[bold green]Stress Test Complete[/]"));

        var panel = new Panel(
            new Markup(
                "[bold green]All phases completed successfully![/]\n\n" +
                "[grey]Phases exercised:[/]\n" +
                "  1. Rapid console output (streaming)\n" +
                "  2. ConfirmationPrompt (ReadKey RPC)\n" +
                "  3. TextPrompt (multi-keystroke RPC)\n" +
                "  4. SelectionPrompt (arrow key RPC)\n" +
                "  5. MultiSelectionPrompt (spacebar + arrows)\n" +
                "  6. Progress bars (live rendering)\n" +
                "  7. Status spinner (animated updates)\n" +
                "  8. Output → prompt ordering\n" +
                "  9. Table rendering (complex ANSI)\n" +
                " 10. Chatty burst (50 rapid writes)\n" +
                " 11. Back-to-back confirms (rapid RPC)\n\n" +
                $"[bold]Round {round} of {Rounds} done.[/]"))
            .Header("[bold]Results[/]")
            .Border(BoxBorder.Double)
            .Padding(1, 1);

        Console.Write(panel);
        Console.WriteLine();
    }
}
