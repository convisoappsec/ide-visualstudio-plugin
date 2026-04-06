# ide-visualstudio-plugin

Visual Studio plugin for Conviso Platform integration.

## Status

This repository now contains the initial Visual Studio 2022 extension scaffold for the Conviso Platform port.

Current scope:

- VSIX project and solution structure
- `AsyncPackage` bootstrap
- command registration for chat, vulnerabilities, requirements, and pipeline breaks
- chat, vulnerabilities, requirements, and pipeline-break tool windows
- settings service and direct GraphQL API client shell
- broker client shell based on `ClientWebSocket`
- service contracts for broker, platform API, and patching
- architecture notes aligned with the VS Code reference plugin

Not implemented yet:

- WebSocket chat transport via `ide-broker-dotnet`
- GraphQL/API calls
- vulnerability and requirement views
- suggested patch application
- credential persistence

## Integration model

- AI chat via go-drill WebSocket (`/ws`) using `ide-broker-dotnet`
- Vulnerabilities/projects/requirements/status via direct API (GraphQL/REST)
- Plugin stays thin: business rules remain in broker/backend services

## Project layout

- `src/Conviso.Platform.VisualStudio`: Visual Studio VSIX project
- `docs/architecture.md`: port architecture and parity plan

## MVP parity target

1. Streaming chat
2. Vulnerability listing
3. Requirement list
4. Status update
5. Patch application with confirmation

## Build notes

- Target IDE: Visual Studio 2022
- Target framework: .NET Framework 4.7.2
- This scaffold is meant to be built on Windows with the `Visual Studio extension development` workload installed.
- The current macOS environment is sufficient to author the project files, but not to validate the VSIX end to end.

## References

- `../platform-ide-plugins/docs/ide-adapters.md`
- `../platform-ide-plugins/docs/protocol.md`
- `../ide-vscode-plugin/docs/architecture.md`

## Repository navigation

- Platform orchestrator/docs: [`platform-ide-plugins`](https://github.com/convisoappsec/platform-ide-plugins)
- Canonical chat spec: [`ide-broker`](https://github.com/convisoappsec/ide-broker)
- JavaScript runtime SDK: [`ide-broker-js`](https://github.com/convisoappsec/ide-broker-js)
- VS Code plugin: [`ide-vscode-plugin`](https://github.com/convisoappsec/ide-vscode-plugin)
- IntelliJ plugin: [`ide-intellij-plugin`](https://github.com/convisoappsec/ide-intellij-plugin)
- Visual Studio plugin (this repo): [`ide-visualstudio-plugin`](https://github.com/convisoappsec/ide-visualstudio-plugin)
- Eclipse plugin: [`ide-eclipse-plugin`](https://github.com/convisoappsec/ide-eclipse-plugin)
