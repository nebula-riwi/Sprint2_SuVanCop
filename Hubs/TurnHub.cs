using Microsoft.AspNetCore.SignalR;

namespace SuVanCop.Hubs;

public class TurnHub : Hub
{
    public async Task NotifyTurn(int id, int number, string type)
    {
        await Clients.All.SendAsync("UpdateTurn", id, number, type);
    }

    public async Task NotifyAppointment(string patientName, string doctorName, string office)
    {
        await Clients.All.SendAsync("UpdateAppointment", patientName, doctorName, office);
    }
}
