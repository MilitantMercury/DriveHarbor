namespace DriveHarbor.Core.Robocopy;

public static class RobocopyExitCodeInterpreter
{
    private const int FilesCopied = 1;
    private const int ExtraItems = 2;
    private const int Mismatches = 4;
    public static RobocopyOperationStatus GetStatus(int exitCode)
    {
        if (exitCode < 0 || exitCode >= 8)
        {
            return RobocopyOperationStatus.Failed;
        }

        return (exitCode & (ExtraItems | Mismatches)) != 0
            ? RobocopyOperationStatus.CompletedWithWarnings
            : RobocopyOperationStatus.Completed;
    }

    public static string GetUserMessage(int exitCode)
    {
        var status = GetStatus(exitCode);
        if (status == RobocopyOperationStatus.Failed)
        {
            return "La sincronizzazione non è stata completata. Controlla il log per i dettagli.";
        }

        var copied = (exitCode & FilesCopied) != 0;
        return status == RobocopyOperationStatus.CompletedWithWarnings
            ? "Sincronizzazione completata con avvisi. Controlla il riepilogo."
            : copied
                ? "Sincronizzazione completata: file aggiornati."
                : "Sincronizzazione completata: nessun aggiornamento necessario.";
    }
}
