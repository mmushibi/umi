{{- define "umihealth.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "umihealth.fullname" -}}
{{- printf "%s-%s" (include "umihealth.name" .) .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
