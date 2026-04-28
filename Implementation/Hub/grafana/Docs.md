# grafana

## Purpose and Boundary

Grafana configuration files used for automated dashboard provisioning.
This folder contains everything Grafana needs to start up with datasources and dashboards already configured, without any manual UI steps.

## Subfolders

| Subfolder     | Purpose                                                                          |
| ------------- | -------------------------------------------------------------------------------- |
| provisioning/ | Auto-loaded by Grafana on startup; contains datasource and dashboard definitions |

## Interactions and Constraints

- Grafana reads the provisioning/ subfolder automatically at startup via the GF_PATHS_PROVISIONING environment variable or the default /etc/grafana/provisioning path inside the container.
- Changes to files here take effect after a Grafana container restart (or reload via API).
- Do not store Grafana admin credentials or API keys in this folder.

## Relation to Parent Folder

Sits inside Hub.
Mounted as a read-only volume in docker-compose.yml for the grafana container service.
The volume mount maps this folder (or its provisioning/ subfolder) to /etc/grafana/provisioning inside the container.
