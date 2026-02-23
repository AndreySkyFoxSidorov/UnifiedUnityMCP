using SimpleJSON;

namespace Mcp.Editor.Protocol
{
    public static class McpMessages
    {
        public static JSONObject CreateToolSchema(string name, string description, JSONObject properties, JSONArray required = null)
        {
            var tool = new JSONObject();
            tool["name"] = name;
            tool["description"] = description;
            
            var inputSchema = new JSONObject();
            inputSchema["type"] = "object";
            if (properties != null) inputSchema["properties"] = properties;
            if (required != null) inputSchema["required"] = required;
            
            tool["inputSchema"] = inputSchema;
            return tool;
        }

        public static JSONObject CreateStringProperty(string description)
        {
            var prop = new JSONObject();
            prop["type"] = "string";
            prop["description"] = description;
            return prop;
        }

        public static JSONObject CreateIntegerProperty(string description)
        {
            var prop = new JSONObject();
            prop["type"] = "integer";
            prop["description"] = description;
            return prop;
        }

        public static JSONObject CreateBooleanProperty(string description)
        {
            var prop = new JSONObject();
            prop["type"] = "boolean";
            prop["description"] = description;
            return prop;
        }

        public static JSONObject CreateTextContent(string text)
        {
            var content = new JSONObject();
            content["type"] = "text";
            content["text"] = text;
            return content;
        }

        public static JSONObject CreateToolResult(string text, bool isError = false)
        {
            var result = new JSONObject();
            var contentArr = new JSONArray();
            contentArr.Add(CreateTextContent(text));
            result["content"] = contentArr;
            if (isError) result["isError"] = true;
            return result;
        }
    }
}
