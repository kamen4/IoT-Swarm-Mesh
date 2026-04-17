# Server Ops And Observability

## Minimum operational telemetry

- Onboarding outcome counts by stage.
- Command lifecycle counts (sent, acked, timeout, failed).
- Gateway link availability and UART error rate.
- Queue depth and processing latency.
- Device online/offline visibility.

## Logging requirements

- Structured logs with correlation keys.
- Security events for onboarding, revoke, role changes.
- Failure logs for parse/auth/timeout paths.

## OPEN DECISIONS

- Alert thresholds.
- Retention and aggregation windows.
- Dashboard layout conventions.
