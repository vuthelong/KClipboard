# Changelog

All notable changes to K-Clipboard are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `CONTEXT/Component > Copy to K-Clipboard History` copies a component's
  values and pushes them onto a persisted history stack, using
  `UnityEditorInternal.ComponentUtility` and `EditorJsonUtility` - unlike
  Unity's native copy/paste, the target GameObject doesn't need to be the
  same one the values were copied from.
- **Tools > Kingfisher > K-Clipboard > History** window listing the history
  stack, most recent first, with a "Paste to Selected" button per entry and a
  "Clear history" button.
- Configurable history cap (default 20 entries).
- Per-tool settings window for when K-Setting is not installed; with it, the
  settings fold into **Tools > KTools Setting** instead.
- Editor-only: the assembly is `Editor`-platform only, so nothing is compiled
  into player builds.
