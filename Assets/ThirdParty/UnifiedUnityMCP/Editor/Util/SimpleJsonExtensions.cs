using SimpleJSON;

namespace Mcp.Editor.Util
{
    public static class SimpleJsonExtensions
    {
        public static string GetString(this JSONObject obj, string key, string defaultValue = null)
        {
            if (obj == null || !obj.HasKey(key)) return defaultValue;
            var node = obj[key];
            return node == null || node.IsNull ? defaultValue : node.Value;
        }

        public static int GetInt(this JSONObject obj, string key, int defaultValue = 0)
        {
            if (obj == null || !obj.HasKey(key)) return defaultValue;
            var node = obj[key];
            return node == null || node.IsNull ? defaultValue : node.AsInt;
        }

        public static bool GetBool(this JSONObject obj, string key, bool defaultValue = false)
        {
            if (obj == null || !obj.HasKey(key)) return defaultValue;
            var node = obj[key];
            return node == null || node.IsNull ? defaultValue : node.AsBool;
        }

        public static JSONObject GetObject(this JSONObject obj, string key)
        {
            if (obj == null || !obj.HasKey(key)) return null;
            return obj[key] as JSONObject;
        }

        public static JSONArray GetArray(this JSONObject obj, string key)
        {
            if (obj == null || !obj.HasKey(key)) return null;
            return obj[key] as JSONArray;
        }
    }
}
