using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.Services.Editor
{
    internal sealed class EditorContextService : IEditorContextService
    {
        private const int MaxWorkspaceFiles = 20;
        private const int MaxWorkspaceCharacters = 50000;
        private readonly AsyncPackage package;

        public EditorContextService(AsyncPackage package)
        {
            this.package = package;
        }

        public async Task<EditorContextSnapshot?> GetActiveContextAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
            if (dte?.ActiveDocument == null)
            {
                return null;
            }

            var document = dte.ActiveDocument;
            string filePath = document.FullName ?? string.Empty;
            string language = DetectLanguage(filePath);

            TextDocument? textDocument = null;
            try
            {
                textDocument = document.Object("TextDocument") as TextDocument;
            }
            catch
            {
                return null;
            }

            if (textDocument == null)
            {
                return null;
            }

            EditPoint startPoint = textDocument.StartPoint.CreateEditPoint();
            string documentText = startPoint.GetText(textDocument.EndPoint);

            string selectionText = string.Empty;
            if (textDocument.Selection != null && !textDocument.Selection.IsEmpty)
            {
                selectionText = textDocument.Selection.Text ?? string.Empty;
            }

            return new EditorContextSnapshot
            {
                FilePath = filePath,
                Language = language,
                SelectionText = selectionText.Trim(),
                DocumentText = documentText.Trim(),
            };
        }

        public async Task<WorkspaceContextSnapshot?> GetWorkspaceContextAsync(
            EditorContextSnapshot reference,
            CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
            string rootPath = string.Empty;
            if (dte?.Solution != null && !string.IsNullOrWhiteSpace(dte.Solution.FullName))
            {
                rootPath = Path.GetDirectoryName(dte.Solution.FullName) ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(rootPath) && !string.IsNullOrWhiteSpace(reference.FilePath))
            {
                rootPath = Path.GetDirectoryName(reference.FilePath) ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                return null;
            }

            string extension = Path.GetExtension(reference.FilePath ?? string.Empty);
            var result = new WorkspaceContextSnapshot
            {
                RootPath = rootPath,
            };

            int totalLength = 0;
            foreach (string filePath in EnumerateWorkspaceFiles(rootPath, extension))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.Equals(filePath, reference.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string content;
                try
                {
                    content = File.ReadAllText(filePath).Trim();
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                content = Truncate(content, 4000);
                if (result.Files.Count >= MaxWorkspaceFiles || totalLength + content.Length > MaxWorkspaceCharacters)
                {
                    break;
                }

                result.Files.Add(new WorkspaceFileContext
                {
                    FilePath = filePath,
                    Language = DetectLanguage(filePath),
                    Content = content,
                });
                totalLength += content.Length;
            }

            return result;
        }

        private static string DetectLanguage(string filePath)
        {
            string extension = Path.GetExtension(filePath ?? string.Empty).ToLowerInvariant();
            switch (extension)
            {
                case ".cs":
                    return "csharp";
                case ".vb":
                    return "vb";
                case ".js":
                    return "javascript";
                case ".ts":
                    return "typescript";
                case ".json":
                    return "json";
                case ".xml":
                case ".config":
                    return "xml";
                case ".xaml":
                    return "xml";
                case ".sql":
                    return "sql";
                case ".ps1":
                    return "powershell";
                case ".yml":
                case ".yaml":
                    return "yaml";
                case ".md":
                    return "markdown";
                default:
                    return "text";
            }
        }

        private static IEnumerable<string> EnumerateWorkspaceFiles(string rootPath, string extension)
        {
            var excludedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".git",
                ".vs",
                "bin",
                "obj",
                "node_modules",
                "packages",
                "dist",
                "out",
                "build",
            };

            var pending = new Stack<string>();
            pending.Push(rootPath);

            while (pending.Count > 0)
            {
                string current = pending.Pop();
                IEnumerable<string> directories;
                try
                {
                    directories = Directory.EnumerateDirectories(current);
                }
                catch
                {
                    continue;
                }

                foreach (string directory in directories)
                {
                    if (excludedDirectories.Contains(Path.GetFileName(directory)))
                    {
                        continue;
                    }

                    pending.Push(directory);
                }

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(current);
                }
                catch
                {
                    continue;
                }

                foreach (string file in files)
                {
                    if (!string.IsNullOrWhiteSpace(extension) &&
                        !string.Equals(Path.GetExtension(file), extension, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return file;
                }
            }
        }

        private static string Truncate(string content, int maxLength)
        {
            if (content.Length <= maxLength)
            {
                return content;
            }

            return content.Substring(0, maxLength) +
                   "\n\n[truncated: original content exceeded " + maxLength + " characters]";
        }
    }
}
