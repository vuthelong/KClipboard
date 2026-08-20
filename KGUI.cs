#if UNITY_EDITOR
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
            private static readonly Dictionary<(string, bool), GUIContent> Contents = new();

            public static GUIContent GetContent(string name)
            {
                var key = (name, IsDarkTheme);

                if (Contents.TryGetValue(key, out var cached)) return cached;

                return Contents[key] = EditorGUIUtility.IconContent(name);
            }

            public static Texture GetTexture(string name) => GetContent(name)?.image;
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
