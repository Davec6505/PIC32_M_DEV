using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PIC32Mn_PROJ.Services.Abstractions;

namespace PIC32Mn_PROJ
{
    public partial class Form1 : Form
    {
        // Populate any tree with root
        private void PopulateTreeView(TreeView tree, string rootFolderPath)
        {
            _treeSvc.Populate(tree, rootFolderPath);
        }

        // LEFT tree root populate
        private void PopulateTreeViewWithFoldersAndFiles(string rootFolderPath)
        {
            _treeSvc.PopulateLeft(treeView_Project, rootFolderPath);
        }

        private void AddDirectoriesAndFiles(DirectoryInfo directoryInfo, TreeNode parentNode)
        {
            // Kept for compatibility if called elsewhere, but prefer service
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

        // Incrementally add/update a file node under the project tree without collapsing the whole tree
        private void AddOrUpdateFileNode(TreeView tree, string rootFolderPath, string filePath)
        {
            _treeSvc.AddOrUpdateFileNode(tree, rootFolderPath, filePath);
        }

        private void SetupTreeViewContextMenu()
        {
            treeContextMenu = new ContextMenuStrip();

            leftPasteMenuItem = new ToolStripMenuItem("Paste", null, (s, e) => LeftPasteFromRightClipboard());
            treeContextMenu.Items.Add(leftPasteMenuItem);

            var openMenuItem = new ToolStripMenuItem("Open", null, (s, e) => OpenSelectedNodeInTab());
            treeContextMenu.Items.Add(openMenuItem);

            deleteMenuItem = new ToolStripMenuItem("Delete", null, (s, e) => DeleteSelectedNode());
            treeContextMenu.Items.Add(deleteMenuItem);
        }

        private void OpenSelectedNodeInTab()
        {
            var node = contextNode ?? treeView_Project.SelectedNode;
            if (node?.Tag is FileInfo fi && File.Exists(fi.FullName))
            {
                _tabService.OpenFile(tabControl1, fi.FullName, rootPath);
            }
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
                    if (!_treeSvc.IsUnderRoot(fi.FullName, projectDirPath))
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
                    if (!_treeSvc.IsUnderRoot(di.FullName, projectDirPath))
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
            return _treeSvc.IsUnderRoot(path, projectDirPath);
        }

        // DnD
        private void TreeView_Right_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item is not TreeNode node) return;
            var path = _treeSvc.GetNodePath(node);
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
                var path = _treeSvc.GetNodePath(node);
                if (string.IsNullOrEmpty(path)) return;
                var data = new DataObject();
                data.SetData(DataFormats.FileDrop, new[] { path });
                data.SetData("SourceRootPath", projectDirPath ?? string.Empty);
                DoDragDrop(data, DragDropEffects.Copy);
            }
        }

        private void TreeView_Right_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void TreeView_Right_DragOver(object? sender, DragEventArgs e)
        {
            TreeView_SetCopyEffectIfValid(treeView_Right, e);
        }

        private void TreeView_SetCopyEffectIfValid(TreeView tv, DragEventArgs e)
        {
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false)) { e.Effect = DragDropEffects.None; return; }
            e.Effect = DragDropEffects.Copy;
        }

        private void TreeView_Right_DragDrop(object? sender, DragEventArgs e)
        {
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false)) return;
            var files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
            var tv = treeView_Right;
            var clientPoint = tv.PointToClient(new Point(e.X, e.Y));
            var targetNode = tv.GetNodeAt(clientPoint);
            var targetDir = _treeSvc.GetDropTargetDirectory(targetNode, projectDirPathRight);
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir)) return;
            foreach (var srcPath in files)
            {
                try
                {
                    if (Directory.Exists(srcPath))
                    {
                        var srcDir = new DirectoryInfo(srcPath);
                        var destDir = Path.Combine(targetDir, srcDir.Name);
                        _fs.CopyDirectory(srcDir.FullName, destDir);
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
                _treeSvc.Populate(treeView_Right, projectDirPathRight);
        }

        private void TreeView_Project_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void TreeView_Project_DragOver(object? sender, DragEventArgs e)
        {
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false)) { e.Effect = DragDropEffects.None; return; }
            e.Effect = DragDropEffects.Copy;
        }

        private void TreeView_Project_DragDrop(object? sender, DragEventArgs e)
        {
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            var tv = treeView_Project;
            var clientPoint = tv.PointToClient(new Point(e.X, e.Y));
            var targetNode = tv.GetNodeAt(clientPoint);
            var targetDir = _treeSvc.GetDropTargetDirectory(targetNode, projectDirPath);
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir)) return;
            var policy = Services.Abstractions.OverwritePolicy.Ask;
            try
            {
                foreach (var srcPath in files)
                {
                    if (Directory.Exists(srcPath))
                    {
                        var srcDir = new DirectoryInfo(srcPath);
                        var destDir = Path.Combine(targetDir, srcDir.Name);
                        _fs.CopyDirectoryWithPrompt(srcDir.FullName, destDir, ref policy);
                    }
                    else if (File.Exists(srcPath))
                    {
                        var fileName = Path.GetFileName(srcPath);
                        var destFile = Path.Combine(targetDir, fileName);
                        _fs.TryCopyFileWithPrompt(srcPath, destFile, ref policy);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy failed:\n{ex.Message}", "Copy", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            var dirNodeToRefresh = GetDirectoryNodeForDrop(targetNode);
            if (dirNodeToRefresh != null)
                _treeSvc.RepopulateDirectoryNode(dirNodeToRefresh);
        }

        private static string GetDropTargetDirectory(TreeNode? targetNode, string rootPath)
        {
            if (targetNode == null || targetNode.Tag == null) return rootPath;
            return targetNode.Tag switch
            {
                DirectoryInfo di => di.FullName,
                FileInfo fi => Path.GetDirectoryName(fi.FullName) ?? rootPath,
                _ => rootPath
            };
        }

        private TreeNode? GetDirectoryNodeForDrop(TreeNode? node)
        {
            if (node == null) return null;
            if (node.Tag is DirectoryInfo) return node;
            if (node.Tag is FileInfo) return node.Parent;
            return null;
        }

        private string GetNodePath(TreeNode node)
        {
            if (node?.Tag is FileSystemInfo fsi) return fsi.FullName;
            return string.Empty;
        }

        private void RepopulateDirectoryNode(TreeNode dirNode)
        {
            _treeSvc.RepopulateDirectoryNode(dirNode);
        }
    }
}
