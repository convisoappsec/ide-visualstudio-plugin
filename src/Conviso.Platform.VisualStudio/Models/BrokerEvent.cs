namespace Conviso.Platform.VisualStudio.Models
{
    public sealed class BrokerEvent
    {
        public BrokerEvent(string type, string requestId, string content, string rawPayload, string status = "")
        {
            Type = type;
            RequestId = requestId;
            Content = content;
            RawPayload = rawPayload;
            Status = status;
        }

        public string Type { get; }

        public string RequestId { get; }

        public string Content { get; }

        public string RawPayload { get; }

        public string Status { get; }
    }
}
