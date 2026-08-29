using System.Diagnostics;

namespace DriveHarbor.Core.Robocopy;

public sealed class RobocopyRunner : IRobocopyRunner
{
    private const int MaximumCapturedLines = 2_000;

    public async Task<RobocopyResult> RunAsync(
        RobocopyRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var arguments = RobocopyCommandBuilder.BuildArguments(request);
        using var process = CreateProcess(arguments);
        var output = new BoundedLineBuffer(MaximumCapturedLines);
        var errors = new BoundedLineBuffer(MaximumCapturedLines);

        try
        {
            if (!process.Start())
            {
                return FailedToStart("Non è stato possibile avviare il motore di sincronizzazione.");
            }

            var outputTask = ReadLinesAsync(process.StandardOutput, output, progress);
            var errorTask = ReadLinesAsync(process.StandardError, errors, progress: null);

            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryTerminate(process);
                await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
                return new(
                    RobocopyOperationStatus.Cancelled,
                    null,
                    "Sincronizzazione annullata.",
                    RobocopyOutputParser.Parse(output.Snapshot()),
                    output.Snapshot(),
                    errors.Snapshot());
            }

            await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
            var status = RobocopyExitCodeInterpreter.GetStatus(process.ExitCode);
            var outputSnapshot = output.Snapshot();
            return new(
                status,
                process.ExitCode,
                RobocopyExitCodeInterpreter.GetUserMessage(process.ExitCode),
                RobocopyOutputParser.Parse(outputSnapshot),
                outputSnapshot,
                errors.Snapshot());
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return FailedToStart("Il motore di sincronizzazione non è disponibile su questo sistema.");
        }
    }

    private static Process CreateProcess(IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "robocopy.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return new Process { StartInfo = startInfo };
    }

    private static async Task ReadLinesAsync(
        StreamReader reader,
        BoundedLineBuffer destination,
        IProgress<string>? progress)
    {
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            destination.Add(line);
            progress?.Report(line);
        }
    }

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited between the state check and termination.
        }
    }

    private static RobocopyResult FailedToStart(string message) => new(
        RobocopyOperationStatus.Failed,
        null,
        message,
        new(),
        [],
        []);

    private sealed class BoundedLineBuffer(int capacity)
    {
        private readonly Queue<string> lines = new(capacity);
        private readonly Lock syncRoot = new();

        public void Add(string line)
        {
            lock (syncRoot)
            {
                if (lines.Count == capacity)
                {
                    lines.Dequeue();
                }

                lines.Enqueue(line);
            }
        }

        public IReadOnlyList<string> Snapshot()
        {
            lock (syncRoot)
            {
                return [.. lines];
            }
        }
    }
}
