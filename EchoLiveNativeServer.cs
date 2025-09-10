using EchoLiveNativeServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;

namespace EchoLiveNativeServer
{
    public class EchoLiveNativeServer
    {
        private static WebApplication? _app;
        private static WebSocketManagerService? _wsService;

        public static void Register()
        {

        }

        public static void UnRegister()
        {

        }

        [UnmanagedCallersOnly(EntryPoint = "StartService")]
        public static void StartServer(int port)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                WebRootPath = "assets"
            });
            builder.WebHost.UseSetting(WebHostDefaults.PreventHostingStartupKey, "true");
            builder.Services.AddSingleton<WebSocketManagerService>();
            _app = builder.Build();
            _app.UseWebSockets();
            _app.UseStaticFiles();
            _wsService = _app.Services.GetRequiredService<WebSocketManagerService>();

            _app.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Echo-Live Server is Running.....");
            });

            _app.Map("/ws", async context =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("WebSocket 请求无效");
                    return;
                }

                var socket = await context.WebSockets.AcceptWebSocketAsync();
                var wsHandler = new EchoWebSocketHandler(_wsService);
                await wsHandler.HandleAsync(socket);
            });

            _app.MapGet("/echo-live/config.js", async context =>
            {
                var filePath = Path.Combine(_app.Environment.WebRootPath, "echo-live/config.js");
                if (!File.Exists(filePath))
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("File not found");
                    return;
                }
                var jsContent = await File.ReadAllTextAsync(filePath);
                jsContent += @"
 
 function InjectConfig() {
     const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
     const hostname = window.location.hostname;
     const port = window.location.port;
     const wsAddress = `${protocol}//${hostname}:${port}/ws`;
     config.echolive.broadcast.enable = true;
     config.echolive.broadcast.websocket_enable = true;
     config.echolive.broadcast.websocket_url = wsAddress;
     config.editor.websocket.enable = true;
     config.editor.websocket.url = wsAddress;
     config.editor.websocket.auto_url = false;
 }
 InjectConfig();";

                context.Response.ContentType = "application/javascript";
                await context.Response.WriteAsync(jsContent, Encoding.UTF8);
            });
            //_app.UseRequestLocalization(localizationOptions);
            //_app.MapGet("/hello", () => "Hello, HTTP!");
            _app.Run($"http://0.0.0.0:{port}");
        }

        [UnmanagedCallersOnly(EntryPoint = "StopService")]
        public static void StopServer()
        {
            Console.WriteLine("Stopping server...");
            _app.StopAsync().GetAwaiter().GetResult();
            _app.DisposeAsync().GetAwaiter().GetResult();
            _app = null;
            Console.WriteLine("Server stopped.");
        }

        [UnmanagedCallersOnly(EntryPoint = "SendMsg")]
        public static void sendMsg(IntPtr str, int len)
        {
            if (_wsService == null)
            {
                return;
            }
            var buffer = new byte[len];
            Marshal.Copy(str, buffer, 0, len);
            var message = System.Text.Encoding.UTF8.GetString(buffer);

            // Fire-and-forget
            _ = _wsService.BroadcastAsync(message);
        }

        [UnmanagedCallersOnly(EntryPoint = "LoadConfig")]
        public static void LoadConfig()
        {
            Console.WriteLine("Loading config...");
        }
    }
}
