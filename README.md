# K-Clipboard

Two persisted history stacks for things Unity's own clipboard forgets the
moment you copy something else: **Component History** (copy a component's
values, paste them onto a component of the same type on a *different*
GameObject) and **GameObject History** (copy a whole GameObject - including
its children - and paste it back later, in a different session if you like).

One of the Kingfisher K-Tools, built on the shared
[K-Setting](https://github.com/vuthelong/KSetting) backend. This package ships
with its own copy, so installing K-Clipboard on its own still gives you the
combined settings window.

## Features

### Component History

- Right-click any component and choose **Copy to K-Clipboard History** to
  copy its values (via `UnityEditorInternal.ComponentUtility` and
  `EditorJsonUtility`) and push them onto a persisted history stack
- **Tools > Kingfisher > K-Clipboard > Component History** lists the stack,
  most recent first, each entry showing the component type and how long ago
  it was copied
- Hover an entry to slide out its **paste** and **delete** buttons - paste
  applies to every currently selected GameObject, and stays disabled while
  nothing is selected
- Values overwrite a matching component when the GameObject already has one,
  and the component is added first when it does not
- Select an entry to open a resizable preview pane that draws the stored values
  as a real Inspector - edit them in place and press **Save** to write them
  back into the entry
- **Copy** in that pane pushes the entry into Unity's own component clipboard,
  so **Paste Component Values** and **Paste Component As New** pick it up

### GameObject History

- **GameObject > Copy to K-Clipboard History** - also available from the
  Hierarchy's right-click menu - copies the selected GameObject(s), including
  their full hierarchy, via Unity's own internal GameObject copy/paste
  mechanism, and pushes them onto a separate persisted history stack
- **Tools > Kingfisher > K-Clipboard > GameObject History** lists the stack the
  same way, with a **Copy Selection** toolbar button as an alternative to the
  menu item
- Hover an entry to slide out its **paste** and **delete** buttons - paste
  works with or without a GameObject selected, since pasting at the scene root
  is a legitimate target too; placement follows Unity's native Hierarchy-paste
  behavior for whatever is currently selected
- Entries show the copied GameObject's name, or "N GameObjects" for a
  multi-selection copy (hover for the full list of names)
- No live-editable preview pane here - unlike a component's values, a copied
  GameObject's stored data is an opaque blob (see Limitations), so there is
  nothing to decode into an editable Inspector

### Both

- **Pin** an entry to keep it above the rest - pinned entries sort to the top
  in the order you pinned them, and survive both the history cap and
  **Clear history**
- Independently configurable history caps (default 20 entries each)
- **Clear history** button to wipe every unpinned entry

Everything is editor-only - the assembly is `Editor`-platform only, so nothing
here is compiled into player builds.

## Limitations

> [!NOTE]
> **Component History** paste targets **one** component per selected
> GameObject. When a GameObject already has several components of the copied
> type, the values land on the first one in Inspector order and the rest are
> left untouched - there is no way to aim at a specific duplicate. When it has
> none, the component is added first and then filled.
>
> This also means paste never adds a second instance to a GameObject that
> already has one. Use Unity's own **Paste Component As New** when you want
> another copy alongside the existing ones.

> [!NOTE]
> **GameObject History** paste placement (child of the current selection vs.
> scene root, and whether the original local transform is preserved) is
> Unity's own native Hierarchy-paste behavior for whatever is selected at
> paste time - K-Clipboard doesn't control or override it, the same way it
> wouldn't if you pasted manually with Ctrl+V.

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
folder, so it survives updating or re-cloning the repository. Component and
GameObject history are separate files in that folder, so clearing or deleting
one never touches the other.

The folder carries a `.gitignore` of its own that excludes everything inside it,
so it stays out of version control without your project's `.gitignore` needing
an entry. Delete that file to commit the folder instead.

## Settings

**Tools > Kingfisher > K-Clipboard > Setting** opens K-Clipboard's own settings
window, with independent history-cap sliders for Component History and
GameObject History.

Install [K-Setting](https://github.com/vuthelong/KSetting) beside it and you get
**Tools > KTools Setting** instead - one window that every installed Kingfisher
tool folds its settings into. It finds the installed tools by reflection at load
time, so there is nothing to wire up.

K-Setting is optional. Without it, each tool keeps its own window.

## License

Proprietary - see [LICENSE.md](LICENSE.md). Licensed per purchase (Unity Asset
Store or a direct agreement with Kingfisher); it is not open source.
