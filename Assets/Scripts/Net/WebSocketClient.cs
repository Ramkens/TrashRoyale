using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TrashRoyale.Net
{
    /// <summary>
    /// Minimal WebSocket client wrapper around System.Net.WebSockets.ClientWebSocket.
    /// Pumps callbacks on the Unity main thread via Update().
    ///
    /// Includes optional auto-reconnect with exponential backoff so a
    /// brief Render restart (server is on free tier — short cold starts
    /// happen on redeploy) doesn't tear down the match. Enable via
    /// <c>EnableAutoReconnect()</c>; the client will retry the same URL
    /// after disconnect for up to <c>MaxReconnectSeconds</c>.
    /// </summary>
    public class WebSocketClient
    {
        ClientWebSocket _ws;
        CancellationTokenSource _cts;
        readonly ConcurrentQueue<string> _inbox = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<Action> _events = new ConcurrentQueue<Action>();

        public Action OnOpen;
        public Action<string> OnMessage;
        public Action<int, string> OnClose;
        public Action OnReconnecting;
        public Action OnReconnected;

        public bool IsOpen => _ws != null && _ws.State == WebSocketState.Open;

        // ---- Auto-reconnect state ----
        bool _autoReconnect;
        string _lastUrl;
        float _maxReconnectSeconds = 60f;
        int _reconnectAttempt;
        bool _userClosed;

        public void EnableAutoReconnect(float maxReconnectSeconds = 60f)
        {
            _autoReconnect = true;
            _maxReconnectSeconds = maxReconnectSeconds;
        }

        public async void Connect(string url)
        {
            _lastUrl = url;
            _userClosed = false;
            await ConnectInternal(url, isReconnect: false);
        }

        async Task ConnectInternal(string url, bool isReconnect)
        {
            try
            {
                _ws = new ClientWebSocket();
                _cts = new CancellationTokenSource();
                await _ws.ConnectAsync(new Uri(url), _cts.Token);
                _reconnectAttempt = 0;
                _events.Enqueue(() =>
                {
                    if (isReconnect) OnReconnected?.Invoke();
                    OnOpen?.Invoke();
                });
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WS] connect error: {ex.Message}");
                _events.Enqueue(() => OnClose?.Invoke(-1, ex.Message));
                ScheduleReconnect();
            }
        }

        async Task ReceiveLoop()
        {
            var buf = new byte[8192];
            try
            {
                while (_ws != null && _ws.State == WebSocketState.Open)
                {
                    var seg = new ArraySegment<byte>(buf);
                    var sb = new StringBuilder();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(seg, _cts.Token);
                        sb.Append(Encoding.UTF8.GetString(buf, 0, result.Count));
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _events.Enqueue(() => OnClose?.Invoke((int)result.CloseStatus, result.CloseStatusDescription));
                        ScheduleReconnect();
                        return;
                    }
                    var msg = sb.ToString();
                    _inbox.Enqueue(msg);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WS] recv error: {ex.Message}");
                _events.Enqueue(() => OnClose?.Invoke(-1, ex.Message));
                ScheduleReconnect();
            }
        }

        void ScheduleReconnect()
        {
            if (!_autoReconnect || _userClosed || string.IsNullOrEmpty(_lastUrl)) return;
            _reconnectAttempt++;
            // Exponential backoff capped at 5s.
            float delay = Mathf.Min(5f, 0.5f * Mathf.Pow(1.5f, _reconnectAttempt - 1));
            float total = delay * _reconnectAttempt;
            if (total > _maxReconnectSeconds)
            {
                Debug.LogWarning("[WS] giving up auto-reconnect after " + total + "s");
                return;
            }
            _events.Enqueue(() => OnReconnecting?.Invoke());
            _ = ReconnectAfterDelay(delay);
        }

        async Task ReconnectAfterDelay(float seconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            if (_userClosed) return;
            await ConnectInternal(_lastUrl, isReconnect: true);
        }

        public async void Send(string text)
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WS] send error: {ex.Message}");
            }
        }

        public async void Close()
        {
            _userClosed = true;
            try
            {
                _cts?.Cancel();
                if (_ws != null && _ws.State == WebSocketState.Open)
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                }
            }
            catch { }
            _ws?.Dispose();
            _ws = null;
        }

        public void Update()
        {
            while (_events.TryDequeue(out var ev)) ev?.Invoke();
            while (_inbox.TryDequeue(out var msg)) OnMessage?.Invoke(msg);
        }
    }
}
