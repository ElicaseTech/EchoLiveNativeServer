using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace EchoLiveNativeServer.Interfaces
{
    public interface IWebSocketHandler
    {
        Task HandleAsync(WebSocket socket);
    }
}