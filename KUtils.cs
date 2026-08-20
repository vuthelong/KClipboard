#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Type = System.Type;
using Object = UnityEngine.Object;

namespace Kingfisher.KClipboard.Libs
{
    public static class KUtils
    {
        #region Field

        public const BindingFlags MaxBindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private const string DecamelReplacement = "$1 $2";

        private const char PathSeparator = '/';
        private const char WindowsPathSeparator = '\\';

        private const string AssetsPathMarker = "/Assets/";
        private const string PackagesPathMarker = "/Packages/";

        private const float LerpSpeedScale = 2f;
        private const float SmoothDampTimeScale = .5f;

        private static readonly Regex DecamelUpperRun = new(@"(\P{Ll})(\P{Ll}\p{Ll})", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex DecamelLowerToUpper = new(@"(\p{Ll})(\P{Ll})", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Dictionary<(Type, string), FieldInfo> FieldInfoCache = new();
        private static readonly Dictionary<(Type, string), PropertyInfo> PropertyInfoCache = new();
        private static readonly Dictionary<(Type, string, int), MethodInfo> MethodInfoCache = new();
        private static readonly List<Type[]> ArgumentTypeBuffers = new() { Type.EmptyTypes };

        #endregion

        #region Text

        public static string Decamelcase(this string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            return DecamelLowerToUpper.Replace(DecamelUpperRun.Replace(text, DecamelReplacement), DecamelReplacement);
        }

        public static string Remove(this string text, string toRemove) => toRemove == string.Empty ? text : text.Replace(toRemove, string.Empty);

        public static bool IsNullOrEmpty(this string text) => string.IsNullOrEmpty(text);

        #endregion

        #region Collections

        public static T AddAt<T>(this List<T> list, T item, int index)
        {
            if (index < 0) index = 0;

            if (index >= list.Count)
                list.Add(item);
            else
                list.Insert(index, item);

            return item;
        }

        public static T RemoveLast<T>(this List<T> list)
        {
            if (list.Count == 0) return default;

            var lastIndex = list.Count - 1;
            var last = list[lastIndex];

            list.RemoveAt(lastIndex);

            return last;
        }

        public static int IndexOfFirst<T>(this List<T> list, Func<T, bool> predicate)
        {
            for (var i = 0; i < list.Count; i++)
                if (predicate(list[i]))
                    return i;

            return -1;
        }

        public static bool IsInRange(this int value, int min, int max) => value >= min && value <= max;

        public static bool IsInRangeOf(this int index, IList list) => index >= 0 && index < list.Count;

        #endregion

        #region Reflection

        public static FieldInfo GetFieldInfo(this Type type, string fieldName)
        {
            var key = (type, fieldName);

            if (FieldInfoCache.TryGetValue(key, out var cached)) return cached;

            for (var currentType = type; currentType != null; currentType = currentType.BaseType)
                if (currentType.GetField(fieldName, MaxBindingFlags) is { } fieldInfo)
                    return FieldInfoCache[key] = fieldInfo;

            return FieldInfoCache[key] = null;
        }

        public static PropertyInfo GetPropertyInfo(this Type type, string propertyName)
        {
            var key = (type, propertyName);

            if (PropertyInfoCache.TryGetValue(key, out var cached)) return cached;

            for (var currentType = type; currentType != null; currentType = currentType.BaseType)
                if (currentType.GetProperty(propertyName, MaxBindingFlags) is { } propertyInfo)
                    return PropertyInfoCache[key] = propertyInfo;

            return PropertyInfoCache[key] = null;
        }

        public static MethodInfo GetMethodInfo(this Type type, string methodName, Type[] argumentTypes)
        {
            var argumentsHash = 0;

            for (var i = 0; i < argumentTypes.Length; i++)
                argumentsHash ^= argumentTypes[i].GetHashCode();

            var key = (type, methodName, argumentsHash);

            if (MethodInfoCache.TryGetValue(key, out var cached)) return cached;

            for (var currentType = type; currentType != null; currentType = currentType.BaseType)
                if (currentType.GetMethod(methodName, MaxBindingFlags, null, argumentTypes, null) is { } methodInfo)
                    return MethodInfoCache[key] = methodInfo;

            foreach (var interfaceType in type.GetInterfaces())
                if (interfaceType.GetMethod(methodName, MaxBindingFlags, null, argumentTypes, null) is { } methodInfo)
                    return MethodInfoCache[key] = methodInfo;

            return MethodInfoCache[key] = null;
        }

        public static object GetFieldValue(this object source, string fieldName, bool exceptionIfNotFound = true)
        {
            var type = source as Type ?? source.GetType();
            var target = source is Type ? null : source;

            if (type.GetFieldInfo(fieldName) is { } fieldInfo)
                return fieldInfo.GetValue(target);

            if (exceptionIfNotFound)
                throw new Exception($"Field '{fieldName}' not found in type '{type.Name}' and its parent types");

            return null;
        }

        public static object GetPropertyValue(this object source, string propertyName, bool exceptionIfNotFound = true)
        {
            var type = source as Type ?? source.GetType();
            var target = source is Type ? null : source;

            if (type.GetPropertyInfo(propertyName) is { } propertyInfo)
                return propertyInfo.GetValue(target);

            if (exceptionIfNotFound)
                throw new Exception($"Property '{propertyName}' not found in type '{type.Name}' and its parent types");

            return null;
        }

        public static object GetMemberValue(this object source, string memberName, bool exceptionIfNotFound = true)
        {
            var type = source as Type ?? source.GetType();
            var target = source is Type ? null : source;

            if (type.GetFieldInfo(memberName) is { } fieldInfo)
                return fieldInfo.GetValue(target);

            if (type.GetPropertyInfo(memberName) is { } propertyInfo)
                return propertyInfo.GetValue(target);

            if (exceptionIfNotFound)
                throw new Exception($"Member '{memberName}' not found in type '{type.Name}' and its parent types");

            return null;
        }

        public static void SetFieldValue(this object source, string fieldName, object value, bool exceptionIfNotFound = true)
        {
            var type = source as Type ?? source.GetType();
            var target = source is Type ? null : source;

            if (type.GetFieldInfo(fieldName) is { } fieldInfo)
                fieldInfo.SetValue(target, value);
            else if (exceptionIfNotFound)
                throw new Exception($"Field '{fieldName}' not found in type '{type.Name}' and its parent types");
        }

        public static void SetPropertyValue(this object source, string propertyName, object value, bool exceptionIfNotFound = true)
        {
            var type = source as Type ?? source.GetType();
            var target = source is Type ? null : source;

            if (type.GetPropertyInfo(propertyName) is { } propertyInfo)
                propertyInfo.SetValue(target, value);
            else if (exceptionIfNotFound)
                throw new Exception($"Property '{propertyName}' not found in type '{type.Name}' and its parent types");
        }

        public static void SetMemberValue(this object source, string memberName, object value, bool exceptionIfNotFound = true)
        {
            var type = source as Type ?? source.GetType();
            var target = source is Type ? null : source;

            if (type.GetFieldInfo(memberName) is { } fieldInfo)
                fieldInfo.SetValue(target, value);
            else if (type.GetPropertyInfo(memberName) is { } propertyInfo)
                propertyInfo.SetValue(target, value);
            else if (exceptionIfNotFound)
                throw new Exception($"Member '{memberName}' not found in type '{type.Name}' and its parent types");
        }

        public static object InvokeMethod(this object source, string methodName, params object[] parameters)
        {
            var type = source as Type ?? source.GetType();
            var target = source is Type ? null : source;

            if (type.GetMethodInfo(methodName, GetArgumentTypes(parameters)) is { } methodInfo)
                return methodInfo.Invoke(target, parameters);

            throw new Exception($"Method '{methodName}' not found in type '{type.Name}', its parent types and interfaces");
        }

        public static T GetFieldValue<T>(this object source, string fieldName, bool exceptionIfNotFound = true) => (T)source.GetFieldValue(fieldName, exceptionIfNotFound);

        public static T GetPropertyValue<T>(this object source, string propertyName, bool exceptionIfNotFound = true) => (T)source.GetPropertyValue(propertyName, exceptionIfNotFound);

        public static T GetMemberValue<T>(this object source, string memberName, bool exceptionIfNotFound = true) => (T)source.GetMemberValue(memberName, exceptionIfNotFound);

        public static T InvokeMethod<T>(this object source, string methodName, params object[] parameters) => (T)source.InvokeMethod(methodName, parameters);

        private static Type[] GetArgumentTypes(object[] parameters)
        {
            if (parameters.Length == 0) return Type.EmptyTypes;

            while (ArgumentTypeBuffers.Count <= parameters.Length)
                ArgumentTypeBuffers.Add(new Type[ArgumentTypeBuffers.Count]);

            var buffer = ArgumentTypeBuffers[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
                buffer[i] = parameters[i].GetType();

            return buffer;
        }

        #endregion

        #region Math

        public static bool Approx(this float value, float other) => Mathf.Approximately(value, other);

        public static float DistanceTo(this float from, float to) => Mathf.Abs(from - to);

        public static float DistanceTo(this Vector2 from, Vector2 to) => (from - to).magnitude;

        public static float Abs(this float value) => Mathf.Abs(value);

        public static float Clamp(this float value, float min, float max) => Mathf.Clamp(value, min, max);

        public static int Clamp(this int value, int min, int max) => Mathf.Clamp(value, min, max);

        public static float Clamp01(this float value) => Mathf.Clamp(value, 0, 1);

        public static float Round(this float value) => Mathf.Round(value);

        public static int RoundToInt(this float value) => Mathf.RoundToInt(value);

        public static int CeilToInt(this float value) => Mathf.CeilToInt(value);

        public static int FloorToInt(this float value) => Mathf.FloorToInt(value);

        public static float Max(this float value, float other) => Mathf.Max(value, other);

        public static float Min(this float value, float other) => Mathf.Min(value, other);

        public static int Max(this int value, int other) => Mathf.Max(value, other);

        public static int Min(this int value, int other) => Mathf.Min(value, other);

        public static float PingPong(this float value, float boundMin, float boundMax) => boundMin + Mathf.PingPong(value - boundMin, boundMax - boundMin);

        public static float PingPong(this float value, float boundMax) => value.PingPong(0, boundMax);

        public static float Smoothstep(this float value)
        {
            var clamped = value.Clamp01();

            return clamped * clamped * (3 - 2 * clamped);
        }

        public static float LerpT(float lerpSpeed, float deltaTime) => 1 - Mathf.Exp(-lerpSpeed * LerpSpeedScale * deltaTime);

        public static Color Lerp(Color from, Color to, float t) => Color.LerpUnclamped(from, to, t);

        public static Color Lerp(ref Color current, Color target, float t) => current = Lerp(current, target, t);

        public static float Lerp(float current, float target, float speed, float deltaTime) => Mathf.Lerp(current, target, LerpT(speed, deltaTime));

        public static float Lerp(ref float current, float target, float speed, float deltaTime) => current = Lerp(current, target, speed, deltaTime);

        public static float SmoothDamp(float current, float target, float speed, ref float derivative, float deltaTime) => Mathf.SmoothDamp(current, target, ref derivative, SmoothDampTimeScale / speed, Mathf.Infinity, deltaTime);

        public static float SmoothDamp(ref float current, float target, float speed, ref float derivative, float deltaTime) => current = SmoothDamp(current, target, speed, ref derivative, deltaTime);

        #endregion

        #region Colors

        public static Color Greyscale(float brightness, float alpha = 1) => new(brightness, brightness, brightness, alpha);

        public static Color SetAlpha(this Color color, float alpha)
        {
            color.a = alpha;

            return color;
        }

        #endregion

        #region Rects

        public static Rect Resize(this Rect rect, float inset)
        {
            rect.x += inset;
            rect.y += inset;
            rect.width -= inset * 2;
            rect.height -= inset * 2;

            return rect;
        }

        public static Rect SetPos(this Rect rect, Vector2 position) => rect.SetPos(position.x, position.y);

        public static Rect SetPos(this Rect rect, float x, float y)
        {
            rect.x = x;
            rect.y = y;

            return rect;
        }

        public static Rect SetX(this Rect rect, float x)
        {
            rect.x = x;

            return rect;
        }

        public static Rect SetY(this Rect rect, float y)
        {
            rect.y = y;

            return rect;
        }

        public static Rect SetXMax(this Rect rect, float xMax)
        {
            rect.xMax = xMax;

            return rect;
        }

        public static Rect SetYMax(this Rect rect, float yMax)
        {
            rect.yMax = yMax;

            return rect;
        }

        public static Rect Move(this Rect rect, Vector2 offset)
        {
            rect.position += offset;

            return rect;
        }

        public static Rect MoveX(this Rect rect, float offset)
        {
            rect.x += offset;

            return rect;
        }

        public static Rect MoveY(this Rect rect, float offset)
        {
            rect.y += offset;

            return rect;
        }

        public static Rect SetWidth(this Rect rect, float width)
        {
            rect.width = width;

            return rect;
        }

        public static Rect SetWidthFromMid(this Rect rect, float width)
        {
            rect.x += rect.width / 2;
            rect.width = width;
            rect.x -= rect.width / 2;

            return rect;
        }

        public static Rect SetWidthFromRight(this Rect rect, float width)
        {
            rect.x += rect.width;
            rect.width = width;
            rect.x -= rect.width;

            return rect;
        }

        public static Rect SetHeight(this Rect rect, float height)
        {
            rect.height = height;

            return rect;
        }

        public static Rect SetHeightFromMid(this Rect rect, float height)
        {
            rect.y += rect.height / 2;
            rect.height = height;
            rect.y -= rect.height / 2;

            return rect;
        }

        public static Rect SetHeightFromBottom(this Rect rect, float height)
        {
            rect.y += rect.height;
            rect.height = height;
            rect.y -= rect.height;

            return rect;
        }

        public static Rect AddWidthFromMid(this Rect rect, float amount) => rect.SetWidthFromMid(rect.width + amount);

        public static Rect AddHeightFromMid(this Rect rect, float amount) => rect.SetHeightFromMid(rect.height + amount);

        public static Rect SetSize(this Rect rect, Vector2 size)
        {
            rect.width = size.x;
            rect.height = size.y;

            return rect;
        }

        public static Rect SetSizeFromMid(this Rect rect, Vector2 size) => rect.Move(rect.size / 2).SetSize(size).Move(-size / 2);

        public static Rect SetSizeFromMid(this Rect rect, float x, float y) => rect.SetSizeFromMid(new Vector2(x, y));

        public static Rect SetSizeFromMid(this Rect rect, float size) => rect.SetSizeFromMid(new Vector2(size, size));

        public static Vector2 AddY(this Vector2 vector, float amount) => new(vector.x, vector.y + amount);

        #endregion

        #region Paths

        public static string GetParentPath(this string path)
        {
            if (path == null) return string.Empty;

            var separatorIndex = path.LastIndexOf(PathSeparator);

            return separatorIndex <= 0 ? string.Empty : path.Substring(0, separatorIndex);
        }

        public static string CombinePath(this string path, string other) => Path.Combine(path, other).Replace(WindowsPathSeparator, PathSeparator);

        public static string GetExtension(this string path) => Path.GetExtension(path);

        public static string GetFilename(this string path, bool withExtension = false) => withExtension ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);

        public static string GetCallerScriptPath([CallerFilePath] string callerFilePath = "")
        {
            var path = callerFilePath.Replace(WindowsPathSeparator, PathSeparator);

            var assetsIndex = path.LastIndexOf(AssetsPathMarker, StringComparison.Ordinal);

            if (assetsIndex != -1) return path.Substring(assetsIndex + 1);

            var packagesIndex = path.LastIndexOf(PackagesPathMarker, StringComparison.Ordinal);

            if (packagesIndex != -1) return path.Substring(packagesIndex + 1);

            return path;
        }

        #endregion

        #region Asset Database

        public static string ToPath(this string guid) => AssetDatabase.GUIDToAssetPath(guid);

        public static string ToGuid(this string pathInProject) => AssetDatabase.AssetPathToGUID(pathInProject);

        public static string GetPath(this Object target) => AssetDatabase.GetAssetPath(target);

        public static Object LoadGuid(this string guid) => AssetDatabase.LoadAssetAtPath(guid.ToPath(), typeof(Object));

        public static T LoadGuid<T>(this string guid) where T : Object => AssetDatabase.LoadAssetAtPath<T>(guid.ToPath());

        public static GlobalID GetGlobalID(this Object target) => new(target);

        #endregion

        #region Editor

        public static int GetProjectId() => Application.dataPath.GetHashCode();

        public static void Dirty(this Object target) => EditorUtility.SetDirty(target);

        public static void Save(this Object target) => AssetDatabase.SaveAssetIfDirty(target);

        #endregion

        #region Compatibility

        public static Object LoadedObjectFromInstanceId(int instanceId) =>
#if UNITY_6000_3_OR_NEWER
            Resources.EntityIdToObject(instanceId);
#else
            Resources.InstanceIDToObject(instanceId);
#endif

        public static int GlobalIdToInstanceId(GlobalObjectId globalId) =>
#if UNITY_6000_3_OR_NEWER
            GlobalObjectId.GlobalObjectIdentifierToEntityIdSlow(globalId);
#else
            GlobalObjectId.GlobalObjectIdentifierToInstanceIDSlow(globalId);
#endif

        #endregion

        #region Nested Type

        public class GaussianKernel
        {
            private const int DefaultRadius = 7;
            private const float DefaultSharpness = .5f;

            private const float SharpnessCurve = .1f;
            private const float SigmaScale = .99999f;

            public bool IsEvenSize;
            public int Radius;
            public float Sharpness;

            public int Size => this.Radius * 2 + (this.IsEvenSize ? 0 : 1);

            public float Sigma => 1 - Mathf.Pow(this.Sharpness, SharpnessCurve) * SigmaScale;

            public GaussianKernel(bool isEvenSize = false, int radius = DefaultRadius, float sharpness = DefaultSharpness)
            {
                this.IsEvenSize = isEvenSize;
                this.Radius = radius;
                this.Sharpness = sharpness;
            }

            public float[] Array1d()
            {
                var size = Size;
                var kernel = new float[size];

                if (size == 1)
                {
                    kernel[0] = 1;

                    return kernel;
                }

                var denominator = -2f * this.Radius * this.Radius / Mathf.Log(Sigma);
                var sum = 0f;

                for (var i = 0; i < size; i++)
                {
                    var offset = size % 2 == 1 ? i - this.Radius : i - this.Radius + .5f;

                    kernel[i] = Mathf.Exp(-offset * offset / denominator);
                    sum += kernel[i];
                }

                for (var i = 0; i < size; i++)
                    kernel[i] /= sum;

                return kernel;
            }

            public float[,] Array2d()
            {
                var size = Size;
                var kernel1d = Array1d();
                var kernel = new float[size, size];

                for (var y = 0; y < size; y++)
                    for (var x = 0; x < size; x++)
                        kernel[x, y] = kernel1d[x] * kernel1d[y];

                return kernel;
            }
        }

        [Serializable]
        public struct GlobalID : IEquatable<GlobalID>
        {
            private const int NullIdentifierType = 0;
            private const int AssetIdentifierType = 1;
            private const int SceneObjectIdentifierType = 2;

            public string globalObjectIdString;

            private GlobalObjectId _objectId;

            public GlobalObjectId ObjectId => this._objectId.Equals(default) && this.globalObjectIdString != null && GlobalObjectId.TryParse(this.globalObjectIdString, out var parsed) ? this._objectId = parsed : this._objectId;

            public string Guid => ObjectId.assetGUID.ToString();

            public ulong FileId => ObjectId.targetObjectId;

            public bool IsNull => ObjectId.identifierType == NullIdentifierType;

            public bool isAsset => ObjectId.identifierType == AssetIdentifierType;

            public bool IsSceneObject => ObjectId.identifierType == SceneObjectIdentifierType;

            public GlobalID(Object target)
            {
                this._objectId = GlobalObjectId.GetGlobalObjectIdSlow(target);
                this.globalObjectIdString = this._objectId.ToString();
            }

            public GlobalID(string globalObjectIdString)
            {
                GlobalObjectId.TryParse(globalObjectIdString, out this._objectId);

                this.globalObjectIdString = globalObjectIdString;
            }

            public Object GetObject() => GlobalObjectId.GlobalObjectIdentifierToObjectSlow(ObjectId);

            public int GetObjectInstanceId() => GlobalIdToInstanceId(ObjectId);

            public bool Equals(GlobalID other) => string.Equals(this.globalObjectIdString, other.globalObjectIdString, StringComparison.Ordinal);

            public override bool Equals(object other) => other is GlobalID otherGlobalId && Equals(otherGlobalId);

            public override int GetHashCode() => this.globalObjectIdString == null ? 0 : this.globalObjectIdString.GetHashCode();

            public override string ToString() => this.globalObjectIdString;

            public static bool operator ==(GlobalID a, GlobalID b) => a.Equals(b);

            public static bool operator !=(GlobalID a, GlobalID b) => !a.Equals(b);
        }

        [Serializable]
        public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
        {
            [SerializeField] private List<TKey> keys = new();
            [SerializeField] private List<TValue> values = new();

            public void OnBeforeSerialize()
            {
                this.keys.Clear();
                this.values.Clear();

                if (this.keys.Capacity < Count) this.keys.Capacity = Count;
                if (this.values.Capacity < Count) this.values.Capacity = Count;

                foreach (var kvp in this)
                {
                    this.keys.Add(kvp.Key);
                    this.values.Add(kvp.Value);
                }
            }

            public void OnAfterDeserialize()
            {
                Clear();

                for (var i = 0; i < this.keys.Count; i++)
                    this[this.keys[i]] = this.values[i];
            }
        }

        private static class KSettingsBridge
        {
            private const string SettingsTypeName = "Kingfisher.KSetting.KSettings, Kingfisher.KSetting";

            private static readonly object[] OneArg = new object[1];
            private static readonly object[] TwoArgs = new object[2];

            private static readonly Type SettingsType = Type.GetType(SettingsTypeName);

            private static readonly MethodInfo HasBoolMethod = SettingsType?.GetMethod("HasBool", new[] { typeof(string) });
            private static readonly MethodInfo HasIntMethod = SettingsType?.GetMethod("HasInt", new[] { typeof(string) });
            private static readonly MethodInfo HasFloatMethod = SettingsType?.GetMethod("HasFloat", new[] { typeof(string) });

            private static readonly MethodInfo GetBoolMethod = SettingsType?.GetMethod("GetBool", new[] { typeof(string), typeof(bool) });
            private static readonly MethodInfo GetIntMethod = SettingsType?.GetMethod("GetInt", new[] { typeof(string), typeof(int) });
            private static readonly MethodInfo GetFloatMethod = SettingsType?.GetMethod("GetFloat", new[] { typeof(string), typeof(float) });

            private static readonly MethodInfo SetBoolMethod = SettingsType?.GetMethod("SetBool", new[] { typeof(string), typeof(bool) });
            private static readonly MethodInfo SetIntMethod = SettingsType?.GetMethod("SetInt", new[] { typeof(string), typeof(int) });
            private static readonly MethodInfo SetFloatMethod = SettingsType?.GetMethod("SetFloat", new[] { typeof(string), typeof(float) });

            public static bool TryGetBool(string key, out bool value)
            {
                value = default;

                if (!Has(HasBoolMethod, key)) return false;

                value = (bool)Invoke(GetBoolMethod, key, false);

                return true;
            }

            public static bool TryGetInt(string key, out int value)
            {
                value = default;

                if (!Has(HasIntMethod, key)) return false;

                value = (int)Invoke(GetIntMethod, key, 0);

                return true;
            }

            public static bool TryGetFloat(string key, out float value)
            {
                value = default;

                if (!Has(HasFloatMethod, key)) return false;

                value = (float)Invoke(GetFloatMethod, key, 0f);

                return true;
            }

            public static void SetBool(string key, bool value) => Invoke(SetBoolMethod, key, value);

            public static void SetInt(string key, int value) => Invoke(SetIntMethod, key, value);

            public static void SetFloat(string key, float value) => Invoke(SetFloatMethod, key, value);

            private static bool Has(MethodInfo hasMethod, string key)
            {
                if (hasMethod == null) return false;

                OneArg[0] = key;

                return (bool)hasMethod.Invoke(null, OneArg);
            }

            private static object Invoke(MethodInfo method, string key, object value)
            {
                if (method == null) return value;

                TwoArgs[0] = key;
                TwoArgs[1] = value;

                return method.Invoke(null, TwoArgs);
            }
        }

        public static class EditorPrefsCached
        {
            private const int ToolKeyPrefixLength = 1;

            private const string LegacyKeyPrefix = "v";
            private const string LegacyKingfisherToken = "-kingfisher-";
            private const string LegacyVToolsToken = "-vtools-";

            private static readonly string[] RenamedPrefixes = { "KHierarchy-", "KFolders-", "KFavorites-", "KTabs-", "KInspector-" };

            private static readonly Dictionary<string, bool> Bools = new();
            private static readonly Dictionary<string, int> Ints = new();

            public static bool GetBool(string key, bool defaultValue = false)
            {
                if (Bools.TryGetValue(key, out var cached)) return cached;

                if (KSettingsBridge.TryGetBool(key, out var stored)) return Bools[key] = stored;

                if (!EditorPrefs.HasKey(key) && LegacyKey(key) is { } legacy && EditorPrefs.HasKey(legacy))
                    return Bools[key] = EditorPrefs.GetBool(legacy, defaultValue);

                return Bools[key] = EditorPrefs.GetBool(key, defaultValue);
            }

            public static void SetBool(string key, bool value)
            {
                Bools[key] = value;

                KSettingsBridge.SetBool(key, value);
                EditorPrefs.SetBool(key, value);
            }

            public static int GetInt(string key, int defaultValue = 0)
            {
                if (Ints.TryGetValue(key, out var cached)) return cached;

                if (KSettingsBridge.TryGetInt(key, out var stored)) return Ints[key] = stored;

                if (!EditorPrefs.HasKey(key) && LegacyKey(key) is { } legacy && EditorPrefs.HasKey(legacy))
                    return Ints[key] = EditorPrefs.GetInt(legacy, defaultValue);

                return Ints[key] = EditorPrefs.GetInt(key, defaultValue);
            }

            public static void SetInt(string key, int value)
            {
                Ints[key] = value;

                KSettingsBridge.SetInt(key, value);
                EditorPrefs.SetInt(key, value);
            }

            private static string LegacyKey(string key)
            {
                for (var i = 0; i < RenamedPrefixes.Length; i++)
                {
                    if (!key.StartsWith(RenamedPrefixes[i], StringComparison.Ordinal)) continue;

                    var previous = key.Substring(ToolKeyPrefixLength);

                    if (EditorPrefs.HasKey(previous)) return previous;

                    var original = LegacyKeyPrefix + previous.Replace(LegacyKingfisherToken, LegacyVToolsToken);

                    return EditorPrefs.HasKey(original) ? original : null;
                }

                return null;
            }
        }

        #endregion
    }
}
#endif
