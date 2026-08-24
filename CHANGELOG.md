# Changelog

All notable changes to K-Clipboard are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

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
