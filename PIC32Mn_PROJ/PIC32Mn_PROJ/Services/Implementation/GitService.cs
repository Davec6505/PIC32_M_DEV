using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using PIC32Mn_PROJ.Services.Abstractions;

namespace PIC32Mn_PROJ.Services.Implementation
{
    public sealed class GitService : IGitService
    {
        public bool TryDiscoverRepo(string startPath, out RepoInfo? info)
        {
            info = null;
            if (string.IsNullOrWhiteSpace(startPath)) return false;
            var discovered = Repository.Discover(startPath);
            if (string.IsNullOrEmpty(discovered)) return false;

            var repoRoot = Path.GetFullPath(Path.Combine(discovered, ".."));
            using var repo = new Repository(repoRoot);
            var branch = repo.Head.FriendlyName;
            info = new RepoInfo(repoRoot, branch);
            return true;
        }

        public Task<IReadOnlyList<FileStatusItem>> GetStatusAsync(string repoRoot, CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                using var repo = new Repository(repoRoot);
                var status = repo.RetrieveStatus(new StatusOptions { IncludeUntracked = true });
                var list = new List<FileStatusItem>();

                foreach (var e in status)
                {
                    ct.ThrowIfCancellationRequested();
                    var s = NormalizeStatus(e.State);
                    list.Add(new FileStatusItem(e.FilePath, s));
                }
                return (IReadOnlyList<FileStatusItem>)list;
            }, ct);
        }

        public Task<IReadOnlyList<string>> GetBranchesAsync(string repoRoot, CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                using var repo = new Repository(repoRoot);
                var names = repo.Branches.Select(b => b.FriendlyName).ToList();
                return (IReadOnlyList<string>)names;
            }, ct);
        }

        public Task StageAsync(string repoRoot, IEnumerable<string> paths, CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                using var repo = new Repository(repoRoot);
                Commands.Stage(repo, paths);
            }, ct);
        }

        public Task UnstageAsync(string repoRoot, IEnumerable<string> paths, CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                using var repo = new Repository(repoRoot);
                Commands.Unstage(repo, paths);
            }, ct);
        }

        public Task CommitAsync(string repoRoot, string message, string authorName, string authorEmail, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Commit message required.", nameof(message));
            if (string.IsNullOrWhiteSpace(authorName)) throw new ArgumentException("Author name required.", nameof(authorName));
            if (string.IsNullOrWhiteSpace(authorEmail)) throw new ArgumentException("Author email required.", nameof(authorEmail));

            return Task.Run(() =>
            {
                using var repo = new Repository(repoRoot);
                var sig = new Signature(authorName, authorEmail, DateTimeOffset.Now);
                // Commit staged changes
                repo.Commit(message, sig, sig, new CommitOptions { AllowEmptyCommit = false });
            }, ct);
        }

        public Task SwitchBranchAsync(string repoRoot, string branchName, bool createIfMissing = false, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(branchName)) throw new ArgumentException("Branch name is required.", nameof(branchName));
            return Task.Run(() =>
            {
                using var repo = new Repository(repoRoot);
                var branch = repo.Branches[branchName];
                if (branch == null && createIfMissing)
                {
                    branch = repo.CreateBranch(branchName);
                }
                if (branch == null) throw new InvalidOperationException($"Branch '{branchName}' not found.");
                Commands.Checkout(repo, branch);
            }, ct);
        }

        public Task<IReadOnlyList<CommitInfo>> GetLogAsync(string repoRoot, int maxCount = 50, CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                using var repo = new Repository(repoRoot);
                var items = repo.Commits.Take(Math.Max(1, maxCount))
                    .Select(c => new CommitInfo(c.Sha.Substring(0, 7), c.MessageShort ?? string.Empty, c.Author?.Name ?? "unknown", c.Author?.When ?? DateTimeOffset.MinValue))
                    .ToList();
                return (IReadOnlyList<CommitInfo>)items;
            }, ct);
        }

        public Task<string> FetchAsync(string repoRoot, CancellationToken ct = default)
        {
            return RunGitCliAsync(repoRoot, "fetch --all --prune", ct);
        }

        public Task<string> PullAsync(string repoRoot, CancellationToken ct = default)
        {
            return RunGitCliAsync(repoRoot, "pull --ff-only", ct);
        }

        public Task<string> PushAsync(string repoRoot, string remote = "origin", string branch = "", CancellationToken ct = default)
        {
            var args = string.IsNullOrWhiteSpace(branch) ? $"push {Escape(remote)}" : $"push {Escape(remote)} {Escape(branch)}";
            return RunGitCliAsync(repoRoot, args, ct);
        }

        private static string NormalizeStatus(FileStatus s)
        {
            // Compact human-readable status
            if (s.HasFlag(FileStatus.NewInWorkdir)) return "Untracked";
            if (s.HasFlag(FileStatus.NewInIndex)) return "Added";
            if (s.HasFlag(FileStatus.ModifiedInWorkdir)) return "Modified";
            if (s.HasFlag(FileStatus.ModifiedInIndex)) return "Staged";
            if (s.HasFlag(FileStatus.DeletedFromWorkdir)) return "Deleted";
            if (s.HasFlag(FileStatus.DeletedFromIndex)) return "Removed";
            if (s.HasFlag(FileStatus.RenamedInIndex) || s.HasFlag(FileStatus.RenamedInWorkdir)) return "Renamed";
            if (s == FileStatus.Ignored) return "Ignored";
            if (s == FileStatus.Conflicted) return "Conflicted";
            return s.ToString();
        }

        private static Task<string> RunGitCliAsync(string repoRoot, string arguments, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = repoRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = new Process { StartInfo = psi, EnableRaisingEvents = false };
                var sb = new StringBuilder();
                p.OutputDataReceived += (_, e) => { if (e.Data != null) sb.AppendLine(e.Data); };
                p.ErrorDataReceived += (_, e) => { if (e.Data != null) sb.AppendLine(e.Data); };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                while (!p.HasExited)
                {
                    if (ct.IsCancellationRequested)
                    {
                        try { p.Kill(entireProcessTree: true); } catch { }
                        ct.ThrowIfCancellationRequested();
                    }
                    Thread.Sleep(10);
                }
                return sb.ToString().TrimEnd();
            }, ct);
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Contains(' ') ? $"\"{s.Replace("\"", "\\\"")}\"" : s;
        }
    }
}