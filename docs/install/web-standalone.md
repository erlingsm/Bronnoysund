# Kjør Web-versjonen lokalt på Mac eller PC

Web-versjonen er en **portable** Blazor Server-app som kjøres lokalt. Ingen webserver-installasjon kreves — bare .NET 10 Runtime.

## Forutsetning

- **.NET 10 Runtime** installert ([last ned fra Microsoft](https://dotnet.microsoft.com/download/dotnet/10.0)).

Sjekk i terminal:

```bash
dotnet --list-runtimes | grep "App 10"
```

Hvis du ser `Microsoft.NETCore.App 10.0.x` — er du klar.

## Kjør fra source (utvikler)

```bash
git clone https://github.com/erlingsm/Bronnoysund.Lookup.git
cd Bronnoysund.Lookup/Kode
dotnet run --project src/Bronnoysund.Lookup.BlazorWeb --urls http://localhost:5199
```

Åpne <http://localhost:5199/> i nettleser.

## Kjør portable ZIP (sluttbruker)

(Når release-bygg er publisert.)

### På Mac (M3 / Apple Silicon)

1. Last ned `Bronnoysund.Lookup.BlazorWeb-osx-arm64.zip` fra [Releases](https://github.com/erlingsm/Bronnoysund.Lookup/releases)
2. Pakk ut til hvor som helst (også USB-stick)
3. Åpne Terminal i mappen og kjør:

   ```bash
   ./Bronnoysund.Lookup.BlazorWeb --urls http://localhost:5199
   ```

4. Åpne <http://localhost:5199/> i Safari/Chrome

### På Mac (Intel)

Samme som over, men bruk `Bronnoysund.Lookup.BlazorWeb-osx-x64.zip`.

### På Windows

1. Last ned `Bronnoysund.Lookup.BlazorWeb-win-x64.zip`
2. Pakk ut hvor som helst
3. Dobbeltklikk `Bronnoysund.Lookup.BlazorWeb.exe`
4. Standard nettleser åpnes automatisk

## Bygg portable ZIP selv

```bash
cd Kode
dotnet publish src/Bronnoysund.Lookup.BlazorWeb -c Release -r osx-arm64 \
    --self-contained false \
    -o dist/web/osx-arm64

# Tilsvarende for andre RIDer:
# -r osx-x64
# -r win-x64
# -r linux-x64
```

Pakk innhold av `dist/web/<rid>/` som ZIP og distribuer.

## Self-contained (uten .NET Runtime-krav)

Hvis sluttbruker ikke har .NET 10 Runtime installert, kan vi bygge en større self-contained variant:

```bash
dotnet publish src/Bronnoysund.Lookup.BlazorWeb -c Release -r osx-arm64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -o dist/web-sc/osx-arm64
```

Resulterende `dist/web-sc/osx-arm64/Bronnoysund.Lookup.BlazorWeb` er en ~80MB enkelt-binær — ingen avhengighet til system-installert .NET.
