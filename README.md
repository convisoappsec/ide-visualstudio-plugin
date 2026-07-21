# Conviso Platform for Visual Studio

Analyze code, review security vulnerabilities, receive AI-powered remediation guidance, and interact with Conviso AI without leaving Visual Studio.

## 🔐 What can you do with Conviso Platform for Visual Studio?

- **Analyze code and receive AI-powered security recommendations.**  
  Select code in the editor to identify potential security issues, understand their risks, and receive practical remediation guidance.

- **Apply AI-assisted code fixes.**  
  When an AI response includes a suggested code block, apply it directly to the current editor selection after reviewing and confirming the change.

- **Review vulnerabilities from Conviso Platform.**  
  Filter vulnerabilities by company and asset, inspect their details, request an AI-generated fix, and update vulnerability status from Visual Studio.

- **Track projects, security requirements, and activities.**  
  Browse projects and their requirements, inspect related activities, and update project or activity status.

- **Investigate pipeline breaks.**  
  Review security gate executions and inspect their status, source, asset, trigger, and failure reasons.

- **Chat with Conviso AI using IDE context.**  
  Attach a code selection, analyze selected code, include workspace files when checking for similar issues, apply a suggested fix, and mark useful responses.

> The current Visual Studio extension does not include a standalone local repository scanner or a separate Repository Vulnerabilities view.

---

## 🔑 Getting access

You need a Conviso Platform account and a valid Platform API token.

If you do not have an account, sign up or start a free trial:

👉 https://www.convisoappsec.com/

---

## 🛠️ Installation and configuration

### Install the extension

1. Go to **Extensions > Manager Extensions > Online**.
2. Search for **Conviso Platform**.
3. Install.

### Configure Conviso Platform

1. In Visual Studio, open **Tools > Conviso Settings**.
2. On the **Platform API** tab, enter your **API Token**.
3. Select **Load Companies**, choose an accessible company, and select **Use Selected Company**.
4. If necessary, review the **Company ID** and **Requirements Scope ID** on the **Scope** tab.
5. Select **Save Settings**.

You can use **Test API** and **Test Chat** to validate the credentials. The API token is stored using Windows user-level data protection. The Platform API and AI service endpoints are configured by the extension and do not require manual URLs or separate broker credentials.

---

## 🚀 How to use

Open extension features from Visual Studio's **Tools** menu:

- **Conviso Chat** — opens **AI Autonomous AppSec** for security questions, code analysis, workspace similarity checks, and AI-assisted fixes.
- **Conviso Vulnerabilities** — reviews and manages vulnerabilities from Conviso Platform.
- **Conviso Requirements** — browses projects, requirements, and related activities.
- **Conviso Pipeline Breaks** — investigates security gate executions and their failure reasons.
- **Conviso Settings** — configures the API token, company, and requirements scope.

The **Tools** menu also provides direct editor actions:

- **Analyze + Suggest Fix**
- **Attach Selection to Chat**
- **Check Similar Issues**

### Analyze selected code

1. Open a source file and select the relevant code.
2. Choose **Tools > Analyze + Suggest Fix**, or open **Conviso Chat** and use **Analyze + Suggest Fix**.
3. Review the response in **AI Autonomous AppSec**.
4. If the response contains a fenced code block, select the destination code and choose **Apply Suggested Fix**.

The extension always asks for confirmation before replacing the current selection.

### Use selection and workspace context

- **Attach Selection** adds the current code selection to subsequent chat messages.
- **Clear** removes the attached selection.
- **Check Similar Issues** collects supported workspace files and asks Conviso AI to identify similar patterns.
- **Clear Chat** removes the current transcript.

### Manage vulnerabilities

1. Open **Tools > Conviso Vulnerabilities**.
2. Choose a company and optionally filter by asset.
3. Select a vulnerability to inspect its title, description, severity, status, and asset.
4. Use **Generate Fix** to request an AI-assisted remediation result.
5. Enter a permitted status and use **Update Status** to update the finding in Conviso Platform.

### Review requirements and pipeline breaks

- In **Conviso Requirements**, select a project to view its details and requirements, then select a requirement to inspect its activities. Project and activity status can be updated from their detail panels.
- In **Conviso Pipeline Breaks**, select an execution to inspect its status, date, trigger, source, asset, and severity-based failure reasons.

---

## 💡 Pro tip

Use Conviso Platform throughout development: attach the smallest relevant code selection for focused AI guidance, check for similar patterns across the workspace, and review Platform findings before committing your changes.

Build secure software without leaving **Visual Studio** using **Conviso Platform**.