# Registro dei rischi

| Rischio | Gravità | Mitigazione V1 |
| --- | --- | --- |
| SSD assente interpretato come cartella vuota | Critica | Verifica del volume prima di ogni operazione; Mirror bloccato se l'identità non è certa |
| Sorgente e destinazione uguali o annidate | Critica | Normalizzazione e validazione bidirezionale dei percorsi |
| Cancellazioni involontarie con Mirror | Critica | Backup predefinito, doppia conferma, anteprima e avvio fail-closed |
| Cambio della lettera dell'unità | Alta | Identificazione tramite seriale del volume e segnali secondari |
| SSD scollegato durante la copia | Alta | Monitoraggio processo, annullamento e risultato di errore senza ulteriori cancellazioni |
| Destinazione OneDrive indisponibile | Alta | Verifica immediatamente prima dell'avvio; nessuna creazione implicita rischiosa |
| Interpretazione errata degli exit code Robocopy | Alta | Modello esplicito e test per i codici 0-16 |
| Configurazione corrotta o di versione futura | Alta | Default Backup, avviso e nessuna sovrascrittura automatica del file originale |
| Junction o link che aggirano la relazione testuale dei percorsi | Alta | Rischio residuo: risoluzione dei path finali prima di abilitare la sincronizzazione |
| Spazio insufficiente o accesso negato | Media | Preflight, messaggi comprensibili e conservazione del log tecnico locale |
| Crescita incontrollata dei log | Media | Rotazione giornaliera, limite temporale e dimensionale |
| Supply-chain di dipendenze | Media | Dipendenze minime, lock file NuGet e CI con restore bloccato |

Il registro viene aggiornato in ogni PR che introduce un nuovo comportamento
capace di leggere, copiare o eliminare dati.
