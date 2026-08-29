# DriveHarbor

> Sincronizzazione locale, semplice e sicura da SSD esterno a OneDrive per Windows.

![Screenshot placeholder](https://placehold.co/1200x675/13233A/FFFFFF?text=DriveHarbor+Dashboard)

> [!IMPORTANT]
> DriveHarbor è in sviluppo e non è ancora pronto per proteggere dati reali.

## Cos'è DriveHarbor

DriveHarbor è un'applicazione desktop gratuita per Windows 11 che copia una
cartella presente su un SSD esterno verso una cartella locale sincronizzata dal
client ufficiale OneDrive.

```text
SSD esterno → DriveHarbor → cartella OneDrive locale → client OneDrive → cloud
```

L'applicazione funziona completamente in locale: non usa Microsoft Graph, non
richiede account aggiuntivi, non include telemetria e non invia dati all'esterno.

## Stato del progetto

La versione corrente è `0.1.0`. Sono disponibili la foundation tecnica, il
modello di configurazione locale e le regole di sicurezza dei percorsi. Le
funzioni di sincronizzazione verranno aggiunte tramite Pull Request piccole e
verificabili. Consulta la [roadmap](docs/roadmap.md) per la sequenza prevista.

## Configurazione locale

Le impostazioni vengono salvate in:

```text
%LocalAppData%\DriveHarbor\settings.json
```

Il formato JSON è versionato e il salvataggio usa un file temporaneo sostituito
atomicamente. Un file assente produce impostazioni sicure con modalità Backup;
un file corrotto o di versione sconosciuta non viene sovrascritto e richiede la
verifica dell'utente.

La validazione rifiuta:

- sorgente o destinazione mancanti e non disponibili;
- percorsi uguali;
- una cartella contenuta nell'altra, in entrambe le direzioni;
- destinazioni esterne alle cartelle OneDrive note a Windows.

La UI per selezionare e modificare queste impostazioni arriverà nella feature
dedicata alla dashboard.

## Backup e Mirror

| Modalità | Copia file nuovi e modificati | Elimina dalla destinazione |
| --- | --- | --- |
| **Backup** (predefinita) | Sì | Mai |
| **Mirror** | Sì | Sì, solo dopo conferma esplicita |

Mirror verrà progettata con controlli più restrittivi. DriveHarbor non elimina
mai elementi dalla sorgente. Se lo stato del disco o dei percorsi è incerto,
l'operazione deve fermarsi.

## Requisiti

### Per utilizzare una futura build self-contained

- Windows 11 x64.
- Client ufficiale OneDrive configurato.
- Robocopy, incluso in Windows.

### Per lo sviluppo

- Windows 11.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), versione
  definita in `global.json`.
- Visual Studio con supporto .NET 10 e workload desktop .NET, oppure .NET CLI.
- Git.

## Sviluppo locale

```powershell
git clone https://github.com/MilitantMercury/DriveHarbor.git
cd DriveHarbor
dotnet restore DriveHarbor.slnx
dotnet build DriveHarbor.slnx --configuration Release --no-restore
dotnet test DriveHarbor.slnx --configuration Release --no-build
dotnet run --project src/DriveHarbor.App/DriveHarbor.App.csproj
```

I test che coinvolgeranno file useranno esclusivamente directory temporanee
create per il singolo test. Non devono essere eseguiti test distruttivi su
directory reali dell'utente.

## Build pubblicabile

```powershell
dotnet publish src/DriveHarbor.App/DriveHarbor.App.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output artifacts/DriveHarbor-win-x64
```

La pipeline GitHub Actions esegue restore, build, test e produce un artifact
self-contained per Windows x64. La pubblicazione non è ancora una release
firmata né un installer.

## Struttura

```text
src/
  DriveHarbor.App/          WPF, UI e composizione
  DriveHarbor.Core/         Regole e servizi indipendenti dalla UI
tests/
  DriveHarbor.Core.Tests/   Test unitari e test isolati
docs/                       Architettura, rischi e roadmap
.github/workflows/          Build e artifact automatici
```

Le scelte tecniche sono descritte in [Architettura](docs/architecture.md). I
rischi relativi alla perdita di dati sono tracciati nel
[Registro dei rischi](docs/risk-register.md).

## Avvertenze

- La modalità Mirror può eliminare file esclusivamente dalla destinazione.
- Verificare sempre che sorgente e destinazione siano corrette.
- OneDrive gestisce la sincronizzazione cloud; DriveHarbor non può garantire lo
  stato remoto del cloud.
- Conservare sempre una copia indipendente dei dati importanti.

## Contribuire

Lo sviluppo usa branch `feature/*`. Ogni modifica entra in `main` tramite Pull
Request e deve aggiornare README o documentazione pertinente, oltre a mantenere
verdi build e test. È preferito lo squash merge.

## Licenza

Distribuito con licenza [MIT](LICENSE).
