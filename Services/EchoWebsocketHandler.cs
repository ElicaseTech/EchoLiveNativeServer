using EchoLiveNativeServer.Interfaces;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace EchoLiveNativeServer.Services
{
    class EchoWebSocketHandler : IWebSocketHandler
    {

        private readonly WebSocketManagerService _manager;

        public EchoWebSocketHandler(WebSocketManagerService manager)
        {
            _manager = manager;
        }

        public async Task HandleAsync(WebSocket socket)
        {
            var id = _manager.AddSocket(socket);
            var buffer = new byte[1024 * 4];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    var receivedText = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"收到消息: {receivedText}");
                }
            }
            finally
            {
                _manager.RemoveSocket(id);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "连接关闭", CancellationToken.None);
            }
        }
    }
}