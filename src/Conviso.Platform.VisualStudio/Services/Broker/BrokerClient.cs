using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.Models;

namespace Conviso.Platform.VisualStudio.Services.Broker
{
    internal sealed class BrokerClient : IBrokerClient
    {
        private const int ConnectTimeoutMilliseconds = 15000;
        private ClientWebSocket? socket;
        private CancellationTokenSource? receiveLoopCancellation;
        private TaskCompletionSource<bool>? authenticationCompletionSource;

        public event Action<BrokerEvent>? EventReceived;

        public bool IsConnected => socket != null && socket.State == WebSocketState.Open;

        public async Task ConnectAsync(BrokerConnectionOptions options, CancellationToken cancellationToken)
        {
            await DisconnectAsync(cancellationToken);

            socket = new ClientWebSocket();
            string endpoint = NormalizeEndpoint(options.Endpoint);
            string apiKey = options.ApiKey?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Missing chat API key.");
            }

            authenticationCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            await socket.ConnectAsync(new Uri(endpoint), cancellationToken);
            receiveLoopCancellation = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(socket, receiveLoopCancellation.Token), receiveLoopCancellation.Token);

            string authRequestId = CreateRequestId("auth");
            await SendMessageAsync(
                socket,
                new
                {
                    type = "auth",
                    request_id = authRequestId,
                    payload = new
                    {
                        api_key = apiKey,
                    },
                },
                cancellationToken);

            using var timeoutCancellation = new CancellationTokenSource(ConnectTimeoutMilliseconds);
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCancellation.Token);

            using (linkedCancellation.Token.Register(() => authenticationCompletionSource.TrySetCanceled(), useSynchronizationContext: false))
            {
                try
                {
                    await authenticationCompletionSource.Task;
                }
                catch (TaskCanceledException) when (timeoutCancellation.IsCancellationRequested)
                {
                    throw new TimeoutException("Cannot reach chat service. Verify URL and local environment.");
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
            }
        }

        public async Task<string> SendChatMessageAsync(ChatMessage message, CancellationToken cancellationToken)
        {
            if (socket == null || socket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("Broker is not connected.");
            }

            string requestId = CreateRequestId("req");
            await SendMessageAsync(
                socket,
                new
                {
                    type = "analyze_code",
                    request_id = requestId,
                    payload = new
                    {
                        code = message.Content,
                        language = string.IsNullOrWhiteSpace(message.Language) ? "text" : message.Language,
                    },
                },
                cancellationToken);
            return requestId;
        }

        public async Task<AutoFixResult> RequestAutoFixAsync(string findingId, CancellationToken cancellationToken)
        {
            if (socket == null || socket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("Broker is not connected.");
            }

            if (string.IsNullOrWhiteSpace(findingId))
            {
                throw new InvalidOperationException("Missing vulnerability identifier for autofix.");
            }

            string requestId = CreateRequestId("req");
            var responseCompletionSource = new TaskCompletionSource<BrokerEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

            void HandleEvent(BrokerEvent brokerEvent)
            {
                if (!string.Equals(brokerEvent.RequestId, requestId, StringComparison.Ordinal))
                {
                    return;
                }

                if (brokerEvent.Type == "autofix_complete" || brokerEvent.Type == "autofix_suggested")
                {
                    responseCompletionSource.TrySetResult(brokerEvent);
                    return;
                }

                if (brokerEvent.Type == "autofix_error" || brokerEvent.Type == "error")
                {
                    responseCompletionSource.TrySetException(new InvalidOperationException(
                        string.IsNullOrWhiteSpace(brokerEvent.Content)
                            ? "Autofix request failed."
                            : brokerEvent.Content));
                }
            }

            EventReceived += HandleEvent;

            try
            {
                await SendMessageAsync(
                    socket,
                    new
                    {
                        type = "request_autofix",
                        request_id = requestId,
                        payload = new
                        {
                            finding_id = findingId,
                        },
                    },
                    cancellationToken);

                using var timeoutCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutCancellation.Token);

                using (linkedCancellation.Token.Register(
                    () => responseCompletionSource.TrySetCanceled(),
                    useSynchronizationContext: false))
                {
                    try
                    {
                        BrokerEvent response = await responseCompletionSource.Task;
                        return ExtractAutoFixResult(response.RawPayload);
                    }
                    catch (TaskCanceledException) when (timeoutCancellation.IsCancellationRequested)
                    {
                        throw new TimeoutException("Timeout waiting for autofix completion.");
                    }
                }
            }
            finally
            {
                EventReceived -= HandleEvent;
            }
        }

        public async Task UpdateExtractorAcceptedAsync(int extractorId, CancellationToken cancellationToken)
        {
            if (socket == null || socket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("Broker is not connected.");
            }

            if (extractorId <= 0)
            {
                throw new InvalidOperationException("Missing extractor identifier for acceptance update.");
            }

            string requestId = CreateRequestId("req");
            var responseCompletionSource = new TaskCompletionSource<BrokerEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

            void HandleEvent(BrokerEvent brokerEvent)
            {
                if (!string.Equals(brokerEvent.RequestId, requestId, StringComparison.Ordinal))
                {
                    return;
                }

                if (brokerEvent.Type == "success")
                {
                    responseCompletionSource.TrySetResult(brokerEvent);
                    return;
                }

                if (brokerEvent.Type == "error")
                {
                    responseCompletionSource.TrySetException(new InvalidOperationException(
                        string.IsNullOrWhiteSpace(brokerEvent.Content)
                            ? "Unable to update extractor acceptance."
                            : brokerEvent.Content));
                }
            }

            EventReceived += HandleEvent;

            try
            {
                await SendMessageAsync(
                    socket,
                    new
                    {
                        type = "update_extractor",
                        request_id = requestId,
                        payload = new
                        {
                            extractor_id = extractorId,
                        },
                    },
                    cancellationToken);

                using var timeoutCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutCancellation.Token);

                using (linkedCancellation.Token.Register(
                    () => responseCompletionSource.TrySetCanceled(),
                    useSynchronizationContext: false))
                {
                    try
                    {
                        await responseCompletionSource.Task;
                    }
                    catch (TaskCanceledException) when (timeoutCancellation.IsCancellationRequested)
                    {
                        throw new TimeoutException("Timeout waiting for extractor acceptance update.");
                    }
                }
            }
            finally
            {
                EventReceived -= HandleEvent;
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            authenticationCompletionSource?.TrySetCanceled();
            authenticationCompletionSource = null;
            receiveLoopCancellation?.Cancel();
            receiveLoopCancellation?.Dispose();
            receiveLoopCancellation = null;

            ClientWebSocket? activeSocket = socket;
            socket = null;
            if (activeSocket == null)
            {
                return;
            }

            try
            {
                if (activeSocket.State == WebSocketState.Open)
                {
                    await activeSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "disconnect", cancellationToken);
                }
            }
            catch (WebSocketException) when (activeSocket.State == WebSocketState.Aborted ||
                                              activeSocket.State == WebSocketState.Closed)
            {
                // An aborted socket is already disconnected; cleanup is enough.
            }
            catch (InvalidOperationException) when (activeSocket.State == WebSocketState.Aborted ||
                                                    activeSocket.State == WebSocketState.Closed)
            {
                // CloseAsync cannot be used after the receive loop aborts the socket.
            }
            finally
            {
                activeSocket.Dispose();
            }
        }

        private async Task ReceiveLoopAsync(ClientWebSocket activeSocket, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];

            while (!cancellationToken.IsCancellationRequested && activeSocket.State == WebSocketState.Open)
            {
                var builder = new StringBuilder();
                WebSocketReceiveResult result;

                do
                {
                    result = await activeSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        authenticationCompletionSource?.TrySetException(
                            new InvalidOperationException("Chat connection closed before authentication completed."));
                        return;
                    }

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                string raw = builder.ToString();
                ProcessIncomingMessage(raw);
            }
        }

        private void ProcessIncomingMessage(string raw)
        {
            BrokerEvent brokerEvent = ParseEvent(raw);

            if (brokerEvent.Type == "auth_response")
            {
                if (brokerEvent.Status == "success")
                {
                    authenticationCompletionSource?.TrySetResult(true);
                    authenticationCompletionSource = null;
                }
                else
                {
                    authenticationCompletionSource?.TrySetException(
                        new InvalidOperationException(string.IsNullOrWhiteSpace(brokerEvent.Content)
                            ? "Chat authentication failed."
                            : brokerEvent.Content));
                    authenticationCompletionSource = null;
                }

                return;
            }

            if (brokerEvent.Type == "auth_error")
            {
                authenticationCompletionSource?.TrySetException(
                    new InvalidOperationException(string.IsNullOrWhiteSpace(brokerEvent.Content)
                        ? "Chat authentication failed."
                        : brokerEvent.Content));
                authenticationCompletionSource = null;
                return;
            }

            EventReceived?.Invoke(brokerEvent);
        }

        private static BrokerEvent ParseEvent(string raw)
        {
            string type = "message";
            string requestId = "evt-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string content = raw;
            string status = string.Empty;

            try
            {
                using var document = JsonDocument.Parse(raw);
                if (document.RootElement.TryGetProperty("type", out JsonElement eventType) && eventType.ValueKind == JsonValueKind.String)
                {
                    type = eventType.GetString() ?? type;
                }

                if (document.RootElement.TryGetProperty("request_id", out JsonElement request) && request.ValueKind == JsonValueKind.String)
                {
                    requestId = request.GetString() ?? requestId;
                }

                if (document.RootElement.TryGetProperty("status", out JsonElement rootStatus) && rootStatus.ValueKind == JsonValueKind.String)
                {
                    status = rootStatus.GetString() ?? string.Empty;
                }

                if (document.RootElement.TryGetProperty("payload", out JsonElement payload))
                {
                    if (payload.TryGetProperty("status", out JsonElement payloadStatus) && payloadStatus.ValueKind == JsonValueKind.String)
                    {
                        status = payloadStatus.GetString() ?? status;
                    }

                    if (payload.TryGetProperty("content", out JsonElement payloadContent) && payloadContent.ValueKind == JsonValueKind.String)
                    {
                        return new BrokerEvent(type, requestId, payloadContent.GetString() ?? raw, raw, status);
                    }

                    if (payload.TryGetProperty("message", out JsonElement message) && message.ValueKind == JsonValueKind.String)
                    {
                        return new BrokerEvent(type, requestId, message.GetString() ?? raw, raw, status);
                    }

                    if (payload.TryGetProperty("chunk", out JsonElement chunk) && chunk.ValueKind == JsonValueKind.String)
                    {
                        return new BrokerEvent(type, requestId, chunk.GetString() ?? raw, raw, status);
                    }

                    if (payload.TryGetProperty("full_response", out JsonElement fullResponse) && fullResponse.ValueKind == JsonValueKind.String)
                    {
                        return new BrokerEvent(type, requestId, fullResponse.GetString() ?? raw, raw, status);
                    }
                }

                if (document.RootElement.TryGetProperty("message", out JsonElement rootMessage) && rootMessage.ValueKind == JsonValueKind.String)
                {
                    content = rootMessage.GetString() ?? raw;
                }
            }
            catch
            {
                // Fall back to raw payload for now.
            }

            return new BrokerEvent(type, requestId, content, raw, status);
        }

        private static async Task SendMessageAsync(
            ClientWebSocket socket,
            object payload,
            CancellationToken cancellationToken)
        {
            string json = JsonSerializer.Serialize(payload);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
        }

        private static AutoFixResult ExtractAutoFixResult(string rawPayload)
        {
            try
            {
                using var document = JsonDocument.Parse(rawPayload);
                JsonElement payload = document.RootElement.TryGetProperty("payload", out JsonElement payloadElement)
                    ? payloadElement
                    : document.RootElement;

                if (payload.TryGetProperty("success", out JsonElement successElement) &&
                    successElement.ValueKind == JsonValueKind.False)
                {
                    string errorMessage = "Autofix request failed.";
                    if (payload.TryGetProperty("error", out JsonElement errorElement) &&
                        errorElement.ValueKind == JsonValueKind.Object &&
                        errorElement.TryGetProperty("message", out JsonElement nestedMessage) &&
                        nestedMessage.ValueKind == JsonValueKind.String)
                    {
                        errorMessage = nestedMessage.GetString() ?? errorMessage;
                    }
                    else if (payload.TryGetProperty("message", out JsonElement payloadMessage) &&
                             payloadMessage.ValueKind == JsonValueKind.String)
                    {
                        errorMessage = payloadMessage.GetString() ?? errorMessage;
                    }

                    throw new InvalidOperationException(errorMessage);
                }

                JsonElement data = payload.TryGetProperty("data", out JsonElement dataElement) &&
                                   dataElement.ValueKind == JsonValueKind.Object
                    ? dataElement
                    : payload;

                string? prUrl = null;
                if (data.TryGetProperty("pr_url", out JsonElement prUrlElement) && prUrlElement.ValueKind == JsonValueKind.String)
                {
                    prUrl = prUrlElement.GetString();
                }
                else if (data.TryGetProperty("prUrl", out JsonElement prUrlAltElement) && prUrlAltElement.ValueKind == JsonValueKind.String)
                {
                    prUrl = prUrlAltElement.GetString();
                }

                string? summary = null;
                if (data.TryGetProperty("summary", out JsonElement summaryElement) && summaryElement.ValueKind == JsonValueKind.String)
                {
                    summary = summaryElement.GetString();
                }

                return new AutoFixResult
                {
                    PrUrl = string.IsNullOrWhiteSpace(prUrl) ? null : prUrl,
                    Summary = string.IsNullOrWhiteSpace(summary) ? null : summary,
                };
            }
            catch (JsonException)
            {
                return new AutoFixResult
                {
                    Summary = string.IsNullOrWhiteSpace(rawPayload) ? null : rawPayload,
                };
            }
        }

        private static string NormalizeEndpoint(string endpoint)
        {
            string trimmed = endpoint?.Trim().TrimEnd('/') ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                throw new InvalidOperationException("Missing chat endpoint.");
            }

            if (trimmed.EndsWith("/cable", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring(0, trimmed.Length - "/cable".Length) + "/ws";
            }

            if (trimmed.EndsWith("/ws", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            return trimmed + "/ws";
        }

        private static string CreateRequestId(string prefix)
        {
            return prefix + "-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}
