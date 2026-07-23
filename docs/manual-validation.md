# Manual validation

This repository has not been fully validated on Windows yet.
Use this checklist to verify the installed extension with the smallest possible ambiguity.

This guide assumes the tester is an end user, not a developer:

- no repository access is required
- no solution build is required
- no Visual Studio extension workload is required
- the tester only needs an installed `.vsix`, credentials, and a repository or project opened in Visual Studio

## Test environment

- Windows machine
- Visual Studio 2022
- the Conviso Visual Studio extension already installed from a `.vsix`
- access to a valid Conviso API token
- access to a broker endpoint and API key
- a sample repository opened in Visual Studio with editable source files

## What the internal team must provide first

Before the tester starts, the internal team must provide:

- a `.vsix` package ready to install
- installation instructions
- the API base URL to use
- an API token with access to at least one company
- the broker endpoint
- the broker API key
- one sample project or repository the tester can open in Visual Studio

## Installation check

1. Install the provided `.vsix`.
2. Start Visual Studio 2022.
3. Open any sample project or repository.
4. Confirm the extension loads without startup errors.

Expected result:

- Visual Studio starts normally.
- Conviso commands appear under `Tools`.
- No package load failure dialog appears.

## Settings window

1. Open `Tools > Conviso Settings`.
2. Verify all fields render:
   - API Token
   - Company
3. Enter a valid API token.
4. Click `Save Settings`.
5. Click `Test API`.
6. Open the `Scope` tab.
7. Confirm that accessible companies load automatically.
8. If this is the first configuration, confirm that the first company is selected automatically.
9. Select another company, close the settings window, reopen it, and return to `Scope`.
10. Click `Test Chat`.

Expected result:

- status messages change coherently after each action
- accessible companies load for a valid token
- the selected company is saved immediately and restored when settings are reopened
- API test succeeds or returns a clear permission error
- chat test succeeds or returns a clear connection error

## Chat window

1. Open `Tools > Conviso Chat`.
2. Send a plain message without connecting manually.
3. Confirm that `Thinking...` appears centered between the transcript and message input while the response is pending, then changes to an empty reserved line.
4. Open a code file, select a snippet, and click `Attach Selection`.
5. Click `Analyze + Suggest Fix`.
6. Click `Check Similar Issues`.
7. If a response contains a fenced code block, select the target code and click `Apply Suggested Fix`.
8. Click `Mark Helpful`.

Expected result:

- the chat connects automatically when an action needs the broker
- transcript shows user and assistant messages
- the transient `Thinking...` indicator is empty when no response is pending without shifting the input
- attach-selection updates the context summary
- analyze and similarity actions append responses instead of failing silently
- apply-fix asks for confirmation and replaces only the current selection

## Menu command shortcuts

1. Select code in the editor.
2. Run `Tools > Analyze + Suggest Fix`.
3. Run `Tools > Attach Selection to Chat`.
4. Run `Tools > Check Similar Issues`.

Expected result:

- each command opens the chat window automatically
- the expected action runs without requiring manual navigation

## Vulnerabilities window

1. Open `Tools > Conviso Vulnerabilities`.
2. Confirm that data uses the company configured in `Conviso Settings > Scope`.
3. Change the asset filter.
4. Click `Refresh`.
5. Select a vulnerability row.
6. Review the details panel.
7. Click `Generate Fix`.
8. Enter a new status and click `Update Status`.

Expected result:

- list loads without UI crashes
- the title is the first column and horizontal scrolling is available when columns overflow
- filters do not break the view
- selecting a row loads details
- generate-fix returns a meaningful result or a clear backend error
- update-status persists the new status or returns a clear failure

## Requirements window

1. Open `Tools > Conviso Requirements`.
2. Click `Refresh`.
3. Select a project.
4. Verify project details load.
5. Select a requirement.
6. Verify requirement details and activities load.
7. Select an activity.
8. Change project status and click `Update Project`.
9. Change activity status and click `Update Activity`.

Expected result:

- projects load
- project, requirement, and activity lists show horizontal scrollbars when their columns overflow
- selecting a project populates requirements
- selecting a requirement populates activities
- detail sidebars update correctly
- status updates succeed or fail with clear feedback

## Pipeline breaks window

1. Open `Tools > Conviso Pipeline Breaks`.
2. Click `Refresh`.
3. Select an item.

Expected result:

- list loads
- the list shows a horizontal scrollbar when its columns overflow
- selecting a row loads execution details
- empty-state or setup-state messages remain readable

## Diagnostics to capture

Capture these artifacts for any failure:

- Visual Studio version and edition
- whether the issue happened immediately after installation or during normal use
- the exact command or button clicked
- the status text shown in the tool window
- ActivityLog entries related to `Conviso Platform`
- screenshots for UI inconsistencies

## Exit criteria

Consider the current port minimally validated only after:

- all windows open successfully
- settings can be saved and reloaded
- API connectivity is confirmed
- broker connectivity is confirmed
- chat round-trip works
- at least one editor-driven action works from the `Tools` menu
- at least one end-to-end list/detail flow works in each data window
