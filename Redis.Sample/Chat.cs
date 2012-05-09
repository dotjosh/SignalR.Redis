using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SignalR.Hubs;
using System.Threading.Tasks;

namespace Redis.Sample
{
    public class Chat : Hub, IConnected, IDisconnect
    {
        public void Send(string message)
        {
            // Call the addMessage method on all clients
            Clients.addMessage(message);
        }

        public Task Connect()
        {
            return Clients.join(Context.ConnectionId);
        }

        public Task Reconnect(IEnumerable<string> groups)
        {
            return Clients.reconnect(Context.ConnectionId);
        }

        public Task Disconnect()
        {
            return Clients.drop(Context.ConnectionId);
        }
    }
}