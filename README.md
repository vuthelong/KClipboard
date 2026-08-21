# K-Clipboard

Copy a component's values and paste them onto a component of the same type on
a *different* GameObject. Unity's native right-click copy/paste already does
this, but only for one component at a time - K-Clipboard adds a persisted
history stack on top, so you can reach back further than the last thing you
copied.

One of the Kingfisher K-Tools, built on the shared
[K-Setting](https://github.com/vuthelong/KSetting) backend. This package ships
with its own copy, so installing K-Clipboard on its own still gives you the
combined settings window.

## Features

- Right-click any component and choose **Copy to K-Clipboard History** to
  copy its values (via `UnityEditorInternal.ComponentUtility` and
  `EditorJsonUtility`) and push them onto a persisted history stack
- **Tools > Kingfisher > K-Clipboard > History** lists the stack, most recent
  first, with a **Paste to Selected** button per entry
- Paste applies to every currently selected GameObject - values overwrite a
  matching component when the GameObject already has one, and the component is
  added first when it does not
- Configurable history cap (default 20 entries)
- **Clear history** button to wipe the stack

Everything is editor-only - the assembly is `Editor`-platform only, so nothing
here is compiled into player builds.

## Install

Two channels. Take whichever suits - the package is the same either way.

### Package Manager (git URL)

Package Manager > **+** > **Install package from git URL...**, then paste
K-Clipboard's URL:

```
https://github.com/vuthelong/KClipboard.git
```

This tracks the default branch, so Package Manager's **Update** button pulls
new commits as they land. Unity keeps packages read-only in
`Library/PackageCache`.

> [!TIP]
> Install [K-Setting](https://github.com/vuthelong/KSetting) alongside it to
> fold K-Clipboard's settings into **Tools > KTools Setting** instead of
> opening its own window:
>
> ```
> https://github.com/vuthelong/KSetting.git
> ```

#### Pin a version (optional)

Append `#<version>` to either URL above to install a specific tag instead of
tracking the branch:

```
https://github.com/vuthelong/KSetting.git#1.0.4
https://github.com/vuthelong/KClipboard.git#0.1.0
```

A tag is a fixed point, so Update has nothing new to fetch while pinned to
one - move to a newer tag by repeating this step with the new `#<version>`.

### `.unitypackage`

Download the `.unitypackage` from the
[latest release](https://github.com/vuthelong/KClipboard/releases/latest) and
import it via **Assets > Import Package > Custom Package**. This is a
point-in-time snapshot, not a tracked install - re-download it to update.

Keep one copy per project, whichever channel you use - Unity rejects a second
with `Assembly with name 'Kingfisher.KClipboard' already exists`.

## Where your data lives

Your copy history and settings are written to a `.KData` folder at the root
of your project, next to `Assets/` and `Packages/` - not into the tool's
folder, so it survives updating or re-cloning the repository.

The folder carries a `.gitignore` of its own that excludes everything inside it,
so it stays out of version control without your project's `.gitignore` needing
an entry. Delete that file to commit the folder instead.

## Settings

**Tools > Kingfisher > KClipboard Setting** opens K-Clipboard's own settings
window.

Install [K-Setting](https://github.com/vuthelong/KSetting) beside it and you get
**Tools > KTools Setting** instead - one window that every installed Kingfisher
tool folds its settings into. It finds the installed tools by reflection at load
time, so there is nothing to wire up.

K-Setting is optional. Without it, each tool keeps its own window.

## License

Proprietary - see [LICENSE.md](LICENSE.md). Licensed per purchase (Unity Asset
Store or a direct agreement with Kingfisher); it is not open source.
