using System;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.Services.Patching
{
    internal sealed class DocumentPatchService : IPatchService
    {
        private readonly AsyncPackage package;

        public DocumentPatchService(AsyncPackage package)
        {
            this.package = package;
        }

        public async Task ApplyPatchAsync(string filePath, string replacement, CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
            if (dte?.ActiveDocument == null)
            {
                throw new InvalidOperationException("Open the target file before applying the suggested fix.");
            }

            var document = dte.ActiveDocument;
            if (!string.IsNullOrWhiteSpace(filePath) &&
                !string.Equals(document.FullName, filePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The active editor does not match the file that was analyzed.");
            }

            TextDocument? textDocument;
            try
            {
                textDocument = document.Object("TextDocument") as TextDocument;
            }
            catch
            {
                throw new InvalidOperationException("The active document does not support text editing.");
            }

            if (textDocument?.Selection == null || textDocument.Selection.IsEmpty)
            {
                throw new InvalidOperationException("Select the target code region before applying the suggested fix.");
            }

            textDocument.Selection.Delete();
            textDocument.Selection.Insert(replacement, (int)vsInsertFlags.vsInsertFlagsContainNewText);
        }
    }
}
