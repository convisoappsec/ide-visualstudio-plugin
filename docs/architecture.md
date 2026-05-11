# Architecture

## Goal

This repository is the Visual Studio port of the Conviso IDE plugin ecosystem.

The first objective is structural parity with the VS Code plugin:

- keep integration services isolated from IDE wiring
- keep UI orchestration thin
- preserve the broker/API boundary
- keep patch application behind an explicit confirmation flow

The current codebase already implements that structure and a first usable runtime slice. What is still missing is validation on a real Windows + Visual Studio environment and a tighter parity pass against the VS Code plugin.

## Main layers

- `Package`: Visual Studio bootstrap and command registration
- `Commands`: entry points from menus/toolbars
- `ToolWindows`: host-specific UI containers
- `ViewModels`: UI state and workflow orchestration for each tool window
- `Services`: broker, API, editor context, patching, and configuration-backed state
- `Models`: transport and view models
- `Configuration`: option keys and defaults
- `Infrastructure`: command helpers, observable primitives, and diagnostics logging

## Initial mapping from VS Code

- `src/services/apiService.ts` -> `Services/Platform/*`
- `src/services/brokerService.ts` -> `Services/Broker/*`
- `src/features/*` -> future orchestration classes under `Features/*`
- `src/views/*` -> `ToolWindows/*` and future WPF view models
- `src/services/localAstService.ts` -> future local scanner service for Windows shells

Current parity note:

- GraphQL request failures are logged to the Visual Studio ActivityLog with request and response details, mirroring the stricter diagnostics added to the VS Code extension.
- Chat, vulnerabilities, requirements, and pipeline-break flows now exist as Visual Studio-native tool windows, even though they have not been fully validated yet.

## UI direction

Visual Studio does not map directly to VS Code's TreeView/Webview split.
The initial host shell uses:

- menu command for entry
- tool window for chat and future repository views
- WPF user controls inside the tool window

Current tool windows:

- Chat
- Vulnerabilities
- Requirements
- Pipeline Breaks
- Settings

## Port strategy

1. Load a stable VSIX package and register all menu entry points.
2. Persist configuration and secrets locally.
3. Support broker-backed chat and editor-driven actions.
4. Support GraphQL-backed list/detail windows for platform data.
5. Validate runtime behavior on Windows and close parity gaps found in testing.

## Runtime boundaries

- Chat flows use the broker WebSocket endpoint.
- Platform data flows use direct GraphQL/API calls.
- Editor-aware actions depend on Visual Studio selection and open-document context.
- Patch application is local and guarded by an explicit confirmation dialog.
- Diagnostics for GraphQL failures go to the Visual Studio ActivityLog.

## Known gaps

- The repository has not gone through a complete Windows validation pass yet.
- There is no automated coverage for VSIX runtime behavior.
- Local AST scanner parity with the VS Code plugin is still pending.
- Operational guidance now lives mostly in `README.md` and `docs/manual-validation.md`, but the runtime itself still needs confirmation.

## Build assumptions

- Visual Studio 2022
- .NET Framework 4.7.2
- Windows host with `Visual Studio extension development` workload

This repository should now be treated as an unvalidated port rather than a pure scaffold. The code already contains the main extension seams and first-pass feature wiring; the next step is disciplined manual validation and targeted fixes from the gaps that testing exposes.
