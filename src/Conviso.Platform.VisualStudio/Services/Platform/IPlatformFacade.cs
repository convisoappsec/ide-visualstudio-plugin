using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.Models;

namespace Conviso.Platform.VisualStudio.Services.Platform
{
    public interface IPlatformFacade
    {
        Task<IReadOnlyList<AccessibleCompanyOption>> GetAccessibleCompaniesAsync(CancellationToken cancellationToken);

        Task<IReadOnlyList<AssetOption>> GetAssetsAsync(string companyId, CancellationToken cancellationToken);

        Task<IReadOnlyList<ProjectSummary>> GetProjectsAsync(CancellationToken cancellationToken);

        Task<ProjectDetails> GetProjectDetailsAsync(string projectId, CancellationToken cancellationToken);

        Task UpdateProjectStatusAsync(string projectId, string status, CancellationToken cancellationToken);

        Task<IReadOnlyList<RequirementSummary>> GetProjectRequirementsAsync(string projectId, CancellationToken cancellationToken);

        Task<RequirementDetails> GetRequirementDetailsAsync(string projectId, string requirementId, CancellationToken cancellationToken);

        Task<IReadOnlyList<ProjectActivitySummary>> GetProjectActivitiesAsync(string projectId, string requirementId, CancellationToken cancellationToken);

        Task UpdateActivityStatusAsync(string activityId, string status, CancellationToken cancellationToken);

        Task<IReadOnlyList<VulnerabilitySummary>> GetVulnerabilitiesAsync(string? companyId, string? assetId, CancellationToken cancellationToken);

        Task<VulnerabilityDetails> GetVulnerabilityDetailsAsync(string id, CancellationToken cancellationToken);

        Task UpdateVulnerabilityStatusAsync(string id, string status, CancellationToken cancellationToken);

        Task<IReadOnlyList<PipelineBreakSummary>> GetPipelineBreaksAsync(CancellationToken cancellationToken);

        Task<PipelineBreakDetails> GetPipelineBreakDetailsAsync(string id, CancellationToken cancellationToken);
    }
}
