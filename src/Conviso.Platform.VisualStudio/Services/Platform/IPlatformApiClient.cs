using System.Threading;
using System.Threading.Tasks;

namespace Conviso.Platform.VisualStudio.Services.Platform
{
    internal interface IPlatformApiClient
    {
        Task<string> QueryAsync(string graphqlDocument, string variablesJson, CancellationToken cancellationToken);
    }
}
