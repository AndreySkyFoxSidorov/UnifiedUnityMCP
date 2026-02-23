using System;
using System.Net;
using System.Text;
using System.Threading;
using Mcp.Editor.Util;

namespace Mcp.Editor.Transport
{
    public class SseStream : IDisposable
    {
        private readonly HttpListenerResponse _response;
        private readonly string _sessionId;
        private bool _isDisposed;
        private readonly byte[] _keepAliveBytes = Encoding.UTF8.GetBytes(":\n\n");

        public string SessionId => _sessionId;
        public bool IsAlive => !_isDisposed;

        public SseStream(HttpListenerResponse response, string sessionId)
        {
            _response = response;
            _sessionId = sessionId;

            try
            {
                _response.ContentType = "text/event-stream";
                _response.Headers.Add("Cache-Control", "no-cache");
                _response.Headers.Add("Connection", "keep-alive");
                _response.KeepAlive = true;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "SseStream Init");
                Dispose();
            }
        }

        public void SendEvent(string eventName, string data)
        {
            if (_isDisposed) return;

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(eventName))
                sb.Append($"event: {eventName}\n");
            
            // SSE requires double newline at the end of data
            sb.Append($"data: {data}\n\n");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            
            lock (_response)
            {
                try
                {
                    _response.OutputStream.Write(bytes, 0, bytes.Length);
                    _response.OutputStream.Flush();
                }
                catch
                {
                    // If write fails, the client probably disconnected
                    Dispose();
                }
            }
        }

        public void SendKeepAlive()
        {
            if (_isDisposed) return;

            lock (_response)
            {
                try
                {
                    _response.OutputStream.Write(_keepAliveBytes, 0, _keepAliveBytes.Length);
                    _response.OutputStream.Flush();
                }
                catch
                {
                    Dispose();
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _response.Close();
            }
            catch { }
        }
    }
}
