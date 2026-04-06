using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.Configuration;
using Conviso.Platform.VisualStudio.Commands;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.Services.Broker;
using Conviso.Platform.VisualStudio.Services.Editor;
using Conviso.Platform.VisualStudio.Services.Patching;
using Conviso.Platform.VisualStudio.Services.Platform;
using Conviso.Platform.VisualStudio.ToolWindows;
using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("Conviso Platform", "Conviso Platform integration for Visual Studio", "0.1.0")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ChatToolWindow))]
    [ProvideToolWindow(typeof(VulnerabilitiesToolWindow))]
    [ProvideToolWindow(typeof(RequirementsToolWindow))]
    [ProvideToolWindow(typeof(PipelineBreaksToolWindow))]
    [ProvideToolWindow(typeof(SettingsToolWindow))]
    [Guid(PackageGuids.PackageString)]
    public sealed class ConvisoPlatformPackage : AsyncPackage
    {
        public static ConvisoPlatformPackage? Instance { get; private set; }

        public ToolWindowContext ToolWindowContext { get; private set; } = null!;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Instance = this;
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var settingsService = new SettingsService(this);
            var apiClient = new PlatformApiClient(settingsService);
            var platformFacade = new PlatformFacade(apiClient, settingsService);
            var brokerClient = new BrokerClient();
            var editorContextService = new EditorContextService(this);
            var patchService = new DocumentPatchService(this);
            ToolWindowContext = new ToolWindowContext(settingsService, platformFacade, brokerClient, editorContextService, patchService);

            await OpenChatToolWindowCommand.InitializeAsync(this);
            await OpenVulnerabilitiesToolWindowCommand.InitializeAsync(this);
            await OpenRequirementsToolWindowCommand.InitializeAsync(this);
            await OpenPipelineBreaksToolWindowCommand.InitializeAsync(this);
            await OpenSettingsToolWindowCommand.InitializeAsync(this);
            await AnalyzeSecurityAndSuggestFixCommand.InitializeAsync(this);
            await AttachSelectionToChatCommand.InitializeAsync(this);
            await CheckSimilarIssuesCommand.InitializeAsync(this);
        }
    }
}
