using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.Configuration;
using Conviso.Platform.VisualStudio.Models;

namespace Conviso.Platform.VisualStudio.Services.Platform
{
internal sealed class PlatformFacade : IPlatformFacade
{
    private readonly IPlatformApiClient apiClient;
    private readonly ISettingsService settingsService;

    public PlatformFacade(IPlatformApiClient apiClient, ISettingsService settingsService)
    {
        this.apiClient = apiClient;
        this.settingsService = settingsService;
    }

    public async Task<IReadOnlyList<ProjectSummary>> GetProjectsAsync(CancellationToken cancellationToken)
    {
        string companyId = settingsService.GetString(ConvisoOptions.CompanyIdKey, string.Empty);
        if (string.IsNullOrWhiteSpace(companyId))
        {
            return new[] { new ProjectSummary { Id = "info", Label = "Configure Company ID to load projects.", Status = "SETUP" } };
        }

        object scopeIdEq = int.TryParse(companyId, out int numericCompanyId) ? numericCompanyId : companyId;
        string variables = SerializeVariables(new
        {
            page = 1,
            limit = 50,
            sortBy = "createdAt",
            descending = true,
            @params = new
            {
                scopeIdEq,
            },
        });

        string json = await apiClient.QueryAsync(GraphQlDocuments.Projects, variables, cancellationToken);

        using JsonDocument document = JsonDocument.Parse(json);
        if (!TryGetCollection(document, "projects", out JsonElement collection))
        {
            return new[] { new ProjectSummary { Id = "info", Label = "No project data returned by API.", Status = "EMPTY" } };
        }

        var result = new List<ProjectSummary>();
        foreach (JsonElement item in collection.EnumerateArray())
        {
            result.Add(new ProjectSummary
            {
                Id = item.GetPropertyOrDefault("id"),
                Label = item.GetPropertyOrDefault("label"),
                Status = item.GetPropertyOrDefault("status"),
                ProjectTypeLabel = item.TryGetProperty("projectType", out JsonElement projectType) ? projectType.GetPropertyOrDefault("label") : string.Empty,
                AssetNames = item.TryGetProperty("assets", out JsonElement assets) ? JoinNames(assets) : string.Empty,
            });
        }

        return result;
    }

    public async Task<ProjectDetails> GetProjectDetailsAsync(string projectId, CancellationToken cancellationToken)
    {
        string variables = SerializeVariables(new
        {
            projectId,
        });

        string json = await apiClient.QueryAsync(GraphQlDocuments.ProjectDetails, variables, cancellationToken);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement project = document.RootElement.GetProperty("data").GetProperty("project");
        return new ProjectDetails
        {
            Id = project.GetPropertyOrDefault("id"),
            Label = project.GetPropertyOrDefault("label"),
            Status = project.GetPropertyOrDefault("status"),
            CreatedAt = project.GetPropertyOrDefault("createdAt"),
            UpdatedAt = project.GetPropertyOrDefault("updatedAt"),
            Goal = project.GetPropertyOrDefault("goal"),
            Scope = project.GetPropertyOrDefault("scope"),
            StartDate = project.GetPropertyOrDefault("startDate"),
            EndDate = project.GetPropertyOrDefault("endDate"),
            EstimatedHours = ReadValueAsString(project, "estimatedHours"),
            ProjectTypeLabel = project.TryGetProperty("projectType", out JsonElement projectType) ? projectType.GetPropertyOrDefault("label") : string.Empty,
            TagsText = project.TryGetProperty("tags", out JsonElement tags) ? JoinNames(tags) : string.Empty,
            AssetsText = project.TryGetProperty("assets", out JsonElement assets) ? JoinNames(assets) : string.Empty,
            RequirementsText = project.TryGetProperty("playbooks", out JsonElement playbooks) ? JoinLabels(playbooks) : string.Empty,
        };
    }

    public async Task UpdateProjectStatusAsync(string projectId, string status, CancellationToken cancellationToken)
    {
        string variables = SerializeVariables(new
        {
            input = new
            {
                id = projectId,
                projectStatus = status,
            },
        });

        await apiClient.QueryAsync(GraphQlDocuments.UpdateProjectStatus, variables, cancellationToken);
    }

    public async Task<IReadOnlyList<RequirementSummary>> GetProjectRequirementsAsync(string projectId, CancellationToken cancellationToken)
    {
        string variables = SerializeVariables(new
        {
            projectId,
        });

        string json = await apiClient.QueryAsync(GraphQlDocuments.ProjectRequirements, variables, cancellationToken);

        using JsonDocument document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("data", out JsonElement data) ||
            !data.TryGetProperty("project", out JsonElement project) ||
            !project.TryGetProperty("playbooks", out JsonElement playbooks) ||
            playbooks.ValueKind != JsonValueKind.Array)
        {
            return new[] { new RequirementSummary { Id = "info", Label = "No requirements returned for this project." } };
        }

        string projectLabel = project.GetPropertyOrDefault("label");
        var result = new List<RequirementSummary>();
        foreach (JsonElement item in playbooks.EnumerateArray())
        {
            result.Add(new RequirementSummary
            {
                Id = item.GetPropertyOrDefault("id"),
                Label = item.GetPropertyOrDefault("label"),
                ProjectLabel = projectLabel,
                ChecklistTypeLabel = item.TryGetProperty("checklistType", out JsonElement checklistType) ? checklistType.GetPropertyOrDefault("label") : string.Empty,
            });
        }

        return result;
    }

    public async Task<RequirementDetails> GetRequirementDetailsAsync(string projectId, string requirementId, CancellationToken cancellationToken)
    {
        string variables = SerializeVariables(new
        {
            projectId,
        });

        string json = await apiClient.QueryAsync(GraphQlDocuments.RequirementDetails, variables, cancellationToken);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement project = document.RootElement.GetProperty("data").GetProperty("project");
        JsonElement selected = project.GetProperty("playbooks").EnumerateArray().FirstOrDefault(item => item.GetPropertyOrDefault("id") == requirementId);

        if (selected.ValueKind == JsonValueKind.Undefined)
        {
            return new RequirementDetails
            {
                Id = requirementId,
                ProjectId = projectId,
                ProjectLabel = project.GetPropertyOrDefault("label"),
                Label = "Requirement not found",
            };
        }

        return new RequirementDetails
        {
            Id = selected.GetPropertyOrDefault("id"),
            Label = selected.GetPropertyOrDefault("label"),
            ProjectId = projectId,
            ProjectLabel = project.GetPropertyOrDefault("label"),
            ChecklistTypeLabel = selected.TryGetProperty("checklistType", out JsonElement checklistType) ? checklistType.GetPropertyOrDefault("label") : string.Empty,
            ChecksText = selected.TryGetProperty("check", out JsonElement checks) ? JoinCheckDescriptions(checks) : string.Empty,
        };
    }

    public async Task<IReadOnlyList<ProjectActivitySummary>> GetProjectActivitiesAsync(string projectId, string requirementId, CancellationToken cancellationToken)
    {
        string variables = SerializeVariables(new
        {
            @params = new
            {
                projectId,
                playbookId = requirementId,
            },
        });

        string json = await apiClient.QueryAsync(GraphQlDocuments.ProjectActivities, variables, cancellationToken);

        using JsonDocument document = JsonDocument.Parse(json);
        if (!TryGetCollection(document, "activities", out JsonElement collection))
        {
            return new[] { new ProjectActivitySummary { Id = "info", Title = "No activities returned for this requirement.", Status = "EMPTY" } };
        }

        var result = new List<ProjectActivitySummary>();
        foreach (JsonElement item in collection.EnumerateArray())
        {
            string permittedStatuses = item.TryGetProperty("permittedStatus", out JsonElement permittedStatus) && permittedStatus.ValueKind == JsonValueKind.Array
                ? string.Join(", ", permittedStatus.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()).Where(value => !string.IsNullOrWhiteSpace(value)))
                : string.Empty;

            result.Add(new ProjectActivitySummary
            {
                Id = item.GetPropertyOrDefault("id"),
                Title = item.GetPropertyOrDefault("title"),
                Status = item.GetPropertyOrDefault("status"),
                PermittedStatusText = permittedStatuses,
                AssigneeEmailsText = item.TryGetProperty("assignedUsers", out JsonElement assignedUsers) ? JoinEmails(assignedUsers) : string.Empty,
                CheckLabel = item.TryGetProperty("check", out JsonElement check) ? check.GetPropertyOrDefault("label") : string.Empty,
                CheckDescription = item.TryGetProperty("check", out JsonElement descriptionCheck) ? descriptionCheck.GetPropertyOrDefault("description") : string.Empty,
                UpdatedAt = item.GetPropertyOrDefault("updatedAt"),
            });
        }

        return result;
    }

    public async Task UpdateActivityStatusAsync(string activityId, string status, CancellationToken cancellationToken)
    {
        string variables = SerializeVariables(new
        {
            input = new
            {
                id = activityId,
                status,
            },
        });

        await apiClient.QueryAsync(GraphQlDocuments.UpdateActivityStatus, variables, cancellationToken);
    }

    public async Task<IReadOnlyList<VulnerabilitySummary>> GetVulnerabilitiesAsync(CancellationToken cancellationToken)
    {
        string companyId = settingsService.GetString(ConvisoOptions.CompanyIdKey, string.Empty);
        if (string.IsNullOrWhiteSpace(companyId))
        {
            return new[] { new VulnerabilitySummary { Id = "info", Title = "Configure Company ID to load vulnerabilities.", Severity = "INFO", Status = "SETUP" } };
        }

        string variables = SerializeVariables(new
        {
            pagination = new
            {
                page = 1,
                limit = 20,
            },
            filters = new { },
            companyId,
            sortOptions = new[]
            {
                new
                {
                    field = "updatedAt",
                    direction = "DESC",
                },
            },
        });
        string json = await apiClient.QueryAsync(GraphQlDocuments.IssuesList, variables, cancellationToken);

        using JsonDocument document = JsonDocument.Parse(json);
        if (!TryGetCollection(document, "issues", out JsonElement collection))
        {
            return new[] { new VulnerabilitySummary { Id = "info", Title = "No vulnerability data returned by API.", Severity = "INFO", Status = "EMPTY" } };
        }

        var result = new List<VulnerabilitySummary>();
        foreach (JsonElement item in collection.EnumerateArray())
        {
            result.Add(new VulnerabilitySummary
            {
                Id = item.GetPropertyOrDefault("id"),
                Title = item.GetPropertyOrDefault("title"),
                Severity = item.GetPropertyOrDefault("severity"),
                Status = item.GetPropertyOrDefault("status"),
                AssetName = item.TryGetProperty("asset", out JsonElement asset) ? asset.GetPropertyOrDefault("name") : string.Empty,
            });
        }

        return result;
    }

    public async Task<VulnerabilityDetails> GetVulnerabilityDetailsAsync(string id, CancellationToken cancellationToken)
    {
        string variables = SerializeVariables(new
        {
            id,
        });
        string json = await apiClient.QueryAsync(GraphQlDocuments.VulnerabilityDetails, variables, cancellationToken);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement issue = document.RootElement.GetProperty("data").GetProperty("issue");
        JsonElement asset = issue.TryGetProperty("asset", out JsonElement assetValue) ? assetValue : default;

        return new VulnerabilityDetails
        {
            Id = issue.GetPropertyOrDefault("id"),
            Title = issue.GetPropertyOrDefault("title"),
            Description = issue.GetPropertyOrDefault("description"),
            Severity = issue.GetPropertyOrDefault("severity"),
            Status = issue.GetPropertyOrDefault("status"),
            AssetName = asset.ValueKind != JsonValueKind.Undefined ? asset.GetPropertyOrDefault("name") : string.Empty,
        };
    }

    public async Task UpdateVulnerabilityStatusAsync(string id, string status, CancellationToken cancellationToken)
    {
        string variables = SerializeVariables(new
        {
            input = new
            {
                id,
                status,
            },
        });
        await apiClient.QueryAsync(GraphQlDocuments.ChangeIssueStatus, variables, cancellationToken);
    }

    public async Task<IReadOnlyList<PipelineBreakSummary>> GetPipelineBreaksAsync(CancellationToken cancellationToken)
    {
        string companyId = settingsService.GetString(ConvisoOptions.CompanyIdKey, string.Empty);
        if (string.IsNullOrWhiteSpace(companyId))
        {
            return new[] { new PipelineBreakSummary { Id = "info", Status = "SETUP", AssetName = "Configure Company ID to load pipeline breaks." } };
        }

        string variables = SerializeVariables(new
        {
            companyId,
            pagination = new
            {
                page = 1,
                perPage = 20,
            },
            filters = new
            {
                statuses = new[] { "FAIL" },
            },
            sortOption = new
            {
                sortBy = "EXECUTED_AT",
                order = "DESC",
            },
        });
        string json = await apiClient.QueryAsync(GraphQlDocuments.PipelineBreaks, variables, cancellationToken);

        using JsonDocument document = JsonDocument.Parse(json);
        if (!TryGetCollection(document, "securityGateExecutions", out JsonElement collection))
        {
            return new[] { new PipelineBreakSummary { Id = "info", AssetName = "No pipeline break data returned by API.", Status = "EMPTY" } };
        }

        var result = new List<PipelineBreakSummary>();
        foreach (JsonElement item in collection.EnumerateArray())
        {
            result.Add(new PipelineBreakSummary
            {
                Id = item.GetPropertyOrDefault("id"),
                Status = item.GetPropertyOrDefault("status"),
                ExecutionDate = item.GetPropertyOrDefault("executionDate"),
                TriggeredBy = item.GetPropertyOrDefault("triggeredBy"),
                AssetName = item.TryGetProperty("asset", out JsonElement asset) ? asset.GetPropertyOrDefault("name") : string.Empty,
            });
        }

        return result;
    }

    public async Task<PipelineBreakDetails> GetPipelineBreakDetailsAsync(string id, CancellationToken cancellationToken)
    {
        string variables = SerializeVariables(new
        {
            id,
        });

        string json = await apiClient.QueryAsync(GraphQlDocuments.PipelineBreakDetails, variables, cancellationToken);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement execution = document.RootElement.GetProperty("data").GetProperty("securityGateExecution");
        return new PipelineBreakDetails
        {
            Id = execution.GetPropertyOrDefault("id"),
            Status = execution.GetPropertyOrDefault("status"),
            ExecutionDate = execution.GetPropertyOrDefault("executionDate"),
            TriggeredBy = execution.GetPropertyOrDefault("triggeredBy"),
            Source = execution.GetPropertyOrDefault("source"),
            AssetName = execution.TryGetProperty("asset", out JsonElement asset) ? asset.GetPropertyOrDefault("name") : string.Empty,
            ReasonText = execution.TryGetProperty("reason", out JsonElement reason) ? BuildPipelineReasonText(reason) : string.Empty,
        };
    }

    private static bool TryGetCollection(JsonDocument document, string rootProperty, out JsonElement collection)
    {
        if (document.RootElement.TryGetProperty("data", out JsonElement data) &&
            data.TryGetProperty(rootProperty, out JsonElement root) &&
            root.TryGetProperty("collection", out collection) &&
            collection.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        collection = default;
        return false;
    }

    private static string SerializeVariables(object value)
    {
        return JsonSerializer.Serialize(value);
    }

    private static string JoinNames(JsonElement array)
    {
        return string.Join(", ", array.EnumerateArray().Select(item => item.GetPropertyOrDefault("name")).Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string JoinLabels(JsonElement array)
    {
        return string.Join(", ", array.EnumerateArray().Select(item => item.GetPropertyOrDefault("label")).Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string JoinEmails(JsonElement array)
    {
        return string.Join(", ", array.EnumerateArray().Select(item => item.GetPropertyOrDefault("email")).Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string JoinCheckDescriptions(JsonElement array)
    {
        return string.Join("\n\n", array.EnumerateArray().Select(item =>
        {
            string label = item.GetPropertyOrDefault("label");
            string description = item.GetPropertyOrDefault("description");
            return string.IsNullOrWhiteSpace(description) ? label : label + ": " + description;
        }).Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string BuildPipelineReasonText(JsonElement reason)
    {
        string[] severities = new[] { "critical", "high", "medium", "low" };
        var lines = new List<string>();
        foreach (string severity in severities)
        {
            if (!reason.TryGetProperty(severity, out JsonElement block))
            {
                continue;
            }

            lines.Add(
                severity.ToUpperInvariant() +
                ": status=" + ReadValueAsString(block, "status") +
                ", count=" + ReadValueAsString(block, "count") +
                ", expired=" + ReadValueAsString(block, "expiredCount") +
                ", limit=" + ReadValueAsString(block, "limit") +
                ", maxDaysToFix=" + ReadValueAsString(block, "maxDaysToFix"));
        }

        return string.Join("\n", lines);
    }

    private static string ReadValueAsString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
        {
            return string.Empty;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.ToString();
        }

        return string.Empty;
    }
}
}
