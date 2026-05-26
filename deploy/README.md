# Deploy

One pipeline per platform target. Each can be triggered manually from the
GitHub Actions tab, locally via the matching `deploy-*.sh` script in this
folder, or — if you uncomment the `workflow_run` block in each CD workflow —
automatically after `CI` passes on `master`.

## The pipeline matrix

| Platform | Channel | GitHub Actions | Local script | Account / signing |
| --- | --- | --- | --- | --- |
| Blazor Web (UI) | Azure Container Apps | `cd-web.yml` | `deploy/deploy-web.sh` | Azure (OIDC) |
| WebApi (JSON endpoint) | Azure Container Apps | `cd-webapi.yml` | `deploy/deploy-webapi.sh` | Azure (OIDC) |
| MAUI Mobile iOS | TestFlight → App Store | `cd-ios-mobile.yml` | `deploy/deploy-ios-mobile.sh` | Apple Developer (have) |
| MAUI Mobile Android | Google Play | `cd-android-mobile.yml` | `deploy/deploy-android-mobile.sh` | Google Play (have) |
| MAUI Desktop Mac | Mac App Store | `cd-mac-desktop.yml` | `deploy/deploy-mac-desktop.sh` | Apple Developer (have) |
| MAUI Desktop Windows | Microsoft Store | `cd-windows-desktop.yml` | `deploy/deploy-windows-desktop.sh` | Partner Center (missing) |

`cd-windows-desktop.yml` is disabled-by-default (`if: false` on the package
job) until the Partner Center account is set up. Every other store-bound
pipeline is a runnable skeleton: it builds the artifact, then prints a
"signing + upload stubbed" notice. The TODO blocks inside each workflow
explain exactly what to wire up once the signing material is in place.

## CI gate

`ci.yml` runs on every push, PR, and manual trigger. It builds + tests the
non-MAUI graph on `ubuntu-latest` (the MAUI projects can't restore on
linux without the platform workloads — they get exercised inside their own
CD pipelines on macOS / windows runners). Use it as a required status
check in branch protection.

## How they chain

```text
                              ┌─── cd-web.yml         (Azure)
                              │
push to master ─► ci.yml ─────┼─── cd-webapi.yml      (Azure)
                              │
                              ├─── cd-mac-desktop.yml (App Store)
                              ├─── cd-ios-mobile.yml  (TestFlight)
                              ├─── cd-android-mobile.yml (Play)
                              └─── cd-windows-desktop.yml (disabled)
```

Each CD workflow has its own `workflow_dispatch` trigger so you can run
them independently from the Actions tab. Each also has an opt-in
`workflow_run` block (commented out by default) that fires it
automatically after `CI` succeeds on `master` — uncomment per platform if
you want auto-deploy.

Each CD also runs its own build+test gate by default. The `run_tests`
input on the `workflow_dispatch` form lets you skip it for back-to-back
re-deploys.

## Local manual deploy

Every CD has a sibling `deploy/*.sh`. They mirror the workflow logic so
you can ship from a terminal — useful for hot-fixes, off-hour deploys, or
when you want to demo the pipeline without going through GitHub.

```bash
./deploy/deploy-web.sh            # full deploy
./deploy/deploy-web.sh --dry-run  # build + test only
```

The Apple and Google ones additionally need signing material installed in
the user's environment (Keychain on macOS, keystore in `~/.bronnoysund`
for Android). The script prints a one-line "missing X" hint if anything is
absent.

## Azure resources used (Web + WebApi)

| Resource | Name |
| --- | --- |
| Resource Group | `bronnoysund-lookup-rg` (`norwayeast`) |
| Container Registry | `bronnoysundlookup30020.azurecr.io` |
| Container Apps env | `bronnoysund-env` |
| Container App — Web | `bronnoysund-web` (BlazorWeb, port 8080) |
| Container App — WebApi | `bronnoysund-webapi` (port 8080) |
| Azure AD App Registration | `github-bronnoysund` (federated credential for OIDC) |
| GitHub repo secrets | `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` (workflows read via `secrets.*`) |

The federated credential trusts OIDC tokens with subject
`repo:erlingsm/Bronnoysund.Lookup:ref:refs/heads/master`. The service
principal has `AcrPush` on the registry and `Contributor` on the two
Container Apps only — nothing wider.

## One-time provisioning of bronnoysund-web

The container app for BlazorWeb does not yet exist. Run once:

```bash
az containerapp create \
    --name bronnoysund-web \
    --resource-group bronnoysund-lookup-rg \
    --environment bronnoysund-env \
    --image "mcr.microsoft.com/k8se/quickstart:latest" \
    --target-port 8080 --ingress external \
    --min-replicas 1 --max-replicas 1 \
    --cpu 0.5 --memory 1.0Gi
```

After that, `deploy-web.sh` (or the GitHub workflow) flips the image to
the real BlazorWeb build.

## Teardown

```bash
./deploy/teardown-web.sh         # prints what would happen, exits
./deploy/teardown-web.sh --yes   # async delete of the resource group
```

The Azure AD App Registration lives in the tenant, not the resource group,
and survives `az group delete`. Remove it separately if you want a fully
clean slate.
