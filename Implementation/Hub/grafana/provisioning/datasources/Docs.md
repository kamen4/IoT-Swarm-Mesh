# datasources

## Purpose and Boundary

Grafana datasource YAML files loaded automatically at container startup.
This folder defines the data source connections that all dashboards in this deployment use.

## Files

| File                       | Purpose                                                                                               |
| -------------------------- | ----------------------------------------------------------------------------------------------------- |
| influxdb.yaml (or similar) | Defines the InfluxDB connection: URL, token/credentials, organization, bucket, and the datasource UID |

## Interactions and Constraints

- The InfluxDB datasource UID must be exactly "influxdb-mesh" (literal string, no spaces).
- All dashboard JSON files in provisioning/dashboards/ reference this UID directly; changing the UID here breaks all dashboards.
- The YAML file must conform to the Grafana datasource provisioning schema (apiVersion, datasources list).
- Credentials in the YAML (token, password) should be injected via environment variables where possible.
- A datasource with the same UID already existing in the Grafana database will be overwritten by the provisioned version on restart.

## Relation to Parent Folder

Sits inside provisioning/.
Grafana loads all YAML files in this folder at startup before dashboards are loaded, ensuring that the "influxdb-mesh" UID is available when dashboard JSON files are processed.
