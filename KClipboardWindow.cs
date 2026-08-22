#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static Kingfisher.KClipboard.Libs.KUtils;
using static Kingfisher.KClipboard.Libs.KGUI;

namespace Kingfisher.KClipboard
{
    public class KClipboardWindow : EditorWindow
    {
        #region Field

        private const string MenuPath = "Tools/Kingfisher/K-Clipboard/History";
        private const string WindowTitle = "K-Clipboard History";
        private const int MenuPriority = 912;

        private const string EmptyTitle = "Nothing here";
        private const string EmptyBody = "You will see your copied components here once you have copied something.";
        private const string NoSelectionHint = "Select GameObjects to paste into.";
        private const string PreviewUnavailable = "This component type is no longer available in the project.";
        private const string PreviewHeaderLabel = "Preview";

        private const string PasteFailureLogFormat = "K-Clipboard: {0}";

        private const string PreviewHostName = "KClipboard Preview";
        private const HideFlags PreviewHostFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

        private const string SelectedRowStyleName = "OL SelectedRow";
        private const string TitlebarStyleName = "IN Title";

        private const string PinnedIconName = "pinned";
        private const string UnpinnedIconName = "pin";
        private const string PasteIconName = "Clipboard";
        private const string DeleteIconName = "TreeEditor.Trash";

        private const string PinnedTooltip = "Pinned - kept when the history is trimmed or cleared";
        private const string UnpinnedTooltip = "Pin so this entry survives trimming and Clear history";
        private const string PasteTooltip = "Paste these values onto the selected GameObjects";
        private const string DeleteTooltip = "Remove from history";

        private const string JustNowLabel = "just now";
        private const string MinutesAgoFormat = "{0}m ago";
        private const string HoursAgoFormat = "{0}h ago";
        private const string DaysAgoFormat = "{0}d ago";

        private const int NoIndex = -1;
        private const int LeftMouseButton = 0;
        private const int ActionButtonCount = 2;

        private const double TimeLabelRefreshInterval = 1d;

        private const float ActionsLerpSpeed = 12f;
        private const float ActionsSnapAmount = .005f;
        private const float MaxDeltaTime = .05f;
        private const float FallbackDeltaTime = .0166f;

        private const float MinWindowWidth = 360f;
        private const float MinWindowHeight = 240f;
        private const float Padding = 4f;
        private const float RowPadding = 8f;
        private const float TitleTimeGap = 2f;

        private const float ActionGap = 4f;
        private const float ActionButtonGap = 2f;
        private const float PinIconSize = 16f;
        private const float RowIconSize = 16f;
        private const float PinButtonSize = 26f;
        private const int PinIconPadding = (int)((PinButtonSize - PinIconSize) * .5f);
        private const float EmptyTextWidth = 260f;
        private const float EmptyTitleGap = 2f;
        private const float UnpinnedIconAlpha = .5f;
        private const float LabelIndent = PinButtonSize + ActionGap;

        private const float SaveButtonWidth = 48f;
        private const float CopyButtonWidth = 48f;
        private const int PreviewPadding = 6;
        private const float PreviewLabelWidthRatio = .4f;
        private const float MinPreviewLabelWidth = 110f;
        private const float DividerThickness = 1f;
        private const float ListBottomPadding = 50f;
        private const float MinPreviewHeaderHeight = 22f;
        private const float PreviewIconSize = 16f;
        private const float DefaultPreviewHeight = 200f;
        private const float MinPreviewHeight = 60f;
        private const float MinListHeight = 60f;

        private static readonly float RowHeight = RowPadding * 2f + EditorGUIUtility.singleLineHeight * 2f + TitleTimeGap;
        private static readonly float ActionButtonSize = RowHeight;

        private static readonly Color DividerColorDark = Greyscale(.13f);
        private static readonly Color DividerColorLight = Greyscale(.6f);
        private static readonly Color RowEvenColorDark = Greyscale(.249f);
        private static readonly Color RowEvenColorLight = Greyscale(.82f);
        private static readonly Color RowOddColorDark = Greyscale(.228f);
        private static readonly Color RowOddColorLight = Greyscale(.85f);
        private static readonly Color PinnedIconColor = Greyscale(1f);
        private static readonly Color UnpinnedIconColor = Greyscale(1f, UnpinnedIconAlpha);
        private static readonly float ActionsWidth = (ActionButtonSize + ActionButtonGap) * ActionButtonCount;

        private static readonly GUIContent NoSelectionHintContent = new(NoSelectionHint);
        private static readonly GUIContent EmptyTitleContent = new(EmptyTitle);
        private static readonly GUIContent EmptyBodyContent = new(EmptyBody);
        private static readonly GUIContent PreviewUnavailableContent = new(PreviewUnavailable);
        private static readonly GUIContent PreviewHeaderContent = new(PreviewHeaderLabel);
        private static readonly GUIContent ClearButtonContent = new("Clear history");
        private static readonly GUIContent SaveButtonContent = new("Save", "Write these values back to the history entry");
        private static readonly GUIContent CopyButtonContent = new("Copy", "Copy these values to Unity's component clipboard");

        private static readonly GUILayoutOption[] ExpandWidthOptions = { GUILayout.ExpandWidth(true) };
        private static readonly Dictionary<string, Texture> IconsByKey = new();

        private static GUIStyle _selectedRowStyle;
        private static float _previewHeaderHeight;
        private static GUIStyle _actionButtonStyle;
        private static GUIStyle _emptyTitleStyle;
        private static GUIStyle _emptyBodyStyle;
        private static GUIStyle _previewBodyStyle;
        private static GUIStyle _iconButtonStyle;
        private static GUIContent _pinnedIconContent;
        private static GUIContent _unpinnedIconContent;
        private static GUIContent _pasteIconContent;
        private static GUIContent _deleteIconContent;
        private static bool _hasBuiltStyles;
        private static bool _isStyleDark;

        [SerializeField] private float previewHeight = DefaultPreviewHeight;

        private readonly List<string> _timeLabels = new();

        private KClipboardData.HistoryEntry _selectedEntry;
        private KClipboardData.HistoryEntry _pendingSelection;
        private KClipboardData.HistoryEntry _previewEntry;
        private GameObject _previewHost;
        private Component _previewComponent;
        private Editor _previewEditor;
        private GUIContent _previewTitleContent;
        private Vector2 _scroll;
        private Vector2 _previewScroll;
        private int _hoveredIndex = NoIndex;
        private int _animatedActionsIndex = NoIndex;
        private int _pendingRemovalIndex = NoIndex;
        private int _pendingPinIndex = NoIndex;
        private int _timeLabelsVersion = NoIndex;
        private float _actionsAmount;
        private float _deltaTime;
        private double _lastLayoutTime;
        private double _nextTimeLabelRefresh;
        private bool _hasPendingSelection;
        private bool _hasPreviewEdits;
        private bool _isResizingPreview;
        private bool _isMouseOverList;
        private bool _hasSelection;

        #endregion

        #region Property

        private static List<KClipboardData.HistoryEntry> Entries => KClipboard.EnsureData().entries;

        private static Color DividerColor => IsDarkTheme ? DividerColorDark : DividerColorLight;

        private static Color RowEvenColor => IsDarkTheme ? RowEvenColorDark : RowEvenColorLight;

        private static Color RowOddColor => IsDarkTheme ? RowOddColorDark : RowOddColorLight;

        private bool HasPreview => this._selectedEntry != null;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            KClipboard.EnsureData();

            wantsMouseMove = true;

            this._hasSelection = Selection.gameObjects.Length > 0;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            DestroyPreview();
        }

        private void OnGUI()
        {
            BuildStyles();
            RefreshTimeLabels();
            UpdateActionsAnimation();
            EnsurePreview();

            DrawToolbar();

            var toolbarHeight = EditorStyles.toolbar.fixedHeight;

            DrawBody(new Rect(0f, toolbarHeight, position.width, position.height - toolbarHeight));

            ApplyPendingSelection();
            ApplyPendingPin();
            ApplyPendingRemoval();
            RepaintOnHoverChange();
        }

        private void OnSelectionChange()
        {
            this._hasSelection = Selection.gameObjects.Length > 0;

            Repaint();
        }

        #endregion

        #region Drawing

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawSelectionHint();

            GUILayout.FlexibleSpace();

            SetGUIEnabled(KClipboard.EnsureData().HasUnpinned());

            if (GUILayout.Button(ClearButtonContent, EditorStyles.toolbarButton))
            {
                KClipboard.ClearHistory();

                ValidateSelection();
            }

            ResetGUIEnabled();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectionHint()
        {
            if (this._hasSelection) return;
            if (Entries.Count == 0) return;

            SetGUIEnabled(false);

            GUILayout.Label(NoSelectionHintContent, EditorStyles.miniLabel);

            ResetGUIEnabled();
        }

        private void DrawBody(Rect rect)
        {
            var paneHeight = GetPreviewHeight(rect.height);
            var listRect = new Rect(rect.x, rect.y, rect.width, rect.height - paneHeight);

            this._isMouseOverList = listRect.IsHovered();

            DrawList(listRect);

            if (paneHeight <= 0f) return;

            DrawPreview(new Rect(rect.x, rect.yMax - paneHeight, rect.width, paneHeight));
        }

        private float GetPreviewHeight(float bodyHeight)
        {
            if (!HasPreview) return 0f;

            var maxHeight = bodyHeight - MinListHeight;

            if (maxHeight < MinPreviewHeight) return 0f;

            return Mathf.Clamp(this.previewHeight, MinPreviewHeight, maxHeight);
        }

        private void DrawList(Rect rect)
        {
            GUILayout.BeginArea(rect);

            if (Entries.Count == 0)
                DrawEmptyMessage(new Rect(0f, 0f, rect.width, rect.height));
            else
                DrawScrolledRows();

            GUILayout.EndArea();
        }

        private void DrawScrolledRows()
        {
            this._scroll = EditorGUILayout.BeginScrollView(this._scroll, GUIStyle.none, GUIStyle.none);

            DrawRows();

            GUILayout.Space(ListBottomPadding);

            EditorGUILayout.EndScrollView();
        }

        private static void DrawEmptyMessage(Rect rect)
        {
            var width = Mathf.Min(rect.width - Padding * 2f, EmptyTextWidth);

            if (width <= 0f) return;

            var titleHeight = EditorGUIUtility.singleLineHeight;
            var bodyHeight = _emptyBodyStyle.CalcHeight(EmptyBodyContent, width);
            var x = rect.x + (rect.width - width) * .5f;
            var y = rect.y + (rect.height - titleHeight - EmptyTitleGap - bodyHeight) * .5f;

            GUI.Label(new Rect(x, y, width, titleHeight), EmptyTitleContent, _emptyTitleStyle);

            SetGUIEnabled(false);

            GUI.Label(new Rect(x, y + titleHeight + EmptyTitleGap, width, bodyHeight), EmptyBodyContent, _emptyBodyStyle);

            ResetGUIEnabled();
        }

        private void DrawRows()
        {
            if (CurEvent.IsRepaint)
                this._hoveredIndex = NoIndex;

            var entries = Entries;

            for (var i = 0; i < entries.Count; i++)
                DrawRow(entries[i], this._timeLabels[i], i);
        }

        private void DrawRow(KClipboardData.HistoryEntry entry, string timeLabel, int index)
        {
            var rowRect = GUILayoutUtility.GetRect(0f, RowHeight, ExpandWidthOptions);
            var actionsAmount = index == this._animatedActionsIndex ? this._actionsAmount : 0f;

            DrawRowBackground(rowRect, index, entry == this._selectedEntry, actionsAmount);

            if (CurEvent.IsRepaint && this._isMouseOverList && rowRect.IsHovered())
                this._hoveredIndex = index;

            var contentRect = new Rect(rowRect.x + Padding, rowRect.y + RowPadding, rowRect.width - Padding * 2f, rowRect.height - RowPadding * 2f);

            contentRect.width -= ActionsWidth * actionsAmount;

            DrawEntry(contentRect, entry, timeLabel, index);
            DrawActionButtons(rowRect, entry, index, actionsAmount);
            HandleRowClick(rowRect, entry);
        }

        private static void DrawRowBackground(Rect rowRect, int index, bool isSelected, float actionsAmount)
        {
            if (!CurEvent.IsRepaint) return;

            rowRect.Draw(index % 2 == 0 ? RowEvenColor : RowOddColor);

            if (!isSelected) return;

            var highlightRect = rowRect;

            highlightRect.width -= (ActionsWidth + ActionButtonGap) * actionsAmount;

            if (highlightRect.width <= 0f) return;

            _selectedRowStyle?.Draw(highlightRect, false, false, true, true);
        }

        private void DrawEntry(Rect contentRect, KClipboardData.HistoryEntry entry, string timeLabel, int index)
        {
            DrawPinButton(new Rect(contentRect.x, contentRect.y + (contentRect.height - PinButtonSize) * .5f, PinButtonSize, PinButtonSize), entry, index);

            var iconX = contentRect.x + LabelIndent;
            var labelX = iconX + RowIconSize + ActionGap;
            var labelWidth = Mathf.Max(contentRect.xMax - labelX, 0f);
            var timeWidth = Mathf.Max(contentRect.xMax - iconX, 0f);

            DrawRowIcon(new Rect(iconX, contentRect.y + (EditorGUIUtility.singleLineHeight - RowIconSize) * .5f, RowIconSize, RowIconSize), entry);

            GUI.Label(new Rect(labelX, contentRect.y, labelWidth, EditorGUIUtility.singleLineHeight), entry.componentTypeLabel);

            DrawTimeLabel(new Rect(iconX, contentRect.yMax - EditorGUIUtility.singleLineHeight, timeWidth, EditorGUIUtility.singleLineHeight), timeLabel);
        }

        private static void DrawRowIcon(Rect rect, KClipboardData.HistoryEntry entry)
        {
            var icon = GetComponentIcon(entry);

            if (icon == null) return;

            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
        }

        private static Texture GetComponentIcon(KClipboardData.HistoryEntry entry)
        {
            var key = string.IsNullOrEmpty(entry.iconName) ? entry.componentTypeName : entry.iconName;

            if (string.IsNullOrEmpty(key)) return null;
            if (IconsByKey.TryGetValue(key, out var cached)) return cached;

            return IconsByKey[key] = LoadComponentIcon(entry);
        }

        private static Texture LoadComponentIcon(KClipboardData.HistoryEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.iconName))
            {
                var namedIcon = EditorGUIUtility.FindTexture(entry.iconName);

                if (namedIcon != null) return namedIcon;
            }

            if (string.IsNullOrEmpty(entry.componentTypeName)) return null;

            var componentType = Type.GetType(entry.componentTypeName);

            return componentType == null ? null : EditorGUIUtility.ObjectContent(null, componentType).image;
        }

        private void DrawPinButton(Rect rect, KClipboardData.HistoryEntry entry, int index)
        {
            SetGUIColor(entry.pinned ? PinnedIconColor : UnpinnedIconColor);

            var wasClicked = GUI.Button(rect, entry.pinned ? _pinnedIconContent : _unpinnedIconContent, _iconButtonStyle);

            ResetGUIColor();

            if (!wasClicked) return;

            this._pendingPinIndex = index;
        }

        private static void DrawTimeLabel(Rect rect, string timeLabel)
        {
            SetGUIEnabled(false);

            GUI.Label(rect, timeLabel, EditorStyles.miniLabel);

            ResetGUIEnabled();
        }

        private void DrawActionButtons(Rect rowRect, KClipboardData.HistoryEntry entry, int index, float amount)
        {
            var slideOffset = ActionsWidth * (1f - amount);
            var deleteRect = new Rect(rowRect.xMax - ActionButtonGap - ActionButtonSize + slideOffset, rowRect.y, ActionButtonSize, ActionButtonSize);
            var pasteRect = new Rect(deleteRect.x - ActionButtonGap - ActionButtonSize, rowRect.y, ActionButtonSize, ActionButtonSize);

            SetGUIEnabled(this._hasSelection);

            var wasPasteClicked = GUI.Button(pasteRect, _pasteIconContent, _actionButtonStyle);

            ResetGUIEnabled();

            if (wasPasteClicked)
                PasteEntry(entry);

            if (!GUI.Button(deleteRect, _deleteIconContent, _actionButtonStyle)) return;

            this._pendingRemovalIndex = index;
        }

        private void HandleRowClick(Rect rowRect, KClipboardData.HistoryEntry entry)
        {
            if (!this._isMouseOverList) return;
            if (!CurEvent.IsMouseDown) return;
            if (CurEvent.MouseButton != LeftMouseButton) return;
            if (!rowRect.IsHovered()) return;

            CurEvent.Use();

            this._pendingSelection = this._selectedEntry == entry ? null : entry;
            this._hasPendingSelection = true;
        }

        #endregion

        #region Preview

        private void DrawPreview(Rect rect)
        {
            var headerRect = new Rect(rect.x, rect.y + DividerThickness, rect.width, _previewHeaderHeight);
            var saveRect = new Rect(headerRect.xMax - CopyButtonWidth - SaveButtonWidth, headerRect.y, SaveButtonWidth, headerRect.height);
            var copyRect = new Rect(saveRect.xMax, headerRect.y, CopyButtonWidth, headerRect.height);

            new Rect(rect.x, rect.y, rect.width, DividerThickness).Draw(DividerColor);

            DrawPreviewHeader(headerRect, saveRect, copyRect);
            HandlePreviewResize(new Rect(headerRect.x, headerRect.y, saveRect.x - headerRect.x, headerRect.height));

            GUILayout.BeginArea(new Rect(rect.x, headerRect.yMax, rect.width, rect.yMax - headerRect.yMax));

            this._previewScroll = EditorGUILayout.BeginScrollView(this._previewScroll, GUIStyle.none, GUIStyle.none);

            DrawPreviewBody();

            EditorGUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawPreviewHeader(Rect rect, Rect saveRect, Rect copyRect)
        {
            if (CurEvent.IsRepaint)
                EditorStyles.toolbar.Draw(rect, false, false, false, false);

            var headerWidth = PreviewHeaderLabel.GetLabelWidth();
            var titleX = rect.x + Padding + headerWidth + ActionGap;

            SetGUIEnabled(false);

            GUI.Label(new Rect(rect.x + Padding, rect.y, headerWidth, rect.height), PreviewHeaderContent, EditorStyles.miniLabel);

            ResetGUIEnabled();

            DrawPreviewTitle(new Rect(titleX, rect.y, saveRect.x - Padding - titleX, rect.height));

            DrawSaveButton(saveRect);
            DrawCopyButton(copyRect);

            EditorGUIUtility.AddCursorRect(new Rect(rect.x, rect.y, saveRect.x - rect.x, rect.height), MouseCursor.ResizeVertical);
        }

        private void DrawCopyButton(Rect rect)
        {
            SetGUIEnabled(this._previewComponent != null);

            var wasClicked = GUI.Button(rect, CopyButtonContent, EditorStyles.toolbarButton);

            ResetGUIEnabled();

            if (!wasClicked) return;

            ComponentUtility.CopyComponent(this._previewComponent);
        }

        private void DrawPreviewTitle(Rect rect)
        {
            if (rect.width <= 0f) return;
            if (this._previewTitleContent == null) return;

            var labelX = rect.x;

            if (this._previewTitleContent.image != null)
            {
                GUI.DrawTexture(new Rect(rect.x, rect.y + (rect.height - PreviewIconSize) * .5f, PreviewIconSize, PreviewIconSize), this._previewTitleContent.image, ScaleMode.ScaleToFit);

                labelX += PreviewIconSize + ActionGap;
            }

            if (labelX >= rect.xMax) return;

            GUI.Label(new Rect(labelX, rect.y, rect.xMax - labelX, rect.height), this._previewTitleContent.text);
        }

        private void DrawSaveButton(Rect rect)
        {
            SetGUIEnabled(this._hasPreviewEdits);

            var wasClicked = GUI.Button(rect, SaveButtonContent, EditorStyles.toolbarButton);

            ResetGUIEnabled();

            if (!wasClicked) return;

            GUIUtility.keyboardControl = 0;

            KClipboard.UpdateEntry(this._previewEntry, this._previewComponent);

            RefreshPreviewTitle();

            this._hasPreviewEdits = false;
        }

        private void DrawPreviewBody()
        {
            if (!this._previewEditor)
            {
                GUILayout.Label(PreviewUnavailableContent, _emptyBodyStyle);

                return;
            }

            var previousLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = Mathf.Max(position.width * PreviewLabelWidthRatio, MinPreviewLabelWidth);

            EditorGUILayout.BeginVertical(_previewBodyStyle);

            EditorGUI.BeginChangeCheck();

            this._previewEditor.OnInspectorGUI();

            if (EditorGUI.EndChangeCheck())
                this._hasPreviewEdits = true;

            EditorGUILayout.EndVertical();

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        private void HandlePreviewResize(Rect headerRect)
        {
            if (CurEvent.IsMouseUp)
                this._isResizingPreview = false;

            if (CurEvent.IsMouseDown && CurEvent.MouseButton == LeftMouseButton && headerRect.IsHovered())
            {
                this._isResizingPreview = true;

                CurEvent.Use();
            }

            if (!this._isResizingPreview) return;
            if (!CurEvent.IsMouseDrag) return;

            var maxHeight = Mathf.Max(MinPreviewHeight, position.height - MinListHeight);

            this.previewHeight = Mathf.Clamp(this.previewHeight - CurEvent.MouseDelta.y, MinPreviewHeight, maxHeight);

            CurEvent.Use();

            Repaint();
        }

        private void EnsurePreview()
        {
            if (this._previewEntry == this._selectedEntry) return;

            DestroyPreview();

            this._previewEntry = this._selectedEntry;

            if (this._previewEntry == null) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (string.IsNullOrEmpty(this._previewEntry.componentTypeName)) return;

            var componentType = Type.GetType(this._previewEntry.componentTypeName);

            if (componentType == null || componentType.IsAbstract || !typeof(Component).IsAssignableFrom(componentType)) return;

            this._previewHost = EditorUtility.CreateGameObjectWithHideFlags(PreviewHostName, PreviewHostFlags);
            this._previewComponent = this._previewHost.GetComponent(componentType);

            if (this._previewComponent == null)
                this._previewComponent = this._previewHost.AddComponent(componentType);

            if (this._previewComponent == null)
            {
                DestroyPreview();

                return;
            }

            this._previewComponent.hideFlags = PreviewHostFlags;

            EditorJsonUtility.FromJsonOverwrite(this._previewEntry.json, this._previewComponent);

            this._previewEditor = Editor.CreateEditor(this._previewComponent);

            RefreshPreviewTitle();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode && change != PlayModeStateChange.EnteredEditMode) return;

            DestroyPreview();
        }

        private void RefreshPreviewTitle()
        {
            if (this._previewEntry == null) return;

            this._previewTitleContent = new GUIContent(this._previewEntry.componentTypeLabel, GetComponentIcon(this._previewEntry));
        }

        private void DestroyPreview()
        {
            if (this._previewEditor)
                DestroyImmediate(this._previewEditor);

            if (this._previewHost)
                DestroyImmediate(this._previewHost);

            this._previewTitleContent = null;
            this._previewEditor = null;
            this._previewComponent = null;
            this._previewHost = null;
            this._previewEntry = null;
            this._hasPreviewEdits = false;
        }

        #endregion

        #region Entry Action

        private static void PasteEntry(KClipboardData.HistoryEntry entry)
        {
            if (KClipboard.TryPasteToSelected(entry, out var message)) return;

            Debug.LogError(string.Format(PasteFailureLogFormat, message));
        }

        private void ApplyPendingSelection()
        {
            if (!this._hasPendingSelection) return;

            this._selectedEntry = this._pendingSelection;
            this._pendingSelection = null;
            this._hasPendingSelection = false;

            Repaint();
        }

        private void ApplyPendingPin()
        {
            if (this._pendingPinIndex == NoIndex) return;

            KClipboard.TogglePinned(this._pendingPinIndex);

            this._pendingPinIndex = NoIndex;

            Repaint();
        }

        private void ApplyPendingRemoval()
        {
            if (this._pendingRemovalIndex == NoIndex) return;

            KClipboard.RemoveEntry(this._pendingRemovalIndex);

            this._pendingRemovalIndex = NoIndex;

            ValidateSelection();

            Repaint();
        }

        private void ValidateSelection()
        {
            if (this._selectedEntry == null) return;
            if (Entries.Contains(this._selectedEntry)) return;

            this._selectedEntry = null;
        }

        #endregion

        #region Animation

        private void UpdateActionsAnimation()
        {
            if (!CurEvent.IsLayout) return;

            UpdateDeltaTime();

            if (this._hoveredIndex != NoIndex && this._hoveredIndex != this._animatedActionsIndex)
            {
                this._animatedActionsIndex = this._hoveredIndex;
                this._actionsAmount = 0f;
            }

            if (this._animatedActionsIndex == NoIndex) return;

            var target = this._animatedActionsIndex == this._hoveredIndex ? 1f : 0f;

            if (this._actionsAmount == target)
            {
                if (target == 0f)
                    this._animatedActionsIndex = NoIndex;

                return;
            }

            Lerp(ref this._actionsAmount, target, ActionsLerpSpeed, this._deltaTime);

            if (Mathf.Abs(target - this._actionsAmount) < ActionsSnapAmount)
                this._actionsAmount = target;

            Repaint();
        }

        private void RepaintOnHoverChange()
        {
            if (CurEvent.IsNull) return;
            if (!CurEvent.IsMouseMove && CurEvent.Type != EventType.MouseLeaveWindow) return;

            Repaint();
        }

        private void UpdateDeltaTime()
        {
            this._deltaTime = (float)(EditorApplication.timeSinceStartup - this._lastLayoutTime);

            if (this._deltaTime > MaxDeltaTime)
                this._deltaTime = FallbackDeltaTime;

            this._lastLayoutTime = EditorApplication.timeSinceStartup;
        }

        #endregion

        #region Time Label

        private void RefreshTimeLabels()
        {
            var data = KClipboard.EnsureData();
            var entries = data.entries;
            var hasDataChanged = this._timeLabelsVersion != data.Version;

            if (!hasDataChanged && EditorApplication.timeSinceStartup < this._nextTimeLabelRefresh) return;

            if (hasDataChanged)
                ValidateSelection();

            this._timeLabelsVersion = data.Version;
            this._nextTimeLabelRefresh = EditorApplication.timeSinceStartup + TimeLabelRefreshInterval;

            this._timeLabels.Clear();

            for (var i = 0; i < entries.Count; i++)
                this._timeLabels.Add(GetRelativeLabel(entries[i].timestampTicks));
        }

        private static string GetRelativeLabel(long timestampTicks)
        {
            var elapsed = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - timestampTicks);

            if (elapsed.TotalSeconds < 60) return JustNowLabel;
            if (elapsed.TotalMinutes < 60) return string.Format(MinutesAgoFormat, (int)elapsed.TotalMinutes);
            if (elapsed.TotalHours < 24) return string.Format(HoursAgoFormat, (int)elapsed.TotalHours);

            return string.Format(DaysAgoFormat, (int)elapsed.TotalDays);
        }

        #endregion

        #region Method

        private static void BuildStyles()
        {
            if (_hasBuiltStyles && _isStyleDark == IsDarkTheme) return;

            _hasBuiltStyles = true;
            _isStyleDark = IsDarkTheme;

            IconsByKey.Clear();

            _selectedRowStyle = GUI.skin.FindStyle(SelectedRowStyleName);
            _previewHeaderHeight = Mathf.Max(GUI.skin.FindStyle(TitlebarStyleName)?.fixedHeight ?? 0f, MinPreviewHeaderHeight);

            _actionButtonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fixedHeight = 0f, fixedWidth = 0f };

            _emptyTitleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true };

            _emptyBodyStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter, wordWrap = true };

            _previewBodyStyle = new GUIStyle { padding = new RectOffset(PreviewPadding, PreviewPadding, PreviewPadding, PreviewPadding) };

            _iconButtonStyle = new GUIStyle(EditorStyles.iconButton)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(PinIconPadding, PinIconPadding, PinIconPadding, PinIconPadding),
                fixedHeight = 0f,
                fixedWidth = 0f,
            };

            _pinnedIconContent = new GUIContent(EditorIcons.GetTexture(PinnedIconName), PinnedTooltip);
            _unpinnedIconContent = new GUIContent(EditorIcons.GetTexture(UnpinnedIconName), UnpinnedTooltip);
            _pasteIconContent = new GUIContent(EditorIcons.GetTexture(PasteIconName), PasteTooltip);
            _deleteIconContent = new GUIContent(EditorIcons.GetTexture(DeleteIconName), DeleteTooltip);
        }

        [MenuItem(MenuPath, false, MenuPriority)]
        public static void Open()
        {
            var window = GetWindow<KClipboardWindow>(utility: false, title: WindowTitle, focus: true);

            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        }

        #endregion
    }
}
#endif
