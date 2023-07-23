using GCServices.CustomerPortal.Contract.Models;
using Newtonsoft.Json;
using Plugin.LocalNotification;
using System.Net.WebSockets;
using System.Text;

namespace GCServices.CustomerPortal;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
        LocalNotificationCenter.Current.NotificationActionTapped += Current_NotificationActionTapped;
        _ = ConnectWebSocketAsync();

    }
    private void Current_NotificationActionTapped(Plugin.LocalNotification.EventArgs.NotificationActionEventArgs e)
    {
        if (e.IsDismissed)
        {

        }
        else if (e.IsTapped)
        {

        }
    }
    private async Task ConnectWebSocketAsync()
    {
        ClientWebSocket webSocket = null;
        if (webSocket == null || webSocket.State != WebSocketState.Open)
        {
            try
            {
                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(new Uri("wss://gcservice-customer-portal-api.azurewebsites.net//api/notifications"), CancellationToken.None);

                var buffer = new byte[1024];
                WebSocketReceiveResult result;

                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), default);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // Handle the received message here
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        // For demonstration purposes, we'll just print the received message
                        Console.WriteLine("Received Message from Server: " + message);
                        PushNotification(JsonConvert.DeserializeObject<Message>(message));
                    }

                } while (!result.CloseStatus.HasValue);

                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Handle connection errors
                Console.WriteLine("WebSocket connection error: " + ex.Message);
            }
        }
        else
        {
            // WebSocket is already connected
        }
    }
    private void PushNotification(Message message) {
        var request = new NotificationRequest
        {
            NotificationId = message.Id,
            Title = message.Title,
            Subtitle = message.SubTitle,
            Description = message.Description,
            BadgeNumber = message.BadgeNumber,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = DateTime.Now.AddSeconds(5),
                NotifyRepeatInterval = TimeSpan.FromDays(1),
            }
        };

        LocalNotificationCenter.Current.Show(request);
    }
}
