# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
#
# Multi-stage build for the Blazor Server UI. Final image is Alpine for size.
# Build from the Kode/ root:
#
#   docker build -f src/Bronnoysund.BlazorWeb/Dockerfile -t bronnoysund-web .
#
# In ACR: az acr build --registry <acr> --image bronnoysund-web:<tag> \
#         --file src/Bronnoysund.BlazorWeb/Dockerfile Kode

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Directory.Build.props .

# Copy csproj files first to leverage Docker layer cache on dependency restore.
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
COPY src/Bronnoysund.ViewModels/Bronnoysund.ViewModels.csproj                                           src/Bronnoysund.ViewModels/
COPY src/Bronnoysund.Speech/Bronnoysund.Speech.csproj                                                   src/Bronnoysund.Speech/
COPY src/Bronnoysund.Speech.Web/Bronnoysund.Speech.Web.csproj                                           src/Bronnoysund.Speech.Web/
COPY src/Bronnoysund.Components/Bronnoysund.Components.csproj                                           src/Bronnoysund.Components/
COPY src/Bronnoysund.BlazorWeb/Bronnoysund.BlazorWeb.csproj                                             src/Bronnoysund.BlazorWeb/

# Now full source.
COPY src/Bronnoysund.Domain/                    src/Bronnoysund.Domain/
COPY src/Bronnoysund.Application/               src/Bronnoysund.Application/
COPY src/Bronnoysund.Infrastructure/             src/Bronnoysund.Infrastructure/
COPY src/Bronnoysund.Infrastructure.Brreg.Generated/ src/Bronnoysund.Infrastructure.Brreg.Generated/
COPY src/Bronnoysund.Infrastructure.Persistence/ src/Bronnoysund.Infrastructure.Persistence/
COPY src/Bronnoysund.Infrastructure.Croatia/    src/Bronnoysund.Infrastructure.Croatia/
COPY src/Bronnoysund.Infrastructure.Denmark/    src/Bronnoysund.Infrastructure.Denmark/
COPY src/Bronnoysund.Infrastructure.Estonia/    src/Bronnoysund.Infrastructure.Estonia/
COPY src/Bronnoysund.Infrastructure.Finland/    src/Bronnoysund.Infrastructure.Finland/
COPY src/Bronnoysund.Infrastructure.Finland.Generated/ src/Bronnoysund.Infrastructure.Finland.Generated/
COPY src/Bronnoysund.Infrastructure.Greece/     src/Bronnoysund.Infrastructure.Greece/
COPY src/Bronnoysund.Infrastructure.Ireland/    src/Bronnoysund.Infrastructure.Ireland/
COPY src/Bronnoysund.Infrastructure.Latvia/     src/Bronnoysund.Infrastructure.Latvia/
COPY src/Bronnoysund.Infrastructure.Lithuania/  src/Bronnoysund.Infrastructure.Lithuania/
COPY src/Bronnoysund.Infrastructure.OpenCorporates/ src/Bronnoysund.Infrastructure.OpenCorporates/
COPY src/Bronnoysund.Infrastructure.Poland/     src/Bronnoysund.Infrastructure.Poland/
COPY src/Bronnoysund.Infrastructure.Slovenia/   src/Bronnoysund.Infrastructure.Slovenia/
COPY src/Bronnoysund.Infrastructure.Sweden/     src/Bronnoysund.Infrastructure.Sweden/
COPY src/Bronnoysund.ViewModels/                src/Bronnoysund.ViewModels/
COPY src/Bronnoysund.Speech/                    src/Bronnoysund.Speech/
COPY src/Bronnoysund.Speech.Web/                src/Bronnoysund.Speech.Web/
COPY src/Bronnoysund.Components/                src/Bronnoysund.Components/
COPY src/Bronnoysund.BlazorWeb/                 src/Bronnoysund.BlazorWeb/

RUN dotnet publish src/Bronnoysund.BlazorWeb/Bronnoysund.BlazorWeb.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# icu-libs + tzdata so CultureInfo("en"/"nb-NO"/"nn-NO") works (Alpine ships
# globalization-invariant by default).
RUN apk add --no-cache icu-libs tzdata

# Non-root user.
RUN adduser -D -u 10001 appuser
WORKDIR /app
COPY --from=build --chown=appuser:appuser /app/publish .

# SQLite path for the persistence layer — see IDatabasePathProvider. /tmp is
# ephemeral on Container Apps, which is fine: settings/cache rebuild on start.
RUN mkdir -p /tmp/bronnoysund && chown -R appuser:appuser /tmp/bronnoysund
ENV BRONNOYSUND_DB_PATH=/tmp/bronnoysund/bronnoysund.db

USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false

ENTRYPOINT ["dotnet", "Bronnoysund.BlazorWeb.dll"]
