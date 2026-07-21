using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.Models;
using Conviso.Platform.VisualStudio.Services.Platform;

namespace Conviso.Platform.VisualStudio.ViewModels
{
internal sealed class RequirementsToolWindowViewModel : ObservableObject
{
    private readonly IPlatformFacade platformFacade;
    private ProjectSummary? selectedProject;
    private RequirementSummary? selectedRequirement;
    private ProjectActivitySummary? selectedActivity;
    private string status = "Ready";
    private string projectLabel = string.Empty;
    private string projectStatus = string.Empty;
    private string projectType = string.Empty;
    private string projectGoal = string.Empty;
    private string projectScope = string.Empty;
    private string projectAssets = string.Empty;
    private string projectRequirements = string.Empty;
    private string newProjectStatus = string.Empty;
    private string projectCommandStatus = "Select a project";
    private string requirementLabel = string.Empty;
    private string requirementProject = string.Empty;
    private string requirementChecklistType = string.Empty;
    private string requirementChecks = string.Empty;
    private string activityTitle = string.Empty;
    private string activityStatus = string.Empty;
    private string activityAssignees = string.Empty;
    private string activityCheck = string.Empty;
    private string activityDescription = string.Empty;
    private string activityUpdatedAt = string.Empty;
    private string newActivityStatus = string.Empty;
    private string activityCommandStatus = "Select an activity";

    public RequirementsToolWindowViewModel(IPlatformFacade platformFacade)
    {
        this.platformFacade = platformFacade;
        Projects = new ObservableCollection<ProjectSummary>();
        Requirements = new ObservableCollection<RequirementSummary>();
        Activities = new ObservableCollection<ProjectActivitySummary>();
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync);
        UpdateProjectStatusCommand = new AsyncDelegateCommand(UpdateProjectStatusAsync, () => selectedProject != null && !string.IsNullOrWhiteSpace(NewProjectStatus));
        UpdateActivityStatusCommand = new AsyncDelegateCommand(UpdateActivityStatusAsync, () => selectedActivity != null && !string.IsNullOrWhiteSpace(NewActivityStatus));
    }

    public ObservableCollection<ProjectSummary> Projects { get; }

    public ObservableCollection<RequirementSummary> Requirements { get; }

    public ObservableCollection<ProjectActivitySummary> Activities { get; }

    public string Status
    {
        get => status;
        set => SetProperty(ref status, value);
    }

    public ProjectSummary? SelectedProject
    {
        get => selectedProject;
        set
        {
            if (SetProperty(ref selectedProject, value))
            {
                _ = LoadProjectAsync(value);
            }
        }
    }

    public RequirementSummary? SelectedRequirement
    {
        get => selectedRequirement;
        set
        {
            if (SetProperty(ref selectedRequirement, value))
            {
                _ = LoadRequirementAsync(value);
            }
        }
    }

    public ProjectActivitySummary? SelectedActivity
    {
        get => selectedActivity;
        set
        {
            if (SetProperty(ref selectedActivity, value))
            {
                LoadActivity(value);
            }
        }
    }

    public string ProjectLabel { get => projectLabel; set => SetProperty(ref projectLabel, value); }

    public string ProjectStatus { get => projectStatus; set => SetProperty(ref projectStatus, value); }

    public string ProjectType { get => projectType; set => SetProperty(ref projectType, value); }

    public string ProjectGoal { get => projectGoal; set => SetProperty(ref projectGoal, value); }

    public string ProjectScope { get => projectScope; set => SetProperty(ref projectScope, value); }

    public string ProjectAssets { get => projectAssets; set => SetProperty(ref projectAssets, value); }

    public string ProjectRequirements { get => projectRequirements; set => SetProperty(ref projectRequirements, value); }

    public string NewProjectStatus
    {
        get => newProjectStatus;
        set
        {
            if (SetProperty(ref newProjectStatus, value))
            {
                UpdateProjectStatusCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ProjectCommandStatus { get => projectCommandStatus; set => SetProperty(ref projectCommandStatus, value); }

    public string RequirementLabel { get => requirementLabel; set => SetProperty(ref requirementLabel, value); }

    public string RequirementProject { get => requirementProject; set => SetProperty(ref requirementProject, value); }

    public string RequirementChecklistType { get => requirementChecklistType; set => SetProperty(ref requirementChecklistType, value); }

    public string RequirementChecks { get => requirementChecks; set => SetProperty(ref requirementChecks, value); }

    public string ActivityTitle { get => activityTitle; set => SetProperty(ref activityTitle, value); }

    public string ActivityStatus { get => activityStatus; set => SetProperty(ref activityStatus, value); }

    public string ActivityAssignees { get => activityAssignees; set => SetProperty(ref activityAssignees, value); }

    public string ActivityCheck { get => activityCheck; set => SetProperty(ref activityCheck, value); }

    public string ActivityDescription { get => activityDescription; set => SetProperty(ref activityDescription, value); }

    public string ActivityUpdatedAt { get => activityUpdatedAt; set => SetProperty(ref activityUpdatedAt, value); }

    public string NewActivityStatus
    {
        get => newActivityStatus;
        set
        {
            if (SetProperty(ref newActivityStatus, value))
            {
                UpdateActivityStatusCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ActivityCommandStatus { get => activityCommandStatus; set => SetProperty(ref activityCommandStatus, value); }

    public AsyncDelegateCommand RefreshCommand { get; }

    public AsyncDelegateCommand UpdateProjectStatusCommand { get; }

    public AsyncDelegateCommand UpdateActivityStatusCommand { get; }

    public async Task RefreshAsync()
    {
        Status = "Loading projects...";
        Projects.Clear();
        Requirements.Clear();
        Activities.Clear();
        SelectedProject = null;
        SelectedRequirement = null;
        SelectedActivity = null;
        ResetProjectDetails();
        ResetRequirementDetails();
        ResetActivityDetails();

        try
        {
            foreach (ProjectSummary item in await platformFacade.GetProjectsAsync(CancellationToken.None))
            {
                Projects.Add(item);
            }

            Status = $"Loaded {Projects.Count} project(s)";
        }
        catch (System.Exception error)
        {
            Status = "Unable to load projects: " + error.Message;
            DiagnosticsLogger.LogError("Unable to load projects: " + error);
        }
    }

    private async Task LoadProjectAsync(ProjectSummary? project)
    {
        Requirements.Clear();
        Activities.Clear();
        SelectedRequirement = null;
        SelectedActivity = null;
        ResetRequirementDetails();
        ResetActivityDetails();
        UpdateProjectStatusCommand.RaiseCanExecuteChanged();

        if (project == null)
        {
            ResetProjectDetails();
            return;
        }

        ProjectCommandStatus = "Loading project details...";
        ProjectDetails details;
        try
        {
            details = await platformFacade.GetProjectDetailsAsync(project.Id, CancellationToken.None);
        }
        catch (System.Exception error)
        {
            ProjectCommandStatus = "Unable to load project: " + error.Message;
            DiagnosticsLogger.LogError("Unable to load project details: " + error);
            return;
        }
        ProjectLabel = details.Label;
        ProjectStatus = details.Status;
        ProjectType = details.ProjectTypeLabel;
        ProjectGoal = details.Goal;
        ProjectScope = details.Scope;
        ProjectAssets = details.AssetsText;
        ProjectRequirements = details.RequirementsText;
        NewProjectStatus = details.Status;
        ProjectCommandStatus = "Project details loaded";

        foreach (RequirementSummary item in await platformFacade.GetProjectRequirementsAsync(project.Id, CancellationToken.None))
        {
            Requirements.Add(item);
        }

        Status = $"Loaded {Requirements.Count} requirement(s) for {project.Label}";
    }

    private async Task LoadRequirementAsync(RequirementSummary? requirement)
    {
        Activities.Clear();
        SelectedActivity = null;
        ResetActivityDetails();

        if (requirement == null || selectedProject == null)
        {
            ResetRequirementDetails();
            return;
        }

        RequirementDetails details = await platformFacade.GetRequirementDetailsAsync(selectedProject.Id, requirement.Id, CancellationToken.None);
        RequirementLabel = details.Label;
        RequirementProject = details.ProjectLabel;
        RequirementChecklistType = details.ChecklistTypeLabel;
        RequirementChecks = details.ChecksText;

        foreach (ProjectActivitySummary item in await platformFacade.GetProjectActivitiesAsync(selectedProject.Id, requirement.Id, CancellationToken.None))
        {
            Activities.Add(item);
        }

        Status = $"Loaded {Activities.Count} activit(y/ies) for {requirement.Label}";
    }

    private void LoadActivity(ProjectActivitySummary? activity)
    {
        UpdateActivityStatusCommand.RaiseCanExecuteChanged();

        if (activity == null)
        {
            ResetActivityDetails();
            return;
        }

        ActivityTitle = activity.Title;
        ActivityStatus = activity.Status;
        ActivityAssignees = activity.AssigneeEmailsText;
        ActivityCheck = activity.CheckLabel;
        ActivityDescription = activity.CheckDescription;
        ActivityUpdatedAt = activity.UpdatedAt;
        NewActivityStatus = activity.Status;
        ActivityCommandStatus = string.IsNullOrWhiteSpace(activity.PermittedStatusText)
            ? "Activity details loaded"
            : "Permitted status: " + activity.PermittedStatusText;
    }

    private async Task UpdateProjectStatusAsync()
    {
        if (selectedProject == null)
        {
            return;
        }

        await platformFacade.UpdateProjectStatusAsync(selectedProject.Id, NewProjectStatus, CancellationToken.None);
        ProjectStatus = NewProjectStatus;
        selectedProject.Status = NewProjectStatus;
        ProjectCommandStatus = "Project status updated";
        Status = $"Project {selectedProject.Label} updated";
    }

    private async Task UpdateActivityStatusAsync()
    {
        if (selectedActivity == null)
        {
            return;
        }

        await platformFacade.UpdateActivityStatusAsync(selectedActivity.Id, NewActivityStatus, CancellationToken.None);
        ActivityStatus = NewActivityStatus;
        selectedActivity.Status = NewActivityStatus;
        ActivityCommandStatus = "Activity status updated";
        Status = $"Activity {selectedActivity.Title} updated";
    }

    private void ResetProjectDetails()
    {
        ProjectLabel = string.Empty;
        ProjectStatus = string.Empty;
        ProjectType = string.Empty;
        ProjectGoal = string.Empty;
        ProjectScope = string.Empty;
        ProjectAssets = string.Empty;
        ProjectRequirements = string.Empty;
        NewProjectStatus = string.Empty;
        ProjectCommandStatus = "Select a project";
    }

    private void ResetRequirementDetails()
    {
        RequirementLabel = string.Empty;
        RequirementProject = string.Empty;
        RequirementChecklistType = string.Empty;
        RequirementChecks = string.Empty;
    }

    private void ResetActivityDetails()
    {
        ActivityTitle = string.Empty;
        ActivityStatus = string.Empty;
        ActivityAssignees = string.Empty;
        ActivityCheck = string.Empty;
        ActivityDescription = string.Empty;
        ActivityUpdatedAt = string.Empty;
        NewActivityStatus = string.Empty;
        ActivityCommandStatus = "Select an activity";
    }
}
}
