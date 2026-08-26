#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static Kingfisher.KClipboard.Libs.KUtils;

namespace Kingfisher.KClipboard.Libs
{
    public static class KGUI
    {
        #region Field

        private const int MaxCachedLabelWidths = 1024;
        private const int InheritedLabelFontSize = 0;

        private const float DefaultSpacing = 6;

        private const int RoundedPixelsPerPoint = 2;

        private const float BlurPixelsPerPoint = .5f;
        private const int MinScaledBlurRadius = 1;
        private const int MaxScaledBlurRadius = 123;

        private const int GradientResolution = 256;
        private const int GradientThickness = 1;

        private const int CurtainDirectionCount = 4;
        private const int CurtainUpIndex = 0;
        private const int CurtainDownIndex = 1;
        private const int CurtainLeftIndex = 2;
        private const int CurtainRightIndex = 3;

        private const string PixelsPerPointPropertyName = "pixelsPerPoint";
        private const string EventCurrentFieldName = "s_Current";

        private static readonly GUIContent SharedContent = new();
        private static readonly Dictionary<(string, int, FontStyle), float> LabelWidths = new();

        private static readonly Dictionary<int, GUIStyle> RoundedStylesByCornerRadius = new();
        private static readonly Dictionary<(int, int), GUIStyle> BlurredStylesByTextureSize = new();

        private static readonly FieldInfo EventCurrentField = typeof(Event).GetField(EventCurrentFieldName, MaxBindingFlags);

        private static Texture2D[] _gradientTextures;

        private static bool _wasGuiEnabled = true;
        private static bool _isGuiColorModified;
        private static Color _defaultGuiColor;

        #endregion

        #region Property

        public static Rect LastRect => GUILayoutUtility.GetLastRect();

        public static bool IsDarkTheme => EditorGUIUtility.isProSkin;

        public static WrappedEvent CurEvent => new(Event.current ?? EventCurrentField?.GetValue(null) as Event);

        #endregion

        #region Label

        public static float GetLabelWidth(this string text)
        {
            if (text == null) return 0;

            var style = GUI.skin.label;
            var key = (text, style.fontSize, style.fontStyle);

            if (LabelWidths.TryGetValue(key, out var cached)) return cached;

            if (LabelWidths.Count > MaxCachedLabelWidths) LabelWidths.Clear();

            SharedContent.text = text;

            var width = style.CalcSize(SharedContent).x;

            SharedContent.text = null;

            return LabelWidths[key] = width;
        }

        public static float GetLabelWidth(this string text, int fontSize)
        {
            SetLabelFontSize(fontSize);

            var width = text.GetLabelWidth();

            ResetLabelStyle();

            return width;
        }

        public static float GetLabelWidth(this string text, bool isBold)
        {
            if (isBold)
                SetLabelBold();

            var width = text.GetLabelWidth();

            if (isBold)
                ResetLabelStyle();

            return width;
        }

        public static void SetLabelFontSize(int size) => GUI.skin.label.fontSize = size;

        public static void SetLabelBold() => GUI.skin.label.fontStyle = FontStyle.Bold;

        public static void SetLabelAlignmentCenter() => GUI.skin.label.alignment = TextAnchor.MiddleCenter;

        public static void ResetLabelStyle()
        {
            var style = GUI.skin.label;

            style.fontSize = InheritedLabelFontSize;
            style.fontStyle = FontStyle.Normal;
            style.alignment = TextAnchor.MiddleLeft;
        }

        #endregion

        #region GUI State

        public static void SetGUIEnabled(bool isEnabled)
        {
            _wasGuiEnabled = GUI.enabled;

            GUI.enabled = isEnabled;
        }

        public static void ResetGUIEnabled() => GUI.enabled = _wasGuiEnabled;

        public static void SetGUIColor(Color color)
        {
            if (!_isGuiColorModified)
                _defaultGuiColor = GUI.color;

            _isGuiColorModified = true;

            GUI.color = _defaultGuiColor * color;
        }

        public static void ResetGUIColor()
        {
            GUI.color = _isGuiColorModified ? _defaultGuiColor : Color.white;

            _isGuiColorModified = false;
        }

        #endregion

        #region Events

        public static WrappedEvent Wrap(this Event rawEvent) => new(rawEvent);

        public static bool IsHovered(this Rect rect)
        {
            var currentEvent = CurEvent;

            return !currentEvent.IsNull && rect.Contains(currentEvent.MousePosition);
        }

        #endregion

        #region Drawing

        public static Rect Draw(this Rect rect, Color color)
        {
            EditorGUI.DrawRect(rect, color);

            return rect;
        }

        public static Rect DrawWithRoundedCorners(this Rect rect, Color color, int cornerRadius)
        {
            if (!CurEvent.IsRepaint) return rect;

            cornerRadius = cornerRadius.Min((rect.height / 2).FloorToInt()).Min((rect.width / 2).FloorToInt());

            if (cornerRadius <= 0) return rect.Draw(color);

            if (!RoundedStylesByCornerRadius.TryGetValue(cornerRadius, out var style))
                RoundedStylesByCornerRadius[cornerRadius] = style = CreateRoundedStyle(cornerRadius);

            SetGUIColor(color);

            style.Draw(rect, false, false, false, false);

            ResetGUIColor();

            return rect;
        }

        public static Rect DrawWithRoundedCorners(this Rect rect, Color color, float cornerRadius) => rect.DrawWithRoundedCorners(color, cornerRadius.RoundToInt());

        public static Rect DrawBlurred(this Rect rect, Color color, int blurRadius)
        {
            if (!CurEvent.IsRepaint) return rect;

            var scaledBlurRadius = (blurRadius * BlurPixelsPerPoint).RoundToInt().Max(MinScaledBlurRadius).Min(MaxScaledBlurRadius);

            var croppedRectWidth = (rect.width * BlurPixelsPerPoint).RoundToInt().Min(scaledBlurRadius * 2);
            var croppedRectHeight = (rect.height * BlurPixelsPerPoint).RoundToInt().Min(scaledBlurRadius * 2);

            var textureWidth = croppedRectWidth + scaledBlurRadius * 2;
            var textureHeight = croppedRectHeight + scaledBlurRadius * 2;

            if (!BlurredStylesByTextureSize.TryGetValue((textureWidth, textureHeight), out var style))
                BlurredStylesByTextureSize[(textureWidth, textureHeight)] = style = CreateBlurredStyle(textureWidth, textureHeight, scaledBlurRadius);

            SetGUIColor(color);

            style.Draw(rect.SetSizeFromMid(rect.width + blurRadius * 2, rect.height + blurRadius * 2), false, false, false, false);

            ResetGUIColor();

            return rect;
        }

        public static Rect DrawBlurred(this Rect rect, Color color, float blurRadius) => rect.DrawBlurred(color, blurRadius.RoundToInt());

        public static void DrawCurtainUp(this Rect rect, Color color) => rect.DrawCurtain(color, CurtainUpIndex);

        public static void DrawCurtainDown(this Rect rect, Color color) => rect.DrawCurtain(color, CurtainDownIndex);

        public static void DrawCurtainLeft(this Rect rect, Color color) => rect.DrawCurtain(color, CurtainLeftIndex);

        public static void DrawCurtainRight(this Rect rect, Color color) => rect.DrawCurtain(color, CurtainRightIndex);

        private static void DrawCurtain(this Rect rect, Color color, int directionIndex)
        {
            _gradientTextures ??= CreateGradientTextures();

            SetGUIColor(color);

            GUI.DrawTexture(rect, _gradientTextures[directionIndex]);

            ResetGUIColor();
        }

        #endregion

        #region Style Creation

        private static GUIStyle CreateRoundedStyle(int cornerRadius)
        {
            var resolution = cornerRadius * 2 * RoundedPixelsPerPoint;
            var pixels = new Color[resolution * resolution];

            var white = Greyscale(1);
            var clear = Greyscale(1, 0);
            var halfResolution = resolution / 2;
            var sqrRadius = halfResolution * halfResolution;

            for (var y = 0; y < resolution; y++)
            {
                var dy = y - halfResolution + .5f;
                var rowOffset = y * resolution;

                for (var x = 0; x < resolution; x++)
                {
                    var dx = x - halfResolution + .5f;

                    pixels[x + rowOffset] = dx * dx + dy * dy <= sqrRadius ? white : clear;
                }
            }

            var texture = new Texture2D(resolution, resolution);

            texture.SetPropertyValue(PixelsPerPointPropertyName, RoundedPixelsPerPoint);
            texture.hideFlags = HideFlags.DontSave;
            texture.SetPixels(pixels);
            texture.Apply();

            return new GUIStyle
            {
                normal = { background = texture },
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(cornerRadius, cornerRadius, cornerRadius, cornerRadius),
            };
        }

        private static GUIStyle CreateBlurredStyle(int textureWidth, int textureHeight, int scaledBlurRadius)
        {
            var kernel1d = new GaussianKernel(false, scaledBlurRadius).Array1d();

            var weightsX = AccumulateAxisWeights(kernel1d, textureWidth, scaledBlurRadius);
            var weightsY = AccumulateAxisWeights(kernel1d, textureHeight, scaledBlurRadius);

            var pixels = new Color[textureWidth * textureHeight];

            for (var y = 0; y < textureHeight; y++)
            {
                var weightY = weightsY[y];
                var rowOffset = y * textureWidth;

                for (var x = 0; x < textureWidth; x++)
                    pixels[x + rowOffset] = Greyscale(1, weightsX[x] * weightY);
            }

            var texture = new Texture2D(textureWidth, textureHeight);

            texture.SetPropertyValue(PixelsPerPointPropertyName, BlurPixelsPerPoint);
            texture.hideFlags = HideFlags.DontSave;
            texture.SetPixels(pixels);
            texture.Apply();

            var borderX = ((textureWidth / 2f - 1) / BlurPixelsPerPoint).FloorToInt();
            var borderY = ((textureHeight / 2f - 1) / BlurPixelsPerPoint).FloorToInt();

            return new GUIStyle
            {
                normal = { background = texture },
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(borderX, borderX, borderY, borderY),
            };
        }

        private static float[] AccumulateAxisWeights(float[] kernel1d, int length, int scaledBlurRadius)
        {
            var weights = new float[length];

            for (var i = 0; i < length; i++)
            {
                var from = (i - scaledBlurRadius).Max(scaledBlurRadius);
                var to = (i + scaledBlurRadius).Min(length - 1 - scaledBlurRadius);

                var sum = 0f;

                for (var sample = from; sample <= to; sample++)
                    sum += kernel1d[scaledBlurRadius + sample - i];

                weights[i] = sum;
            }

            return weights;
        }

        private static Texture2D[] CreateGradientTextures()
        {
            var ramp = new Color[GradientResolution];
            var rampReversed = new Color[GradientResolution];

            for (var i = 0; i < GradientResolution; i++)
            {
                ramp[i] = Greyscale(1, (i / (GradientResolution - 1f)).Smoothstep());
                rampReversed[GradientResolution - 1 - i] = ramp[i];
            }

            var textures = new Texture2D[CurtainDirectionCount];

            textures[CurtainUpIndex] = CreateGradientTexture(GradientThickness, GradientResolution, rampReversed);
            textures[CurtainDownIndex] = CreateGradientTexture(GradientThickness, GradientResolution, ramp);
            textures[CurtainLeftIndex] = CreateGradientTexture(GradientResolution, GradientThickness, ramp);
            textures[CurtainRightIndex] = CreateGradientTexture(GradientResolution, GradientThickness, rampReversed);

            return textures;
        }

        private static Texture2D CreateGradientTexture(int width, int height, Color[] pixels)
        {
            var texture = new Texture2D(width, height);

            texture.SetPixels(pixels);
            texture.Apply();

            texture.hideFlags = HideFlags.DontSave;
            texture.wrapMode = TextureWrapMode.Clamp;

            return texture;
        }

        #endregion

        #region Method

        public static void Space(float pixels = DefaultSpacing) => GUILayout.Space(pixels);

        #endregion

        #region Nested Type

        public static class EditorIcons
        {
            private const int HexBase = 16;
            private const char PngBytesSeparator = '-';
            private const int PlaceholderTextureSize = 1;
            private const string LoadIconMethodName = "LoadIcon";

            private static readonly Dictionary<string, Texture2D> Icons = new();
            private static readonly HashSet<string> Resolved = new();
            private static readonly Dictionary<(string, bool), GUIContent> Contents = new();
            private static readonly Dictionary<(string, bool), Texture2D> FoundTextures = new();

            private static readonly Dictionary<string, string> CustomIcons = new()
            {
                ["Paste values"] = "89-50-4E-47-0D-0A-1A-0A-00-00-00-0D-49-48-44-52-00-00-00-40-00-00-00-40-08-06-00-00-00-AA-69-71-DE-00-00-00-09-70-48-59-73-00-00-0B-13-00-00-0B-13-01-00-9A-9C-18-00-00-00-01-73-52-47-42-00-AE-CE-1C-E9-00-00-00-04-67-41-4D-41-00-00-B1-8F-0B-FC-61-05-00-00-02-AB-49-44-41-54-78-01-ED-9B-CD-4D-EB-40-10-80-67-A3-57-C0-3B-3D-BD-FC-1C-92-0E-A0-03-E8-00-D2-00-50-01-A1-02-4C-05-88-0A-08-0D-04-A8-80-12-A0-84-5C-92-40-4E-BE-46-8A-BC-CC-10-83-C0-10-B0-37-B3-B3-63-B1-9F-14-D9-B1-12-D0-7C-9E-DD-9D-DD-75-00-22-91-48-24-12-89-44-7E-29-06-04-98-CD-66-5D-6B-ED-25-9E-6E-E1-EB-2F-54-C4-18-93-34-9B-CD-33-F0-80-77-01-79-F0-F7-E0-10-F8-7B-7C-49-68-80-67-30-F8-73-D8-30-F8-FC-EF-24-28-F3-14-98-F1-9E-01-D3-E9-D4-02-23-DC-99-E0-3D-03-B8-E1-CE-84-DA-09-20-38-25-D4-52-00-C1-25-41-64-18-FC-89-7C-A4-B8-C3-D3-2E-54-64-D3-3E-41-85-00-22-94-04-35-02-88-5C-C2-35-AC-0A-A6-4A-B4-5A-2D-A7-58-54-F5-01-78-17-C7-8B-C5-62-17-4F-1F-40-08-75-9D-60-AF-D7-4B-25-25-38-37-01-89-FA-1E-8B-A8-21-1E-0E-A0-04-A2-4D-E0-5D-7D-BF-03-8E-65-6E-99-61-0C-83-3A-C4-C3-15-78-C4-49-80-64-7D-4F-12-B2-2C-BB-00-4F-B8-F6-01-7B-C0-44-19-09-9D-4E-67-80-9F-F3-32-1D-56-D1-09-96-91-D0-6E-B7-13-1F-12-D4-8C-02-65-25-00-33-AA-86-41-5F-73-FE-EF-50-57-07-48-4B-50-39-1B-94-94-A0-76-3A-4C-12-40-80-DA-AD-07-60-05-79-84-87-31-30-51-3B-01-58-3E-0F-51-02-CD-15-C6-C0-80-66-01-6B-4B-60-9A-35-E2-81-A5-3A-D4-2A-E0-2A-9F-07-AC-05-B3-E0-06-18-D0-28-E0-CB-E0-FB-4F-D6-CB-A8-A0-4D-C0-FA-E0-2D-24-E0-01-4D-02-C4-83-27-B4-08-08-12-3C-A1-41-40-B0-E0-89-D0-02-58-82-C7-AA-F1-16-1C-09-29-80-EB-CE-A7-8D-46-63-00-8E-FC-81-30-6C-1C-7C-5E-0C-6D-BC-AF-11-22-03-82-B6-F9-22-D2-02-54-05-4F-48-0A-50-17-3C-E1-D4-86-1C-9E-FA-B8-C1-E0-F7-8B-17-83-04-6F-20-C5-D7-70-F4-CF-9C-D0-5B-91-0C-C0-89-CB-49-F1-5A-B0-3B-6F-71-3F-23-83-41-7F-B6-5A-70-91-10-90-E6-3D-F6-1B-A1-D3-FE-05-03-C7-74-10-11-F0-E9-4A-A6-67-5B-5E-42-40-77-3E-9F-7F-D8-EF-1F-35-4D-82-19-E0-65-A7-A7-2C-36-5B-2D-B8-B8-0A-48-AB-7C-78-B9-5C-9E-17-AF-05-93-40-9D-20-FE-DF-EB-96-19-AC-DE-3A-80-A3-00-3D-CA-B2-53-E5-3B-58-AF-0F-71-67-E7-A8-78-FD-A5-33-32-50-6A-B1-63-F4-DF-B0-37-1D-A7-0C-C8-57-66-2B-65-01-7E-E7-70-32-99-5C-16-AF-87-6E-0E-4E-02-A8-57-C7-80-B6-F1-B4-D2-BA-9C-46-09-AA-1E-92-FA-A9-39-A8-69-02-BE-08-91-09-EA-56-85-A5-25-A8-DC-17-90-94-A0-76-67-48-4A-82-EA-BD-41-0D-15-A3-0A-68-74-E8-3F-5A-D6-1F-5E-D4-8E-D7-E9-6B-84-99-67-84-DC-66-24-9B-76-8E-1A-00-00-00-00-49-45-4E-44-AE-42-60-82",
            };

            public static GUIContent GetContent(string name)
            {
                var key = (name, IsDarkTheme);

                if (Contents.TryGetValue(key, out var cached)) return cached;

                return Contents[key] = EditorGUIUtility.IconContent(name);
            }

            public static Texture GetTexture(string name) => GetContent(name)?.image;

            public static Texture2D FindTexture(string name)
            {
                var key = (name, IsDarkTheme);

                if (FoundTextures.TryGetValue(key, out var cached) && cached) return cached;

                return FoundTextures[key] = EditorGUIUtility.FindTexture(name);
            }

            public static Texture2D GetIcon(string iconNameOrPath, bool returnNullIfNotFound = false)
            {
                iconNameOrPath ??= string.Empty;

                if (!Icons.TryGetValue(iconNameOrPath, out var icon) || (icon == null && !Resolved.Contains(iconNameOrPath)))
                {
                    icon = LoadIcon(iconNameOrPath);

                    Icons[iconNameOrPath] = icon;
                    Resolved.Add(iconNameOrPath);
                }

                if (icon == null && !returnNullIfNotFound) return Texture2D.grayTexture;

                return icon;
            }

            private static Texture2D LoadIcon(string iconNameOrPath)
            {
                if (!CustomIcons.TryGetValue(iconNameOrPath, out var pngBytesString))
                    return typeof(EditorGUIUtility).InvokeMethod<Texture2D>(LoadIconMethodName, iconNameOrPath);

                var parts = pngBytesString.Split(PngBytesSeparator);
                var pngBytes = new byte[parts.Length];

                for (var i = 0; i < parts.Length; i++)
                    pngBytes[i] = Convert.ToByte(parts[i], HexBase);

                var custom = new Texture2D(PlaceholderTextureSize, PlaceholderTextureSize);

                custom.hideFlags = HideFlags.DontSave;
                custom.LoadImage(pngBytes);

                return custom;
            }
        }

        public struct WrappedEvent
        {
            public Event RawEvent;

            public bool IsNull => this.RawEvent == null;

            public bool IsRepaint => !IsNull && this.RawEvent.type == EventType.Repaint;

            public bool IsLayout => !IsNull && this.RawEvent.type == EventType.Layout;

            public bool IsUsed => !IsNull && this.RawEvent.type == EventType.Used;

            public bool IsContextClick => !IsNull && this.RawEvent.type == EventType.ContextClick;

            public bool IsKeyDown => !IsNull && this.RawEvent.type == EventType.KeyDown;

            public bool IsKeyUp => !IsNull && this.RawEvent.type == EventType.KeyUp;

            public KeyCode KeyCode => IsNull ? default : this.RawEvent.keyCode;

            public bool IsMouse => !IsNull && this.RawEvent.isMouse;

            public bool IsMouseDown => !IsNull && this.RawEvent.type == EventType.MouseDown;

            public bool IsMouseUp => !IsNull && this.RawEvent.type == EventType.MouseUp;

            public bool IsMouseDrag => !IsNull && this.RawEvent.type == EventType.MouseDrag;

            public bool IsMouseMove => !IsNull && this.RawEvent.type == EventType.MouseMove;

            public bool IsScroll => !IsNull && this.RawEvent.type == EventType.ScrollWheel;

            public int MouseButton => IsNull ? default : this.RawEvent.button;

            public int ClickCount => IsNull ? default : this.RawEvent.clickCount;

            public Vector2 MousePosition => IsNull ? default : this.RawEvent.mousePosition;

            public Vector2 MouseDelta => IsNull ? default : this.RawEvent.delta;

            public bool IsDragUpdate => !IsNull && this.RawEvent.type == EventType.DragUpdated;

            public bool IsDragPerform => !IsNull && this.RawEvent.type == EventType.DragPerform;

            public bool IsDragExit => !IsNull && this.RawEvent.type == EventType.DragExited;

            public EventModifiers Modifiers => IsNull ? default : this.RawEvent.modifiers;

            public bool HoldingAlt => !IsNull && this.RawEvent.alt;

            public bool HoldingShift => !IsNull && this.RawEvent.shift;

            public bool HoldingCtrl => !IsNull && this.RawEvent.control;

            public bool HoldingCmd => !IsNull && this.RawEvent.command;

            public EventType Type => this.RawEvent.type;

            public WrappedEvent(Event rawEvent) => this.RawEvent = rawEvent;

            public void Use() => this.RawEvent?.Use();

            public override string ToString() => this.RawEvent == null ? "null" : this.RawEvent.ToString();
        }

        #endregion
    }
}
#endif
