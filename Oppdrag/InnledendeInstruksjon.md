
@Oppdrag/Hjemmeoppgave - Artisan.pdf  
Analyser denne oppgaven. 
1) JEg vil at du skal sjekke GitHub: Noen har kanskje laget dette allerede. Det må vi i så fall studere, gjenbruke om lov og dokumentere. 
2) Gå i dybden hos Brønnøysundregistrene og deres åpne data: https://www.brreg.no/bruke-data-fra-bronnoysundregistrene/datasett-og-api/
MEst relevant for oss: https://www.brreg.no/bruke-data-fra-bronnoysundregistrene/datasett-og-api/data-om-virksomheter/
3) JEg har en plan om å gjøre dette i MAUI på desktop og levere en webtjeneste og lage en APP til iOs og en APP til android. Jeg ønsker samme backend til alle tjenester. Backend skal skrives i .Net 10. Frontend i Web ønsker jeg i Blazor (eller tilsvarende). DEsktop-applikasjonen skal fungere både på Mac og Windows. iOS og Android applikasjonene skal også være Cross-platform, slik at det blir minst mulig kodevedlikehold. Jeg lurer også på om vi skal lage et lite grensesnitt på WatchOs og Android-klokker. Der skal vi kunne si et organisasjonsnummer eller navn, og returen skal kunne leses. 
4) Jeg har også en plan om å integrere med andre offentlige registre, som for eksempel gjeldsregisteret samt hente regnskapsinformasjon og eierkonsetllasjoner, slik at vi kan levere mer informasjon om bedriftene. Det er mulig noen allerede har gjort dette og at det ligger på GitHub. Hvis ikke, skal vi gjøre klart for det i planen og koden vår. Jeg skal finne nærmere spesifikasjoner.
5) JEg tror at det finnes eksempelimplementasjoner av det meste som er etterspurt. Noe i offentlige registre. Noe på GitHup. Finn ut hva du kan. 
6) Organisasjonsnummer valideres etter en bestmt formel. DEn er beskrevet her: https://www.brreg.no/om-oss/registrene-vare/om-enhetsregisteret/organisasjonsnummeret/
7) I tillegg til å skrive inn et organisasjonsnummer, ønsker jeg å kunne si et nummer muntlig. Jeg vil også kunne søke på navn både skriftlig og muntlig. Ved flere treff, skal vi få en liste med sammenfattet informasjon som vi så kan drille ned i. 
8) Jeg skal senere spesifisere hvilke data jeg ønsker ut på Appen. Nå må vi opprette innledende planer for å løse oppgaven gitt meg, samt mine egne ønsker. PLanene skal legge sinn under mappen "Plan". 

Opprett en overordnet plan og underplaner. 
Del inn i faser fra MVP på Desktop, Web-grensesnitt, Apps. 

Overfor skrev jeg at jeg vil ha samme backend. DEt jeg mener, er at jeg så langt som mulig vil ha felles kodebase for funksjonaliteten. Jeg mener at iOs og Android Appen skal hoses lokalt på enhetene, og at eneste kall ut av Appen skal være mot eksterne tjenester. Det samme når vi deployer mot en Web-server eller Desktop: Vi ønsker samme kodebase begge steder, men de kjører i Runtime på respektive platformer. Stand Alone applications, altså; men med mest mulig felles kode. Kun det som er spesifikt for hver plattform, skal skilles ut. 

I oppgaven står det at jeg skal begrunne valg. Jeg tror at de blant annet vil vite hvilke mønster / patterns jeg følkger og hvilke arkitektur-regler jeg følger. Her må du se hen til litteraturen.
