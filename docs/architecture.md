# Architecture

## Goal

This repository is the Visual Studio port of the Conviso IDE plugin ecosystem.

The first objective is structural parity with the VS Code plugin:

- keep integration services isolated from IDE wiring
- keep UI orchestration thin
- preserve the broker/API boundary
- keep patch application behind an explicit confirmation flow

## Main layers

- `Package`: Visual Studio bootstrap and command registration
- `Commands`: entry points from menus/toolbars
- `ToolWindows`: host-specific UI containers
- `Services`: broker, API, patching, and future credential/state services
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

## UI direction

Visual Studio does not map directly to VS Code's TreeView/Webview split.
The initial host shell uses:

- menu command for entry
- tool window for chat and future repository views
- WPF user controls inside the tool window

Planned next tool windows:

- Vulnerabilities
- Projects and Requirements
- Pipeline Breaks

## Port strategy

1. Stand up a loadable VSIX package with command and tool window.
2. Add broker connectivity through `ide-broker-dotnet`.
3. Add API client and credential/configuration flows.
4. Add list views for vulnerabilities and requirements.
5. Add patch application and AI actions.

## Build assumptions

- Visual Studio 2022
- .NET Framework 4.7.2
- Windows host with `Visual Studio extension development` workload

This repository is intentionally scaffold-first right now. The current code is meant to create stable extension seams before the transport and API logic are moved in.
