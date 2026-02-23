using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using SimpleJSON;

namespace Mcp.Editor.Tests
{
    public static class McpSmokeTest
    {
        private const string Endpoint = "http://127.0.0.1:18008/mcp";

        [MenuItem("MCP/Start Server")]
        public static void StartServer()
        {
            McpToolbarToggle.SetRunning(true);
            Debug.Log("[MCP SmokeTest] Requested Start Server via Toggle.");
        }

        [MenuItem("MCP/Stop Server")]
        public static void StopServer()
        {
            McpToolbarToggle.SetRunning(false);
            Debug.Log("[MCP SmokeTest] Requested Stop Server via Toggle.");
        }

        [MenuItem("MCP/Run Smoke Test")]
        public static async void RunSmokeTest()
        {
            if (!UnityMcpServer.IsRunning)
            {
                Debug.LogError("[MCP SmokeTest] Server is not running. Start it first.");
                return;
            }

            Debug.Log("[MCP SmokeTest] Starting smoke test...");

            using (var client = new HttpClient())
            {
                try
                {
                    // Test 1: Initialize
                    var initReq = new JSONObject();
                    initReq["jsonrpc"] = "2.0";
                    initReq["id"] = 1;
                    initReq["method"] = "initialize";
                    
                    var initContent = new StringContent(initReq.ToString(), Encoding.UTF8, "application/json");
                    var initResponse = await client.PostAsync(Endpoint, initContent);
                    initResponse.EnsureSuccessStatusCode();
                    
                    string initResultStr = await initResponse.Content.ReadAsStringAsync();
                    var initJson = JSON.Parse(initResultStr);
                    if (initJson["result"]["serverInfo"]["name"]?.Value == "UnityMcpServer")
                    {
                        Debug.Log("[MCP SmokeTest] Initialize OK!");
                    }
                    else
                    {
                        Debug.LogError($"[MCP SmokeTest] Initialize Failed. Output: {initResultStr}");
                        return;
                    }

                    // Test 2: tools/list
                    var toolsReq = new JSONObject();
                    toolsReq["jsonrpc"] = "2.0";
                    toolsReq["id"] = 2;
                    toolsReq["method"] = "tools/list";

                    var toolsContent = new StringContent(toolsReq.ToString(), Encoding.UTF8, "application/json");
                    var toolsResponse = await client.PostAsync(Endpoint, toolsContent);
                    toolsResponse.EnsureSuccessStatusCode();

                    string toolsResultStr = await toolsResponse.Content.ReadAsStringAsync();
                    var toolsJson = JSON.Parse(toolsResultStr);
                    if (toolsJson["result"]["tools"] != null && toolsJson["result"]["tools"].AsArray.Count > 0)
                    {
                        Debug.Log($"[MCP SmokeTest] tools/list OK! Found {toolsJson["result"]["tools"].AsArray.Count} tools.");
                    }
                    else
                    {
                        Debug.LogError($"[MCP SmokeTest] tools/list Failed. Output: {toolsResultStr}");
                        return;
                    }

                    // Test 3: tools/call (unity_ping)
                    var invokeReq = new JSONObject();
                    invokeReq["jsonrpc"] = "2.0";
                    invokeReq["id"] = 3;
                    invokeReq["method"] = "tools/call";
                    var invokeParams = new JSONObject();
                    invokeParams["name"] = "unity_ping";
                    invokeParams["arguments"] = new JSONObject();
                    invokeReq["params"] = invokeParams;

                    var invokeContent = new StringContent(invokeReq.ToString(), Encoding.UTF8, "application/json");
                    var invokeResponse = await client.PostAsync(Endpoint, invokeContent);
                    invokeResponse.EnsureSuccessStatusCode();

                    string invokeResultStr = await invokeResponse.Content.ReadAsStringAsync();
                    var invokeJson = JSON.Parse(invokeResultStr);
                    string pingResult = invokeJson["result"]?["content"]?[0]?["text"]?.Value;
                    
                    if (!string.IsNullOrEmpty(pingResult) && pingResult.Contains("pong"))
                    {
                        Debug.Log($"[MCP SmokeTest] tools/call (unity_ping) OK! Result: {pingResult}");
                    }
                    else
                    {
                        Debug.LogError($"[MCP SmokeTest] tools/call Failed. Output: {invokeResultStr}");
                    }

                    Debug.Log("[MCP SmokeTest] ALL TESTS PASSED!");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MCP SmokeTest] Failed: {e.Message}");
                }
            }
        }
    }
}
