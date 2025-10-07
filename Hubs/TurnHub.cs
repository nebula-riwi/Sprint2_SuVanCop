using Microsoft.AspNetCore.SignalR;

namespace SuVanCop.Hubs;

public class TurnHub : Hub
{
    public async Task NotifyTurn(int number, string type, string desk)
    {
        await Clients.All.SendAsync("UpdateTurn", number, type, desk);
    }
}