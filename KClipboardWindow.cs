#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
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

        private const string PasteFailureLogFormat = "K-Clipboard: {0}";

        private const string EvenRowStyleName = "CN EntryBackEven";
        private const string OddRowStyleName = "CN EntryBackOdd";

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
        private const float PinIconSize = 16f;
        private const float PinButtonSize = 26f;
        private const int PinIconPadding = (int)((PinButtonSize - PinIconSize) * .5f);
        private const float EmptyTextWidth = 260f;
        private const float EmptyTitleGap = 2f;
        private const float UnpinnedIconAlpha = .5f;
        private const float LabelIndent = PinButtonSize + ActionGap;

        private static readonly float RowHeight = RowPadding * 2f + EditorGUIUtility.singleLineHeight * 2f + TitleTimeGap;
        private static readonly float ActionButtonSize = RowHeight;

        private static readonly Color PinnedIconColor = Greyscale(1f);
        private static readonly Color UnpinnedIconColor = Greyscale(1f, UnpinnedIconAlpha);
        private static readonly float ActionsWidth = (ActionButtonSize + ActionGap) * ActionButtonCount;

        private static readonly GUIContent NoSelectionHintContent = new(NoSelectionHint);
        private static readonly GUIContent EmptyTitleContent = new(EmptyTitle);
        private static readonly GUIContent EmptyBodyContent = new(EmptyBody);
        private static readonly GUIContent ClearButtonContent = new("Clear history");

        private static readonly GUILayoutOption[] ExpandWidthOptions = { GUILayout.ExpandWidth(true) };
        private static readonly GUILayoutOption[] ExpandOptions = { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) };

        private static GUIStyle _evenRowStyle;
        private static GUIStyle _oddRowStyle;
        private static GUIStyle _actionButtonStyle;
        private static GUIStyle _emptyTitleStyle;
        private static GUIStyle _emptyBodyStyle;
        private static GUIStyle _iconButtonStyle;
        private static GUIContent _pinnedIconContent;
        private static GUIContent _unpinnedIconContent;
        private static GUIContent _pasteIconContent;
        private static GUIContent _deleteIconContent;
        private static bool _hasBuiltStyles;
        private static bool _isStyleDark;

        private readonly List<string> _timeLabels = new();

        private Vector2 _scroll;
        private int _hoveredIndex = NoIndex;
        private int _animatedActionsIndex = NoIndex;
        private int _pendingRemovalIndex = NoIndex;
        private int _pendingPinIndex = NoIndex;
        private float _actionsAmount;
        private float _deltaTime;
        private double _lastLayoutTime;
        private double _nextTimeLabelRefresh;
        private bool _hasSelection;

        #endregion

        #region Property

        private static List<KClipboardData.HistoryEntry> Entries => KClipboard.EnsureData().entries;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            KClipboard.EnsureData();

            wantsMouseMove = true;

            this._hasSelection = Selection.gameObjects.Length > 0;
        }

        private void OnGUI()
        {
            BuildStyles();
            RefreshTimeLabels();
            UpdateActionsAnimation();

            DrawToolbar();
            DrawBody();

            ApplyPendingRemoval();
            ApplyPendingPin();
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
                KClipboard.ClearHistory();

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

        private void DrawBody()
        {
            if (Entries.Count == 0)
            {
                DrawEmptyMessage(GUILayoutUtility.GetRect(0f, 0f, ExpandOptions));

                return;
            }

            this._scroll = EditorGUILayout.BeginScrollView(this._scroll);

            DrawRows();

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

            DrawZebraBackground(rowRect, index);

            if (CurEvent.IsRepaint && rowRect.IsHovered())
                this._hoveredIndex = index;

            var contentRect = new Rect(rowRect.x + Padding, rowRect.y + RowPadding, rowRect.width - Padding * 2f, rowRect.height - RowPadding * 2f);
            var isAnimated = index == this._animatedActionsIndex && this._actionsAmount > 0f;

            if (isAnimated)
                contentRect.width -= ActionsWidth * this._actionsAmount;

            if (contentRect.width <= 0f) return;

            DrawEntry(contentRect, entry, timeLabel, index);

            if (!isAnimated) return;

            DrawActionButtons(rowRect, entry, index);
        }

        private static void DrawZebraBackground(Rect rowRect, int index)
        {
            if (!CurEvent.IsRepaint) return;

            var style = index % 2 == 0 ? _evenRowStyle : _oddRowStyle;

            style?.Draw(rowRect, false, false, false, false);
        }

        private void DrawEntry(Rect contentRect, KClipboardData.HistoryEntry entry, string timeLabel, int index)
        {
            DrawPinButton(new Rect(contentRect.x, contentRect.y + (contentRect.height - PinButtonSize) * .5f, PinButtonSize, PinButtonSize), entry, index);

            var labelX = contentRect.x + LabelIndent;
            var labelWidth = contentRect.xMax - labelX;

            if (labelWidth <= 0f) return;

            GUI.Label(new Rect(labelX, contentRect.y, labelWidth, EditorGUIUtility.singleLineHeight), entry.componentTypeLabel);

            DrawTimeLabel(new Rect(labelX, contentRect.yMax - EditorGUIUtility.singleLineHeight, labelWidth, EditorGUIUtility.singleLineHeight), timeLabel);
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

        private void DrawActionButtons(Rect rowRect, KClipboardData.HistoryEntry entry, int index)
        {
            var slideOffset = ActionsWidth * (1f - this._actionsAmount);
            var deleteRect = new Rect(rowRect.xMax - Padding - ActionButtonSize + slideOffset, rowRect.y, ActionButtonSize, ActionButtonSize);
            var pasteRect = new Rect(deleteRect.x - ActionGap - ActionButtonSize, rowRect.y, ActionButtonSize, ActionButtonSize);

            SetGUIEnabled(this._hasSelection);

            var wasPasteClicked = GUI.Button(pasteRect, _pasteIconContent, _actionButtonStyle);

            ResetGUIEnabled();

            if (wasPasteClicked)
                PasteEntry(entry);

            if (!GUI.Button(deleteRect, _deleteIconContent, _actionButtonStyle)) return;

            this._pendingRemovalIndex = index;
        }

        #endregion

        #region Entry Action

        private static void PasteEntry(KClipboardData.HistoryEntry entry)
        {
            if (KClipboard.TryPasteToSelected(entry, out var message)) return;

            Debug.LogError(string.Format(PasteFailureLogFormat, message));
        }

        private void ApplyPendingPin()
        {
            if (this._pendingPinIndex == NoIndex) return;

            KClipboard.TogglePinned(this._pendingPinIndex);

            this._pendingPinIndex = NoIndex;

            this._timeLabels.Clear();

            Repaint();
        }

        private void ApplyPendingRemoval()
        {
            if (this._pendingRemovalIndex == NoIndex) return;

            KClipboard.RemoveEntry(this._pendingRemovalIndex);

            this._pendingRemovalIndex = NoIndex;

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
            var entries = Entries;

            if (this._timeLabels.Count == entries.Count && EditorApplication.timeSinceStartup < this._nextTimeLabelRefresh) return;


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

            _evenRowStyle = GUI.skin.FindStyle(EvenRowStyleName);
            _oddRowStyle = GUI.skin.FindStyle(OddRowStyleName);

            _actionButtonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fixedHeight = 0f, fixedWidth = 0f };

            _emptyTitleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true };

            _emptyBodyStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter, wordWrap = true };

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
