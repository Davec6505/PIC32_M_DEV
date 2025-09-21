using PIC32Mn_PROJ.Services.Abstractions;
using System.IO;
using System.Windows.Forms;

namespace PIC32Mn_PROJ.Services.Implementation
{
    public class ProjectTreeService : IProjectTreeService
    {
        public void Populate(TreeView tree, string rootFolderPath)
        {
            tree.BeginUpdate();
            try
            {
                tree.Nodes.Clear();
                var rootDirectoryInfo = new DirectoryInfo(rootFolderPath);
                var rootNode = new TreeNode(rootDirectoryInfo.Name) { Tag = rootDirectoryInfo };
                tree.Nodes.Add(rootNode);
                AddDirectoriesAndFiles(rootDirectoryInfo, rootNode);
                rootNode.Expand();
            }
            finally
            {
                tree.EndUpdate();
            }
        }

        public void PopulateLeft(TreeView leftTree, string rootFolderPath)
        {
            leftTree.Nodes.Clear();
            var rootDirectoryInfo = new DirectoryInfo(rootFolderPath);
            var rootNode = new TreeNode(rootDirectoryInfo.Name) { Tag = rootDirectoryInfo };
            leftTree.Nodes.Add(rootNode);
            AddDirectoriesAndFiles(rootDirectoryInfo, rootNode);
            rootNode.Expand();
        }

        public void AddOrUpdateFileNode(TreeView tree, string rootFolderPath, string filePath)
        {
            if (tree.Nodes.Count == 0 || string.IsNullOrEmpty(rootFolderPath) || string.IsNullOrEmpty(filePath))
                return;

            var rootNode = tree.Nodes[0];
            if (rootNode.Tag is not DirectoryInfo)
                return;

            var normRoot = Path.GetFullPath(rootFolderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normFile = Path.GetFullPath(filePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!normFile.StartsWith(normRoot, StringComparison.OrdinalIgnoreCase))
                return;

            var rel = Path.GetRelativePath(normRoot, normFile);
            var dirRel = Path.GetDirectoryName(rel) ?? string.Empty;
            var segments = dirRel.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            TreeNode current = rootNode;
            string currentPath = normRoot;

            foreach (var seg in segments)
            {
                currentPath = Path.Combine(currentPath, seg);
                TreeNode? dirNode = null;
                foreach (TreeNode child in current.Nodes)
                {
                    if (child.Tag is DirectoryInfo cdi && string.Equals(
                            Path.GetFullPath(cdi.FullName).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                            currentPath, StringComparison.OrdinalIgnoreCase))
                    {
                        dirNode = child;
                        break;
                    }
                }
                if (dirNode == null)
                {
                    var di = new DirectoryInfo(currentPath);
                    if (!di.Exists) Directory.CreateDirectory(di.FullName);
                    dirNode = new TreeNode(di.Name) { Tag = di };
                    current.Nodes.Add(dirNode);
                }
                current = dirNode;
            }

            var fileName = Path.GetFileName(normFile);
            TreeNode? fileNode = null;
            foreach (TreeNode child in current.Nodes)
            {
                if (child.Tag is FileInfo cfi && string.Equals(
                        Path.GetFullPath(cfi.FullName).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                        normFile, StringComparison.OrdinalIgnoreCase))
                {
                    fileNode = child;
                    break;
                }
            }

            if (fileNode == null)
            {
                var fi = new FileInfo(normFile);
                fileNode = new TreeNode(fi.Name) { Tag = fi };
                current.Nodes.Add(fileNode);
            }
            else
            {
                fileNode.Text = fileName;
                fileNode.Tag = new FileInfo(normFile);
            }

            current.Expand();
        }

        public void RepopulateDirectoryNode(TreeNode dirNode)
        {
            if (dirNode == null || dirNode.Tag is not DirectoryInfo di) return;
            dirNode.Nodes.Clear();
            AddDirectoriesAndFiles(di, dirNode);
            dirNode.Expand();
        }

        public bool IsUnderRoot(string path, string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath)) return false;
            var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        public string GetDropTargetDirectory(TreeNode? targetNode, string rootPath)
        {
            if (targetNode == null || targetNode.Tag == null) return rootPath;
            return targetNode.Tag switch
            {
                DirectoryInfo di => di.FullName,
                FileInfo fi => Path.GetDirectoryName(fi.FullName) ?? rootPath,
                _ => rootPath
            };
        }

        public string GetNodePath(TreeNode node)
        {
            if (node?.Tag is FileSystemInfo fsi) return fsi.FullName;
            return string.Empty;
        }

        private void AddDirectoriesAndFiles(DirectoryInfo directoryInfo, TreeNode parentNode)
        {
            foreach (var directory in directoryInfo.GetDirectories())
            {
                var dirNode = new TreeNode(directory.Name) { Tag = directory };
                parentNode.Nodes.Add(dirNode);
                AddDirectoriesAndFiles(directory, dirNode);
            }
            foreach (var file in directoryInfo.GetFiles())
            {
                var fileNode = new TreeNode(file.Name) { Tag = file };
                parentNode.Nodes.Add(fileNode);
            }
        }
    }
}
