# Changelog

All notable changes to K-Clipboard are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `GameObject > Copy to K-Clipboard History` (also available from the
  Hierarchy right-click menu) copies the selected GameObject(s) - including
  their full hierarchy - and pushes them onto a new, separate persisted
  history stack. Implemented via a temporary `.prefab` asset under
  `Assets/KClipboardTemp` (created and deleted within the same call) rather
  than Unity's native Editor copy/paste command, which has no public API to
  read back what it captures and so can't be persisted.
- **Tools > Kingfisher > K-Clipboard > GameObject History** window, mirroring
  the Component history's pin/paste/delete UX, plus a **Copy Selection**
  toolbar button. Paste works with or without a GameObject selected - pasted
  roots land as children of the current selection, or at the scene root
  otherwise.
- A resizable preview pane for GameObject entries showing a read-only,
  collapsible tree of the copied hierarchy's names and icons, captured by
  walking the hierarchy at copy time rather than decoding the stored data.
- The preview tree has zebra-striped rows, indent-guide lines connecting each
  row to its parent (dimensions and color matched directly against
  K-Hierarchy's own `DrawHierarchyLines`, not approximated), a component
  minimap (per-row component icons, Transform excluded), and a path bar
  showing the hovered row's full path within the copied hierarchy.
- A second, independent history-cap setting for the GameObject history.

### Fixed

- The GameObject history's row-count caches could desync from the entry list
  after a domain reload (a private, non-serialized version counter happened
  to reset to the same value on both sides), causing an
  `IndexOutOfRangeException` the next time the window repainted. Change
  detection now also checks the cache length directly.
- Every preview tree node now always shows a GameObject icon before its name,
  falling back to the generic GameObject icon instead of leaving it blank -
  and icon names are now resolved via `EditorGUIUtility.IconContent` instead
  of `EditorGUIUtility.FindTexture`, since names captured through
  `ObjectContent` (the API used to derive them) are not reliably resolvable
  through `FindTexture`, a different and older icon lookup API.
- GameObject copy silently produced no data (or pasted whatever Unity's
  internal pasteboard happened to still hold from the same session, not
  necessarily the requested entry) - `EditorGUIUtility.systemCopyBuffer` is
  the OS text clipboard, unrelated to the buffer
  `Unsupported.CopyGameObjectsToPasteboard` actually writes to. See Added.

### Changed

- K-Clipboard is now an umbrella for two sibling history stacks, so the
  Component-specific classes/files were renamed: `KClipboard` ->
  `KClipboardComponents`, `KClipboardData` -> `KClipboardComponentsData`,
  `KClipboardWindow` -> `KClipboardComponentsWindow`.
- Renamed the Component history menu item from
  `Tools/Kingfisher/K-Clipboard/History` to
  `Tools/Kingfisher/K-Clipboard/Component History`.
- Renamed the `MaxHistoryCount` setting to `MaxComponentHistoryCount`
  (including its backing key) - resets to the default of 20 for anyone who had
  already changed it on a pre-release build.
- Renamed the Component history's data file on disk to
  `KClipboard Components Data.asset`; a stale `.KData/KClipboard Data.asset`
  from a pre-release install is orphaned, not migrated.
- Moved the Settings menu item from the top-level `Tools/Kingfisher/KClipboard
  Setting` into `Tools/Kingfisher/K-Clipboard/Setting`, nested alongside
  History under the tool's own submenu.

## [0.1.0] - 2026-08-24

### Added

- `CONTEXT/Component > Copy to K-Clipboard History` copies a component's
  values and pushes them onto a persisted history stack, using
  `UnityEditorInternal.ComponentUtility` and `EditorJsonUtility` - unlike
  Unity's native copy/paste, the target GameObject doesn't need to be the
  same one the values were copied from.
- **Tools > Kingfisher > K-Clipboard > History** window listing the stack,
  most recent first, each entry showing the component's icon, its type and
  how long ago it was copied.
- Hovering an entry slides out its paste and delete buttons. Paste applies to
  every selected GameObject as a single undo step, and stays disabled while
  nothing is selected.
- Pasting overwrites a matching component when the GameObject already has one,
  and adds the component first when it does not.
- Pinning, which holds an entry above the rest in the order it was pinned and
  exempts it from both the history cap and **Clear history**.
- A resizable preview pane that draws the selected entry's stored values as a
  real Inspector - edit them in place, and **Save** writes them back into the
  entry.
- **Copy** in that pane pushes the entry into Unity's own component clipboard,
  so **Paste Component Values** and **Paste Component As New** pick it up.
- Configurable history cap (default 20 entries), and a **Clear history** button
  that wipes every unpinned entry.
- Per-tool settings window for when K-Setting is not installed; with it, the
  settings fold into **Tools > KTools Setting** instead.
- Editor-only: the assembly is `Editor`-platform only, so nothing is compiled
  into player builds.
