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
        private const string WindowTitle = "K-Clipboard GameObject History";
        private const int MenuPriority = 913;

        private const string EmptyTitle = "Nothing here";
        private const string EmptyBody = "You will see your copied GameObjects here once you have copied something.";
        private const string EmptyEntryLabel = "GameObject";

        private const string MultiRootLabelFormat = "{0} GameObjects";
        private const string MultiRootTooltipSeparator = ", ";

        private const string PasteFailureLogFormat = "K-Clipboard: {0}";

        private const string PinnedIconName = "pinned";
        private const string UnpinnedIconName = "pin";
        private const string PasteIconName = "Paste values";
        private const string DeleteIconName = "CrossIcon";

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

        private const float ListBottomPadding = 50f;

        private static readonly float RowHeight = RowPadding * 2f + EditorGUIUtility.singleLineHeight * 2f + TitleTimeGap;
        private static readonly float ActionButtonSize = RowHeight;

        private static readonly Color RowEvenColorDark = Greyscale(.249f);
        private static readonly Color RowEvenColorLight = Greyscale(.82f);
        private static readonly Color RowOddColorDark = Greyscale(.228f);
        private static readonly Color RowOddColorLight = Greyscale(.85f);
        private static readonly Color PinnedIconColor = Greyscale(1f);
        private static readonly Color UnpinnedIconColor = Greyscale(1f, UnpinnedIconAlpha);
        private static readonly Color ActionIconColorDark = Greyscale(ActionIconBrightnessDark);
        private static readonly Color ActionIconColorLight = Greyscale(ActionIconBrightnessLight);
        private static readonly float ActionsWidth = (ActionButtonSize + ActionButtonGap) * ActionButtonCount;

        private static readonly GUIContent EmptyTitleContent = new(EmptyTitle);
        private static readonly GUIContent EmptyBodyContent = new(EmptyBody);
        private static readonly GUIContent EmptyEntryLabelContent = new(EmptyEntryLabel);
        private static readonly GUIContent ClearButtonContent = new("Clear history");
        private static readonly GUIContent CopySelectionButtonContent = new("Copy Selection", CopySelectionTooltip);
        private static readonly GUIContent PasteButtonContent = new(string.Empty, PasteTooltip);
        private static readonly GUIContent DeleteButtonContent = new(string.Empty, DeleteTooltip);

        private static readonly GUILayoutOption[] ExpandWidthOptions = { GUILayout.ExpandWidth(true) };

        private static GUIStyle _actionButtonStyle;
        private static GUIStyle _emptyTitleStyle;
        private static GUIStyle _emptyBodyStyle;
        private static GUIStyle _iconButtonStyle;
        private static GUIContent _pinnedIconContent;
        private static GUIContent _unpinnedIconContent;
        private static Texture _pasteIcon;
        private static Texture _deleteIcon;
        private static bool _hasBuiltStyles;
        private static bool _isStyleDark;

        private readonly List<string> _timeLabels = new();
        private readonly List<GUIContent> _labelContents = new();

        private Vector2 _scroll;
        private int _hoveredIndex = NoIndex;
        private int _animatedActionsIndex = NoIndex;
        private int _pendingRemovalIndex = NoIndex;
        private int _pendingPinIndex = NoIndex;
        private int _timeLabelsVersion = NoIndex;
        private float _actionsAmount;
        private float _deltaTime;
        private double _lastLayoutTime;
        private double _nextTimeLabelRefresh;
        private bool _isMouseOverList;

        #endregion

        #region Property

        private static List<KClipboardGameObjectsData.HistoryEntry> Entries => KClipboardGameObjects.EnsureData().entries;

        private static Color RowEvenColor => IsDarkTheme ? RowEvenColorDark : RowEvenColorLight;

        private static Color RowOddColor => IsDarkTheme ? RowOddColorDark : RowOddColorLight;

        private static Color ActionIconColor => IsDarkTheme ? ActionIconColorDark : ActionIconColorLight;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            KClipboardGameObjects.EnsureData();

            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            BuildStyles();
            RefreshTimeLabels();
            UpdateActionsAnimation();

            DrawToolbar();

            var toolbarHeight = EditorStyles.toolbar.fixedHeight;

            DrawBody(new Rect(0f, toolbarHeight, position.width, position.height - toolbarHeight));

            ApplyPendingPin();
            ApplyPendingRemoval();
            RepaintOnHoverChange();
        }

        private void OnSelectionChange() => Repaint();

        #endregion

        #region Drawing

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            SetGUIEnabled(Selection.gameObjects.Length > 0);

            if (GUILayout.Button(CopySelectionButtonContent, EditorStyles.toolbarButton))
                CopySelection();

            ResetGUIEnabled();

            GUILayout.FlexibleSpace();

            SetGUIEnabled(KClipboardGameObjects.EnsureData().HasUnpinned());

            if (GUILayout.Button(ClearButtonContent, EditorStyles.toolbarButton))
                KClipboardGameObjects.ClearHistory();

            ResetGUIEnabled();

            EditorGUILayout.EndHorizontal();
        }

        private static void CopySelection()
        {
            if (KClipboardGameObjects.CopySelectionToHistory(Selection.gameObjects, out var message)) return;

            Debug.LogError(string.Format(PasteFailureLogFormat, message));
        }

        private void DrawBody(Rect rect)
        {
            this._isMouseOverList = rect.IsHovered();

            DrawList(rect);
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
                DrawRow(entries[i], this._timeLabels[i], this._labelContents[i], i);
        }

        private void DrawRow(KClipboardGameObjectsData.HistoryEntry entry, string timeLabel, GUIContent labelContent, int index)
        {
            var rowRect = GUILayoutUtility.GetRect(0f, RowHeight, ExpandWidthOptions);
            var actionsAmount = index == this._animatedActionsIndex ? this._actionsAmount : 0f;

            DrawRowBackground(rowRect, index);

            if (CurEvent.IsRepaint && this._isMouseOverList && rowRect.IsHovered())
                this._hoveredIndex = index;

            var contentRect = new Rect(rowRect.x + Padding, rowRect.y + RowPadding, rowRect.width - Padding * 2f, rowRect.height - RowPadding * 2f);

            contentRect.width -= ActionsWidth * actionsAmount;

            DrawEntry(contentRect, entry, timeLabel, labelContent, index);
            DrawActionButtons(rowRect, entry, index, actionsAmount);
        }

        private static void DrawRowBackground(Rect rowRect, int index)
        {
            if (!CurEvent.IsRepaint) return;

            rowRect.Draw(index % 2 == 0 ? RowEvenColor : RowOddColor);
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

        private static GUIContent BuildLabelContent(KClipboardGameObjectsData.HistoryEntry entry)
        {
            if (entry.rootNames == null || entry.rootNames.Length == 0) return EmptyEntryLabelContent;

            if (entry.rootNames.Length == 1) return new GUIContent(entry.rootNames[0]);

            return new GUIContent(string.Format(MultiRootLabelFormat, entry.rootNames.Length), string.Join(MultiRootTooltipSeparator, entry.rootNames));
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

        #endregion

        #region Entry Action

        private static void PasteEntry(KClipboardGameObjectsData.HistoryEntry entry)
        {
            if (KClipboardGameObjects.TryPaste(entry, out var message)) return;

            Debug.LogError(string.Format(PasteFailureLogFormat, message));
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
            var hasDataChanged = this._timeLabelsVersion != data.Version;

            if (!hasDataChanged && EditorApplication.timeSinceStartup < this._nextTimeLabelRefresh) return;

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
            _pasteIcon = EditorIcons.GetIcon(PasteIconName);
            _deleteIcon = EditorIcons.GetIcon(DeleteIconName);
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
