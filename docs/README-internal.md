# ide-visualstudio-plugin

Visual Studio 2022 extension for Conviso Platform integration.

## What this repository is

This repository is the Visual Studio port of the existing Conviso IDE plugin ecosystem.
It is no longer just a blank scaffold: the extension already exposes commands, tool windows, settings persistence, GraphQL-backed views, and broker-backed chat actions.

What is still true is that the port has not been validated end to end on a Windows + Visual Studio environment yet. Because of that, the main risk today is not missing code structure, but unclear operator guidance and unknown runtime gaps.

## Current scope

Implemented in the repository:

- VSIX project and solution structure for Visual Studio 2022
- `AsyncPackage` bootstrap and command registration
- `Conviso Settings` tool window
- `Conviso Chat` tool window with:
  - broker connection
  - free-form chat
  - attach selection
  - analyze selected code
  - check similar issues across the workspace
  - apply suggested fix from the latest assistant response
  - mark response as helpful
- `Conviso Vulnerabilities` tool window with:
  - globally configured company
  - asset filter
  - vulnerability list
  - vulnerability details
  - generate-fix action
  - vulnerability status update
- `Conviso Requirements` tool window with:
  - projects list
  - requirement list per project
  - activities list per requirement
  - project status update
  - activity status update
- `Conviso Pipeline Breaks` tool window with list and details
- GraphQL diagnostics routed to the Visual Studio ActivityLog
- Secrets stored with Windows user protection through the settings service

Known gaps and risks:

- no validated Windows test pass yet
- build and installation flow has not been documented until now
- broker compatibility still depends on the target environment
- local AST scanner parity with the VS Code extension is not present
- there is no automated validation in this repository for the VSIX runtime behavior

## Quick start

### Prerequisites

- Windows
- Visual Studio 2022
- access to a Conviso API token
- access to a broker endpoint and API key when chat features are used

### Install and open

For end users and QA:

1. Install the provided `.vsix` package.
2. Open Visual Studio 2022.
3. Open a repository or project with editable source files.

For maintainers preparing a test build:

1. Open `Conviso.Platform.VisualStudio.sln` in Visual Studio 2022.
2. Restore packages if Visual Studio prompts for it.
3. Build the solution in `Debug` or `Release`.
4. Start the extension in the Experimental Instance with `F5`, or distribute the generated `.vsix` on Windows.

Notes:

- The project targets `.NET Framework 4.7.2`.
- This repository can be edited on macOS, but the VSIX cannot be validated end to end outside Windows + Visual Studio 2022.

## How to use the extension

After installation, open the commands from the Visual Studio `Tools` menu:

- `Conviso Chat`
- `Conviso Vulnerabilities`
- `Conviso Requirements`
- `Conviso Pipeline Breaks`
- `Conviso Settings`
- `Analyze + Suggest Fix`
- `Attach Selection to Chat`
- `Check Similar Issues`

### 1. Configure settings first

Open `Tools > Conviso Settings` and fill:

- `API Token`
- `Company` on the `Scope` tab

Useful behavior already implemented:

- opening `Scope` fetches accessible companies from the API
- the first accessible company is selected automatically on first configuration
- changing `Company` persists the global company immediately
- `Save Settings` stores the API token with Windows user protection
- `Test API` performs a GraphQL round-trip
- `Test Broker` attempts a WebSocket connection

### 2. Use chat features

Open `Tools > Conviso Chat`.

The chat window supports:

- `Send Message`
- `Attach Selection`
- `Analyze + Suggest Fix`
- `Check Similar Issues`
- `Apply Suggested Fix`
- `Mark Helpful`

The broker connection is established automatically when a chat action needs it. A centered `Thinking...` indicator uses a permanently reserved line and is empty when no response is pending.

Expected workflow:

1. Open a file and select the code you want to analyze.
2. Use `Attach Selection` if you want the selected code added as context to the chat session.
3. Use `Analyze + Suggest Fix` to send the current selection for analysis.
4. Use `Check Similar Issues` to scan the workspace for similar patterns.
5. If the assistant returns a fenced code block, use `Apply Suggested Fix` to replace the current selection after confirmation.

Important behavior:

- `Analyze + Suggest Fix`, `Attach Selection to Chat`, and `Check Similar Issues` are also exposed as top-level menu commands and will open the chat window automatically.
- `Apply Suggested Fix` only works when there is a current editor selection and the assistant response contains a fenced code block.

### 3. Use data views

`Conviso Vulnerabilities`

- use the globally configured company and filter by asset
- refresh the list
- inspect details
- generate a fix suggestion
- update vulnerability status

`Conviso Requirements`

- load projects
- inspect project details
- inspect requirements for a selected project
- inspect activities for a selected requirement
- update project status
- update activity status

`Conviso Pipeline Breaks`

- load the list of pipeline break executions
- inspect execution details for the selected row

## Recommended validation flow

Use the checklist in `docs/manual-validation.md` before calling this port production-ready.

## Troubleshooting

- If API requests fail, inspect the Visual Studio ActivityLog. GraphQL request and response details are logged there.
- If company discovery fails, verify `API Base URL` and `API Token` first.
- If chat does not connect, verify the broker endpoint resolves to `/ws` and that the broker API key is valid.
- If `Apply Suggested Fix` is disabled or fails, confirm that:
  - the assistant returned a fenced code block
  - the target file is open
  - the intended replacement region is selected

## Repository layout

- `src/Conviso.Platform.VisualStudio`: Visual Studio VSIX project
- `docs/architecture.md`: port architecture and runtime boundaries
- `docs/manual-validation.md`: manual test checklist for Windows validation

## References

- `../platform-ide-plugins/docs/ide-adapters.md`
- `../platform-ide-plugins/docs/protocol.md`
- `../ide-vscode-plugin/docs/architecture.md`
