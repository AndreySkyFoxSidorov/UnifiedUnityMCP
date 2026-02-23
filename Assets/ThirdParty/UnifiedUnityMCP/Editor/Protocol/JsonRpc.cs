using SimpleJSON;

namespace Mcp.Editor.Protocol
{
    public static class JsonRpc
    {
        public const string Version = "2.0";

        public static JSONObject CreateResponse(JSONNode id, JSONNode result)
        {
            var json = new JSONObject();
            json["jsonrpc"] = Version;
            if (id != null) json["id"] = id;
            if (result != null) json["result"] = result;
            return json;
        }

        public static JSONObject CreateError(JSONNode id, int code, string message, JSONNode data = null)
        {
            var error = new JSONObject();
            error["code"] = code;
            error["message"] = message;
            if (data != null) error["data"] = data;

            var json = new JSONObject();
            json["jsonrpc"] = Version;
            if (id != null) json["id"] = id;
            json["error"] = error;
            return json;
        }

        public static JSONObject CreateNotification(string method, JSONNode paramsNode = null)
        {
            var json = new JSONObject();
            json["jsonrpc"] = Version;
            json["method"] = method;
            if (paramsNode != null) json["params"] = paramsNode;
            return json;
        }

        // Standard JSON-RPC 2.0 Error Codes
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;
    }
}
