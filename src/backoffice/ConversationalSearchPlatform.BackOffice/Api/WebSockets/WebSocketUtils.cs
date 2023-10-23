using System.Net.WebSockets;
using System.Text;

namespace ConversationalSearchPlatform.BackOffice.Api.WebSockets;

public static class WebSocketUtils
{

    public static async Task ReceiveMessage(WebSocket ws, Func<byte[], int, Task> parseMessage)
    {
        var buffer = new byte[1024 * 4];

        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                await parseMessage(buffer, result.Count);
            }
            else if (result.MessageType == WebSocketMessageType.Close || ws.State == WebSocketState.Aborted)
            {
                if (result.CloseStatus != null)
                    await ws.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
        }
    }

    public static async Task SendResponse(WebSocket ws, string message)
    {
        // maybe convert to generics
        var bytes = Encoding.UTF8.GetBytes(message);

        if (ws.State == WebSocketState.Open)
        {
            var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
            await ws.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}