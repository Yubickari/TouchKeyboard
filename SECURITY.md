# Security

TouchKeyboard is an embedded UI library. It makes no network requests of its
own and does not collect telemetry.

## Reporting a vulnerability

Please do not open a public issue. Use GitHub's private vulnerability reporting
on this repository (Security → Report a vulnerability), or contact the
maintainer directly. Include:

- the package and version,
- the Avalonia version and target framework,
- the smallest reproduction you can give with invented field names,
- what an attacker could do with it.

You will normally get an answer within a week.

## Scope worth knowing

- The keyboard edits a session buffer and writes back to the target control only
  on Confirm/Enter. Closing discards.
- Password fields are masked in the panel, but the value is held as a .NET
  string in process memory; it cannot be reliably zeroed. Do not log input.
- The library does not install global hooks, does not inject OS-level key
  events, and does not read files or the network.
- A host that injects its own `IEditorSurface` (for example a real `TextBox`)
  takes on that control's platform behaviour, including the system on-screen
  keyboard.
