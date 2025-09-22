# PIC32_M_DEV

PIC32_M_DEV is a Windows desktop workspace for PIC32 firmware development. It focuses on fast project navigation, side‑by‑side project mirroring, a simple code editor, integrated Git operations, and a lightweight console – all in one window.

Built with .NET 8 (WinForms) and hosting AvalonEdit for code editing.

## Key features

- Project tree (Left) and Mirror project tree (Right)
  - Open one or both roots and browse files/folders
  - Copy from the right tree into the left project via context menu
  - Delete files/folders within the right tree (safe‑guarded to root)
- Tabbed code editor (AvalonEdit)
  - Opens files from the tree into tabs
  - Syntax highlighting for C/C headers
  - Per‑tab context menu: Save, Save As, Close Tab
- Integrated Git tab
  - Branch list, Checkout, New Branch
  - Status list (Untracked/Modified/Staged/etc.)
  - Stage/Unstage selected items
  - Commit (with message and author info dialog)
  - Fetch/Pull/Push (via `git` on PATH); local ops via LibGit2Sharp
- Built‑in console panel
  - Toggle open/closed and run simple commands
  - Intended for quick project actions (e.g., make/build scripts)
- Project bootstrap
  - Create a new project folder with a Makefile layout and starter files
- Configurable hotkeys
  - Change, unbind, or reset defaults from the Hotkeys dialog

## Status and scope

- Code generation: The menu item exists but generation is not implemented in this branch.
- Target: Windows 10+ (.NET 8). Tested with Visual Studio 2022.

## Prerequisites

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or later (recommended)
- Git installed and on PATH (for Fetch/Pull/Push)

## Build

1. Clone the repository:
   ```
   git clone https://github.com/Davec6505/PIC32_M_DEV.git
   ```
2. Open `PIC32Mn_PROJ.sln` in Visual Studio 2022.
3. Restore NuGet packages when prompted.
4. Build the solution.

## Run

- Start the app from Visual Studio (F5).
- On first launch, use the `File` menu to set the Left project (`Open`) and optional Right mirror project (`Open Right`).

## Using the app
- Dependancies
  - .NET 8 runtime (installed with SDK or separately)
  - Git (for network operations)
  - XC32 toolchain (for building projects; not integrated)
  - MPLAB X IDE (optional, for advanced project management)
  - MCC-Standalone (optional, for code generation)
- Projects
  - `File -> Open`: choose the left project root
  - `File -> Open Right`: choose a right/mirror root (optional)
	- mirror tree is for browsing and copying files into the left project mainly to copy Harmony based mcc outputs
	  to the left project, built from MPLAB X IDE / MCC-Standalone.
	  The main purpose is to expose Harmony projects to AI assisted code generation in VS Code or similar.
  - Right‑click in either tree for context actions (delete, copy, etc.)
- Editing
  - Select a file in the tree to open it in a new editor tab
  - `Edit -> New -> source .c` or `header .h` to start a new file
  - Save/Save As from the editor tab menu or from the `File` menu
- Git
  - Open the Git tab from the `Git` menu (or hotkey)
  - Pick a branch, Checkout/New Branch, and manage status (Stage/Unstage)
  - Enter a commit message and Commit; Fetch/Pull/Push available from toolbar and menu
- Console
  - `Options -> Open Console` to show the bottom console pane
  - `Options -> Close Console` to hide it
- Hotkeys
  - `Options -> Hotkeys...` to view and change bindings
  - Double‑click a hotkey to assign a new combination; use Clear to unbind

## Default hotkeys

- Save: Ctrl+S
- Toggle Console: Ctrl+' (OemQuotes)
- Open Git Tab: Ctrl+G
- Close Git Tab: Ctrl+Shift+G
- Stage Selected: Ctrl+Shift+S
- Commit: Ctrl+Enter
- Fetch: Ctrl+F5
- Pull: Ctrl+Shift+P
- Push: Ctrl+Shift+U

Hotkeys are applied globally in the app and can be changed at any time from `Options -> Hotkeys...`.

## Notes

- Git author info is requested during commit via a small dialog.
- Network Git operations (Fetch/Pull/Push) are executed through the system `git` to use existing credentials; local repo operations use LibGit2Sharp.
- Settings such as last used project paths and hotkeys are persisted between runs.

## License

MIT. See `LICENSE`.

## Credits

- ICSharpCode.AvalonEdit — code editor control
- LibGit2Sharp — local Git operations
- Windows API Code Pack — modern folder picker dialogs
