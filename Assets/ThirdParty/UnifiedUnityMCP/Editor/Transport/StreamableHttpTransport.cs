using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;

namespace Mcp.Editor.Transport
{
    public class StreamableHttpTransport
    {
        private const int InvalidSessionErrorCode = -32001;

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
                if (!IsLocalRequest(request))
                {
                    SendStatusCode(response, 403, "Forbidden - Localhost only");
                    return;
                }

                if (!IsAllowedOrigin(request, out string allowedOrigin))
                {
                    SendStatusCode(response, 403, "Forbidden - Origin is not allowed");
                    return;
                }

                AppendCorsHeaders(response, allowedOrigin);

                if (request.HttpMethod == "OPTIONS")
                {
                    SendStatusCode(response, 200);
                    return;
                }

                string requestPath = request.Url != null ? request.Url.AbsolutePath.TrimEnd('/') : string.Empty;
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
                else if (request.HttpMethod == "DELETE")
                {
                    HandleDeleteSession(context);
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

        private bool IsLocalRequest(HttpListenerRequest request)
        {
            if (request == null)
            {
                return false;
            }

            if (request.IsLocal)
            {
                return true;
            }

            if (request.RemoteEndPoint == null)
            {
                return false;
            }

            return IPAddress.IsLoopback(request.RemoteEndPoint.Address);
        }

        private bool IsAllowedOrigin(HttpListenerRequest request, out string allowedOrigin)
        {
            allowedOrigin = "http://127.0.0.1";
            string origin = request.Headers["Origin"];

            if (string.IsNullOrEmpty(origin))
            {
                return true;
            }

            if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri originUri))
            {
                return false;
            }

            if (!string.Equals(originUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(originUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(originUri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                allowedOrigin = origin;
                return true;
            }

            if (IPAddress.TryParse(originUri.Host, out IPAddress parsedAddress) && IPAddress.IsLoopback(parsedAddress))
            {
                allowedOrigin = origin;
                return true;
            }

            if (request.Url != null && string.Equals(originUri.Host, request.Url.Host, StringComparison.OrdinalIgnoreCase))
            {
                allowedOrigin = origin;
                return true;
            }

            return false;
        }

        private void AppendCorsHeaders(HttpListenerResponse response, string allowedOrigin)
        {
            response.Headers["Access-Control-Allow-Origin"] = allowedOrigin;
            response.Headers["Vary"] = "Origin";
            response.Headers["Access-Control-Allow-Methods"] = "GET, POST, DELETE, OPTIONS";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Accept, Origin, Mcp-Session-Id";
        }

        private bool Accepts(HttpListenerRequest request, string mediaTypeA, string mediaTypeB = null)
        {
            string accept = request.Headers["Accept"];
            if (string.IsNullOrEmpty(accept))
            {
                return true;
            }

            string lowered = accept.ToLowerInvariant();
            if (lowered.Contains("*/*"))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(mediaTypeA) && lowered.Contains(mediaTypeA.ToLowerInvariant()))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(mediaTypeB) && lowered.Contains(mediaTypeB.ToLowerInvariant()))
            {
                return true;
            }

            return false;
        }

        private bool IsJsonContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return true;
            }

            return contentType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void HandleGetSse(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (!Accepts(request, "text/event-stream"))
            {
                SendStatusCode(response, 406, "Not Acceptable - GET requires Accept: text/event-stream");
                return;
            }

            string sessionId = request.Headers["Mcp-Session-Id"];
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = McpSession.CreateSession();
            }
            else if (!McpSession.IsValidSession(sessionId))
            {
                SendStatusCode(response, 404, "Invalid or expired session");
                return;
            }

            if (_sseStreams.TryRemove(sessionId, out SseStream previousStream))
            {
                previousStream.Dispose();
            }

            response.Headers["Mcp-Session-Id"] = sessionId;

            var sseStream = new SseStream(response, sessionId);
            _sseStreams.TryAdd(sessionId, sseStream);

            // endpoint event tells the client where to send POSTs.
            // Streamable HTTP uses a single endpoint path.
            sseStream.SendEvent("endpoint", _endpointUrl);

            var connectedMsg = new JSONObject();
            connectedMsg["status"] = "connected";
            connectedMsg["sessionId"] = sessionId;
            sseStream.SendEvent("info", connectedMsg.ToString());

            // Do not close response. SSE stream remains open.
        }

        private void HandleDeleteSession(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            string sessionId = request.Headers["Mcp-Session-Id"];
            if (string.IsNullOrEmpty(sessionId) && request.QueryString != null)
            {
                sessionId = request.QueryString["sessionId"];
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                SendStatusCode(response, 400, "Missing Mcp-Session-Id header");
                return;
            }

            if (_sseStreams.TryRemove(sessionId, out SseStream stream))
            {
                stream.Dispose();
            }

            McpSession.InvalidateSession(sessionId);
            SendStatusCode(response, 204);
        }

        private void HandlePostJsonRpc(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (!Accepts(request, "application/json", "text/event-stream"))
            {
                SendStatusCode(response, 406, "Not Acceptable - POST requires Accept: application/json or text/event-stream");
                return;
            }

            if (!IsJsonContentType(request.ContentType))
            {
                SendStatusCode(response, 415, "Unsupported Media Type - expected application/json");
                return;
            }

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

                string methodName = jsonObj["method"] != null ? jsonObj["method"].Value : string.Empty;
                bool isInitialize = string.Equals(methodName, "initialize", StringComparison.Ordinal);

                string sessionId = request.Headers["Mcp-Session-Id"];
                if (!string.IsNullOrEmpty(sessionId))
                {
                    if (!McpSession.IsValidSession(sessionId))
                    {
                        SendJsonResponse(response, JsonRpc.CreateError(jsonObj["id"], InvalidSessionErrorCode, "Invalid or expired session id").ToString(), 400);
                        return;
                    }
                }
                else if (isInitialize)
                {
                    sessionId = McpSession.CreateSession();
                }

                if (!string.IsNullOrEmpty(sessionId))
                {
                    response.Headers["Mcp-Session-Id"] = sessionId;
                }

                if (OnMessageReceived == null)
                {
                    SendJsonResponse(response, JsonRpc.CreateError(jsonObj["id"], JsonRpc.InternalError, "Command router is not initialized").ToString(), 500);
                    return;
                }

                // Delegate to command router
                OnMessageReceived.Invoke(jsonObj,
                    (res) => SendJsonResponse(response, JsonRpc.CreateResponse(jsonObj["id"], res).ToString(), 200),
                    (code, msg) => SendJsonResponse(response, JsonRpc.CreateError(jsonObj["id"], code, msg).ToString(), 200)
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
                response.ContentEncoding = Encoding.UTF8;
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
