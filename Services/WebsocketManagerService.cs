using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace EchoLiveNativeServer.Services
{
    public class WebSocketManagerService
    {
        private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new();
        public Guid AddSocket(WebSocket socket)
        {
            var id = Guid.NewGuid();
            _sockets.TryAdd(id, socket);
            return id;
        }
        public void RemoveSocket(Guid id)
        {
            _sockets.TryRemove(id, out _);
        }
        public async Task BroadcastAsync(string message)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);

            foreach (var socket in _sockets.Values)
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}