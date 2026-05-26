# bronnoysund — Helm chart

Helm chart for the `Bronnoysund.WebApi` service. Works on any standard Kubernetes
distribution — AKS, EKS, GKE, k3s, kind, minikube — and reuses the same Docker image we
push to Azure Container Apps. Pick this when you outgrow Container Apps or want to host
the service in your own cluster.

## Quick start

### 1. Add an image pull secret (if pulling from a private registry)

```bash
kubectl create secret docker-registry acr-creds \
  --docker-server=bronnoysundlookup30020.azurecr.io \
  --docker-username=<acr-admin-username> \
  --docker-password=<acr-admin-password>
```

### 2. Install

```bash
helm install bronnoysund ./charts/bronnoysund \
  --set imagePullSecrets[0].name=acr-creds
```

Or with custom values:

```bash
helm install bronnoysund ./charts/bronnoysund --values my-values.yaml
```

### 3. Verify

```bash
kubectl rollout status deployment/bronnoysund-bronnoysund
kubectl port-forward svc/bronnoysund-bronnoysund 8080:80
# then in another shell:
curl http://localhost:8080/health
curl http://localhost:8080/companies/974760843
```

## Common overrides

### Public ingress with cert-manager

`prod-values.yaml`:

```yaml
ingress:
  enabled: true
  className: nginx
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
  hosts:
    - host: brreg.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: brreg-tls
      hosts:
        - brreg.example.com
```

```bash
helm upgrade --install bronnoysund ./charts/bronnoysund \
  --values prod-values.yaml \
  --set imagePullSecrets[0].name=acr-creds
```

### Application Insights

Wire the WebApi to Azure Application Insights (also possible outside Azure as long as the
ingestion endpoint is reachable):

```yaml
secretEnv:
  APPLICATIONINSIGHTS_CONNECTION_STRING: "InstrumentationKey=...;IngestionEndpoint=..."
```

### Persistent storage for the SQLite cache

Off by default — each replica uses ephemeral storage and rebuilds the cache on restart.
Turn it on if you want history/favourites/cache to survive pod recreation:

```yaml
persistence:
  enabled: true
  storageClass: managed-csi   # or empty for cluster default
  size: 5Gi
env:
  BRONNOYSUND_DB_PATH: /data/bronnoysund.db
```

Note: SQLite does not handle concurrent writes from multiple replicas well. With
`persistence.enabled: true` you typically want `replicaCount: 1` and `autoscaling.enabled: false`,
or run a managed Postgres behind a different IDatabasePathProvider implementation.

### Pinned image tag

```yaml
image:
  repository: bronnoysundlookup30020.azurecr.io/bronnoysund-webapi
  tag: v4
  pullPolicy: IfNotPresent
```

### Lock down to a fixed number of replicas

```yaml
autoscaling:
  enabled: false
replicaCount: 3
```

## All values

See `values.yaml` for the complete annotated list. Highlights:

| Key | Default | Purpose |
|---|---|---|
| `image.tag` | `v4` | Container image tag |
| `replicaCount` | `2` | Used only when `autoscaling.enabled: false` |
| `autoscaling.enabled` | `true` | HPA on CPU + memory |
| `autoscaling.minReplicas` | `2` | |
| `autoscaling.maxReplicas` | `10` | |
| `resources.requests` | `250m CPU / 512Mi` | Matches Container Apps profile |
| `service.type` | `ClusterIP` | Switch to `LoadBalancer` for cluster-external access without ingress |
| `ingress.enabled` | `false` | Opt-in |
| `persistence.enabled` | `false` | Opt-in PVC at `/data` |
| `env.Brreg__CacheTtl` | `1.00:00:00` | 24 hours |
| `env.BRONNOYSUND_DB_PATH` | `/tmp/bronnoysund/bronnoysund.db` | Container-writable ephemeral path |
| `secretEnv` | `{}` | Keyed map mapped into a Kubernetes Secret |
| `podSecurityContext.runAsUser` | `10001` | Matches the appuser in the Dockerfile |

## Develop / test the chart

```bash
# Static template render (no cluster needed)
helm template bronnoysund ./charts/bronnoysund

# Lint
helm lint ./charts/bronnoysund

# Dry-run install against a real cluster
helm install bronnoysund ./charts/bronnoysund --dry-run --debug

# Test against a kind cluster
kind create cluster --name brreg
helm install bronnoysund ./charts/bronnoysund --set image.pullPolicy=IfNotPresent
kubectl port-forward svc/bronnoysund-bronnoysund 8080:80
```

## Uninstall

```bash
helm uninstall bronnoysund
# PVC (if persistence was enabled) is kept by default. Remove explicitly:
kubectl delete pvc bronnoysund-bronnoysund-data
```

## When to use this vs. Azure Container Apps

| | Container Apps | Helm chart |
|---|---|---|
| Time to first deploy | ~30 min | ~10 min (assuming cluster exists) |
| Scale-to-zero billing | ✅ Built-in | Needs KEDA or similar |
| Cluster operations skill | None | Required |
| Cost at low traffic | ~50-200 NOK/mo | ~minimum cluster cost (typically ≥ 1000 NOK/mo) |
| Multi-region / hybrid | Per-region resource | Standard cluster federation |
| Fine-grained networking | Limited | Full Kubernetes networking |

Bronnoysund uses Container Apps as the default deployment because it fits the
"small public API with scale-to-zero" sweet spot. This Helm chart is the migration path
when richer cluster integration becomes necessary.
