using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PIC32Mn_PROJ.Services.Abstractions
{
    public interface IGitService
    {
        bool TryDiscoverRepo(string startPath, out RepoInfo? info);

        Task<IReadOnlyList<FileStatusItem>> GetStatusAsync(string repoRoot, CancellationToken ct = default);
        Task StageAsync(string repoRoot, IEnumerable<string> paths, CancellationToken ct = default);
        Task UnstageAsync(string repoRoot, IEnumerable<string> paths, CancellationToken ct = default);
        Task CommitAsync(string repoRoot, string message, string authorName, string authorEmail, CancellationToken ct = default);
        // Stage all and commit convenience
        Task CommitAllAsync(string repoRoot, string message, string authorName, string authorEmail, CancellationToken ct = default);

        Task SwitchBranchAsync(string repoRoot, string branchName, bool createIfMissing = false, CancellationToken ct = default);
        Task<IReadOnlyList<CommitInfo>> GetLogAsync(string repoRoot, int maxCount = 50, CancellationToken ct = default);

        // Branches
        Task<IReadOnlyList<string>> GetBranchesAsync(string repoRoot, CancellationToken ct = default);

        // Network ops via git.exe to leverage existing credentials
        Task<string> FetchAsync(string repoRoot, CancellationToken ct = default);
        Task<string> PullAsync(string repoRoot, CancellationToken ct = default);
        Task<string> PushAsync(string repoRoot, string remote = "origin", string branch = "", CancellationToken ct = default);
    }

    public sealed record RepoInfo(string Root, string CurrentBranch);
    public sealed record FileStatusItem(string Path, string Status);
    public sealed record CommitInfo(string Sha, string MessageShort, string Author, System.DateTimeOffset When);
}