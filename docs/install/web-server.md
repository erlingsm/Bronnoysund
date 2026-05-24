# Deploy Web på en dedikert server

Web-versjonen er en standard ASP.NET Core-app. Den kan kjøres bak nginx/Apache som reverse proxy, eller direkte med Kestrel hvis serveren er liten.

## Forutsetning

- Linux-server (Ubuntu 22.04+ / Debian 12+) eller Windows Server 2022+
- **.NET 10 Runtime** installert
- (valgfri) nginx eller Apache som reverse proxy med HTTPS-sertifikat

## Last ned og pakk ut

```bash
ssh deploy@server
cd /var/www/
sudo mkdir bronnoysund-lookup
cd bronnoysund-lookup
sudo wget https://github.com/erlingsm/Bronnoysund.Lookup/releases/latest/download/Bronnoysund.Lookup.BlazorWeb-linux-x64.zip
sudo unzip Bronnoysund.Lookup.BlazorWeb-linux-x64.zip
sudo chown -R www-data:www-data .
sudo chmod +x Bronnoysund.Lookup.BlazorWeb
```

## Sett opp som systemd-service

Opprett `/etc/systemd/system/bronnoysund-lookup.service`:

```ini
[Unit]
Description=Bronnoysund.Lookup Blazor Web
After=network.target

[Service]
WorkingDirectory=/var/www/bronnoysund-lookup
ExecStart=/var/www/bronnoysund-lookup/Bronnoysund.Lookup.BlazorWeb --urls http://localhost:5199
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=bronnoysund-lookup
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

Aktiver og start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable bronnoysund-lookup
sudo systemctl start bronnoysund-lookup
sudo systemctl status bronnoysund-lookup
```

Sjekk logger:

```bash
sudo journalctl -u bronnoysund-lookup -f
```

## Nginx reverse proxy (anbefalt)

Opprett `/etc/nginx/sites-available/bronnoysund-lookup`:

```nginx
server {
    listen 80;
    server_name bronnoysund.example.no;

    location / {
        proxy_pass         http://localhost:5199;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

Aktiver:

```bash
sudo ln -s /etc/nginx/sites-available/bronnoysund-lookup /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

Legg til HTTPS med Let's Encrypt:

```bash
sudo certbot --nginx -d bronnoysund.example.no
```

## Konfigurer Brreg-innstillinger

Lag `appsettings.Production.json` i deploy-mappen for å overstyre standarder:

```json
{
  "Brreg": {
    "BaseUrl": "https://data.brreg.no/enhetsregisteret/api/",
    "RequestTimeout": "00:00:15",
    "CacheTtl": "1.00:00:00",
    "UserAgent": "Bronnoysund.Lookup/0.1 (+kontakt@dittfirma.no)"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  }
}
```

Server-process leser denne automatisk når `ASPNETCORE_ENVIRONMENT=Production`.

## Senere — Kubernetes-deployment

Når Fase 6 er ferdig (se [Plan/07-Fase6-Sentral-backend-opsjonell.md](../../Plan/07-Fase6-Sentral-backend-opsjonell.md)) tilbys også Dockerfile og Helm-chart for AWS EKS / Azure AKS-deployment.
