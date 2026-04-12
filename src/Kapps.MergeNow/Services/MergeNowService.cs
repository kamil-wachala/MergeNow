using EnvDTE;
using MergeNow.Core.Utils;
using MergeNow.Model;
using MergeNow.Settings;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TeamFoundation;
using Microsoft.VisualStudio.TeamFoundation.VersionControl;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergeNow.Services
{
    internal class MergeNowService : IMergeNowService
    {
        private const string MergeNotStartedSummary = "Merge not started.";
        private const string MergeCancelledSummary = "Merge cancelled.";
        private const string MergeCompletedSummary = "Merge completed.";
        private const string MergePartiallyCompletedSummary = "Merge partially completed.";

        private readonly AsyncPackage _asyncPackage;
        private readonly IMergeNowSettings _settings;
        private readonly IMessageService _messageService;
        private AsyncLazy<VersionControlServer> _versionControlConnectionTask;

        public MergeNowService(AsyncPackage asyncPackage, IMergeNowSettings settings, IMessageService messageService)
        {
            _asyncPackage = asyncPackage ?? throw new ArgumentNullException(nameof(asyncPackage));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            RenewVersionControlConnection();
        }

        public async Task<bool> IsOnlineAsync()
        {
            var pendingChangesPage = await GetPendingChangesPageAsync();
            var viewModel = GetPendingChangesPageViewModel(pendingChangesPage);
            var workspaceName = ReflectionUtils.GetProperty<string>("CurrentWorkspaceName", viewModel);

            if (workspaceName == "No workspace")
            {
                return false;
            }

            var versionControlServer = await GetVersionControlAsync();
            return versionControlServer != null;
        }

        public async Task<Changeset> FindChangesetAsync(string changesetNumber)
        {
            if (!int.TryParse(changesetNumber, out int changesetNo))
            {
                throw new ArgumentException("Please provide a valid changeset number.");
            }

            var versionControlServer = await GetVersionControlAsync();
            var changeset = versionControlServer?.GetChangeset(changesetNo);
            return changeset;
        }

        public async Task<Changeset> BrowseChangesetAsync()
        {
            var versionControlExt = await GetVersionControlExtAsync();
            var changeset = versionControlExt?.FindChangeset();
            return changeset;
        }

        public async Task<Changeset[]> GetHistoryViewSelectedChangesetsAsync()
        {
            var versionControlExt = await GetVersionControlExtAsync();
            #pragma warning disable CS0618 // Type or member is obsolete
            Changeset[] changesets = versionControlExt?.History?.SelectedItems ?? new Changeset[0];
            #pragma warning restore CS0618 // Type or member is obsolete
            return changesets;
        }

        public async Task ViewChangesetDetailsAsync(Changeset changeset)
        {
            if (changeset == null)
            {
                throw new ArgumentException("Please provide a valid changeset.");
            }

            var versionControlExt = await GetVersionControlExtAsync();
            versionControlExt?.ViewChangesetDetails(changeset.ChangesetId);
        }

        public async Task<IEnumerable<string>> GetTargetBranchesAsync(Changeset changeset)
        {
            if (changeset == null)
            {
                throw new ArgumentException("Please provide a valid changeset.");
            }

            var versionControlServer = await GetVersionControlAsync();
            var sourceBranches = GetSourceBranches(versionControlServer, changeset);

            var targetBranches = new List<string>();

            foreach (var sourceBranch in sourceBranches)
            {
                var branches = GetMergeBranches(versionControlServer, sourceBranch);
                targetBranches.AddRange(branches);
            }

            return targetBranches.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<MergeResult> MergeAsync(Changeset changeset, string targetBranch, MergeHistory mergeHistory)
        {
            if (changeset == null)
            {
                throw new ArgumentException("Please provide a valid changeset.");
            }

            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                throw new ArgumentException("Please provide a valid target branch.");
            }

            if (mergeHistory == null)
            {
                throw new ArgumentException("Please provide a merge history.");
            }

            var pendingChangesPage = await GetPendingChangesPageAsync();
            Workspace workspace = GetCurrentWorkspace(pendingChangesPage);

            if (workspace == null)
            {
                return CreateResult(MergeResultType.Warning, MergeNotStartedSummary, "No TFS workspace found.");
            }

            var versionControlServer = await GetVersionControlAsync();
            var sourceBranches = GetSourceBranches(versionControlServer, changeset);

            if (sourceBranches == null || !sourceBranches.Any())
            {
                return CreateResult(MergeResultType.Warning, MergeCancelledSummary, "There are no source branches to merge.");
            }

            var mergeBranches = new List<string>();

            foreach (var sourceBranch in sourceBranches)
            {
                var branches = GetMergeBranches(versionControlServer, sourceBranch);
                if (branches.Any(b => string.Equals(b, targetBranch, StringComparison.OrdinalIgnoreCase)))
                {
                    mergeBranches.Add(sourceBranch);
                }
            }

            if (!mergeBranches.Any())
            {
                return CreateResult(MergeResultType.Warning, MergeCancelledSummary, "There are no target branches to merge.");
            }

            ChangesetVersionSpec changesetVersionSpec = new ChangesetVersionSpec(changeset.ChangesetId);

            GetStatus mergeStatus = null;

            for (int i = 0; i < mergeBranches.Count; i++)
            {
                var status = workspace.Merge(mergeBranches[i], targetBranch, changesetVersionSpec, changesetVersionSpec, LockLevel.None, RecursionType.Full, MergeOptionsEx.None);

                if (i == 0)
                {
                    mergeStatus = status;
                }
                else
                {
                    mergeStatus.Combine(status);
                }
            }

            var mergeResult = ReportMergeStatus(mergeStatus);
            if (mergeResult == null)
            {
                return CreateResult(MergeResultType.Warning, MergeCancelledSummary, "Merge did not produce a status.");
            }

            bool isFirstMerge = !mergeHistory.Any();

            mergeHistory.Add(mergeBranches, targetBranch);
            var mergeComment = GetMergeComment(mergeHistory, changeset);

            // below main thread only UI parts

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_asyncPackage.DisposalToken);

            if (isFirstMerge)
            {
                ClearAssociatedWorkItems(pendingChangesPage);
            }

            SetComment(mergeComment, pendingChangesPage);

            foreach (var workItem in changeset.WorkItems)
            {
                await AssociateWorkItemAsync(workItem.Id, pendingChangesPage);
            }

            if (workspace.QueryConflicts(new string[] { targetBranch }, true).Any())
            {
                await OpenResolveConfiltsPageAsync();
            }

            pendingChangesPage?.Refresh();
            return mergeResult;
        }

        public async Task ClearPendingChangesPageAsync()
        {
            var pendingChangesPage = await GetPendingChangesPageAsync();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_asyncPackage.DisposalToken);
            SetComment(null, pendingChangesPage);
            ClearAssociatedWorkItems(pendingChangesPage);
            ExcludeAll(pendingChangesPage);
        }

        public async Task NavigateToPendingChangePageAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_asyncPackage.DisposalToken);
            var teamExplorer = await GetTeamExplorerAsync();
            teamExplorer?.NavigateToPage(new Guid(TeamExplorerPageIds.PendingChanges), null);
        }

        private MergeResult ReportMergeStatus(GetStatus mergeStatus)
        {
            if (mergeStatus == null)
            {
                return null;
            }

            var status = new StringBuilder();

            if (mergeStatus.NoActionNeeded)
            {
                status.AppendLine(MergeCancelledSummary);
                status.AppendLine();
                status.AppendLine("There are no changes to be merged.");
                return CreateResult(MergeResultType.Info, MergeCancelledSummary, status.ToString());
            }

            void AddStatusInfo()
            {
                status.AppendLine();
                status.AppendLine($"Merge statistics:");

                if (mergeStatus.NumFiles > 0)
                {
                    status.AppendLine($"Files: {mergeStatus.NumFiles}");
                }

                if (mergeStatus.NumUpdated > 0)
                {
                    status.AppendLine($"Updates: {mergeStatus.NumUpdated}");
                }

                if (mergeStatus.NumOperations > 0)
                {
                    status.AppendLine($"Operations: {mergeStatus.NumOperations}");
                }

                if (mergeStatus.NumConflicts > 0)
                {
                    status.AppendLine($"Conflicts: {mergeStatus.NumConflicts}");
                }

                if (mergeStatus.NumResolvedConflicts > 0)
                {
                    status.AppendLine($"Resolved Conflicts: {mergeStatus.NumResolvedConflicts}");
                }

                if (mergeStatus.NumWarnings > 0)
                {
                    status.AppendLine($"Warnings: {mergeStatus.NumWarnings}");
                }

                if (mergeStatus.NumFailures > 0)
                {
                    status.AppendLine($"Failures: {mergeStatus.NumFailures}");
                }

                status.AppendLine();
            }

            var failures = mergeStatus.GetFailures();
            if (failures.Any())
            {
                status.AppendLine("Merge partially complited.");
                AddStatusInfo();
                status.AppendLine("Open Team Explorer Output panel to see failure details.");
                return CreateResult(MergeResultType.Warning, MergePartiallyCompletedSummary, status.ToString());
            }

            status.AppendLine(MergeCompletedSummary);
            AddStatusInfo();
            status.AppendLine("Please review the changes and check-in manually.");
            return CreateResult(MergeResultType.Info, MergeCompletedSummary, status.ToString());
        }

        private static List<string> GetSourceBranches(VersionControlServer versionControlServer, Changeset changeset)
        {
            if (versionControlServer == null || changeset == null)
            {
                return new List<string>();
            }

            var branchOwnerships = versionControlServer.QueryBranchObjectOwnership(new[] { changeset.ChangesetId });
            var sourceBranches = branchOwnerships?
                .Where(bo => !bo.RootItem.IsDeleted)
                .Select(bo => bo.RootItem.Item).ToList();

            return sourceBranches ?? new List<string>();
        }

        private static IEnumerable<string> GetMergeBranches(VersionControlServer versionControlServer, string sourceBranch)
        {
            var mergeBranches = versionControlServer?.QueryMergeRelationships(sourceBranch)
                    .Where(i => !i.IsDeleted)
                    .Select(i => i.Item)
                    .Reverse() ?? Enumerable.Empty<string>();

            return mergeBranches;
        }

        private async Task<VersionControlServer> GetVersionControlAsync()
        {
            try
            {
                return await _versionControlConnectionTask.GetValueAsync();
            }
            catch
            {
                RenewVersionControlConnection();
                return await _versionControlConnectionTask.GetValueAsync();
            }
        }

        private void RenewVersionControlConnection()
        {
            _versionControlConnectionTask = new AsyncLazy<VersionControlServer>(ConnectToVersionControlAsync, ThreadHelper.JoinableTaskFactory);
        }

        private async Task<VersionControlServer> ConnectToVersionControlAsync()
        {
            var foundationServerExt = await GetTfsObjectAsync<TeamFoundationServerExt>("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt");

            if (string.IsNullOrWhiteSpace(foundationServerExt?.ActiveProjectContext?.DomainUri))
            {
                throw new InvalidOperationException("The TFS is not online at the moment.");
            }

            TfsTeamProjectCollection projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(foundationServerExt.ActiveProjectContext.DomainUri));
            projectCollection.Connect(ConnectOptions.None);
            projectCollection.EnsureAuthenticated();

            var versionControlServer = projectCollection.GetService<VersionControlServer>();
            return versionControlServer;
        }

        private async Task<T> GetTfsObjectAsync<T>(string name) where T : class
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_asyncPackage.DisposalToken);
            var dte = (DTE)await _asyncPackage.GetServiceAsync(typeof(DTE));

            T tfsObject = dte?.GetObject(name) as T;
            return tfsObject;
        }

        private async Task ExecuteCommandAsync(string commandName, string commandArgs = "")
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_asyncPackage.DisposalToken);
            var dte = (DTE)await _asyncPackage.GetServiceAsync(typeof(DTE));

            dte?.ExecuteCommand(commandName, commandArgs);
        }

        private async Task<VersionControlExt> GetVersionControlExtAsync()
        {
            var versionControlExt = await GetTfsObjectAsync<VersionControlExt>("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt");
            return versionControlExt ?? throw new InvalidOperationException("VersionControlExt not available.");
        }

        private Task OpenResolveConfiltsPageAsync()
        {
            return ExecuteCommandAsync("TeamFoundationContextMenus.PendingChangesPageMoreLink.TfsContextPendingChangesResolveConflicts");
        }

        private async Task<ITeamExplorer> GetTeamExplorerAsync()
        {
            var teamExplorer = await _asyncPackage.GetServiceAsync(typeof(ITeamExplorer));
            return (ITeamExplorer)teamExplorer;
        }

        private async Task<ITeamExplorerPage> GetPendingChangesPageAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_asyncPackage.DisposalToken);
            var teamExplorer = await GetTeamExplorerAsync();

            if (teamExplorer?.CurrentPage?.Title == "Pending Changes")
            {
                return teamExplorer.CurrentPage;
            }

            ITeamExplorerPage pendingChangesPage = teamExplorer?.NavigateToPage(new Guid(TeamExplorerPageIds.PendingChanges), null);
            return pendingChangesPage;
        }

        private static object GetPendingChangesPageModel(ITeamExplorerPage pendingChangesPage)
        {
            var model = ReflectionUtils.GetProperty("Model", pendingChangesPage);
            return model;
        }

        private static object GetPendingChangesPageViewModel(ITeamExplorerPage pendingChangesPage)
        {
            var viewModel = ReflectionUtils.GetProperty("ViewModel", pendingChangesPage);
            return viewModel;
        }

        private static object GetPendingCheckinManager(ITeamExplorerPage pendingChangesPage)
        {
            var model = GetPendingChangesPageModel(pendingChangesPage);
            if (model == null)
            {
                return null;
            }

            var pendingCheckinManager = ReflectionUtils.GetProperty<object>("PendingCheckinManager", model);
            return pendingCheckinManager;
        }

        private static Workspace GetCurrentWorkspace(ITeamExplorerPage pendingChangesPage)
        {
            var model = GetPendingChangesPageModel(pendingChangesPage);
            if (model == null)
            {
                return null;
            }

            var workspace = ReflectionUtils.GetProperty<Workspace>("Workspace", model);
            return workspace;
        }

        private string GetMergeComment(MergeHistory mergeHistory, Changeset changeset)
        {
            var mergeFromTo = new StringBuilder();

            var delimeter = _settings.MergeDelimeter;

            int index = 0;
            foreach (var mergeHistoryItem in mergeHistory)
            {
                string sourceBranch = mergeHistoryItem.Key;
                List<string> targetBranches = mergeHistoryItem.Value;

                var sourceBranchShort = string.Empty;
                var targetBranchesShort = new List<string>();

                for (int i = 0; i < targetBranches.Count; i++)
                {
                    string targetBranch = targetBranches[i];
                    var commonPrefix = StringUtils.FindCommonPrefix(sourceBranch, targetBranch, StringComparison.OrdinalIgnoreCase);
                    commonPrefix = StringUtils.TakeTillLastChar(commonPrefix, '/');

                    var currentSourceBranchShort = sourceBranch.Substring(commonPrefix.Length);
                    var currentTargetBranchShort = targetBranch.Substring(commonPrefix.Length);

                    targetBranchesShort.Add(currentTargetBranchShort);

                    if (i == 0)
                    {
                        sourceBranchShort = currentSourceBranchShort;
                    }
                    else
                    {
                        sourceBranchShort = StringUtils.PickShortest(sourceBranchShort, currentSourceBranchShort);
                    }
                }

                if (index > 0)
                {
                    mergeFromTo.Append("; ");
                }

                mergeFromTo.Append(sourceBranchShort);
                mergeFromTo.Append(delimeter);
                mergeFromTo.Append(string.Join(", ", targetBranchesShort));

                index++;
            }

            var replacements = new Dictionary<string, string>
            {
                { "MergeFromTo", mergeFromTo.ToString() },
                { "ChangesetNumber", changeset.ChangesetId.ToString() },
                { "ChangesetComment", changeset.Comment },
                { "ChangesetOwner", changeset.OwnerDisplayName }
            };

            string mergeComment = _settings.CommentFormat;

            foreach (var placeholder in replacements)
            {
                mergeComment = mergeComment.Replace($"{{{placeholder.Key}}}", placeholder.Value);
            }

            return mergeComment;
        }

        private static void SetComment(string comment, ITeamExplorerPage pendingChangesPage)
        {
            var model = GetPendingChangesPageModel(pendingChangesPage);
            if (model == null)
            {
                return;
            }

            ReflectionUtils.SetProperty("CheckinComment", model, comment);
        }

        private static void ExcludeAll(ITeamExplorerPage pendingChangesPage)
        {
            var pendingCheckinManager = GetPendingCheckinManager(pendingChangesPage);
            if (pendingCheckinManager == null)
            {
                return;
            }

            ReflectionUtils.InvokeMethod("ExcludeAllIncludedPendingChanges", pendingCheckinManager);
        }

        private static void ClearAssociatedWorkItems(ITeamExplorerPage pendingChangesPage)
        {
            var pendingCheckinManager = GetPendingCheckinManager(pendingChangesPage);
            if (pendingCheckinManager == null)
            {
                return;
            }

            var wiInfo = ReflectionUtils.GetProperty<IEnumerable<WorkItemCheckedInfo>>("CheckinWorkItems", pendingCheckinManager);
            if (wiInfo == null || !wiInfo.Any())
            {
                return;
            }

            ReflectionUtils.InvokeMethod("RemoveCheckinWorkItems", pendingCheckinManager, wiInfo);
        }

        private static Task AssociateWorkItemAsync(int workItemId, ITeamExplorerPage pendingChangesPage)
        {
            var model = GetPendingChangesPageModel(pendingChangesPage);
            if (model == null)
            {
                return Task.CompletedTask;
            }

            var enumType = ReflectionUtils.GetNestedType("WorkItemsAddSource", model.GetType().BaseType);
            if (enumType == null)
            {
                return Task.CompletedTask;
            }

            var addByIdValue = Enum.Parse(enumType, "AddById");
            if (addByIdValue == null)
            {
                return Task.CompletedTask;
            }

            return ReflectionUtils.InvokeMethod<Task>("AddWorkItemsByIdAsync", model, new int[] { workItemId }, addByIdValue)
                ?? Task.CompletedTask;
        }

        private static MergeResult CreateResult(MergeResultType kind, string summary, string details)
        {
            return new MergeResult
            {
                ResultType = kind,
                Summary = summary,
                Details = details
            };
        }
    }
}
