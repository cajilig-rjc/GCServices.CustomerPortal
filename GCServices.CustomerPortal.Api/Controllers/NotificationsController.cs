using GCServices.CustomerPortal.Contract.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace GCServices.CustomerPortal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, WebSocket> ConnectedClients = new ConcurrentDictionary<string, WebSocket>();

        [HttpGet]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                var clientId = GenerateClientId();

                ConnectedClients.TryAdd(clientId, webSocket);

                await ListenWebSocket(clientId, webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        private async Task ListenWebSocket(string clientId, WebSocket webSocket)
        {
            var buffer = new byte[1024];
            WebSocketReceiveResult result;

            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), default);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    // Process the received message here (if needed)
                }

            } while (!result.CloseStatus.HasValue);

            WebSocket removedWebSocket;
            ConnectedClients.TryRemove(clientId, out removedWebSocket);
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody]Message message)
        {
            foreach (var clientWebSocket in ConnectedClients.Values)
            {
                if (clientWebSocket.State == WebSocketState.Open)
                {
                    var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
                    await clientWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }

            return Ok();
        }

        private static string GenerateClientId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
