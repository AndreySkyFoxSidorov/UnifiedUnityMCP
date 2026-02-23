using System;
using System.Linq;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;

namespace Mcp.Editor.Tools
{
    public class TestRunTool : ITool, ICallbacks
    {
        public string Name => "unity.test.run";
        public string Description => "Run Unity Tests (EditMode or PlayMode) and get results.";

        private Action<JSONObject> _currentResponseCallback;
        private Action<string> _currentErrorCallback;
        private TestRunnerApi _testRunner;
        private JSONObject _testResults;

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["mode"] = McpMessages.CreateStringProperty("Test mode: 'editmode' or 'playmode'.");

                var required = new JSONArray();
                required.Add("mode");

                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            string modeStr = arguments.GetString("mode")?.ToLower();

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    TestMode testMode = TestMode.EditMode;
                    if (modeStr == "playmode") testMode = TestMode.PlayMode;
                    else if (modeStr != "editmode")
                    {
                        sendError($"Invalid mode '{modeStr}'. Use 'editmode' or 'playmode'.");
                        return;
                    }

                    if (_currentResponseCallback != null)
                    {
                        sendError("A test run is already in progress.");
                        return;
                    }

                    _currentResponseCallback = sendResponse;
                    _currentErrorCallback = sendError;
                    _testResults = new JSONObject();

                    _testRunner = ScriptableObject.CreateInstance<TestRunnerApi>();
                    _testRunner.RegisterCallbacks(this);

                    _testRunner.Execute(new ExecutionSettings(new Filter { testMode = testMode }));
                }
                catch (Exception e)
                {
                    sendError($"Test Run failed to start: {e.Message}");
                    Cleanup();
                }
            });
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            // Do nothing yet
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            if (_currentResponseCallback == null) return;

            _testResults["totalStatus"] = result.TestStatus.ToString();
            _testResults["passCount"] = result.PassCount;
            _testResults["failCount"] = result.FailCount;
            _testResults["skipCount"] = result.SkipCount;

            var failedArr = new JSONArray();
            ExtractFailures(result, failedArr);
            _testResults["failedTests"] = failedArr;

            _currentResponseCallback(McpMessages.CreateToolResult(_testResults.ToString()));
            Cleanup();
        }

        public void TestStarted(ITestAdaptor test)
        {
            // Do nothing per test start
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            // Do nothing per test finish, handled in RunFinished
        }

        private void ExtractFailures(ITestResultAdaptor result, JSONArray arr)
        {
            if (result.TestStatus == TestStatus.Failed && !result.Test.IsSuite)
            {
                var obj = new JSONObject();
                obj["name"] = result.Test.Name;
                obj["message"] = result.Message;
                arr.Add(obj);
            }

            foreach (var child in result.Children)
            {
                ExtractFailures(child, arr);
            }
        }

        private void Cleanup()
        {
            if (_testRunner != null)
            {
                _testRunner.UnregisterCallbacks(this);
                UnityEngine.Object.DestroyImmediate(_testRunner);
                _testRunner = null;
            }
            _currentResponseCallback = null;
            _currentErrorCallback = null;
            _testResults = null;
        }
    }
}
