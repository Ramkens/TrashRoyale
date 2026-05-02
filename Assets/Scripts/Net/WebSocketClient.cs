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

        public bool IsOpen => _ws != null && _ws.State == WebSocketState.Open;

        public async void Connect(string url)
        {
            try
            {
                _ws = new ClientWebSocket();
                _cts = new CancellationTokenSource();
                await _ws.ConnectAsync(new Uri(url), _cts.Token);
                _events.Enqueue(() => OnOpen?.Invoke());
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WS] connect error: {ex.Message}");
                _events.Enqueue(() => OnClose?.Invoke(-1, ex.Message));
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
            }
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
