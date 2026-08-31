# Registro dei rischi

| Rischio | Gravità | Mitigazione V1 |
| --- | --- | --- |
| SSD assente interpretato come cartella vuota | Critica | Verifica del volume prima di ogni operazione; Mirror bloccato se l'identità non è certa |
| Sorgente e destinazione uguali o annidate | Critica | Normalizzazione e validazione bidirezionale dei percorsi |
| Cancellazioni involontarie con Mirror | Critica | Backup predefinito, doppia conferma, anteprima e avvio fail-closed |
| Cambio della lettera dell'unità | Alta | GUID/seriale del volume e ricostruzione controllata del percorso relativo |
| Seriale duplicato o identità volume parziale | Alta | Label solo come disambiguazione; corrispondenze multiple bloccate come ambigue |
| SSD USB riportato da Windows come disco fisso | Media | Nessun rifiuto basato solo su `DriveType`; identità stabile obbligatoria |
| SSD scollegato durante la copia | Alta | Rilevamento periodico, arresto del processo Robocopy e risultato dedicato; nessun rollback dei file già elaborati |
| Destinazione OneDrive indisponibile | Alta | Verifica immediatamente prima dell'avvio; nessuna creazione implicita rischiosa |
| Interpretazione errata degli exit code Robocopy | Alta | Modello esplicito e test per i codici 0-16 |
| Comando Robocopy costruito con argomenti non sicuri | Alta | `ArgumentList`, esclusioni validate e test dei flag per ogni modalità |
| Mirror parzialmente applicato prima di un errore | Critica | Rischio intrinseco Robocopy: preflight, anteprima e conferma prima dell'avvio; mai operare sulla sorgente |
| Output molto grande esaurisce la memoria | Media | Buffer limitato alle ultime 2.000 righe per stdout e stderr |
| Configurazione corrotta o di versione futura | Alta | Default Backup, avviso e nessuna sovrascrittura automatica del file originale |
| Junction o link che aggirano la relazione testuale dei percorsi | Alta | Ogni segmento esistente viene controllato; presenza o errore di lettura bloccano la configurazione |
| Log configurato dentro i dati sincronizzati | Alta | Validatore dedicato rifiuta uguaglianza e annidamento in entrambe le direzioni |
| Stato cambiato dopo l'apertura della UI | Alta | Preflight ripetuto immediatamente prima di preview e sincronizzazione |
| Spazio insufficiente o accesso negato | Alta | Riserva minima 256 MB, classificazione errori e log tecnico locale |
| Stima esatta dello spazio non disponibile prima di Robocopy | Media | Soglia minima più gestione ERROR 112; il preflight non garantisce capienza totale |
| Crescita incontrollata dei log | Media | Rotazione giornaliera, limite temporale e dimensionale |
| Supply-chain di dipendenze | Media | Dipendenze minime, lock file NuGet e CI con restore bloccato |
| Release pubblicata senza verifica distruttiva isolata | Alta | Workflow crea solo draft e checklist manuale obbligatoria |

Il registro viene aggiornato in ogni PR che introduce un nuovo comportamento
capace di leggere, copiare o eliminare dati.
