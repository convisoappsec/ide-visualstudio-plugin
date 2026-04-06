using Conviso.Platform.VisualStudio.Configuration;
using Conviso.Platform.VisualStudio.Services.Broker;
using Conviso.Platform.VisualStudio.Services.Editor;
using Conviso.Platform.VisualStudio.Services.Patching;
using Conviso.Platform.VisualStudio.Services.Platform;

namespace Conviso.Platform.VisualStudio.Infrastructure
{
    public sealed class ToolWindowContext
    {
        public ToolWindowContext(
            ISettingsService settingsService,
            IPlatformFacade platformFacade,
            IBrokerClient brokerClient,
            IEditorContextService editorContextService,
            IPatchService patchService)
        {
            SettingsService = settingsService;
            PlatformFacade = platformFacade;
            BrokerClient = brokerClient;
            EditorContextService = editorContextService;
            PatchService = patchService;
        }

        public ISettingsService SettingsService { get; }

        public IPlatformFacade PlatformFacade { get; }

        public IBrokerClient BrokerClient { get; }

        public IEditorContextService EditorContextService { get; }

        public IPatchService PatchService { get; }
    }
}
