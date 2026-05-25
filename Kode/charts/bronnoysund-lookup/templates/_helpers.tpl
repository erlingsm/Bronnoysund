{{/*
Expand the name of the chart.
*/}}
{{- define "bronnoysund-lookup.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Fully qualified app name (release-name-chart-name).
*/}}
{{- define "bronnoysund-lookup.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Chart name + version label.
*/}}
{{- define "bronnoysund-lookup.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Standard Kubernetes recommended labels.
*/}}
{{- define "bronnoysund-lookup.labels" -}}
helm.sh/chart: {{ include "bronnoysund-lookup.chart" . }}
{{ include "bronnoysund-lookup.selectorLabels" . }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels — must be stable across upgrades.
*/}}
{{- define "bronnoysund-lookup.selectorLabels" -}}
app.kubernetes.io/name: {{ include "bronnoysund-lookup.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}
