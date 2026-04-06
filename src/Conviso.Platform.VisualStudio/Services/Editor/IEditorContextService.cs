using System.Threading;
using System.Threading.Tasks;

namespace Conviso.Platform.VisualStudio.Services.Editor
{
    public interface IEditorContextService
    {
        Task<EditorContextSnapshot?> GetActiveContextAsync(CancellationToken cancellationToken);

        Task<WorkspaceContextSnapshot?> GetWorkspaceContextAsync(
            EditorContextSnapshot reference,
            CancellationToken cancellationToken);
    }
}
