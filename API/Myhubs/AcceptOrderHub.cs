using Microsoft.AspNetCore.SignalR;

namespace API.Myhubs
{
    public class AcceptOrderHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("UserConnected");
        }
    }
}
