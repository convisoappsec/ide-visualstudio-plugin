namespace Conviso.Platform.VisualStudio.Services.Platform
{
internal static class GraphQlDocuments
{
    public const string IssuesList = @"
query Issues(
  $pagination: PaginationInput!
  $filters: IssuesFiltersInput
  $companyId: ID!
  $sortOptions: [IssueSortOptionInput!]
) {
  issues(
    pagination: $pagination
    filters: $filters
    companyId: $companyId
    sortOptions: $sortOptions
  ) {
    collection {
      id
      title
      status
      severity
      asset {
        id
        name
      }
    }
  }
}";

    public const string Projects = @"
query Projects(
  $page: Int
  $limit: Int
  $params: ProjectSearch
  $sortBy: String
  $descending: Boolean
  ) {
    projects(
    page: $page
    limit: $limit
    params: $params
    sortBy: $sortBy
    descending: $descending
    ) {
      collection {
        id
        label
        status
        projectType {
          label
        }
        assets {
          id
          name
        }
      }
    }
  }";

    public const string ProjectDetails = @"
query ProjectDetails($projectId: ID!) {
  project(id: $projectId) {
    id
    label
    status
    createdAt
    updatedAt
    goal
    scope
    startDate
    endDate
    estimatedHours
    projectType {
      label
    }
    tags {
      name
    }
    assets {
      id
      name
    }
    playbooks {
      id
      label
    }
  }
}";

    public const string ProjectRequirements = @"
query ProjectRequirements($projectId: ID!) {
  project(id: $projectId) {
    id
    label
    playbooks {
      id
      label
      checklistType {
        label
      }
    }
  }
}";

    public const string Requirements = @"
query Requirements(
  $scopeId: Int!
  $pagination: BasePaginationInput!
  $filters: RequirementsFilterInput
) {
  requirements(
    scopeId: $scopeId
    pagination: $pagination
    filters: $filters
  ) {
    collection {
      id
      label
      description
    }
  }
}";

    public const string RequirementDetails = @"
query RequirementDetails($projectId: ID!) {
  project(id: $projectId) {
    id
    label
    playbooks {
      id
      label
      checklistType {
        label
      }
      check {
        id
        label
        description
      }
    }
  }
}";

    public const string ProjectActivities = @"
query ProjectActivities($params: ActivitiesSearch!) {
  activities(params: $params) {
    collection {
      id
      title
      status
      permittedStatus
      assignedUsers {
        name
        email
      }
      check {
        id
        label
        description
      }
      updatedAt
    }
  }
}";

    public const string PipelineBreaks = @"
query SecurityGateExecutions(
  $companyId: ID!
  $pagination: BasePaginationInput!
  $filters: SecurityGateExecutionsSearch
  $sortOption: SecurityGateExecutionsSortOptionInput
) {
  securityGateExecutions(
    companyId: $companyId
    pagination: $pagination
    filters: $filters
    sortOption: $sortOption
  ) {
    collection {
      id
      status
      executionDate
      triggeredBy
      asset {
        id
        name
      }
    }
  }
}";

    public const string PipelineBreakDetails = @"
query SecurityGateExecution($id: ID!) {
  securityGateExecution(id: $id) {
    id
    status
    executionDate
    triggeredBy
    source
    asset {
      id
      name
    }
    reason {
      low {
        status
        count
        expiredCount
        limit
        maxDaysToFix
      }
      medium {
        status
        count
        expiredCount
        limit
        maxDaysToFix
      }
      high {
        status
        count
        expiredCount
        limit
        maxDaysToFix
      }
      critical {
        status
        count
        expiredCount
        limit
        maxDaysToFix
      }
    }
  }
}";

    public const string VulnerabilityDetails = @"
query IssueDetails($id: ID!) {
  issue(id: $id) {
    id
    title
    description
    status
    severity
    asset {
      id
      name
    }
  }
}";

    public const string ChangeIssueStatus = @"
mutation ChangeIssueStatus($input: ChangeIssueStatusInput!) {
  changeIssueStatus(input: $input) {
    issue {
      id
      status
      permittedStatus
    }
  }
}";

    public const string UpdateProjectStatus = @"
mutation UpdateProjectStatus($input: UpdateProjectStatusInput!) {
  updateProjectStatus(input: $input) {
    clientMutationId
    errors
    project {
      id
      status
    }
  }
}";

    public const string UpdateActivityStatus = @"
mutation UpdateActivityStatus($input: UpdateActivityStatusInput!) {
  updateActivityStatus(input: $input) {
    clientMutationId
    errors
    activity {
      id
      status
      permittedStatus
    }
  }
}";
}
}
