using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PIC32Mn_PROJ
{
    public partial class Form1 : Form
    {
        // Populate a tree recursively
        private void PopulateTreeView(TreeView tree, string rootFolderPath)
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

        private void PopulateTreeViewWithFoldersAndFiles(string rootFolderPath)
        {
            treeView_Project.Nodes.Clear();
            var rootDirectoryInfo = new DirectoryInfo(rootFolderPath);
            var rootNode = new TreeNode(rootDirectoryInfo.Name) { Tag = rootDirectoryInfo };
            treeView_Project.Nodes.Add(rootNode);
            AddDirectoriesAndFiles(rootDirectoryInfo, rootNode);
            rootNode.Expand();
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

        private void SetupTreeViewContextMenu()
        {
            treeContextMenu = new ContextMenuStrip();

            leftPasteMenuItem = new ToolStripMenuItem("Paste", null, (s, e) => LeftPasteFromRightClipboard());
            treeContextMenu.Items.Add(leftPasteMenuItem);

            deleteMenuItem = new ToolStripMenuItem("Delete", null, (s, e) => DeleteSelectedNode());
            treeContextMenu.Items.Add(deleteMenuItem);
        }

        private void treeView_Project_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            treeView_Project.SelectedNode = e.Node;
            contextNode = e.Node;

            leftPasteMenuItem.Enabled = rightCopyBufferPaths != null && rightCopyBufferPaths.Count > 0;
            deleteMenuItem.Enabled = e.Node.Parent != null;

            treeContextMenu.Show(treeView_Project, e.Location);
        }

        private void treeView_Project_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedNode();
                e.Handled = true;
            }
        }

        private void DeleteSelectedNode()
        {
            var node = contextNode ?? treeView_Project.SelectedNode;
            contextNode = null;
            if (node == null) return;

            if (node.Parent == null)
            {
                MessageBox.Show("Cannot delete the project root folder.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                if (node.Tag is FileInfo fi)
                {
                    if (!IsUnderProjectRoot(fi.FullName))
                    {
                        MessageBox.Show("Blocked: outside project root.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var confirm = MessageBox.Show($"Delete file:\n{fi.FullName}?", "Confirm Delete",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    if (confirm != DialogResult.Yes) return;

                    if (!string.IsNullOrEmpty(currentViewFilePath) &&
                        string.Equals(currentViewFilePath, fi.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        avalonEditor.Text = string.Empty;
                        currentViewFilePath = string.Empty;
                        UpdateViewHeader(null);
                    }

                    if (File.Exists(fi.FullName))
                    {
                        File.SetAttributes(fi.FullName, FileAttributes.Normal);
                        File.Delete(fi.FullName);
                    }

                    node.Remove();
                }
                else if (node.Tag is DirectoryInfo di)
                {
                    if (!IsUnderProjectRoot(di.FullName))
                    {
                        MessageBox.Show("Blocked: outside project root.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var confirm = MessageBox.Show($"Delete folder and all contents:\n{di.FullName}?",
                        "Confirm Delete Folder", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);
                    if (confirm != DialogResult.Yes) return;

                    if (Directory.Exists(di.FullName))
                        Directory.Delete(di.FullName, recursive: true);

                    node.Remove();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsUnderProjectRoot(string path)
        {
            if (string.IsNullOrEmpty(projectDirPath)) return false;
            var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var root = Path.GetFullPath(projectDirPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        // Drag and Drop handlers
        private void TreeView_Right_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item is not TreeNode node) return;
            var path = GetNodePath(node);
            if (string.IsNullOrEmpty(path)) return;

            var data = new DataObject();
            data.SetData(DataFormats.FileDrop, new[] { path });
            data.SetData("SourceRootPath", projectDirPathRight ?? string.Empty);
            DoDragDrop(data, DragDropEffects.Copy);
        }

        private void TreeView_Left_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node)
            {
                var path = GetNodePath(node);
                if (string.IsNullOrEmpty(path)) return;

                var data = new DataObject();
                data.SetData(DataFormats.FileDrop, new[] { path });
                data.SetData("SourceRootPath", projectDirPath ?? string.Empty);
                DoDragDrop(data, DragDropEffects.Copy);
            }
        }

        private void TreeView_Right_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void TreeView_Right_DragOver(object? sender, DragEventArgs e)
        {
            TreeView_SetCopyEffectIfValid(treeView_Right, e);
        }

        private void TreeView_SetCopyEffectIfValid(TreeView tv, DragEventArgs e)
        {
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            var targetNode = tv.GetNodeAt(tv.PointToClient(new Point(e.X, e.Y)));
            e.Effect = DragDropEffects.Copy; // valid for file or folder
        }

        private void TreeView_Right_DragDrop(object? sender, DragEventArgs e)
        {
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false)) return;
            var files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;

            var tv = treeView_Right;
            var clientPoint = tv.PointToClient(new Point(e.X, e.Y));
            var targetNode = tv.GetNodeAt(clientPoint);

            var targetDir = GetDropTargetDirectory(targetNode, projectDirPathRight);
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir)) return;

            foreach (var srcPath in files)
            {
                try
                {
                    if (Directory.Exists(srcPath))
                    {
                        var srcDir = new DirectoryInfo(srcPath);
                        var destDir = Path.Combine(targetDir, srcDir.Name);
                        CopyDirectory(srcDir.FullName, destDir);
                    }
                    else if (File.Exists(srcPath))
                    {
                        var fileName = Path.GetFileName(srcPath);
                        var destFile = Path.Combine(targetDir, fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                        File.Copy(srcPath, destFile, overwrite: true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Copy failed:\n{ex.Message}", "Copy", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (!string.IsNullOrEmpty(projectDirPathRight))
                PopulateTreeView(treeView_Right, projectDirPathRight);
        }

        private void TreeView_Project_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void TreeView_Project_DragOver(object? sender, DragEventArgs e)
        {
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false))
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            e.Effect = DragDropEffects.Copy;
        }

        private void TreeView_Project_DragDrop(object? sender, DragEventArgs e)
        {
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;

            var tv = treeView_Project;
            var clientPoint = tv.PointToClient(new Point(e.X, e.Y));
            var targetNode = tv.GetNodeAt(clientPoint);

            var targetDir = GetDropTargetDirectory(targetNode, projectDirPath);
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir)) return;

            var policy = OverwritePolicy.Ask;

            try
            {
                foreach (var srcPath in files)
                {
                    if (Directory.Exists(srcPath))
                    {
                        var srcDir = new DirectoryInfo(srcPath);
                        var destDir = Path.Combine(targetDir, srcDir.Name);
                        CopyDirectoryWithPrompt(srcDir.FullName, destDir, ref policy);
                    }
                    else if (File.Exists(srcPath))
                    {
                        var fileName = Path.GetFileName(srcPath);
                        var destFile = Path.Combine(targetDir, fileName);
                        TryCopyFileWithPrompt(srcPath, destFile, ref policy);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // user cancelled
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy failed:\n{ex.Message}", "Copy", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            var dirNodeToRefresh = GetDirectoryNodeForDrop(targetNode);
            if (dirNodeToRefresh != null)
                RepopulateDirectoryNode(dirNodeToRefresh);
        }

        private static string GetDropTargetDirectory(TreeNode? targetNode, string rootPath)
        {
            if (targetNode == null || targetNode.Tag == null)
                return rootPath;

            return targetNode.Tag switch
            {
                DirectoryInfo di => di.FullName,
                FileInfo fi => Path.GetDirectoryName(fi.FullName) ?? rootPath,
                _ => rootPath
            };
        }

        private string GetNodePath(TreeNode node)
        {
            if (node?.Tag is FileSystemInfo fsi)
                return fsi.FullName;
            return string.Empty;
        }

        private void RepopulateDirectoryNode(TreeNode dirNode)
        {
            if (dirNode == null || dirNode.Tag is not DirectoryInfo di) return;

            dirNode.Nodes.Clear();
            AddDirectoriesAndFiles(di, dirNode);
            dirNode.Expand();
        }

        private enum OverwritePolicy
        {
            Ask,
            YesToAll,
            NoToAll
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            var src = new DirectoryInfo(sourceDir);
            if (!src.Exists) return;

            Directory.CreateDirectory(destDir);

            foreach (var file in src.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var target = Path.Combine(destDir, file.Name);
                file.CopyTo(target, overwrite: true);
                File.SetAttributes(target, FileAttributes.Normal);
            }

            foreach (var dir in src.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                CopyDirectory(dir.FullName, Path.Combine(destDir, dir.Name));
            }
        }

        private static void CopyDirectoryWithPrompt(string sourceDir, string destDir, ref OverwritePolicy policy)
        {
            var src = new DirectoryInfo(sourceDir);
            if (!src.Exists) return;

            Directory.CreateDirectory(destDir);

            foreach (var file in src.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var target = Path.Combine(destDir, file.Name);
                TryCopyFileWithPrompt(file.FullName, target, ref policy);
            }

            foreach (var dir in src.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                var subDest = Path.Combine(destDir, dir.Name);
                CopyDirectoryWithPrompt(dir.FullName, subDest, ref policy);
            }
        }

        private static bool TryCopyFileWithPrompt(string srcFile, string destFile, ref OverwritePolicy policy)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

            if (!File.Exists(destFile))
            {
                File.Copy(srcFile, destFile, overwrite: true);
                File.SetAttributes(destFile, FileAttributes.Normal);
                return true;
            }

            switch (policy)
            {
                case OverwritePolicy.YesToAll:
                    File.Copy(srcFile, destFile, overwrite: true);
                    File.SetAttributes(destFile, FileAttributes.Normal);
                    return true;
                case OverwritePolicy.NoToAll:
                    return false;
                case OverwritePolicy.Ask:
                default:
                    var res = MessageBox.Show(
                        $"The file already exists:\n{destFile}\nDo you want to replace it?",
                        "Copy and Replace",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);

                    if (res == DialogResult.Cancel)
                        throw new OperationCanceledException("User cancelled copy.");

                    if (res == DialogResult.Yes)
                    {
                        var applyAll = MessageBox.Show(
                            "Apply this choice (Replace) to all remaining existing files?",
                            "Apply to All",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (applyAll == DialogResult.Yes)
                            policy = OverwritePolicy.YesToAll;

                        File.Copy(srcFile, destFile, overwrite: true);
                        File.SetAttributes(destFile, FileAttributes.Normal);
                        return true;
                    }
                    else
                    {
                        var applyAll = MessageBox.Show(
                            "Apply this choice (Skip) to all remaining existing files?",
                            "Apply to All",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (applyAll == DialogResult.Yes)
                            policy = OverwritePolicy.NoToAll;
                        return false;
                    }
            }
        }
    }
}
