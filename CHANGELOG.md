# Changelog

Tutte le modifiche rilevanti di DriveHarbor sono documentate in questo file.
Il progetto segue il versionamento semantico.

## [Unreleased]

### Added

- Dashboard WPF e pagina Impostazioni.
- Configurazione JSON locale e versionata.
- Rilevamento stabile del volume tramite GUID, seriale e label.
- Backup, anteprima Mirror e Mirror con doppia conferma.
- Robocopy asincrono con annullamento, log e risultati comprensibili.
- Validazione fail-closed di percorsi, junction e posizione log.
- Preflight dello spazio libero e classificazione degli errori comuni.
- Build e draft release self-contained Windows x64 tramite GitHub Actions.

### Security

- Backup predefinito e nessuna modifica della sorgente.
- Mirror bloccato senza anteprima e conferma esplicita.
- Percorsi uguali, annidati, ambigui o non verificabili vengono rifiutati.
