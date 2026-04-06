using System.Threading;
using System.Threading.Tasks;

namespace Conviso.Platform.VisualStudio.Services.Patching
{
    public interface IPatchService
    {
        Task ApplyPatchAsync(string filePath, string replacement, CancellationToken cancellationToken);
    }
}
