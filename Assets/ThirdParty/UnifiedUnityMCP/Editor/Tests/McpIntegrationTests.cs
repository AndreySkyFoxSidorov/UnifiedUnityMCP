using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Networking;
using SimpleJSON;
using Mcp.Editor.Transport;

namespace Mcp.Editor.Tests
{
    public class McpIntegrationTests
    {
        private StreamableHttpTransport _transport;
        private string _url = "http://127.0.0.1:18008/mcp";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _transport = new StreamableHttpTransport("http://127.0.0.1:18008/", "/mcp", 18008, "test-session-id");
            _transport.OnMessageReceived = delegate (JSONObject o, System.Action<JSONObject> r, System.Action<int, string> e)
            {
                // mock command router
                if (!o.HasKey("method"))
                {
                    e(-32600, "Invalid Request");
                    return;
                }

                string method = o["method"];
                if (method == "ping") r(new JSONObject());
                else if (method == "initialize") r(new JSONObject());
                else r(new JSONObject());
            };
            _transport.Start();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _transport?.Stop();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BatchRequest_MixedArray_ProcessesCorrectly()
        {
            JSONArray batch = new JSONArray();
            JSONObject req1 = new JSONObject();
            req1["jsonrpc"] = "2.0";
            req1["id"] = 1;
            req1["method"] = "ping";

            JSONObject notif1 = new JSONObject();
            notif1["jsonrpc"] = "2.0";
            notif1["method"] = "ping"; // no id = notification

            JSONObject reqInvalid = new JSONObject();
            reqInvalid["jsonrpc"] = "2.0";
            reqInvalid["id"] = 2; // missing method

            batch.Add(req1);
            batch.Add(notif1);
            batch.Add(reqInvalid);

            using (UnityWebRequest req = new UnityWebRequest(_url, "POST"))
            {
                byte[] jsonToSend = new UTF8Encoding().GetBytes(batch.ToString());
                req.uploadHandler = new UploadHandlerRaw(jsonToSend);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                Assert.AreEqual(200, req.responseCode);

                var resArray = JSON.Parse(req.downloadHandler.text) as JSONArray;
                Assert.IsNotNull(resArray);
                Assert.AreEqual(2, resArray.Count, "Should return 2 responses for 1 valid request and 1 invalid. Notification should not return response.");

                bool foundRes1 = false, foundErr = false;
                for (int i = 0; i < resArray.Count; i++)
                {
                    if (resArray[i]["id"].AsInt == 1) foundRes1 = true;
                    if (resArray[i]["id"].AsInt == 2 && resArray[i].HasKey("error")) foundErr = true;
                }
                Assert.IsTrue(foundRes1, "Response for request id 1 not found");
                Assert.IsTrue(foundErr, "Error response for valid id 2 not found");
            }
        }

        [UnityTest]
        public IEnumerator BatchRequest_OnlyNotifications_Returns204()
        {
            JSONArray batch = new JSONArray();
            JSONObject notif1 = new JSONObject();
            notif1["jsonrpc"] = "2.0";
            notif1["method"] = "ping";
            batch.Add(notif1);

            using (UnityWebRequest req = new UnityWebRequest(_url, "POST"))
            {
                byte[] jsonToSend = new UTF8Encoding().GetBytes(batch.ToString());
                req.uploadHandler = new UploadHandlerRaw(jsonToSend);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                Assert.AreEqual(204, req.responseCode, "Notifications-only batch should return 204 No Content");
            }
        }

        [UnityTest]
        public IEnumerator SessionSemantics_InvalidSession_Returns404()
        {
            string fakeSessionId = "fake-session-123";

            JSONObject pingReq = new JSONObject();
            pingReq["jsonrpc"] = "2.0";
            pingReq["id"] = 2;
            pingReq["method"] = "ping";

            using (UnityWebRequest req2 = new UnityWebRequest(_url, "POST"))
            {
                byte[] jsonToSend = new UTF8Encoding().GetBytes(pingReq.ToString());
                req2.uploadHandler = new UploadHandlerRaw(jsonToSend);
                req2.downloadHandler = new DownloadHandlerBuffer();
                req2.SetRequestHeader("Content-Type", "application/json");
                req2.SetRequestHeader("Mcp-Session-Id", fakeSessionId);

                yield return req2.SendWebRequest();

                Assert.AreEqual(404, req2.responseCode, "Invalid session should return 404");
            }
        }
    }
}
