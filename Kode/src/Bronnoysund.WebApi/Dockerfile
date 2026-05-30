# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

# Multi-stage build for ASP.NET Core 10.0 — final image er Alpine for minimal størrelse.
# Bygges fra repo-roten:
#   docker build -f Kode/src/Bronnoysund.WebApi/Dockerfile -t bronnoysund-webapi Kode/

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Kopier kun csproj-filer først for å utnytte Docker layer-cache
COPY Directory.Build.props .
COPY src/Bronnoysund.Domain/Bronnoysund.Domain.csproj                                                   src/Bronnoysund.Domain/
COPY src/Bronnoysund.Application/Bronnoysund.Application.csproj                                         src/Bronnoysund.Application/
COPY src/Bronnoysund.Infrastructure/Bronnoysund.Infrastructure.csproj                                   src/Bronnoysund.Infrastructure/
COPY src/Bronnoysund.Infrastructure.Brreg.Generated/Bronnoysund.Infrastructure.Brreg.Generated.csproj   src/Bronnoysund.Infrastructure.Brreg.Generated/
COPY src/Bronnoysund.Infrastructure.Persistence/Bronnoysund.Infrastructure.Persistence.csproj           src/Bronnoysund.Infrastructure.Persistence/
# Plan 21 international registries — Infrastructure transitively references each per-country project.
COPY src/Bronnoysund.Infrastructure.Croatia/Bronnoysund.Infrastructure.Croatia.csproj                   src/Bronnoysund.Infrastructure.Croatia/
COPY src/Bronnoysund.Infrastructure.Denmark/Bronnoysund.Infrastructure.Denmark.csproj                   src/Bronnoysund.Infrastructure.Denmark/
COPY src/Bronnoysund.Infrastructure.Estonia/Bronnoysund.Infrastructure.Estonia.csproj                   src/Bronnoysund.Infrastructure.Estonia/
COPY src/Bronnoysund.Infrastructure.Finland/Bronnoysund.Infrastructure.Finland.csproj                   src/Bronnoysund.Infrastructure.Finland/
COPY src/Bronnoysund.Infrastructure.Finland.Generated/Bronnoysund.Infrastructure.Finland.Generated.csproj src/Bronnoysund.Infrastructure.Finland.Generated/
COPY src/Bronnoysund.Infrastructure.Greece/Bronnoysund.Infrastructure.Greece.csproj                     src/Bronnoysund.Infrastructure.Greece/
COPY src/Bronnoysund.Infrastructure.Ireland/Bronnoysund.Infrastructure.Ireland.csproj                   src/Bronnoysund.Infrastructure.Ireland/
COPY src/Bronnoysund.Infrastructure.Latvia/Bronnoysund.Infrastructure.Latvia.csproj                     src/Bronnoysund.Infrastructure.Latvia/
COPY src/Bronnoysund.Infrastructure.Lithuania/Bronnoysund.Infrastructure.Lithuania.csproj               src/Bronnoysund.Infrastructure.Lithuania/
COPY src/Bronnoysund.Infrastructure.OpenCorporates/Bronnoysund.Infrastructure.OpenCorporates.csproj     src/Bronnoysund.Infrastructure.OpenCorporates/
COPY src/Bronnoysund.Infrastructure.Poland/Bronnoysund.Infrastructure.Poland.csproj                     src/Bronnoysund.Infrastructure.Poland/
COPY src/Bronnoysund.Infrastructure.Slovenia/Bronnoysund.Infrastructure.Slovenia.csproj                 src/Bronnoysund.Infrastructure.Slovenia/
COPY src/Bronnoysund.Infrastructure.Sweden/Bronnoysund.Infrastructure.Sweden.csproj                     src/Bronnoysund.Infrastructure.Sweden/
COPY src/Bronnoysund.WebApi/Bronnoysund.WebApi.csproj                                                   src/Bronnoysund.WebApi/

RUN dotnet restore src/Bronnoysund.WebApi/Bronnoysund.WebApi.csproj

# Kopier all kildekode og bygg
COPY src/Bronnoysund.Domain/                          src/Bronnoysund.Domain/
COPY src/Bronnoysund.Application/                     src/Bronnoysund.Application/
COPY src/Bronnoysund.Infrastructure/                  src/Bronnoysund.Infrastructure/
COPY src/Bronnoysund.Infrastructure.Brreg.Generated/  src/Bronnoysund.Infrastructure.Brreg.Generated/
COPY src/Bronnoysund.Infrastructure.Persistence/      src/Bronnoysund.Infrastructure.Persistence/
COPY src/Bronnoysund.Infrastructure.Croatia/          src/Bronnoysund.Infrastructure.Croatia/
COPY src/Bronnoysund.Infrastructure.Denmark/          src/Bronnoysund.Infrastructure.Denmark/
COPY src/Bronnoysund.Infrastructure.Estonia/          src/Bronnoysund.Infrastructure.Estonia/
COPY src/Bronnoysund.Infrastructure.Finland/          src/Bronnoysund.Infrastructure.Finland/
COPY src/Bronnoysund.Infrastructure.Finland.Generated/ src/Bronnoysund.Infrastructure.Finland.Generated/
COPY src/Bronnoysund.Infrastructure.Greece/           src/Bronnoysund.Infrastructure.Greece/
COPY src/Bronnoysund.Infrastructure.Ireland/          src/Bronnoysund.Infrastructure.Ireland/
COPY src/Bronnoysund.Infrastructure.Latvia/           src/Bronnoysund.Infrastructure.Latvia/
COPY src/Bronnoysund.Infrastructure.Lithuania/        src/Bronnoysund.Infrastructure.Lithuania/
COPY src/Bronnoysund.Infrastructure.OpenCorporates/   src/Bronnoysund.Infrastructure.OpenCorporates/
COPY src/Bronnoysund.Infrastructure.Poland/           src/Bronnoysund.Infrastructure.Poland/
COPY src/Bronnoysund.Infrastructure.Slovenia/         src/Bronnoysund.Infrastructure.Slovenia/
COPY src/Bronnoysund.Infrastructure.Sweden/           src/Bronnoysund.Infrastructure.Sweden/
COPY src/Bronnoysund.WebApi/                          src/Bronnoysund.WebApi/

RUN dotnet publish src/Bronnoysund.WebApi/Bronnoysund.WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Slank runtime-image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# Kjør ikke som root
RUN adduser -D -u 10001 appuser
WORKDIR /app
COPY --from=build --chown=appuser:appuser /app/publish .
USER appuser

# Container Apps bruker port 8080 som default
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Health-check (Container Apps probe-er denne)
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Bronnoysund.WebApi.dll"]
