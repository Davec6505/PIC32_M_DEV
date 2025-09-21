using System.IO;
using System.Windows.Forms;

namespace PIC32Mn_PROJ.Services.Abstractions
{
    public interface IProjectTreeService
    {
        void Populate(TreeView tree, string rootFolderPath);
        void PopulateLeft(TreeView leftTree, string rootFolderPath);
        void AddOrUpdateFileNode(TreeView tree, string rootFolderPath, string filePath);
        void RepopulateDirectoryNode(TreeNode dirNode);
        bool IsUnderRoot(string path, string rootPath);
        string GetDropTargetDirectory(TreeNode? targetNode, string rootPath);
        string GetNodePath(TreeNode node);
    }
}
