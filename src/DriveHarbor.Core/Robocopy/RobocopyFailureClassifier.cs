namespace DriveHarbor.Core.Robocopy;

public static class RobocopyFailureClassifier
{
    public static (RobocopyFailureKind Kind, string UserMessage) Classify(
        IEnumerable<string> output,
        IEnumerable<string> errors)
    {
        var text = string.Join('\n', output.Concat(errors));
        if (ContainsAny(text, "ERROR 112", "not enough space", "disk full", "spazio insufficiente", "disco pieno"))
        {
            return (
                RobocopyFailureKind.InsufficientSpace,
                "Spazio insufficiente nella destinazione. Libera spazio e riprova.");
        }

        if (ContainsAny(text, "ERROR 5", "access is denied", "accesso negato"))
        {
            return (
                RobocopyFailureKind.AccessDenied,
                "Accesso negato a uno o più file. Verifica i permessi e riprova.");
        }

        if (ContainsAny(text, "ERROR 32", "being used by another process", "in uso da un altro processo"))
        {
            return (
                RobocopyFailureKind.FileLocked,
                "Uno o più file sono in uso. Chiudi le applicazioni che li utilizzano e riprova.");
        }

        if (ContainsAny(text, "ERROR 206", "filename or extension is too long", "nome di file o estensione troppo lunga"))
        {
            return (
                RobocopyFailureKind.PathTooLong,
                "Uno o più percorsi sono troppo lunghi per Windows. Riduci la lunghezza delle cartelle e riprova.");
        }

        if (ContainsAny(text, "ERROR 2", "ERROR 3", "cannot find the file", "cannot find the path", "impossibile trovare"))
        {
            return (
                RobocopyFailureKind.PathUnavailable,
                "Un file o una cartella non è più disponibile. Controlla SSD e destinazione.");
        }

        return (
            RobocopyFailureKind.Unknown,
            "La sincronizzazione non è stata completata. Controlla il log per i dettagli.");
    }

    private static bool ContainsAny(string text, params string[] values) =>
        values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
}
