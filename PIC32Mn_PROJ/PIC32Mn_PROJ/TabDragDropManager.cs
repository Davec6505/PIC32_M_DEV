using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PIC32Mn_PROJ
{
    public static class TabDragDropManager
    {
        // Raised when a tab-drag operation begins (first time a drag starts)
        public static event Action? BeginDrag;

        private class DragState
        {
            public int MouseDownTabIndex = -1;
            public Rectangle DragBox = Rectangle.Empty;
            public bool IsDragging;
            public TabPage? DraggedPage;
            public TabControl? SourceControl;
            public int LastInsertIndex = -1;
            public Rectangle LastInsertMark = Rectangle.Empty; // screen coords
            public Point DragStartScreen;
        }

        private sealed class TabPayload
        {
            public TabControl Source { get; }
            public TabPage Page { get; }
            public Point StartScreen { get; }
            public TabPayload(TabControl source, TabPage page, Point startScreen)
            {
                Source = source; Page = page; StartScreen = startScreen;
            }
        }

        private static readonly Dictionary<TabControl, DragState> States = new();
        private static readonly HashSet<TabControl> Registered = new();
        private static TabControl? MainHost;

        // Dock overlay state
        private static Rectangle _lastDockFrame = Rectangle.Empty; // screen coords
        private static TabControl? _lastDockTarget;

        public static void Enable(TabControl tc, bool isMainHost)
        {
            if (Registered.Contains(tc)) return;

            Registered.Add(tc);
            States[tc] = new DragState();
            tc.AllowDrop = true;

            tc.MouseDown += OnMouseDown;
            tc.MouseMove += OnMouseMove;
            tc.MouseUp += OnMouseUp;
            tc.DragEnter += OnDragEnter;
            tc.DragOver += OnDragOver;
            tc.DragDrop += OnDragDrop;
            tc.DragLeave += OnDragLeave;
            tc.GiveFeedback += OnGiveFeedback;
            tc.MouseUp += OnMouseUpForContextMenu;

            if (isMainHost)
                MainHost ??= tc;

            EnsureContextMenu(tc);
        }

        public static void Disable(TabControl tc)
        {
            if (!Registered.Contains(tc)) return;

            Registered.Remove(tc);
            States.Remove(tc);

            tc.MouseDown -= OnMouseDown;
            tc.MouseMove -= OnMouseMove;
            tc.MouseUp -= OnMouseUp;
            tc.DragEnter -= OnDragEnter;
            tc.DragOver -= OnDragOver;
            tc.DragDrop -= OnDragDrop;
            tc.DragLeave -= OnDragLeave;
            tc.GiveFeedback -= OnGiveFeedback;
            tc.MouseUp -= OnMouseUpForContextMenu;

            // Remove context menu hook
            if (tc.ContextMenuStrip == _sharedMenu)
                tc.ContextMenuStrip = null;
        }

        // ------------- Drag logic -------------

        private static void OnMouseDown(object? sender, MouseEventArgs e)
        {
            var tc = (TabControl)sender!;
            var state = States[tc];

            if (e.Button != MouseButtons.Left)
            {
                ResetState(tc);
                return;
            }

            state.MouseDownTabIndex = GetTabIndexAt(tc, e.Location);
            if (state.MouseDownTabIndex >= 0)
            {
                var dragSize = SystemInformation.DragSize;
                state.DragBox = new Rectangle(
                    new Point(e.X - dragSize.Width / 2, e.Y - dragSize.Height / 2),
                    dragSize);
            }
            else
            {
                ResetState(tc);
            }
        }

        private static void OnMouseMove(object? sender, MouseEventArgs e)
        {
            var tc = (TabControl)sender!;
            if (!States.TryGetValue(tc, out var state)) return;

            if (e.Button != MouseButtons.Left || state.MouseDownTabIndex < 0)
                return;

            if (!state.IsDragging)
            {
                if (state.DragBox.Contains(e.Location)) return;

                // Start drag
                state.IsDragging = true;

                BeginDrag?.Invoke();

                var page = tc.TabPages[state.MouseDownTabIndex];
                state.DraggedPage = page;
                state.SourceControl = tc;
                state.DragStartScreen = tc.PointToScreen(e.Location);

                // In OnMouseMove, replace the DataObject creation with the WinForms type-based format
                var payload = new TabPayload(tc, page, state.DragStartScreen);
                var data = new System.Windows.Forms.DataObject(typeof(TabPayload).FullName, payload);

                var effect = tc.DoDragDrop(data, DragDropEffects.Move);

                // Drag finished (synchronously returns)
                ClearInsertMark(tc);
                ClearDockFrame();
                state.IsDragging = false;
                state.DragBox = Rectangle.Empty;
                state.MouseDownTabIndex = -1;

                // If not dropped on any registered target, tear-off
                if (effect == DragDropEffects.None && payload.Page.Parent == payload.Source)
                {
                    // Only tear off if cursor is not over any registered tab headers
                    if (!IsOverAnyTabHeader(Control.MousePosition))
                    {
                        TearOffToFloating(payload.Page, Control.MousePosition);
                    }
                }

                state.DraggedPage = null;
                state.SourceControl = null;
                state.LastInsertIndex = -1;
                state.LastInsertMark = Rectangle.Empty;
            }
        }

        private static void OnMouseUp(object? sender, MouseEventArgs e)
        {
            var tc = (TabControl)sender!;
            ResetState(tc);
        }

        private static void OnDragEnter(object? sender, DragEventArgs e)
        {
            if (TryGetPayload(e, out _))
                e.Effect = DragDropEffects.Move;
            else
                e.Effect = DragDropEffects.None;
        }

        private static void OnDragOver(object? sender, DragEventArgs e)
        {
            var tc = (TabControl)sender!;
            if (!TryGetPayload(e, out var payload))
            {
                e.Effect = DragDropEffects.None;
                ClearInsertMark(tc);
                ClearDockFrame();
                return;
            }

            e.Effect = DragDropEffects.Move;

            var client = tc.PointToClient(new Point(e.X, e.Y));
            var header = GetTabHeaderBounds(tc);
            if (header.Contains(client))
            {
                // Over header: show insertion mark
                ClearDockFrame();
                var insertIndex = GetInsertIndexAt(tc, client);
                DrawInsertMark(tc, insertIndex);
            }
            else
            {
                // Over content area: show dock frame to hint dropping into this panel
                ClearInsertMark(tc);
                ShowDockFrame(tc);
            }
        }

        private static void OnDragDrop(object? sender, DragEventArgs e)
        {
            var tc = (TabControl)sender!;
            ClearInsertMark(tc);
            ClearDockFrame();

            if (!TryGetPayload(e, out var payload))
                return;

            var client = tc.PointToClient(new Point(e.X, e.Y));
            var header = GetTabHeaderBounds(tc);
            int insertIndex;
            if (header.Contains(client))
            {
                insertIndex = GetInsertIndexAt(tc, client);
            }
            else
            {
                // Drop anywhere in content area -> append to end
                insertIndex = tc.TabPages.Count;
            }

            MoveTab(payload.Source, tc, payload.Page, insertIndex);
            tc.SelectedTab = payload.Page;
        }

        private static void OnDragLeave(object? sender, EventArgs e)
        {
            var tc = (TabControl)sender!;
            ClearInsertMark(tc);
            ClearDockFrame();
        }

        private static void OnGiveFeedback(object? sender, GiveFeedbackEventArgs e)
        {
            // Use default cursor; could set a custom cursor for nicer UX
            e.UseDefaultCursors = true;
        }

        private static void ResetState(TabControl tc)
        {
            if (!States.TryGetValue(tc, out var state)) return;

            state.IsDragging = false;
            state.MouseDownTabIndex = -1;
            state.DragBox = Rectangle.Empty;
            state.DraggedPage = null;
            state.SourceControl = null;

            ClearInsertMark(tc);
            ClearDockFrame();
        }

        // ------------- Helpers -------------

        // Replace the whole TryGetPayload with this version that supports both formats
        private static bool TryGetPayload(DragEventArgs e, out TabPayload payload)
        {
            payload = null!;
            var data = e.Data;
            if (data is null) return false;

            // Preferred: WinForms type-based format
            if (data.GetDataPresent(typeof(TabPayload)))
            {
                payload = (TabPayload?)data.GetData(typeof(TabPayload))!;
                return payload is not null && payload.Page is not null && payload.Source is not null;
            }

            // Fallback: string-based format (in case other code used it)
            var format = typeof(TabPayload).FullName!;
            if (data.GetDataPresent(format))
            {
                payload = (TabPayload?)data.GetData(format)!;
                return payload is not null && payload.Page is not null && payload.Source is not null;
            }

            return false;
        }

        private static int GetTabIndexAt(TabControl tc, Point clientPoint)
        {
            for (int i = 0; i < tc.TabPages.Count; i++)
            {
                if (tc.GetTabRect(i).Contains(clientPoint))
                    return i;
            }
            return -1;
        }

        private static Rectangle GetTabHeaderBounds(TabControl tc)
        {
            if (tc.TabCount > 0)
            {
                var r = tc.GetTabRect(0);
                // Header spans horizontally across all tabs; take top area height
                int headerHeight = r.Height + 2;
                return new Rectangle(0, r.Top, tc.Width, headerHeight);
            }
            // Approximate a standard header height
            return new Rectangle(0, 0, tc.Width, SystemInformation.CaptionHeight);
        }

        private static int GetInsertIndexAt(TabControl tc, Point clientPoint)
        {
            // If over a tab, choose left/right side; if beyond last tab, append
            int closest = -1;
            for (int i = 0; i < tc.TabPages.Count; i++)
            {
                var r = tc.GetTabRect(i);
                if (clientPoint.X < r.Left + r.Width / 2)
                {
                    closest = i;
                    break;
                }
            }
            if (closest == -1) closest = tc.TabPages.Count; // append to end
            return closest;
        }

        private static void MoveTab(TabControl source, TabControl dest, TabPage page, int destIndex)
        {
            if (destIndex < 0) destIndex = 0;
            if (destIndex > dest.TabPages.Count) destIndex = dest.TabPages.Count;

            // If moving within same control, adjust index when removing before insert
            int originalIndex = source.TabPages.IndexOf(page);

            if (source == dest)
            {
                if (originalIndex == -1) return;
                if (destIndex == originalIndex || destIndex == originalIndex + 1)
                {
                    // No move needed
                    return;
                }

                source.SuspendLayout();
                try
                {
                    source.TabPages.RemoveAt(originalIndex);
                    if (destIndex > originalIndex) destIndex--; // list shrank
                    source.TabPages.Insert(destIndex, page);
                }
                finally
                {
                    source.ResumeLayout();
                }
            }
            else
            {
                source.SuspendLayout();
                dest.SuspendLayout();
                try
                {
                    source.TabPages.Remove(page);
                    dest.TabPages.Insert(destIndex, page);
                }
                finally
                {
                    source.ResumeLayout();
                    dest.ResumeLayout();
                }
            }
        }

        private static bool IsOverAnyTabHeader(Point screenPoint)
        {
            foreach (var tc in Registered.ToList())
            {
                if (tc.IsDisposed) continue;
                var header = GetTabHeaderBounds(tc);
                var headerScreen = new Rectangle(tc.PointToScreen(header.Location), header.Size);
                if (headerScreen.Contains(screenPoint)) return true;
            }
            return false;
        }

        private static void TearOffToFloating(TabPage page, Point screenLocation)
        {
            // Create floating window and move page there
            var floating = new FloatingTabsForm
            {
                Location = new Point(screenLocation.X - 40, screenLocation.Y - 20) // slight offset
            };
            floating.Show();

            floating.TabHost.SuspendLayout();
            try
            {
                var source = page.Parent as TabControl;
                source?.TabPages.Remove(page);
                floating.TabHost.TabPages.Add(page);
                floating.TabHost.SelectedTab = page;
            }
            finally
            {
                floating.TabHost.ResumeLayout();
            }
        }

        // ------------- Insertion mark drawing -------------

        private static void DrawInsertMark(TabControl tc, int insertIndex)
        {
            if (!States.TryGetValue(tc, out var state)) return;

            // Erase previous
            ClearInsertMark(tc);

            // Compute target vertical line in screen coordinates
            Rectangle header = GetTabHeaderBounds(tc);
            if (tc.TabPages.Count == 0)
            {
                // Draw at start of header
                var line = tc.RectangleToScreen(new Rectangle(header.Left + 4, header.Top + 2, 2, header.Height - 4));
                DrawReversible(line);
                state.LastInsertMark = line;
                state.LastInsertIndex = 0;
                return;
            }

            int x;
            if (insertIndex <= 0)
            {
                x = tc.GetTabRect(0).Left;
            }
            else if (insertIndex >= tc.TabPages.Count)
            {
                var r = tc.GetTabRect(tc.TabPages.Count - 1);
                x = r.Right;
            }
            else
            {
                var r = tc.GetTabRect(insertIndex);
                x = r.Left;
            }

            var lineRect = new Rectangle(x - 1, header.Top + 2, 3, header.Height - 4);
            var screen = tc.RectangleToScreen(lineRect);
            DrawReversible(screen);
            state.LastInsertMark = screen;
            state.LastInsertIndex = insertIndex;
        }

        private static void ClearInsertMark(TabControl tc)
        {
            if (!States.TryGetValue(tc, out var state)) return;
            if (!state.LastInsertMark.IsEmpty)
            {
                DrawReversible(state.LastInsertMark); // XOR to erase
                state.LastInsertMark = Rectangle.Empty;
                state.LastInsertIndex = -1;
            }
        }

        private static void DrawReversible(Rectangle screenRect)
        {
            ControlPaint.DrawReversibleFrame(screenRect, Color.Black, FrameStyle.Thick);
        }

        // ------------- Dock frame drawing -------------
        private static void ShowDockFrame(TabControl target)
        {
            var client = new Rectangle(Point.Empty, target.ClientSize);
            // Exclude header to avoid double-indication
            var header = GetTabHeaderBounds(target);
            var content = new Rectangle(client.Left + 2, header.Bottom + 2, client.Width - 4, client.Height - header.Bottom - 4);
            if (content.Width < 10 || content.Height < 10)
                content = client; // fallback

            var screen = target.RectangleToScreen(content);
            if (_lastDockFrame == screen) return;

            ClearDockFrame();
            ControlPaint.DrawReversibleFrame(screen, Color.DodgerBlue, FrameStyle.Thick);
            _lastDockFrame = screen;
            _lastDockTarget = target;
        }

        private static void ClearDockFrame()
        {
            if (!_lastDockFrame.IsEmpty)
            {
                ControlPaint.DrawReversibleFrame(_lastDockFrame, Color.DodgerBlue, FrameStyle.Thick);
                _lastDockFrame = Rectangle.Empty;
                _lastDockTarget = null;
            }
        }

        // ------------- Context menu -------------

        private static readonly ContextMenuStrip _sharedMenu = BuildContextMenu();
        private static int _menuTabIndex = -1;

        private static ContextMenuStrip BuildContextMenu()
        {
            var cms = new ContextMenuStrip();

            var miClose = new ToolStripMenuItem("Close", null, (_, __) =>
            {
                if (!TryGetSenderTab(out var tc, out var page)) return;
                tc.TabPages.Remove(page);
            });

            var miCloseOthers = new ToolStripMenuItem("Close Others", null, (_, __) =>
            {
                if (!TryGetSenderTab(out var tc, out var page)) return;
                var keep = page;
                var toRemove = tc.TabPages.Cast<TabPage>().Where(p => p != keep).ToList();
                foreach (var p in toRemove) tc.TabPages.Remove(p);
            });

            var miToNewWindow = new ToolStripMenuItem("Move to New Window", null, (_, __) =>
            {
                if (!TryGetSenderTab(out var tc, out var page)) return;
                TearOffToFloating(page, Control.MousePosition);
            });

            var miToMain = new ToolStripMenuItem("Move to Main Window", null, (_, __) =>
            {
                if (!TryGetSenderTab(out var tc, out var page)) return;
                if (MainHost is null || MainHost.IsDisposed) return;
                MoveTab(tc, MainHost, page, MainHost.TabPages.Count);
                MainHost.SelectedTab = page;
                if (tc != MainHost && tc.TabPages.Count == 0)
                {
                    // Try closing empty floating host (if any)
                    var form = tc.FindForm();
                    Disable(tc);
                    form?.Close();
                }
            });

            cms.Items.AddRange(new ToolStripItem[] { miClose, miCloseOthers, new ToolStripSeparator(), miToNewWindow, miToMain });
            cms.Opening += (_, e) =>
            {
                if (!TryGetSenderTab(out var tc, out var _))
                {
                    e.Cancel = true;
                    return;
                }
                // Enable/disable "Move to Main" depending on current host
                miToMain.Enabled = MainHost != null && tc != MainHost;
            };

            return cms;
        }

        private static void EnsureContextMenu(TabControl tc)
        {
            // Share one menu instance across all TabControls
            tc.ContextMenuStrip = _sharedMenu;
        }

        private static void OnMouseUpForContextMenu(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var tc = (TabControl)sender!;
            _menuTabIndex = GetTabIndexAt(tc, e.Location);
            if (_menuTabIndex >= 0)
            {
                tc.SelectedIndex = _menuTabIndex;
                // Context menu will be shown by WinForms automatically if attached
            }
            else
            {
                // If right-click not on a tab header, suppress menu
                tc.ContextMenuStrip = null;
                tc.ContextMenuStrip = _sharedMenu; // reattach
            }
        }

        private static bool TryGetSenderTab(out TabControl tc, out TabPage page)
        {
            // The ContextMenuStrip.SourceControl is the TabControl
            var cms = _sharedMenu;
            tc = cms.SourceControl as TabControl ?? Registered.FirstOrDefault(t => t.ContextMenuStrip == cms)!;
            page = default!;
            if (tc == null || _menuTabIndex < 0 || _menuTabIndex >= tc.TabPages.Count)
                return false;

            page = tc.TabPages[_menuTabIndex];
            return true;
        }
    }
}