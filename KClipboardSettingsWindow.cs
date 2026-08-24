#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Kingfisher.KClipboard
{
    internal class KClipboardSettingsWindow : EditorWindow
    {
        #region Field

        private const string MenuPath = "Tools/Kingfisher/KClipboard Setting";
        private const string WindowTitle = "KClipboard";
        private const int MenuPriority = 911;

        private const string KSettingsWindowTypeName = "Kingfisher.KSetting.KSettingsWindow, Kingfisher.KSetting";
        private const string OpenMethodName = "Open";
        private const string RemoveMenuItemMethodName = "RemoveMenuItem";
        private const string MenuItemExistsMethodName = "MenuItemExists";
        private const string RebuildAllMenusMethodName = "RebuildAllMenus";

        private const BindingFlags InternalStaticMemberFlags = BindingFlags.NonPublic | BindingFlags.Static;

        private const string DisabledPropertyName = "PluginDisabled";
        private const string LayoutFieldName = "SettingsLayout";
        private const string DeleteDataMethodName = "DeleteData";
        private const string OpenToolMethodName = "OpenTool";
        private const string DataPathPropertyName = "DataPath";
        private const string KeyPrefixFieldName = "KeyPrefix";
        private const string KeyFieldSuffix = "Key";

        private const BindingFlags StaticMemberFlags = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags KeyFieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private const string OtherSectionTitle = "Other";
        private const string EnabledLabel = "Enabled";
        private const string EnabledSuffix = "Enabled";
        private const char WordSeparator = ' ';

        private const char SectionMarker = '#';
        private const char SliderMarker = '~';
        private const char ChoiceMarker = '*';
        private const char ColorMarker = '&';
        private const char LabelSeparator = '|';

        private const int ColorNameIndex = 0;
        private const int ColorLabelIndex = 1;
        private const int ColorPartCount = 2;

        private const int MarkerLength = 1;
        private const int SeparatorLength = 1;
        private const int NotFoundIndex = -1;
        private const int SliderPartCount = 4;
        private const int SliderNameIndex = 0;
        private const int SliderLabelIndex = 1;
        private const int SliderMinIndex = 2;
        private const int SliderMaxIndex = 3;

        private const string LegacyKeyPrefix = "v";
        private const string CurrentKeyInfix = "-kingfisher-";
        private const string LegacyKeyInfix = "-vtools-";
        private const int LegacyKeyOffset = 1;

        private const char PathSeparator = '/';
        private const char WindowsPathSeparator = '\\';
        private const string DataFolderName = ".KData";
        private const string AssetsFolderMarker = "/Assets";

        private const string DeleteDataTitleFormat = "Delete {0} data?";
        private const string DeleteDataEmptyBodyFormat = "{0} has nothing saved in {1} right now.\n\nIts in-memory copy will still be cleared.";
        private const string DeleteDataBodyFormat = "This permanently deletes:\n\n{0}\n\nfrom {1}. It cannot be undone.";
        private const string ResetSettingsTitleFormat = "Reset {0} settings?";

        private const string ResetSettingsBodyFormat = "This puts every {0} setting back to its default.\n\n" +
                                                       "Saved data in .KData is left alone. Settings are stored per project and per machine, so both copies are cleared, and scripts will reload.";

        private const string FileSeparator = "\n";
        private const string DeleteConfirmLabel = "Delete";
        private const string ResetConfirmLabel = "Reset";
        private const string CancelLabel = "Cancel";
        private const string BreadcrumbRoot = "Tools  ›";

        private const float MinWindowWidth = 400f;
        private const float MinWindowHeight = 360f;
        private const float FooterHeight = 48f;
        private const float HeaderHeight = 34f;
        private const float Padding = 10f;
        private const float DividerThickness = 1f;
        private const float ContentIndent = 22f;
        private const float ContentTopSpacing = 6f;
        private const float ContentBottomSpacing = 10f;
        private const float FirstSectionTopSpacing = 0f;
        private const float SectionTopSpacing = 16f;
        private const float SectionHeaderHeight = 18f;
        private const float SectionHeaderGap = 6f;
        private const float SectionRuleGap = 8f;
        private const float MasterToggleGap = 12f;
        private const float RowSpacing = 6f;
        private const float SliderLabelWidth = 130f;
        private const float ButtonHeight = 28f;
        private const float ButtonGap = 8f;
        private const float DeleteButtonWidth = 112f;
        private const float ResetButtonWidth = 78f;
        private const float OpenButtonWidth = 78f;
        private const float RevealButtonWidth = 78f;
        private const float BreadcrumbGap = 6f;

        private const float BoxSize = 16f;
        private const float BoxRadius = 3f;
        private const float BoxBorder = 1f;
        private const float BoxLabelGap = 6f;
        private const float TickThickness = 1.5f;
        private const float TickShortArm = 4f;
        private const float TickLongArm = 10f;
        private const float TickRotation = -45f;
        private const float TickShiftX = 1f;
        private const float DotSize = 6f;
        private const float DisabledAlpha = .5f;

        private const float BreadcrumbRootAlpha = .5f;
        private const float SectionTextAlpha = .6f;
        private const float FooterTextAlpha = .45f;

        private const double LiveRepaintIntervalSeconds = .05;

        private static readonly float RowHeight = EditorGUIUtility.singleLineHeight + RowSpacing;
        private static readonly float RowInset = RowSpacing * .5f;

        private static readonly Vector2 TickCenterOffset = new((TickShortArm - TickLongArm * 2f) * .25f, (TickShortArm - TickThickness * 2f) * .25f);

        private static readonly Color DarkWindowColor = GetGreyscale(.22f);
        private static readonly Color LightWindowColor = GetGreyscale(.78f);
        private static readonly Color DarkFooterColor = GetGreyscale(.19f);
        private static readonly Color LightFooterColor = GetGreyscale(.735f);
        private static readonly Color DarkDividerColor = GetGreyscale(.13f);
        private static readonly Color LightDividerColor = GetGreyscale(.6f);
        private static readonly Color DarkSettingTextTint = new(.9f, .95f, 1.12f);
        private static readonly Color AccentColor = new(.208f, .455f, .941f);
        private static readonly Color DarkBoxColor = GetGreyscale(.16f);
        private static readonly Color LightBoxColor = GetGreyscale(.98f);
        private static readonly Color DarkBoxBorderColor = GetGreyscale(.44f);
        private static readonly Color LightBoxBorderColor = GetGreyscale(.55f);
        private static readonly Color DarkSettingTextColor = new(.72f, .75f, .83f);
        private static readonly Color LightSettingTextColor = new(.13f, .15f, .2f);
        private static readonly Color DarkDangerTintColor = new(1.35f, .62f, .58f);
        private static readonly Color LightDangerTintColor = new(1.3f, .72f, .68f);
        private static readonly Color DarkDangerTextColor = new(1f, .78f, .75f);
        private static readonly Color LightDangerTextColor = new(.42f, .06f, .04f);

        private static readonly GUIContent BreadcrumbRootContent = new(BreadcrumbRoot);
        private static readonly GUIContent NoSettingsContent = new("This tool has no settings.");
        private static readonly GUIContent DeleteDataButtonContent = new("Delete data");
        private static readonly GUIContent ResetButtonContent = new("Reset");
        private static readonly GUIContent OpenButtonContent = new("Open", "Open this tool's own window");
        private static readonly GUIContent RevealButtonContent = new("Reveal");

        private static readonly GUILayoutOption[] ExpandWidthOptions = { GUILayout.ExpandWidth(true) };
        private static readonly Dictionary<string, (GUIContent Content, float Width, int StylesVersion)> SectionTitleContents = new();
        private static readonly List<string> StoredDataFiles = new();
        private static readonly List<SettingsSection> Sections = new();

        private static Color _previousContentColor;

        private static GUIStyle _breadcrumbRootStyle;
        private static GUIStyle _breadcrumbStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _dangerButtonStyle;
        private static GUIStyle _sectionStyle;
        private static GUIStyle _settingLabelStyle;
        private static GUIStyle _footerStyle;
        private static float _breadcrumbRootWidth;
        private static bool _hasBuiltStyles;
        private static bool _isStyleDark;
        private static int _stylesVersion;
        private static double _nextRepaintAllViewsTime;

        private static bool _isModelBuilt;
        private static SettingsSetting _disabledSetting;
        private static MethodInfo _deleteDataMethod;
        private static MethodInfo _openToolMethod;
        private static PropertyInfo _dataPathProperty;
        private static string _sharedDataFolderPath;

        private Vector2 _scroll;
        private bool _hasDataFolder;

        #endregion

        #region Property

        private static bool IsDark => EditorGUIUtility.isProSkin;

        private static bool IsDragEnd => Event.current.rawType == EventType.MouseUp;

        private static Color WindowBackground => IsDark ? DarkWindowColor : LightWindowColor;

        private static Color FooterBackground => IsDark ? DarkFooterColor : LightFooterColor;

        private static Color DividerColor => IsDark ? DarkDividerColor : LightDividerColor;

        private static Color DangerTint => IsDark ? DarkDangerTintColor : LightDangerTintColor;

        private static Color SettingTextTint => IsDark ? DarkSettingTextTint : Color.white;

        private static Color SettingTextColor => IsDark ? DarkSettingTextColor : LightSettingTextColor;

        private static Color BoxColor => IsDark ? DarkBoxColor : LightBoxColor;

        private static Color BoxBorderColor => IsDark ? DarkBoxBorderColor : LightBoxBorderColor;

        private static bool CanDeleteData => _deleteDataMethod != null;

        private static bool CanOpenTool => _openToolMethod != null;

        private static bool IsToolDisabled => _disabledSetting is { Value: true };

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            BuildModel();

            this._hasDataFolder = Directory.Exists(_sharedDataFolderPath);
        }

        private void OnGUI()
        {
            BuildStyles();

            EditorGUI.DrawRect(new Rect(0f, 0f, position.width, position.height), WindowBackground);

            var bodyHeight = position.height - FooterHeight;

            DrawContent(new Rect(0f, 0f, position.width, bodyHeight));
            DrawFooter(new Rect(0f, bodyHeight, position.width, FooterHeight));
        }

        private void OnFocus() => this._hasDataFolder = Directory.Exists(_sharedDataFolderPath);

        #endregion

        #region Drawing

        private void DrawContent(Rect rect)
        {
            GUILayout.BeginArea(rect);

            DrawBreadcrumb(rect.width);

            this._scroll = GUILayout.BeginScrollView(this._scroll);

            GUILayout.Space(ContentTopSpacing);

            DrawMasterToggle();
            DrawSections();

            GUILayout.Space(ContentBottomSpacing);

            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private static void DrawBreadcrumb(float width)
        {
            var rect = GUILayoutUtility.GetRect(width, HeaderHeight);
            var rootRect = new Rect(rect.x + Padding, rect.y, _breadcrumbRootWidth, rect.height);

            GUI.Label(rootRect, BreadcrumbRootContent, _breadcrumbRootStyle);

            var nameX = rootRect.xMax + BreadcrumbGap;

            GUI.Label(new Rect(nameX, rect.y, rect.xMax - Padding - nameX, rect.height), WindowTitle, _breadcrumbStyle);
        }

        private static void DrawMasterToggle()
        {
            if (_disabledSetting == null) return;

            var isEnabled = !_disabledSetting.Value;
            var newIsEnabled = DrawCheckbox(GetRowRect(), isEnabled, EnabledLabel);

            GUILayout.Space(MasterToggleGap);

            if (newIsEnabled == isEnabled) return;

            Apply(_disabledSetting, !newIsEnabled);
        }

        private static void DrawSections()
        {
            if (Sections.Count == 0)
            {
                DrawNotice(NoSettingsContent);

                return;
            }

            EditorGUI.BeginDisabledGroup(IsToolDisabled);

            for (var i = 0; i < Sections.Count; i++)
            {
                DrawSection(Sections[i], i);
            }

            EditorGUI.EndDisabledGroup();
        }

        private static void DrawSection(SettingsSection section, int index)
        {
            if (section.Title != null)
            {
                DrawSectionHeader(section.Title, index == 0 ? FirstSectionTopSpacing : SectionTopSpacing);
            }

            for (var i = 0; i < section.Entries.Count; i++)
            {
                DrawEntry(section.Entries[i]);
            }
        }

        private static void DrawSectionHeader(string title, float topSpacing)
        {
            GUILayout.Space(topSpacing);

            var rect = GUILayoutUtility.GetRect(0f, SectionHeaderHeight, ExpandWidthOptions);
            var content = GetSectionTitleContent(title, out var width);
            var labelRect = new Rect(rect.x + Padding, rect.y, width, rect.height);

            GUI.Label(labelRect, content, _sectionStyle);

            var ruleX = labelRect.xMax + SectionRuleGap;
            var ruleWidth = rect.xMax - Padding - ruleX;

            if (ruleWidth > 0f)
            {
                EditorGUI.DrawRect(new Rect(ruleX, rect.y + (rect.height - DividerThickness) * .5f, ruleWidth, DividerThickness), DividerColor);
            }

            GUILayout.Space(SectionHeaderGap);
        }

        private static void DrawEntry(SettingsEntry entry)
        {
            if (entry.IsChoice)
            {
                DrawChoice(entry);

                return;
            }

            var setting = entry.Settings[0];

            if (setting.IsSlider)
            {
                DrawSliderRow(setting);

                return;
            }

            if (setting.IsColor)
            {
                DrawColorRow(setting);

                return;
            }

            DrawToggleRow(setting);
        }

        private static void DrawChoice(SettingsEntry entry)
        {
            for (var i = 0; i < entry.Settings.Count; i++)
            {
                var option = entry.Settings[i];

                if (!DrawRadio(GetRowRect(), option.Value, option.Label)) continue;

                if (option.Value) continue;

                Apply(option, true);
            }
        }

        private static void DrawToggleRow(SettingsSetting setting)
        {
            var newIsOn = DrawCheckbox(GetRowRect(), setting.Value, setting.Label);

            if (newIsOn == setting.Value) return;

            Apply(setting, newIsOn);
        }

        private static void DrawColorRow(SettingsSetting setting)
        {
            var rect = GetRowRect();
            var value = setting.ColorValue;
            var previousLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = SliderLabelWidth;

            BeginLabelTint();

            var newValue = EditorGUI.ColorField(rect, setting.Label, value);

            EndLabelTint();

            EditorGUIUtility.labelWidth = previousLabelWidth;

            if (newValue == value) return;

            setting.ColorValue = newValue;

            RequestLiveRepaint();
        }

        private static void DrawSliderRow(SettingsSetting setting)
        {
            var rect = GetRowRect();
            var value = setting.SliderValue;
            var previousLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = SliderLabelWidth;

            BeginLabelTint();

            var newValue = EditorGUI.Slider(rect, setting.Label, value, setting.Min, setting.Max);

            EndLabelTint();

            EditorGUIUtility.labelWidth = previousLabelWidth;

            if (Mathf.Approximately(newValue, value)) return;

            setting.SliderValue = newValue;

            RequestLiveRepaint();
        }

        private static void RequestLiveRepaint()
        {
            var now = EditorApplication.timeSinceStartup;

            if (!IsDragEnd && now < _nextRepaintAllViewsTime) return;

            _nextRepaintAllViewsTime = now + LiveRepaintIntervalSeconds;

            InternalEditorUtility.RepaintAllViews();
        }

        private static void DrawNotice(GUIContent content) => GUI.Label(GetRowRect(), content, _footerStyle);

        private static Rect GetRowRect()
        {
            var rect = GUILayoutUtility.GetRect(0f, RowHeight, ExpandWidthOptions);

            return new Rect(rect.x + Padding + ContentIndent, rect.y + RowInset, rect.width - Padding * 2f - ContentIndent, EditorGUIUtility.singleLineHeight);
        }

        private void DrawFooter(Rect rect)
        {
            EditorGUI.DrawRect(rect, FooterBackground);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, DividerThickness), DividerColor);

            var buttonY = rect.y + (rect.height - ButtonHeight) * .5f;

            var actionsLeft = DrawToolActions(rect, buttonY);

            DrawDataPath(rect, buttonY, actionsLeft);
        }

        private float DrawToolActions(Rect rect, float buttonY)
        {
            var right = rect.xMax - Padding;

            if (CanDeleteData)
            {
                var deleteRect = new Rect(right - DeleteButtonWidth, buttonY, DeleteButtonWidth, ButtonHeight);
                var previousBackground = GUI.backgroundColor;

                GUI.backgroundColor = DangerTint;

                if (GUI.Button(deleteRect, DeleteDataButtonContent, _dangerButtonStyle))
                {
                    ConfirmDeleteData();
                }

                GUI.backgroundColor = previousBackground;

                right = deleteRect.x - ButtonGap;
            }

            var resetRect = new Rect(right - ResetButtonWidth, buttonY, ResetButtonWidth, ButtonHeight);

            if (GUI.Button(resetRect, ResetButtonContent, _buttonStyle))
            {
                ConfirmResetSettings();
            }

            if (!CanOpenTool) return resetRect.x;

            var openRect = new Rect(resetRect.x - ButtonGap - OpenButtonWidth, buttonY, OpenButtonWidth, ButtonHeight);

            if (GUI.Button(openRect, OpenButtonContent, _buttonStyle))
            {
                EditorApplication.delayCall += InvokeOpenTool;
            }

            return openRect.x;
        }

        private void DrawDataPath(Rect rect, float buttonY, float actionsLeft)
        {
            var revealRect = new Rect(rect.x + Padding, buttonY, RevealButtonWidth, ButtonHeight);

            EditorGUI.BeginDisabledGroup(!this._hasDataFolder);

            if (GUI.Button(revealRect, RevealButtonContent, _buttonStyle))
            {
                EditorUtility.RevealInFinder(_sharedDataFolderPath);
            }

            EditorGUI.EndDisabledGroup();

            var labelX = revealRect.xMax + ButtonGap;
            var labelWidth = actionsLeft - ButtonGap - labelX;

            if (labelWidth <= 0f) return;

            GUI.Label(new Rect(labelX, rect.y, labelWidth, rect.height), DataFolderName, _footerStyle);
        }

        #endregion

        #region Setting Control

        private static bool DrawCheckbox(Rect rect, bool isOn, string label)
        {
            var newIsOn = GUI.Toggle(rect, isOn, label, _settingLabelStyle);
            var boxRect = GetBoxRect(rect);

            DrawBox(boxRect, isOn, BoxRadius);

            if (!isOn) return newIsOn;

            DrawTick(boxRect);

            return newIsOn;
        }

        private static bool DrawRadio(Rect rect, bool isOn, string label)
        {
            var newIsOn = GUI.Toggle(rect, isOn, label, _settingLabelStyle);
            var boxRect = GetBoxRect(rect);

            DrawBox(boxRect, isOn, BoxSize * .5f);

            if (!isOn) return newIsOn;

            var dotRect = new Rect(boxRect.center.x - DotSize * .5f, boxRect.center.y - DotSize * .5f, DotSize, DotSize);

            DrawRounded(dotRect, Dim(Color.white), 0f, DotSize * .5f);

            return newIsOn;
        }

        private static void DrawBox(Rect rect, bool isOn, float radius)
        {
            if (isOn)
            {
                DrawRounded(rect, Dim(AccentColor), 0f, radius);

                return;
            }

            DrawRounded(rect, Dim(BoxColor), 0f, radius);
            DrawRounded(rect, Dim(BoxBorderColor), BoxBorder, radius);
        }

        private static void DrawTick(Rect box)
        {
            if (Event.current.type != EventType.Repaint) return;

            var pivot = new Vector2(box.center.x + TickShiftX, box.center.y);
            var previousMatrix = GUI.matrix;
            var color = Dim(Color.white);
            var corner = pivot + TickCenterOffset;

            GUIUtility.RotateAroundPivot(TickRotation, pivot);

            EditorGUI.DrawRect(new Rect(corner.x, corner.y - TickShortArm, TickThickness, TickShortArm + TickThickness), color);
            EditorGUI.DrawRect(new Rect(corner.x, corner.y, TickLongArm, TickThickness), color);

            GUI.matrix = previousMatrix;
        }

        private static void DrawRounded(Rect rect, Color color, float borderWidth, float radius)
        {
            if (Event.current.type != EventType.Repaint) return;

            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, borderWidth, radius);
        }

        private static void BeginLabelTint()
        {
            _previousContentColor = GUI.contentColor;

            GUI.contentColor *= SettingTextTint;
        }

        private static void EndLabelTint() => GUI.contentColor = _previousContentColor;

        private static Rect GetBoxRect(Rect rect) => new(rect.x, rect.y + (rect.height - BoxSize) * .5f, BoxSize, BoxSize);

        private static Color Dim(Color color) => GUI.enabled ? color : new Color(color.r, color.g, color.b, color.a * DisabledAlpha);

        #endregion

        #region Model Building

        private static void BuildModel()
        {
            if (_isModelBuilt) return;

            _isModelBuilt = true;

            var menuType = typeof(KClipboardMenu);

            _deleteDataMethod = menuType.GetMethod(DeleteDataMethodName, StaticMemberFlags, null, Type.EmptyTypes, null);
            _openToolMethod = menuType.GetMethod(OpenToolMethodName, StaticMemberFlags, null, Type.EmptyTypes, null);
            _dataPathProperty = menuType.GetProperty(DataPathPropertyName, StaticMemberFlags);
            _sharedDataFolderPath = GetSharedDataFolderPath();

            var propertiesByName = new Dictionary<string, PropertyInfo>();
            var declarationOrder = new List<string>();

            foreach (var property in menuType.GetProperties(StaticMemberFlags))
            {
                if (!property.CanRead || !property.CanWrite) continue;

                var isBool = property.PropertyType == typeof(bool);

                if (!isBool && property.PropertyType != typeof(float) && property.PropertyType != typeof(Color)) continue;

                if (isBool && property.Name == DisabledPropertyName)
                {
                    _disabledSetting = new SettingsSetting(property);

                    continue;
                }

                propertiesByName[property.Name] = property;

                if (isBool)
                {
                    declarationOrder.Add(property.Name);
                }
            }

            BuildSections(menuType, propertiesByName, declarationOrder);
        }

        private static void BuildSections(Type menuType, Dictionary<string, PropertyInfo> propertiesByName, List<string> declarationOrder)
        {
            var layout = menuType.GetField(LayoutFieldName, StaticMemberFlags)?.GetValue(null) as string[];

            if (layout == null)
            {
                AddSection(null, declarationOrder, propertiesByName);

                return;
            }

            var placed = new HashSet<string>();

            ParseLayout(layout, propertiesByName, placed);
            AddSection(OtherSectionTitle, GetLeftovers(declarationOrder, placed), propertiesByName);
        }

        private static void ParseLayout(string[] layout, Dictionary<string, PropertyInfo> propertiesByName, HashSet<string> placed)
        {
            var section = new SettingsSection(null);

            SettingsEntry choice = null;

            for (var i = 0; i < layout.Length; i++)
            {
                var line = layout[i];

                if (string.IsNullOrEmpty(line)) continue;

                if (line[0] == SectionMarker)
                {
                    AddFilledSection(section);

                    section = new SettingsSection(line.Substring(MarkerLength).Trim());
                    choice = null;

                    continue;
                }

                if (line[0] == SliderMarker)
                {
                    AddSlider(section, line.Substring(MarkerLength), propertiesByName, placed);

                    choice = null;

                    continue;
                }

                if (line[0] == ColorMarker)
                {
                    AddColor(section, line.Substring(MarkerLength), propertiesByName, placed);

                    choice = null;

                    continue;
                }

                var isChoice = line[0] == ChoiceMarker;
                var body = SplitLabel(isChoice ? line.Substring(MarkerLength).Trim() : line.Trim(), out var label);

                if (!propertiesByName.TryGetValue(body, out var property)) continue;

                if (property.PropertyType != typeof(bool)) continue;

                placed.Add(body);

                var setting = new SettingsSetting(property, label);

                if (!isChoice)
                {
                    section.Entries.Add(new SettingsEntry(setting));
                    choice = null;

                    continue;
                }

                if (choice == null)
                {
                    choice = new SettingsEntry();

                    section.Entries.Add(choice);
                }

                choice.Settings.Add(setting);
            }

            AddFilledSection(section);
        }

        private static string SplitLabel(string body, out string label)
        {
            var separator = body.IndexOf(LabelSeparator);

            if (separator == NotFoundIndex)
            {
                label = null;

                return body;
            }

            label = body.Substring(separator + SeparatorLength).Trim();

            return body.Substring(0, separator).Trim();
        }

        private static List<string> GetLeftovers(List<string> declarationOrder, HashSet<string> placed)
        {
            var leftovers = new List<string>();

            for (var i = 0; i < declarationOrder.Count; i++)
            {
                if (placed.Contains(declarationOrder[i])) continue;

                leftovers.Add(declarationOrder[i]);
            }

            return leftovers;
        }

        private static void AddSlider(SettingsSection section, string body, Dictionary<string, PropertyInfo> propertiesByName, HashSet<string> placed)
        {
            var parts = body.Split(LabelSeparator);

            if (parts.Length < SliderPartCount) return;

            var propertyName = parts[SliderNameIndex].Trim();

            if (!propertiesByName.TryGetValue(propertyName, out var property)) return;

            if (!float.TryParse(parts[SliderMinIndex].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var min)) return;

            if (!float.TryParse(parts[SliderMaxIndex].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var max)) return;

            placed.Add(propertyName);

            section.Entries.Add(new SettingsEntry(new SettingsSetting(property, parts[SliderLabelIndex].Trim(), min, max)));
        }

        private static void AddColor(SettingsSection section, string body, Dictionary<string, PropertyInfo> propertiesByName, HashSet<string> placed)
        {
            var parts = body.Split(LabelSeparator);

            var propertyName = parts[ColorNameIndex].Trim();
            var label = parts.Length >= ColorPartCount ? parts[ColorLabelIndex].Trim() : null;

            if (!propertiesByName.TryGetValue(propertyName, out var property)) return;

            if (property.PropertyType != typeof(Color)) return;

            placed.Add(propertyName);

            section.Entries.Add(new SettingsEntry(new SettingsSetting(property, label, isColor: true)));
        }

        private static void AddSection(string title, List<string> propertyNames, Dictionary<string, PropertyInfo> propertiesByName)
        {
            if (propertyNames.Count == 0) return;

            var section = new SettingsSection(title);

            for (var i = 0; i < propertyNames.Count; i++)
            {
                section.Entries.Add(new SettingsEntry(new SettingsSetting(propertiesByName[propertyNames[i]])));
            }

            Sections.Add(section);
        }

        private static void AddFilledSection(SettingsSection section)
        {
            if (section.Entries.Count == 0) return;

            Sections.Add(section);
        }

        private static string GetSharedDataFolderPath()
        {
            var assetsPath = Application.dataPath;
            var rootLength = assetsPath.Length - AssetsFolderMarker.Length;

            return assetsPath.Substring(0, rootLength) + PathSeparator + DataFolderName;
        }

        #endregion

        #region Confirmation Dialog

        private void ConfirmDeleteData()
        {
            CollectStoredDataFiles();

            var folder = GetDisplayDataFolder();

            var body = StoredDataFiles.Count == 0
                ? string.Format(DeleteDataEmptyBodyFormat, WindowTitle, folder)
                : string.Format(DeleteDataBodyFormat, string.Join(FileSeparator, StoredDataFiles), folder);

            if (!EditorUtility.DisplayDialog(string.Format(DeleteDataTitleFormat, WindowTitle), body, DeleteConfirmLabel, CancelLabel)) return;

            EditorApplication.delayCall += () =>
            {
                _deleteDataMethod?.Invoke(null, null);

                this._hasDataFolder = Directory.Exists(_sharedDataFolderPath);

                Repaint();
            };
        }

        private static void ConfirmResetSettings()
        {
            var body = string.Format(ResetSettingsBodyFormat, WindowTitle);

            if (!EditorUtility.DisplayDialog(string.Format(ResetSettingsTitleFormat, WindowTitle), body, ResetConfirmLabel, CancelLabel)) return;

            EditorApplication.delayCall += () =>
            {
                ResetStoredKeys();

                EditorUtility.RequestScriptReload();
            };
        }

        private static void InvokeOpenTool() => _openToolMethod?.Invoke(null, null);

        private static string GetDisplayDataFolder()
        {
            if (_dataPathProperty?.GetValue(null) is string path)
            {
                return Path.GetDirectoryName(path)?.Replace(WindowsPathSeparator, PathSeparator) ?? DataFolderName;
            }

            return DataFolderName;
        }

        private static void CollectStoredDataFiles()
        {
            StoredDataFiles.Clear();

            if (_dataPathProperty?.GetValue(null) is string path)
            {
                if (File.Exists(path))
                {
                    StoredDataFiles.Add(Path.GetFileName(path));
                }

                return;
            }

            if (!Directory.Exists(_sharedDataFolderPath)) return;

            var filePaths = Directory.GetFiles(_sharedDataFolderPath);

            for (var i = 0; i < filePaths.Length; i++)
            {
                var fileName = Path.GetFileName(filePaths[i]);

                if (!fileName.StartsWith(WindowTitle, StringComparison.Ordinal)) continue;

                StoredDataFiles.Add(fileName);
            }
        }

        #endregion

        #region Setting Reset

        // Every property backed by EditorPrefsCached has a "{PropertyName}Key" const alongside it (e.g.
        // MaxHistoryCount / MaxHistoryCountKey). This window only ever runs when K-Setting is
        // absent, so EditorPrefsCached always falls through to raw EditorPrefs - deleting every "*Key"
        // constant that starts with this tool's own KeyPrefix clears all of it, including keys (like a
        // choice group's backing int) that aren't exposed as a public bool/float/Color property.
        private static void ResetStoredKeys()
        {
            var menuType = typeof(KClipboardMenu);

            if (menuType.GetField(KeyPrefixFieldName, KeyFieldFlags)?.GetRawConstantValue() is not string prefix) return;

            var fields = menuType.GetFields(KeyFieldFlags);

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];

                if (!field.IsLiteral || field.FieldType != typeof(string)) continue;

                if (!field.Name.EndsWith(KeyFieldSuffix, StringComparison.Ordinal)) continue;

                if (field.GetRawConstantValue() is not string key) continue;

                if (!key.StartsWith(prefix, StringComparison.Ordinal)) continue;

                DeleteEditorPrefsKey(key);
            }
        }

        private static void DeleteEditorPrefsKey(string key)
        {
            EditorPrefs.DeleteKey(key);

            var previous = key.Substring(LegacyKeyOffset);

            EditorPrefs.DeleteKey(previous);
            EditorPrefs.DeleteKey(LegacyKeyPrefix + previous.Replace(CurrentKeyInfix, LegacyKeyInfix));
        }

        #endregion

        #region Style

        private static void BuildStyles()
        {
            if (_hasBuiltStyles && _isStyleDark == IsDark) return;

            _hasBuiltStyles = true;
            _isStyleDark = IsDark;
            _stylesVersion++;

            _breadcrumbRootStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
            _breadcrumbRootStyle.normal.textColor = GetSkinTextColor(BreadcrumbRootAlpha);
            _breadcrumbRootWidth = _breadcrumbRootStyle.CalcSize(BreadcrumbRootContent).x;

            _breadcrumbStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };

            _buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter };

            var dangerText = IsDark ? DarkDangerTextColor : LightDangerTextColor;

            _dangerButtonStyle = new GUIStyle(_buttonStyle);
            _dangerButtonStyle.normal.textColor = dangerText;
            _dangerButtonStyle.hover.textColor = dangerText;
            _dangerButtonStyle.active.textColor = dangerText;

            _sectionStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
            _sectionStyle.normal.textColor = GetSkinTextColor(SectionTextAlpha);

            _settingLabelStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
            _settingLabelStyle.padding.left = Mathf.RoundToInt(BoxSize + BoxLabelGap);

            SetTextColor(_settingLabelStyle, SettingTextColor);

            _footerStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
            _footerStyle.normal.textColor = GetSkinTextColor(FooterTextAlpha);
        }

        private static void SetTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.onNormal.textColor = color;
            style.hover.textColor = color;
            style.onHover.textColor = color;
            style.active.textColor = color;
            style.onActive.textColor = color;
            style.focused.textColor = color;
            style.onFocused.textColor = color;
        }

        private static Color GetSkinTextColor(float alpha) => GetGreyscale(IsDark ? 1f : 0f, alpha);

        private static Color GetGreyscale(float value, float alpha = 1f) => new(value, value, value, alpha);

        #endregion

        #region Method

        [MenuItem(MenuPath, false, MenuPriority)]
        public static void Open()
        {
            if (Type.GetType(KSettingsWindowTypeName)?.GetMethod(OpenMethodName, StaticMemberFlags) is { } openMethod)
            {
                openMethod.Invoke(null, null);

                return;
            }

            BuildModel();

            var window = GetWindow<KClipboardSettingsWindow>(utility: false, title: WindowTitle, focus: true);

            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        }

        [InitializeOnLoadMethod]
        private static void HideMenuItemWhenKSettingInstalled()
        {
            if (Type.GetType(KSettingsWindowTypeName) == null) return;

            EditorApplication.delayCall += () =>
            {
                var menuType = typeof(Menu);

                menuType.GetMethod(RemoveMenuItemMethodName, InternalStaticMemberFlags)?.Invoke(null, new object[] { MenuPath });

                var hasMenuItem = menuType.GetMethod(MenuItemExistsMethodName, InternalStaticMemberFlags)?.Invoke(null, new object[] { MenuPath });
                if (hasMenuItem is false) return;

                menuType.GetMethod(RebuildAllMenusMethodName, InternalStaticMemberFlags)?.Invoke(null, null);
                InternalEditorUtility.ReloadWindowLayoutMenu();
            };
        }

        private static void Apply(SettingsSetting setting, bool isOn)
        {
            setting.Value = isOn;

            InternalEditorUtility.RepaintAllViews();
        }

        private static GUIContent GetSectionTitleContent(string title, out float width)
        {
            if (SectionTitleContents.TryGetValue(title, out var entry) && entry.StylesVersion == _stylesVersion)
            {
                width = entry.Width;

                return entry.Content;
            }

            var content = entry.Content ?? new GUIContent(title);

            width = _sectionStyle.CalcSize(content).x;

            SectionTitleContents[title] = (content, width, _stylesVersion);

            return content;
        }

        #endregion

        #region Nested Type

        private class SettingsSection
        {
            public string Title { get; }

            public List<SettingsEntry> Entries { get; } = new();

            public SettingsSection(string title) => Title = title;
        }

        private class SettingsEntry
        {
            private readonly bool _forcedChoice;

            public List<SettingsSetting> Settings { get; } = new();

            public bool IsChoice => Settings.Count > 1 || this._forcedChoice;

            public SettingsEntry(SettingsSetting single) => Settings.Add(single);

            public SettingsEntry() => this._forcedChoice = true;
        }

        private class SettingsSetting
        {
            private Func<bool> _getValue;
            private Action<bool> _setValue;
            private Func<float> _getSliderValue;
            private Action<float> _setSliderValue;
            private Func<Color> _getColorValue;
            private Action<Color> _setColorValue;

            public string Label { get; }

            public bool IsSlider { get; }

            public bool IsColor { get; }

            public float Min { get; }

            public float Max { get; }

            public Color ColorValue
            {
                get => this._getColorValue();
                set => this._setColorValue(value);
            }

            public bool Value
            {
                get => this._getValue();
                set => this._setValue(value);
            }

            public float SliderValue
            {
                get => this._getSliderValue();
                set => this._setSliderValue(value);
            }

            public SettingsSetting(PropertyInfo property, string label = null)
            {
                Label = label ?? Prettify(property.Name);

                BindProperty(property);
            }

            public SettingsSetting(PropertyInfo property, string label, bool isColor)
            {
                Label = label ?? Prettify(property.Name);
                IsColor = isColor;

                BindProperty(property);
            }

            public SettingsSetting(PropertyInfo property, string label, float min, float max)
            {
                Label = label ?? Prettify(property.Name);
                IsSlider = true;
                Min = min;
                Max = max;

                BindProperty(property);
            }

            private void BindProperty(PropertyInfo property)
            {
                if (property.PropertyType == typeof(bool))
                {
                    this._getValue = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), property.GetGetMethod());
                    this._setValue = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), property.GetSetMethod());

                    return;
                }

                if (property.PropertyType == typeof(float))
                {
                    this._getSliderValue = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), property.GetGetMethod());
                    this._setSliderValue = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), property.GetSetMethod());

                    return;
                }

                this._getColorValue = (Func<Color>)Delegate.CreateDelegate(typeof(Func<Color>), property.GetGetMethod());
                this._setColorValue = (Action<Color>)Delegate.CreateDelegate(typeof(Action<Color>), property.GetSetMethod());
            }

            private static string Prettify(string propertyName)
            {
                if (propertyName.EndsWith(EnabledSuffix, StringComparison.Ordinal))
                {
                    propertyName = propertyName.Substring(0, propertyName.Length - EnabledSuffix.Length);
                }

                var builder = new StringBuilder();

                for (var i = 0; i < propertyName.Length; i++)
                {
                    var character = propertyName[i];

                    if (i == 0)
                    {
                        builder.Append(char.ToUpperInvariant(character));

                        continue;
                    }

                    if (char.IsUpper(character) && !char.IsUpper(propertyName[i - 1]))
                    {
                        builder.Append(WordSeparator).Append(char.ToLowerInvariant(character));
                    }
                    else
                    {
                        builder.Append(character);
                    }
                }

                return builder.ToString();
            }
        }

        #endregion
    }
}
#endif
