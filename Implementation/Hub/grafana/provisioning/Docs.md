# provisioning

## Purpose and Boundary

Auto-provisioned Grafana resources loaded at container startup.
This folder is the root of the Grafana provisioning tree; it contains subfolders for each resource type.

## Subfolders

| Subfolder    | Purpose                                                                             |
| ------------ | ----------------------------------------------------------------------------------- |
| datasources/ | YAML files defining data source connections (e.g., InfluxDB)                        |
| dashboards/  | JSON dashboard definitions and the provider YAML that points Grafana at this folder |

## Interactions and Constraints

- Grafana scans this folder tree at startup and applies all YAML and JSON files it finds.
- Files must be valid YAML (datasources) or valid Grafana dashboard JSON (dashboards).
- Malformed files cause Grafana to log an error and skip that resource; other resources are still loaded.
- Do not place secrets (passwords, tokens) directly in these files; use Grafana environment variable substitution where supported.

## Relation to Parent Folder

Sits inside grafana/.
Mounted at /etc/grafana/provisioning inside the Grafana container as specified in docker-compose.yml.
All subfolders here are read by Grafana automatically; no additional configuration is needed beyond the volume mount.
