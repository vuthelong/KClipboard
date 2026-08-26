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
  their full hierarchy, and pushes them onto a separate persisted history
  stack. Internally this round-trips through a temporary `.prefab` asset
  (Unity's own hierarchy/reference serializer) rather than Unity's native
  Editor copy/paste command, which has no public way to read back what it
  captured - see Limitations
- **Tools > Kingfisher > K-Clipboard > GameObject History** lists the stack the
  same way, with a **Copy Selection** toolbar button as an alternative to the
  menu item
- Hover an entry to slide out its **paste** and **delete** buttons - paste
  works with or without a GameObject selected: pasted roots land as children
  of the selected GameObject, or at the active scene's root otherwise
- Entries show the copied GameObject's name, or "N GameObjects" for a
  multi-selection copy (hover for the full list of names)
- Select an entry to open a resizable preview pane showing a read-only,
  collapsible tree of the copied hierarchy's names and icons - captured at
  copy time, not decoded from the stored data (see Limitations), so there is
  nothing to edit here
- The preview tree has zebra-striped rows, indent-guide lines connecting each
  row to its parent, a component minimap (small icons per row for every
  component on that GameObject, Transform excluded), and a path bar at the
  bottom showing the hovered row's full path within the copied hierarchy

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
> **GameObject History** copy/paste is implemented via a temporary `.prefab`
> asset, created under `Assets/KClipboardTemp` and deleted again within the
> same Copy or Paste call - not Unity's native Editor GameObject copy/paste
> command. That command (`Unsupported.CopyGameObjectsToPasteboard`) writes to
> an internal buffer with no public API to read the data back out, so it
> can't be persisted into a history stack; the temporary-prefab round-trip is
> the closest fully-public equivalent that preserves the hierarchy and
> internal references between components correctly. One consequence: since
> paste no longer goes through Unity's own paste command, pasted roots always
> land as children of the current selection (or the scene root) rather than
> mimicking whatever placement a manual Ctrl+V would have chosen.
>
> The preview pane shows names and icons only, not component values - it's
> captured by walking the hierarchy at copy time, not decoded from the stored
> prefab data.

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

GameObject History also uses `Assets/KClipboardTemp` as scratch space for a
temporary `.prefab` asset - it's created and deleted again within the same
Copy or Paste action, so it should normally sit empty between operations. It
has to live under `Assets/` (Unity's asset pipeline can't import anything
outside it), so it's a real, VCS-visible folder unlike `.KData` - add it to
your own `.gitignore` if you'd rather not see it show up in diffs.

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
