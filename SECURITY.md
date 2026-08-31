# Politica di sicurezza

## Versioni supportate

DriveHarbor riceve correzioni di sicurezza soltanto sulla versione stabile più
recente.

| Versione | Supporto |
| --- | --- |
| 1.0.x | Sì |
| 1.0.0-beta.x | No |

## Segnalare una vulnerabilità

Non aprire una issue pubblica per una vulnerabilità non ancora corretta. Usa la
segnalazione privata tramite
[GitHub Security Advisories](https://github.com/MilitantMercury/DriveHarbor/security/advisories/new)
e includi versione, passaggi per riprodurre il problema, impatto osservato ed
eventuali log privati dei quali hai rimosso percorsi o dati sensibili.

## Modello di sicurezza degli aggiornamenti

- DriveHarbor controlla esclusivamente le release del repository ufficiale.
- Il pacchetto viene scaricato tramite HTTPS in
  `%LocalAppData%\DriveHarbor\Updates` e accettato soltanto se il digest SHA-256
  coincide con quello pubblicato nella stessa release.
- GitHub genera inoltre un'attestazione Sigstore per il pacchetto. La verifica
  automatica dell'attestazione non è ancora inclusa nell'app; il checksum
  protegge da corruzioni e discrepanze, ma non sostituisce una firma
  Authenticode o la verifica Sigstore indipendente.
- L'installazione richiede una seconda conferma. Se la directory è protetta,
  come `Program Files`, Windows mostra una richiesta UAC prima che DriveHarbor
  venga chiuso.
- L'updater lavora da una copia temporanea, estrae l'archivio in staging,
  rifiuta percorsi che escono dalla directory prevista e conserva una copia dei
  file sostituiti. In caso di errore tenta il rollback e riapre l'app mostrando
  l'esito registrato localmente. Il riavvio passa dalla shell desktop per non
  lasciare DriveHarbor in esecuzione con i privilegi amministrativi dell'updater.

Non approvare una richiesta UAC inattesa. Avvia gli aggiornamenti soltanto
dall'interfaccia di DriveHarbor e verifica che il publisher e la provenienza del
pacchetto siano quelli attesi.

## Protezione dei dati sincronizzati

DriveHarbor non modifica mai la sorgente SSD. La modalità Mirror può eliminare
file esclusivamente dalla destinazione e deve essere usata solo con percorsi
verificati e una copia indipendente dei dati. Configurazioni ambigue, percorsi
annidati, junction e destinazioni esterne a OneDrive vengono rifiutati.

L'avvio automatico è facoltativo e usa esclusivamente la chiave `Run` dell'utente
corrente (`HKCU`), senza privilegi amministrativi. La voce contiene il percorso
dell'eseguibile DriveHarbor e l'argomento `--background`; viene rimossa quando
l'opzione viene disattivata.

I log restano locali e non registrano il contenuto dei file. Possono comunque
contenere nomi e percorsi: rimuovi queste informazioni prima di condividerli.
L'esportazione dall'app include soltanto file `.log`, ma non anonimizza nomi e
percorsi presenti al loro interno.
