# dashboards

## Purpose and Boundary

Grafana dashboard JSON files loaded automatically at container startup.
This folder also contains the dashboard provider YAML that tells Grafana where to find the JSON files.

## Files

| File          | Purpose                                                                                               |
| ------------- | ----------------------------------------------------------------------------------------------------- |
| provider.yaml | Dashboard provider configuration; points Grafana at this folder as the source of dashboard JSON files |
| *.json        | One or more Grafana dashboard export files                                                            |

## Interactions and Constraints

- The dashboard provider YAML (provider.yaml) must have its `path` field set to the folder path as seen inside the container (e.g., /etc/grafana/provisioning/dashboards).
- Dashboard JSON files must reference the datasource by its literal UID "influxdb-mesh" -- not a template variable.
- Using a template variable for the datasource UID will cause panels to fail if the variable is not defined at load time.
- Dashboard JSON files are provisioned as read-only by default; changes made in the Grafana UI are not persisted back to disk.

## Relation to Parent Folder

Sits inside provisioning/.
Grafana reads provider.yaml first, then loads all .json files in the configured path.
The datasource UID "influxdb-mesh" must exist (defined in provisioning/datasources/) before dashboards reference it.
