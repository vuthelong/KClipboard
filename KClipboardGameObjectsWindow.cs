#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Kingfisher.KClipboard.Libs.KUtils;
using static Kingfisher.KClipboard.Libs.KGUI;

namespace Kingfisher.KClipboard
{
    public class KClipboardGameObjectsWindow : EditorWindow
    {
        #region Field

        private const string MenuPath = "Tools/Kingfisher/K-Clipboard/GameObject History";
        private const string WindowTitle = "K-Clipboard GameObject";
        private const int MenuPriority = 913;

        private const string EmptyTitle = "Nothing here";
        private const string EmptyBody = "You will see your copied GameObjects here once you have copied something.";
        private const string EmptyEntryLabel = "GameObject";

        private const string MultiRootLabelFormat = "{0} GameObjects";
        private const string MultiRootTooltipSeparator = ", ";

        private const string PreviewUnavailable = "This entry has no stored preview data.";
        private const string PreviewHeaderLabel = "Preview";

        private const string PasteFailureLogFormat = "K-Clipboard: {0}";

        private const string SelectedRowStyleName = "OL SelectedRow";
        private const string TitlebarStyleName = "IN Title";

        private const string PinnedIconName = "pinned";
        private const string UnpinnedIconName = "pin";
        private const string PasteIconName = "Paste values";
        private const string DeleteIconName = "CrossIcon";
        private const string FoldoutIconName = "IN_foldout";
        private const string FoldoutOnIconName = "IN_foldout_on";

        private const string PinnedTooltip = "Pinned - kept when the history is trimmed or cleared";
        private const string UnpinnedTooltip = "Pin so this entry survives trimming and Clear history";
        private const string PasteTooltip = "Paste these GameObject(s)";
        private const string DeleteTooltip = "Remove from history";
        private const string CopySelectionTooltip = "Copy the selected GameObject(s) to K-Clipboard History";

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

        private const float DragStartDistance = 2f;
        private const float RowGapLerpSpeed = 10f;
        private const float RowGapSnapAmount = .1f;
        private const float SelectedGradientWidthRatio = .77f;
        private const float SelectedGradientFlatOverlapWidth = 1f;

        private const float MinWindowWidth = 360f;
        private const float MinWindowHeight = 240f;
        private const float Padding = 4f;
        private const float RowPadding = 8f;
        private const float TitleTimeGap = 2f;

        private const float ActionGap = 4f;
        private const float ActionButtonGap = 2f;
        private const float ActionIconSize = 16f;
        private const float PinIconSize = 16f;
        private const float RowIconSize = 16f;
        private const float PinButtonSize = 26f;
        private const int PinIconPadding = (int)((PinButtonSize - PinIconSize) * .5f);
        private const float EmptyTextWidth = 260f;
        private const float EmptyTitleGap = 2f;
        private const float UnpinnedIconAlpha = .5f;
        private const float ActionIconBrightnessDark = .78f;
        private const float ActionIconBrightnessLight = .49f;
        private const float LabelIndent = PinButtonSize + ActionGap;

        private const int PreviewPadding = 6;
        private const float DividerThickness = 1f;
        private const float PreviewFadeHeight = 12f;
        private const float PreviewFadeAlpha = .25f;
        private const float ListBottomPadding = 50f;
        private const float MinPreviewHeaderHeight = 22f;
        private const float PreviewTitleIconSize = 16f;
        private const float DefaultPreviewHeight = 200f;
        private const float MinPreviewHeight = 60f;
        private const float MinListHeight = 60f;

        private const float PreviewIndent = 14f;
        private const float PreviewNodeIconSize = 14f;
        private const float PreviewFoldoutSize = 14f;
        private const float PreviewComponentIconSize = 12f;
        private const float PreviewComponentIconGap = 2f;
        private const float HierarchyLineThickness = 1f;
        private const float HierarchyLineDarkAlpha = .165f;
        private const float HierarchyLineLightAlpha = .23f;
        private const float HierarchyLineWidthWithChildren = 7f; // KHierarchy/KHierarchyGUI.cs:460 - exact, since PreviewIndent == KHierarchy's IndentWidth (14f)
        private const float HierarchyLineWidth = 17f; // KHierarchy/KHierarchyGUI.cs:461 - same

        private const string PathBarEmptyLabel = "Hover a row to see its path";
        private const string PathSeparator = " › ";
        private static readonly float PathBarHeight = EditorGUIUtility.singleLineHeight + RowPadding;

        private static readonly float RowHeight = RowPadding * 2f + EditorGUIUtility.singleLineHeight * 2f + TitleTimeGap;
        private static readonly float ActionButtonSize = RowHeight;
        private static readonly float PreviewRowHeight = EditorGUIUtility.singleLineHeight;

        private static readonly Color DividerColorDark = Greyscale(.13f);
        private static readonly Color DividerColorLight = Greyscale(.6f);
        private static readonly Color PreviewFadeColor = Greyscale(0f, PreviewFadeAlpha);
        private static readonly Color RowEvenColorDark = Greyscale(.249f);
        private static readonly Color RowEvenColorLight = Greyscale(.82f);
        private static readonly Color RowOddColorDark = Greyscale(.228f);
        private static readonly Color RowOddColorLight = Greyscale(.85f);
        private static readonly Color SelectedRowColorDark = new(.17f, .365f, .535f);
        private static readonly Color SelectedRowColorLight = new Color(.2f, .375f, .555f) * 1.2f;
        private static readonly Color PinnedIconColor = Greyscale(1f);
        private static readonly Color UnpinnedIconColor = Greyscale(1f, UnpinnedIconAlpha);
        private static readonly Color ActionIconColorDark = Greyscale(ActionIconBrightnessDark);
        private static readonly Color ActionIconColorLight = Greyscale(ActionIconBrightnessLight);
        private static readonly Color HierarchyLineColorDark = Greyscale(1f, HierarchyLineDarkAlpha);
        private static readonly Color HierarchyLineColorLight = Greyscale(0f, HierarchyLineLightAlpha);
        private static readonly float ActionsWidth = (ActionButtonSize + ActionButtonGap) * ActionButtonCount;
        private static readonly List<bool> NoAncestorLines = new();

        private static readonly GUIContent EmptyTitleContent = new(EmptyTitle);
        private static readonly GUIContent EmptyBodyContent = new(EmptyBody);
        private static readonly GUIContent PreviewUnavailableContent = new(PreviewUnavailable);
        private static readonly GUIContent PreviewHeaderContent = new(PreviewHeaderLabel);
        private static readonly GUIContent ClearButtonContent = new("Clear history");
        private static readonly GUIContent CopySelectionButtonContent = new("Copy Selection", CopySelectionTooltip);
        private static readonly GUIContent PasteButtonContent = new(string.Empty, PasteTooltip);
        private static readonly GUIContent DeleteButtonContent = new(string.Empty, DeleteTooltip);
        private static readonly GUIContent ComponentTooltipContent = new();

        private static readonly GUILayoutOption[] ExpandWidthOptions = { GUILayout.ExpandWidth(true) };

        private static GUIStyle _selectedRowStyle;
        private static float _previewHeaderHeight;
        private static GUIStyle _actionButtonStyle;
        private static GUIStyle _emptyTitleStyle;
        private static GUIStyle _emptyBodyStyle;
        private static GUIStyle _previewBodyStyle;
        private static GUIStyle _iconButtonStyle;
        private static GUIStyle _tooltipOnlyStyle;
        private static GUIContent _pinnedIconContent;
        private static GUIContent _unpinnedIconContent;
        private static Texture _pasteIcon;
        private static Texture _deleteIcon;
        private static Texture _foldoutClosedIcon;
        private static Texture _foldoutOpenIcon;
        private static bool _hasBuiltStyles;
        private static bool _isStyleDark;

        [SerializeField] private float previewHeight = DefaultPreviewHeight;

        private readonly List<string> _timeLabels = new();
        private readonly List<GUIContent> _labelContents = new();
        private readonly HashSet<KClipboardGameObjectsData.PreviewNode> _collapsedPreviewNodes = new();

        private KClipboardGameObjectsData.HistoryEntry _selectedEntry;
        private KClipboardGameObjectsData.HistoryEntry _pendingSelection;
        private KClipboardGameObjectsData.HistoryEntry _previewEntry;
        private GUIContent _previewTitleContent;
        private Vector2 _scroll;
        private Vector2 _previewScroll;
        private int _previewRowIndex;
        private string _hoveredPreviewPath;
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
        private bool _pendingCopySelection;
        private bool _pendingClearHistory;
        private bool _isResizingPreview;
        private bool _isMouseOverList;

        private readonly List<float> _rowGaps = new();

        private int _pressedIndex = NoIndex;
        private int _draggedFromIndex = NoIndex;
        private Vector2 _rowPressPosition;
        private float _draggedRowHoldOffset;
        private bool _isDraggingRow;
        private KClipboardGameObjectsData.HistoryEntry _draggedEntry;
        private GUIContent _draggedLabelContent;

        #endregion

        #region Property

        private static List<KClipboardGameObjectsData.HistoryEntry> Entries => KClipboardGameObjects.EnsureData().entries;

        private static Color DividerColor => IsDarkTheme ? DividerColorDark : DividerColorLight;

        private static Color RowEvenColor => IsDarkTheme ? RowEvenColorDark : RowEvenColorLight;

        private static Color RowOddColor => IsDarkTheme ? RowOddColorDark : RowOddColorLight;

        private static Color SelectedRowColor => IsDarkTheme ? SelectedRowColorDark : SelectedRowColorLight;

        private static Color ActionIconColor => IsDarkTheme ? ActionIconColorDark : ActionIconColorLight;

        private static Color HierarchyLineColor => IsDarkTheme ? HierarchyLineColorDark : HierarchyLineColorLight;

        private bool HasPreview => this._selectedEntry != null;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            KClipboardGameObjects.EnsureData();

            wantsMouseMove = true;
        }

        private void OnDisable()
        {
            if (this._isDraggingRow)
                CancelDragRow();
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
            ApplyPendingCopySelection();
            ApplyPendingClearHistory();
            RepaintOnHoverChange();
            HandleExternalDrag();
        }

        private void OnSelectionChange() => Repaint();

        #endregion

        #region Drawing

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            SetGUIEnabled(Selection.gameObjects.Length > 0);

            if (GUILayout.Button(CopySelectionButtonContent, EditorStyles.toolbarButton))
                this._pendingCopySelection = true;

            ResetGUIEnabled();

            GUILayout.FlexibleSpace();

            SetGUIEnabled(KClipboardGameObjects.EnsureData().HasUnpinned());

            if (GUILayout.Button(ClearButtonContent, EditorStyles.toolbarButton))
                this._pendingClearHistory = true;

            ResetGUIEnabled();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBody(Rect rect)
        {
            var paneHeight = GetPreviewHeight(rect.height);
            var listRect = new Rect(rect.x, rect.y, rect.width, rect.height - paneHeight);

            this._isMouseOverList = listRect.IsHovered();

            DrawList(listRect);

            if (paneHeight <= 0f) return;

            DrawListFade(listRect);
            DrawPreview(new Rect(rect.x, rect.yMax - paneHeight, rect.width, paneHeight));
        }

        private static void DrawListFade(Rect listRect)
        {
            if (!CurEvent.IsRepaint) return;

            listRect.SetHeightFromBottom(PreviewFadeHeight).DrawCurtainUp(PreviewFadeColor);
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

            if (Entries.Count == 0 && !this._isDraggingRow)
                DrawEmptyMessage(new Rect(0f, 0f, rect.width, rect.height));
            else
                DrawScrolledRows(rect.width);

            GUILayout.EndArea();
        }

        private void DrawScrolledRows(float width)
        {
            this._scroll = EditorGUILayout.BeginScrollView(this._scroll, GUIStyle.none, GUIStyle.none);

            DrawRows(width);

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

        private void DrawRows(float width)
        {
            if (CurEvent.IsRepaint)
                this._hoveredIndex = NoIndex;

            var entries = Entries;
            var gaps = AnimateRowGaps(entries.Count);

            for (var i = 0; i < entries.Count; i++)
            {
                GUILayout.Space(gaps[i]);

                DrawRow(entries[i], this._timeLabels[i], this._labelContents[i], i);
            }

            GUILayout.Space(gaps[entries.Count]);

            if (this._isDraggingRow)
                DrawDraggedRow(width);

            HandleRowReorder();
        }

        private void DrawRow(KClipboardGameObjectsData.HistoryEntry entry, string timeLabel, GUIContent labelContent, int index)
        {
            var rowRect = GUILayoutUtility.GetRect(0f, RowHeight, ExpandWidthOptions);
            var actionsAmount = index == this._animatedActionsIndex ? this._actionsAmount : 0f;

            DrawRowBackground(rowRect, entry == this._selectedEntry, actionsAmount);

            if (CurEvent.IsRepaint && this._isMouseOverList && !this._isDraggingRow && rowRect.IsHovered())
                this._hoveredIndex = index;

            var contentRect = new Rect(rowRect.x + Padding, rowRect.y + RowPadding, rowRect.width - Padding * 2f, rowRect.height - RowPadding * 2f);

            contentRect.width -= ActionsWidth * actionsAmount;

            DrawEntry(contentRect, entry, timeLabel, labelContent, index);
            DrawActionButtons(rowRect, entry, index, actionsAmount);
            HandleRowClick(rowRect, entry, index);
        }

        private void DrawDraggedRow(float width)
        {
            var rowRect = new Rect(0f, GetDraggedRowY(), width, RowHeight);

            DrawRowBackground(rowRect, false, 0f);

            if (CurEvent.IsRepaint)
                _selectedRowStyle?.Draw(rowRect, false, false, true, true);

            var contentRect = new Rect(rowRect.x + Padding, rowRect.y + RowPadding, rowRect.width - Padding * 2f, rowRect.height - RowPadding * 2f);
            var iconX = contentRect.x + LabelIndent;
            var labelX = iconX + RowIconSize + ActionGap;
            var labelWidth = Mathf.Max(contentRect.xMax - labelX, 0f);

            DrawRowIcon(new Rect(iconX, contentRect.y + (EditorGUIUtility.singleLineHeight - RowIconSize) * .5f, RowIconSize, RowIconSize));

            GUI.Label(new Rect(labelX, contentRect.y, labelWidth, EditorGUIUtility.singleLineHeight), this._draggedLabelContent);
        }

        private float GetDraggedRowY() => Mathf.Max(CurEvent.MousePosition.y - RowHeight * .5f + this._draggedRowHoldOffset, 0f);

        private static void DrawRowBackground(Rect rowRect, bool isSelected, float actionsAmount)
        {
            if (!CurEvent.IsRepaint) return;

            rowRect.Draw(Lerp(RowEvenColor, RowOddColor, rowRect.y.PingPong(RowHeight) / RowHeight));

            if (!isSelected) return;

            var highlightRect = rowRect;

            highlightRect.width -= (ActionsWidth + ActionButtonGap) * actionsAmount;

            if (highlightRect.width <= 0f) return;

            var gradientWidth = highlightRect.width * SelectedGradientWidthRatio;
            var gradientRect = highlightRect.SetWidthFromRight(gradientWidth);
            var flatRect = highlightRect.SetXMax(gradientRect.x + SelectedGradientFlatOverlapWidth);

            flatRect.Draw(SelectedRowColor);
            gradientRect.DrawCurtainRight(SelectedRowColor);
        }

        private void DrawEntry(Rect contentRect, KClipboardGameObjectsData.HistoryEntry entry, string timeLabel, GUIContent labelContent, int index)
        {
            DrawPinButton(new Rect(contentRect.x, contentRect.y + (contentRect.height - PinButtonSize) * .5f, PinButtonSize, PinButtonSize), entry, index);

            var iconX = contentRect.x + LabelIndent;
            var labelX = iconX + RowIconSize + ActionGap;
            var labelWidth = Mathf.Max(contentRect.xMax - labelX, 0f);
            var timeWidth = Mathf.Max(contentRect.xMax - iconX, 0f);

            DrawRowIcon(new Rect(iconX, contentRect.y + (EditorGUIUtility.singleLineHeight - RowIconSize) * .5f, RowIconSize, RowIconSize));

            GUI.Label(new Rect(labelX, contentRect.y, labelWidth, EditorGUIUtility.singleLineHeight), labelContent);

            DrawTimeLabel(new Rect(iconX + 5f, contentRect.yMax - EditorGUIUtility.singleLineHeight, timeWidth, EditorGUIUtility.singleLineHeight), timeLabel);
        }

        private static void DrawRowIcon(Rect rect)
        {
            var icon = EditorIcons.FindTexture(KClipboardGameObjects.PrefabIconName);

            if (icon == null) return;

            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
        }

        private static string BuildEntryLabel(KClipboardGameObjectsData.HistoryEntry entry, out string tooltip)
        {
            var roots = entry.previewRoots;

            if (roots == null || roots.Length == 0)
            {
                tooltip = string.Empty;

                return EmptyEntryLabel;
            }

            if (roots.Length == 1)
            {
                tooltip = string.Empty;

                return roots[0].name;
            }

            var names = new string[roots.Length];

            for (var i = 0; i < roots.Length; i++)
                names[i] = roots[i].name;

            tooltip = string.Join(MultiRootTooltipSeparator, names);

            return string.Format(MultiRootLabelFormat, roots.Length);
        }

        private static GUIContent BuildLabelContent(KClipboardGameObjectsData.HistoryEntry entry)
        {
            var text = BuildEntryLabel(entry, out var tooltip);

            return new GUIContent(text, tooltip);
        }

        private void DrawPinButton(Rect rect, KClipboardGameObjectsData.HistoryEntry entry, int index)
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

        private void DrawActionButtons(Rect rowRect, KClipboardGameObjectsData.HistoryEntry entry, int index, float amount)
        {
            var slideOffset = ActionsWidth * (1f - amount);
            var deleteRect = new Rect(rowRect.xMax - ActionButtonGap - ActionButtonSize + slideOffset, rowRect.y, ActionButtonSize, ActionButtonSize);
            var pasteRect = new Rect(deleteRect.x - ActionButtonGap - ActionButtonSize, rowRect.y, ActionButtonSize, ActionButtonSize);

            var wasPasteClicked = GUI.Button(pasteRect, PasteButtonContent, _actionButtonStyle);

            DrawActionIcon(pasteRect, _pasteIcon, ActionIconColor);

            if (wasPasteClicked)
                PasteEntry(entry);

            var wasDeleteClicked = GUI.Button(deleteRect, DeleteButtonContent, _actionButtonStyle);

            DrawActionIcon(deleteRect, _deleteIcon, ActionIconColor);

            if (!wasDeleteClicked) return;

            this._pendingRemovalIndex = index;
        }

        private static void DrawActionIcon(Rect buttonRect, Texture icon, Color color)
        {
            if (icon == null) return;

            var iconRect = new Rect(buttonRect.center.x - ActionIconSize * .5f, buttonRect.center.y - ActionIconSize * .5f, ActionIconSize, ActionIconSize);

            SetGUIColor(color);

            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            ResetGUIColor();
        }

        private void HandleRowClick(Rect rowRect, KClipboardGameObjectsData.HistoryEntry entry, int index)
        {
            if (this._isDraggingRow) return;
            if (!this._isMouseOverList) return;

            if (CurEvent.IsMouseDown && CurEvent.MouseButton == LeftMouseButton && rowRect.IsHovered())
            {
                this._pressedIndex = index;
                this._rowPressPosition = CurEvent.MousePosition;

                CurEvent.Use();

                return;
            }

            if (!CurEvent.IsMouseUp) return;
            if (this._pressedIndex != index) return;

            this._pressedIndex = NoIndex;

            CurEvent.Use();

            this._pendingSelection = this._selectedEntry == entry ? null : entry;
            this._hasPendingSelection = true;
        }

        #endregion

        #region Reorder

        private List<float> AnimateRowGaps(int rowCount)
        {
            var gaps = GetRowGaps(rowCount);

            if (!CurEvent.IsLayout) return gaps;

            var targetIndex = this._isDraggingRow ? GetInsertIndex(rowCount) : NoIndex;
            var isSettled = true;

            for (var i = 0; i < gaps.Count; i++)
            {
                var target = i == targetIndex ? RowHeight : 0f;

                gaps[i] = Lerp(gaps[i], target, RowGapLerpSpeed, this._deltaTime);

                if (Mathf.Abs(target - gaps[i]) < RowGapSnapAmount)
                    gaps[i] = target;
                else
                    isSettled = false;
            }

            if (this._isDraggingRow || !isSettled)
                Repaint();

            return gaps;
        }

        private List<float> GetRowGaps(int rowCount)
        {
            while (this._rowGaps.Count < rowCount + 1) this._rowGaps.Add(0f);
            while (this._rowGaps.Count > rowCount + 1) this._rowGaps.RemoveLast();

            return this._rowGaps;
        }

        private int GetInsertIndex(int rowCount) => ((CurEvent.MousePosition.y + this._draggedRowHoldOffset) / RowHeight).FloorToInt().Clamp(0, rowCount);

        private void HandleRowReorder()
        {
            if (this._pressedIndex == NoIndex && !this._isDraggingRow) return;

            var currentEvent = CurEvent;

            if (!currentEvent.IsNull && currentEvent.Type == EventType.MouseLeaveWindow)
            {
                if (this._isDraggingRow) CancelDragRow();
                else this._pressedIndex = NoIndex;

                return;
            }

            if (!this._isDraggingRow)
            {
                if (!currentEvent.IsMouseDrag) return;
                if (currentEvent.MousePosition.DistanceTo(this._rowPressPosition) < DragStartDistance) return;

                BeginDragRow();

                return;
            }

            if (!currentEvent.IsMouseUp) return;

            AcceptDragRow();
        }

        private void BeginDragRow()
        {
            var entry = KClipboardGameObjects.DetachEntryForReorder(this._pressedIndex);

            if (entry == null)
            {
                this._pressedIndex = NoIndex;

                return;
            }

            this._draggedFromIndex = this._pressedIndex;
            this._draggedEntry = entry;
            this._draggedLabelContent = BuildLabelContent(entry);
            this._draggedRowHoldOffset = this._pressedIndex * RowHeight + RowHeight * .5f - this._rowPressPosition.y;
            this._isDraggingRow = true;

            this._hoveredIndex = NoIndex;
            this._animatedActionsIndex = NoIndex;
            this._actionsAmount = 0f;

            GetRowGaps(Entries.Count)[this._pressedIndex] = RowHeight;

            CurEvent.Use();

            Repaint();
        }

        private void AcceptDragRow()
        {
            var insertIndex = GetInsertIndex(Entries.Count);
            var gaps = GetRowGaps(Entries.Count);

            gaps[insertIndex] -= RowHeight;
            gaps.AddAt(0f, insertIndex);

            KClipboardGameObjects.InsertEntryForReorder(this._draggedEntry, insertIndex);

            EndDragRow();

            CurEvent.Use();

            Repaint();
        }

        private void CancelDragRow()
        {
            var gaps = GetRowGaps(Entries.Count);

            gaps[this._draggedFromIndex] -= RowHeight;
            gaps.AddAt(0f, this._draggedFromIndex);

            KClipboardGameObjects.InsertEntryForReorder(this._draggedEntry, this._draggedFromIndex);

            EndDragRow();

            Repaint();
        }

        private void EndDragRow()
        {
            this._pressedIndex = NoIndex;
            this._draggedFromIndex = NoIndex;
            this._draggedEntry = null;
            this._draggedLabelContent = null;
            this._isDraggingRow = false;
        }

        #endregion

        #region External Drag

        private void HandleExternalDrag()
        {
            var currentEvent = CurEvent;

            if (!currentEvent.IsDragUpdate && !currentEvent.IsDragPerform) return;
            if (KClipboardMenu.PluginDisabled) return;
            if (!TryGetDraggedGameObjects(out var gameObjects)) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (currentEvent.IsDragUpdate)
            {
                currentEvent.Use();

                return;
            }

            DragAndDrop.AcceptDrag();

            if (!KClipboardGameObjects.CopySelectionToHistory(gameObjects, out var message))
                Debug.LogError(string.Format(PasteFailureLogFormat, message));

            currentEvent.Use();

            Repaint();
        }

        private static bool TryGetDraggedGameObjects(out GameObject[] gameObjects)
        {
            var objectReferences = DragAndDrop.objectReferences;
            var list = new List<GameObject>(objectReferences.Length);

            for (var i = 0; i < objectReferences.Length; i++)
                if (objectReferences[i] is GameObject gameObject)
                    list.Add(gameObject);

            gameObjects = list.ToArray();

            return gameObjects.Length > 0;
        }

        #endregion

        #region Preview

        private void DrawPreview(Rect rect)
        {
            var headerRect = new Rect(rect.x, rect.y + DividerThickness, rect.width, _previewHeaderHeight);
            var pathBarRect = new Rect(rect.x, rect.yMax - PathBarHeight, rect.width, PathBarHeight);
            var bodyRect = new Rect(rect.x, headerRect.yMax, rect.width, Mathf.Max(pathBarRect.y - headerRect.yMax, 0f));

            new Rect(rect.x, rect.y, rect.width, DividerThickness).Draw(DividerColor);

            DrawPreviewHeader(headerRect);
            HandlePreviewResize(headerRect);

            GUILayout.BeginArea(bodyRect);

            this._previewScroll = EditorGUILayout.BeginScrollView(this._previewScroll, GUIStyle.none, GUIStyle.none);

            DrawPreviewBody();

            EditorGUILayout.EndScrollView();

            GUILayout.EndArea();

            DrawPathBar(pathBarRect);
        }

        private void DrawPathBar(Rect rect)
        {
            new Rect(rect.x, rect.y, rect.width, DividerThickness).Draw(DividerColor);

            if (CurEvent.IsRepaint)
                rect.Draw(RowEvenColor);

            var labelRect = new Rect(rect.x + Padding, rect.y, Mathf.Max(rect.width - Padding * 2f, 0f), rect.height);

            SetGUIEnabled(false);

            GUI.Label(labelRect, string.IsNullOrEmpty(this._hoveredPreviewPath) ? PathBarEmptyLabel : this._hoveredPreviewPath, EditorStyles.miniLabel);

            ResetGUIEnabled();
        }

        private void DrawPreviewHeader(Rect rect)
        {
            if (CurEvent.IsRepaint)
                EditorStyles.toolbar.Draw(rect, false, false, false, false);

            var headerWidth = PreviewHeaderLabel.GetLabelWidth();
            var titleX = rect.x + Padding + headerWidth + ActionGap;

            SetGUIEnabled(false);

            GUI.Label(new Rect(rect.x + Padding, rect.y, headerWidth, rect.height), PreviewHeaderContent, EditorStyles.miniLabel);

            ResetGUIEnabled();

            DrawPreviewTitle(new Rect(titleX, rect.y, rect.xMax - Padding - titleX, rect.height));

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
        }

        private void DrawPreviewTitle(Rect rect)
        {
            if (rect.width <= 0f) return;
            if (this._previewTitleContent == null) return;

            var labelX = rect.x;

            if (this._previewTitleContent.image != null)
            {
                GUI.DrawTexture(new Rect(rect.x, rect.y + (rect.height - PreviewTitleIconSize) * .5f, PreviewTitleIconSize, PreviewTitleIconSize), this._previewTitleContent.image, ScaleMode.ScaleToFit);

                labelX += PreviewTitleIconSize + ActionGap;
            }

            if (labelX >= rect.xMax) return;

            var labelHeight = EditorGUIUtility.singleLineHeight;

            GUI.Label(new Rect(labelX, rect.y + (rect.height - labelHeight) * .5f, rect.xMax - labelX, labelHeight), this._previewTitleContent.text);
        }

        private void DrawPreviewBody()
        {
            if (CurEvent.IsRepaint)
                this._hoveredPreviewPath = null;

            this._previewRowIndex = 0;

            EditorGUILayout.BeginVertical(_previewBodyStyle);

            var roots = this._previewEntry?.previewRoots;

            if (roots == null || roots.Length == 0)
                GUILayout.Label(PreviewUnavailableContent, _emptyBodyStyle);
            else
                for (var i = 0; i < roots.Length; i++)
                    DrawPreviewNode(roots[i], 0, roots[i].name, NoAncestorLines, i == roots.Length - 1);

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewNode(KClipboardGameObjectsData.PreviewNode node, int depth, string path, List<bool> ancestorContinues, bool isLast)
        {
            var hasChildren = node.children != null && node.children.Count > 0;
            var isCollapsed = hasChildren && this._collapsedPreviewNodes.Contains(node);

            var rowRect = GUILayoutUtility.GetRect(0f, PreviewRowHeight, ExpandWidthOptions);

            DrawPreviewRowBackground(rowRect);
            DrawHierarchyLines(rowRect, depth, ancestorContinues, isLast, hasChildren);

            if (CurEvent.IsRepaint && rowRect.IsHovered())
                this._hoveredPreviewPath = path;

            var indent = depth * PreviewIndent;
            var foldoutRect = new Rect(rowRect.x + indent, rowRect.y + (rowRect.height - PreviewFoldoutSize) * .5f, PreviewIndent, PreviewFoldoutSize);
            var iconRect = new Rect(foldoutRect.xMax, rowRect.y + (rowRect.height - PreviewNodeIconSize) * .5f, PreviewNodeIconSize, PreviewNodeIconSize);
            var labelX = iconRect.xMax + ActionGap;
            var labelWidth = node.name.GetLabelWidth();
            var labelRect = new Rect(labelX, rowRect.y, Mathf.Min(labelWidth, Mathf.Max(rowRect.xMax - labelX, 0f)), rowRect.height);

            if (hasChildren && GUI.Button(foldoutRect, GUIContent.none, GUIStyle.none))
                ToggleCollapsed(node);

            if (hasChildren)
            {
                var foldoutIcon = isCollapsed ? _foldoutClosedIcon : _foldoutOpenIcon;

                if (foldoutIcon != null)
                    GUI.DrawTexture(foldoutRect, foldoutIcon, ScaleMode.ScaleToFit);
            }

            var nodeIcon = GetPreviewNodeIcon(node);

            if (nodeIcon != null)
                GUI.DrawTexture(iconRect, nodeIcon, ScaleMode.ScaleToFit);

            GUI.Label(labelRect, node.name);

            DrawComponentMinimap(new Rect(labelX + labelWidth + ActionGap, rowRect.y, Mathf.Max(rowRect.xMax - (labelX + labelWidth + ActionGap), 0f), rowRect.height), node);

            if (!hasChildren || isCollapsed) return;

            var childAncestorContinues = new List<bool>(ancestorContinues) { !isLast };

            for (var i = 0; i < node.children.Count; i++)
                DrawPreviewNode(node.children[i], depth + 1, path + PathSeparator + node.children[i].name, childAncestorContinues, i == node.children.Count - 1);
        }

        private void DrawPreviewRowBackground(Rect rowRect)
        {
            if (!CurEvent.IsRepaint) return;

            rowRect.Draw(this._previewRowIndex++ % 2 == 0 ? RowEvenColor : RowOddColor);
        }

        // Standard indent-guide tree lines: a straight vertical pass-through for every ancestor
        // column that still has a following sibling further down, plus this row's own elbow -
        // full-height if it has a following sibling of its own (so the line continues into the
        // next row), or stopping at the row's midpoint if it's the last child.
        private static void DrawHierarchyLines(Rect rowRect, int depth, List<bool> ancestorContinues, bool isLast, bool hasChildren)
        {
            if (depth == 0) return;
            if (!CurEvent.IsRepaint) return;

            var lineColor = HierarchyLineColor;
            var halfThickness = HierarchyLineThickness * .5f;
            var midY = rowRect.y + rowRect.height * .5f;

            for (var d = 0; d < depth - 1; d++)
            {
                // ancestorContinues[0] is always the root's own (unused - depth 0 never draws an
                // elbow) status, so the entry for the ancestor whose elbow sits at column d is one
                // slot further in, at index d + 1.
                if (!ancestorContinues[d + 1]) continue;

                var x = rowRect.x + d * PreviewIndent + PreviewIndent * .5f - halfThickness;

                new Rect(x, rowRect.y, HierarchyLineThickness, rowRect.height).Draw(lineColor);
            }

            var ownX = rowRect.x + (depth - 1) * PreviewIndent + PreviewIndent * .5f;
            var elbowHeight = isLast ? midY - rowRect.y : rowRect.height;
            var horizontalWidth = hasChildren ? HierarchyLineWidthWithChildren : HierarchyLineWidth;

            new Rect(ownX - halfThickness, rowRect.y, HierarchyLineThickness, elbowHeight).Draw(lineColor);
            new Rect(ownX, midY - halfThickness, horizontalWidth - halfThickness, HierarchyLineThickness).Draw(lineColor);
        }

        private static void DrawComponentMinimap(Rect rect, KClipboardGameObjectsData.PreviewNode node)
        {
            var iconNames = node.componentIconNames;
            var names = node.componentNames;

            if (iconNames == null || iconNames.Length == 0) return;

            var totalWidth = iconNames.Length * PreviewComponentIconSize + (iconNames.Length - 1) * PreviewComponentIconGap;
            var startX = Mathf.Max(rect.xMax - totalWidth, rect.x);

            var iconRect = new Rect(startX, rect.y + (rect.height - PreviewComponentIconSize) * .5f, PreviewComponentIconSize, PreviewComponentIconSize);

            for (var i = 0; i < iconNames.Length; i++)
            {
                if (iconRect.xMax > rect.xMax) break;

                var icon = string.IsNullOrEmpty(iconNames[i]) ? null : EditorIcons.GetTexture(iconNames[i]);

                if (icon != null)
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                if (names != null && i < names.Length && !string.IsNullOrEmpty(names[i]))
                {
                    ComponentTooltipContent.tooltip = names[i];

                    GUI.Button(iconRect, ComponentTooltipContent, _tooltipOnlyStyle);
                }

                iconRect = iconRect.MoveX(PreviewComponentIconSize + PreviewComponentIconGap);
            }
        }

        private void ToggleCollapsed(KClipboardGameObjectsData.PreviewNode node)
        {
            if (!this._collapsedPreviewNodes.Remove(node))
                this._collapsedPreviewNodes.Add(node);
        }

        private static Texture GetPreviewNodeIcon(KClipboardGameObjectsData.PreviewNode node)
        {
            var iconName = string.IsNullOrEmpty(node.iconName) ? KClipboardGameObjects.DefaultGameObjectIconName : node.iconName;
            var icon = EditorIcons.GetTexture(iconName);

            return icon != null ? icon : EditorIcons.GetTexture(KClipboardGameObjects.DefaultGameObjectIconName);
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

            this._previewEntry = this._selectedEntry;

            this._collapsedPreviewNodes.Clear();

            RefreshPreviewTitle();
        }

        private void RefreshPreviewTitle()
        {
            if (this._previewEntry == null)
            {
                this._previewTitleContent = null;

                return;
            }

            var text = BuildEntryLabel(this._previewEntry, out _);
            var roots = this._previewEntry.previewRoots;

            var iconName = roots != null && roots.Length == 1 && !string.IsNullOrEmpty(roots[0].iconName)
                ? roots[0].iconName
                : KClipboardGameObjects.DefaultGameObjectIconName;

            var icon = EditorIcons.GetTexture(iconName);

            this._previewTitleContent = new GUIContent(text, icon != null ? icon : EditorIcons.GetTexture(KClipboardGameObjects.DefaultGameObjectIconName));
        }

        #endregion

        #region Entry Action

        private static void PasteEntry(KClipboardGameObjectsData.HistoryEntry entry)
        {
            if (KClipboardGameObjects.TryPaste(entry, out var message)) return;

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

            KClipboardGameObjects.TogglePinned(this._pendingPinIndex);

            this._pendingPinIndex = NoIndex;

            Repaint();
        }

        private void ApplyPendingRemoval()
        {
            if (this._pendingRemovalIndex == NoIndex) return;

            KClipboardGameObjects.RemoveEntry(this._pendingRemovalIndex);

            this._pendingRemovalIndex = NoIndex;

            ValidateSelection();

            Repaint();
        }

        private void ValidateSelection()
        {
            if (this._isDraggingRow) return;
            if (this._selectedEntry == null) return;
            if (Entries.Contains(this._selectedEntry)) return;

            this._selectedEntry = null;
        }

        private void ApplyPendingCopySelection()
        {
            if (!this._pendingCopySelection) return;

            this._pendingCopySelection = false;

            if (!KClipboardGameObjects.CopySelectionToHistory(Selection.gameObjects, out var message))
                Debug.LogError(string.Format(PasteFailureLogFormat, message));

            Repaint();
        }

        private void ApplyPendingClearHistory()
        {
            if (!this._pendingClearHistory) return;

            this._pendingClearHistory = false;

            KClipboardGameObjects.ClearHistory();

            ValidateSelection();

            Repaint();
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
            var data = KClipboardGameObjects.EnsureData();
            var entries = data.entries;
            var hasDataChanged = this._timeLabelsVersion != data.Version || this._labelContents.Count != entries.Count;

            if (!hasDataChanged && EditorApplication.timeSinceStartup < this._nextTimeLabelRefresh) return;

            if (hasDataChanged)
                ValidateSelection();

            this._timeLabelsVersion = data.Version;
            this._nextTimeLabelRefresh = EditorApplication.timeSinceStartup + TimeLabelRefreshInterval;

            this._timeLabels.Clear();

            for (var i = 0; i < entries.Count; i++)
                this._timeLabels.Add(GetRelativeLabel(entries[i].timestampTicks));

            if (!hasDataChanged) return;

            this._labelContents.Clear();

            for (var i = 0; i < entries.Count; i++)
                this._labelContents.Add(BuildLabelContent(entries[i]));
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

            _selectedRowStyle = GUI.skin.FindStyle(SelectedRowStyleName);
            _previewHeaderHeight = Mathf.Max(GUI.skin.FindStyle(TitlebarStyleName)?.fixedHeight ?? 0f, MinPreviewHeaderHeight);

            _actionButtonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fixedHeight = 0f, fixedWidth = 0f };

            _emptyTitleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true };

            _emptyBodyStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter, wordWrap = true };

            _previewBodyStyle = new GUIStyle { padding = new RectOffset(PreviewPadding, PreviewPadding, PreviewPadding, PreviewPadding) };

            _tooltipOnlyStyle = new GUIStyle();

            _iconButtonStyle = new GUIStyle(EditorStyles.iconButton)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(PinIconPadding, PinIconPadding, PinIconPadding, PinIconPadding),
                fixedHeight = 0f,
                fixedWidth = 0f,
            };

            _pinnedIconContent = new GUIContent(EditorIcons.GetTexture(PinnedIconName), PinnedTooltip);
            _unpinnedIconContent = new GUIContent(EditorIcons.GetTexture(UnpinnedIconName), UnpinnedTooltip);
            _pasteIcon = EditorIcons.GetIcon(PasteIconName);
            _deleteIcon = EditorIcons.GetIcon(DeleteIconName);
            _foldoutClosedIcon = EditorIcons.GetTexture(FoldoutIconName);
            _foldoutOpenIcon = EditorIcons.GetTexture(FoldoutOnIconName);
        }

        [MenuItem(MenuPath, false, MenuPriority)]
        public static void Open()
        {
            var window = GetWindow<KClipboardGameObjectsWindow>(utility: false, title: WindowTitle, focus: true);

            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        }

        #endregion
    }
}
#endif
