using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Mcp.Editor.Commands;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;

namespace Mcp.Editor.Transport
{
    public class StreamableHttpTransport
    {
        private HttpListener _listener;
        private Thread _serverThread;
        private Thread _keepAliveThread;
        private bool _isRunning;
        private readonly string _endpointPath;
        private readonly string _endpointUrl;

        // sessionId -> SseStream mapping
        private readonly ConcurrentDictionary<string, SseStream> _sseStreams = new ConcurrentDictionary<string, SseStream>();

        // We delegate command handling to this action
        public Action<JSONObject, Action<JSONObject>, Action<int, string>> OnMessageReceived;

        public StreamableHttpTransport(string urlPrefix, string endpointPath)
        {
            // URL prefix must end with slash. e.g. "http://127.0.0.1:18008/mcp/"
            if (!urlPrefix.EndsWith("/")) urlPrefix += "/";
            _endpointPath = endpointPath.StartsWith("/") ? endpointPath : "/" + endpointPath;
            // E.g. "http://127.0.0.1:18008/mcp"
            _endpointUrl = urlPrefix.TrimEnd('/');

            _listener = new HttpListener();
            _listener.Prefixes.Add(urlPrefix);
        }

        public void Start()
        {
            if (_isRunning) return;
            try
            {
                _listener.Start();
                _isRunning = true;

                _serverThread = new Thread(ServerLoop) { IsBackground = true, Name = "McpServerLoop" };
                _serverThread.Start();

                _keepAliveThread = new Thread(KeepAliveLoop) { IsBackground = true, Name = "McpKeepAliveLoop" };
                _keepAliveThread.Start();

                Logging.Log($"Transport started listening on {_endpointUrl}");
            }
            catch (Exception e)
            {
                Logging.LogException(e, "Transport Start");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;

            foreach (var stream in _sseStreams.Values)
            {
                stream.Dispose();
            }
            _sseStreams.Clear();

            try { _listener.Stop(); _listener.Close(); } catch { }
            try { _serverThread?.Join(1000); } catch { }
            try { _keepAliveThread?.Join(1000); } catch { }

            Logging.Log("Transport stopped.");
        }

        private void ServerLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(ProcessRequest, context);
                }
                catch (HttpListenerException)
                {
                    // Normal during Stop()
                }
                catch (ThreadAbortException)
                {
                    // Normal during Unity domain reload
                }
                catch (Exception e)
                {
                    if (_isRunning) Logging.LogException(e, "ServerLoop");
                }
            }
        }

        private void KeepAliveLoop()
        {
            while (_isRunning)
            {
                Thread.Sleep(5000); // 5 sec interval for SSE Keep-Alive ping
                foreach (var kvp in _sseStreams.ToArray())
                {
                    var stream = kvp.Value;
                    if (!stream.IsAlive)
                    {
                        _sseStreams.TryRemove(kvp.Key, out _);
                        McpSession.InvalidateSession(kvp.Key);
                        continue;
                    }
                    stream.SendKeepAlive();
                }
            }
        }

        private void ProcessRequest(object state)
        {
            var context = (HttpListenerContext)state;
            var request = context.Request;
            var response = context.Response;

            try
            {
                // Basic CORS
                response.AppendHeader("Access-Control-Allow-Origin", "*");
                response.AppendHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AppendHeader("Access-Control-Allow-Headers", "Content-Type, Mcp-Session-Id");

                // We only bind to localhost by default via prefixes, but extra check if needed
                if (!request.IsLocal && !request.RemoteEndPoint.Address.Equals(IPAddress.Loopback))
                {
                    SendStatusCode(response, 403, "Forbidden - Localhost only");
                    return;
                }

                if (request.HttpMethod == "OPTIONS")
                {
                    SendStatusCode(response, 200);
                    return;
                }

                string requestPath = request.Url.AbsolutePath.TrimEnd('/');
                string expectedPath = _endpointPath.TrimEnd('/');

                if (requestPath != expectedPath)
                {
                    SendStatusCode(response, 404, "Not Found");
                    return;
                }

                if (request.HttpMethod == "GET")
                {
                    HandleGetSse(context);
                }
                else if (request.HttpMethod == "POST")
                {
                    HandlePostJsonRpc(context);
                }
                else
                {
                    SendStatusCode(response, 405, "Method Not Allowed");
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "ProcessRequest");
                try { SendStatusCode(response, 500); } catch { }
            }
        }

        private void HandleGetSse(HttpListenerContext context)
        {
            var response = context.Response;
            string sessionId = McpSession.CreateSession();

            var sseStream = new SseStream(response, sessionId);
            _sseStreams.TryAdd(sessionId, sseStream);

            // Send standard MCP SSE initialization events
            // endpoint event tells the client where to send POSTs
            sseStream.SendEvent("endpoint", $"{_endpointUrl}?sessionId={sessionId}");

            // Custom info event to pass the sessionId explicitly if the client wants it
            var connectedMsg = new JSONObject();
            connectedMsg["status"] = "connected";
            connectedMsg["sessionId"] = sessionId;
            sseStream.SendEvent("info", connectedMsg.ToString());

            // Do NOT call response.Close() here, Stream is kept alive
        }

        private void HandlePostJsonRpc(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                string body = reader.ReadToEnd();
                var jsonNode = JSON.Parse(body);

                if (jsonNode == null)
                {
                    SendJsonResponse(response, JsonRpc.CreateError(null, JsonRpc.ParseError, "Parse error").ToString(), 400);
                    return;
                }

                // If the message is an array, technically JSON-RPC supports batching, but for simplicity we assume single object
                if (jsonNode is JSONArray)
                {
                    SendJsonResponse(response, JsonRpc.CreateError(null, JsonRpc.InvalidRequest, "Batching not supported").ToString(), 400);
                    return;
                }

                var jsonObj = jsonNode as JSONObject;
                if (jsonObj == null || !jsonObj.HasKey("method"))
                {
                    SendJsonResponse(response, JsonRpc.CreateError(jsonObj?["id"], JsonRpc.InvalidRequest, "Missing method").ToString(), 400);
                    return;
                }

                // Delegate to command router
                OnMessageReceived?.Invoke(jsonObj,
                    (res) => SendJsonResponse(response, JsonRpc.CreateResponse(jsonObj["id"], res).ToString(), 200),
                    (code, msg) => SendJsonResponse(response, JsonRpc.CreateError(jsonObj["id"], code, msg).ToString(), 200) // JSON-RPC errors are usually HTTP 200 with error payload
                );
            }
        }

        public void BroadcastNotification(string method, JSONObject parameters = null)
        {
            var notif = JsonRpc.CreateNotification(method, parameters);
            string data = notif.ToString();

            // Broadast to all active SSE streams
            foreach (var stream in _sseStreams.Values)
            {
                if (stream.IsAlive)
                {
                    // As per spec, mostly for notifications we stream them as "message" events
                    stream.SendEvent("message", data);
                }
            }
        }

        private void SendStatusCode(HttpListenerResponse response, int statusCode, string statusDescription = null)
        {
            response.StatusCode = statusCode;
            if (statusDescription != null)
                response.StatusDescription = statusDescription;
            response.Close();
        }

        private void SendJsonResponse(HttpListenerResponse response, string json, int statusCode)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "SendJsonResponse");
            }
        }
    }
}
