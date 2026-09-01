// King Leo story layer — additive to the Jelly Garden kit.
//
// Unity's JsonUtility cannot read the story files: they contain nested arrays
// (a vine anchor is [[7, 0]]) and optional fields that differ between arc 1 and
// arcs 2-6. Newtonsoft is not in this project's package manifest and adding it
// would force a package resolve, so the story layer carries its own small
// reader instead. It is read-only and allocates a plain tree.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace JellyGarden.Scripts.Story
{
    public enum JsonKind
    {
        Null,
        Bool,
        Number,
        String,
        Array,
        Object
    }

    /// <summary>
    /// One node of a parsed JSON document. Accessors never throw on a missing
    /// key or a wrong type — they return the supplied fallback. Story data is
    /// content, and a typo in content should degrade a screen, not crash it.
    /// </summary>
    public sealed class JsonValue
    {
        public static readonly JsonValue Null = new JsonValue { Kind = JsonKind.Null };

        public JsonKind Kind { get; private set; }

        private bool _bool;
        private double _number;
        private string _string;
        private List<JsonValue> _array;
        private Dictionary<string, JsonValue> _object;

        public bool IsNull
        {
            get { return Kind == JsonKind.Null; }
        }

        public int Count
        {
            get
            {
                if (Kind == JsonKind.Array) return _array.Count;
                if (Kind == JsonKind.Object) return _object.Count;
                return 0;
            }
        }

        public IEnumerable<JsonValue> Items
        {
            get
            {
                if (Kind != JsonKind.Array) yield break;
                for (int i = 0; i < _array.Count; i++)
                    yield return _array[i];
            }
        }

        public JsonValue this[int index]
        {
            get
            {
                if (Kind != JsonKind.Array || index < 0 || index >= _array.Count)
                    return Null;
                return _array[index];
            }
        }

        public JsonValue this[string key]
        {
            get
            {
                JsonValue found;
                if (Kind == JsonKind.Object && _object.TryGetValue(key, out found))
                    return found;
                return Null;
            }
        }

        public bool Has(string key)
        {
            return Kind == JsonKind.Object && _object.ContainsKey(key);
        }

        public string AsString(string fallback = "")
        {
            if (Kind == JsonKind.String) return _string;
            if (Kind == JsonKind.Number) return _number.ToString(CultureInfo.InvariantCulture);
            if (Kind == JsonKind.Bool) return _bool ? "true" : "false";
            return fallback;
        }

        public int AsInt(int fallback = 0)
        {
            if (Kind == JsonKind.Number) return (int)Math.Round(_number);
            if (Kind == JsonKind.String)
            {
                int parsed;
                if (int.TryParse(_string, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
                    return parsed;
            }
            return fallback;
        }

        public float AsFloat(float fallback = 0f)
        {
            if (Kind == JsonKind.Number) return (float)_number;
            if (Kind == JsonKind.String)
            {
                float parsed;
                if (float.TryParse(_string, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
                    return parsed;
            }
            return fallback;
        }

        public bool AsBool(bool fallback = false)
        {
            if (Kind == JsonKind.Bool) return _bool;
            if (Kind == JsonKind.Number) return Math.Abs(_number) > double.Epsilon;
            return fallback;
        }

        public static JsonValue Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return Null;
            int index = 0;
            JsonValue value = ParseValue(text, ref index);
            return value ?? Null;
        }

        #region Parser

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r'))
                i++;
        }

        private static JsonValue ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length) return Null;

            char c = s[i];
            switch (c)
            {
                case '{': return ParseObject(s, ref i);
                case '[': return ParseArray(s, ref i);
                case '"': return new JsonValue { Kind = JsonKind.String, _string = ParseString(s, ref i) };
                case 't':
                    i += 4;
                    return new JsonValue { Kind = JsonKind.Bool, _bool = true };
                case 'f':
                    i += 5;
                    return new JsonValue { Kind = JsonKind.Bool, _bool = false };
                case 'n':
                    i += 4;
                    return Null;
                default: return ParseNumber(s, ref i);
            }
        }

        private static JsonValue ParseObject(string s, ref int i)
        {
            var result = new JsonValue
            {
                Kind = JsonKind.Object,
                _object = new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            };
            i++; // '{'
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return result; }

            while (i < s.Length)
            {
                SkipWhitespace(s, ref i);
                if (i >= s.Length || s[i] != '"') break;
                string key = ParseString(s, ref i);
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ':') i++;
                result._object[key] = ParseValue(s, ref i);
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; break; }
                break;
            }
            return result;
        }

        private static JsonValue ParseArray(string s, ref int i)
        {
            var result = new JsonValue { Kind = JsonKind.Array, _array = new List<JsonValue>() };
            i++; // '['
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return result; }

            while (i < s.Length)
            {
                result._array.Add(ParseValue(s, ref i));
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; break; }
                break;
            }
            return result;
        }

        private static string ParseString(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++; // opening quote
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') break;
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;

                char esc = s[i++];
                switch (esc)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 <= s.Length)
                        {
                            int code;
                            if (int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber,
                                    CultureInfo.InvariantCulture, out code))
                                sb.Append((char)code);
                            i += 4;
                        }
                        break;
                    default: sb.Append(esc); break;
                }
            }
            return sb.ToString();
        }

        private static JsonValue ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length)
            {
                char c = s[i];
                bool numeric = (c >= '0' && c <= '9') || c == '-' || c == '+' ||
                               c == '.' || c == 'e' || c == 'E';
                if (!numeric) break;
                i++;
            }
            double parsed;
            double.TryParse(s.Substring(start, i - start), NumberStyles.Any,
                CultureInfo.InvariantCulture, out parsed);
            return new JsonValue { Kind = JsonKind.Number, _number = parsed };
        }

        #endregion
    }
}
