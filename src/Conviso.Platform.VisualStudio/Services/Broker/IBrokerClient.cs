using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.Models;

namespace Conviso.Platform.VisualStudio.Services.Broker
{
    public interface IBrokerClient
    {
        event System.Action<BrokerEvent>? EventReceived;

        bool IsConnected { get; }

        Task ConnectAsync(BrokerConnectionOptions options, CancellationToken cancellationToken);

        Task<string> SendChatMessageAsync(ChatMessage message, CancellationToken cancellationToken);

        Task<AutoFixResult> RequestAutoFixAsync(string findingId, CancellationToken cancellationToken);

        Task UpdateExtractorAcceptedAsync(int extractorId, CancellationToken cancellationToken);

        Task DisconnectAsync(CancellationToken cancellationToken);
    }
}
