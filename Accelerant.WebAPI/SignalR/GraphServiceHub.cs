using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Accelerant.WebAPI.SignalR
{
    public class GraphServiceHub : Hub
    {
        private readonly static ConnectionMapping<string> _connections =
            new ConnectionMapping<string>();

        public override Task OnConnectedAsync()
        {
            string name = Context.User.Identity.Name;

            var httpContext = Context.GetHttpContext();
            var taskGraphId = httpContext.Request.Query["taskGraphId"];
            Groups.AddToGroupAsync(Context.ConnectionId, taskGraphId);

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception e)
        {
            return base.OnDisconnectedAsync(e);
        }
    }
}
