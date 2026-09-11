; Unshipped analyzer release

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
PANENUM001 | Enumeration | Error | Enumeration and containing types must be partial.
PANENUM002 | Enumeration | Error | Enumeration and containing types cannot be file-local.
PANVO001 | ValueObject | Error | Generated value objects and containing types must be partial.
PANVO002 | ValueObject | Error | Generated value objects and containing types cannot be file-local.
PANVO003 | ValueObject | Error | Automatic equality requires public read-only or init-only auto-properties.
