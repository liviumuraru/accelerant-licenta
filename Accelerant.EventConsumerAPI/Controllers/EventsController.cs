using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accelerant.Services.Mongo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Accelerant.EventConsumerAPI.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        [HttpPost]
        public IActionResult ProcessWorkspaceAddEvent([FromBody]EventGridEvent[] events)
        {
            foreach(var ev in events)
            {
                if(ev.EventType == "Accelerant.Workspaces.AddItem")
                {
                    //ServiceFactory.WorkspaceService.Add((ev.Data as JObject).ToObject<Accelerant.DataTransfer.Events.Workspace>());
                    return Ok(ev.Data);
                }

                if (ev.EventType == "Accelerant.TaskGraphs.AddItem")
                {
                    //ServiceFactory.TaskGraphService.Add((ev.Data as JObject).ToObject<Accelerant.DataTransfer.Events.TaskGraph>());
                    return Ok(ev.Data);
                }

                if (ev.EventType == "Accelerant.TaskNodes.AddItem")
                {
                    var item = (ev.Data as JObject).ToObject<Accelerant.DataTransfer.Events.TaskData>();
                    ServiceFactory.TaskGraphService.AddTask(item, item.TaskGraphId);
                    return Ok(ev.Data);
                }

                if (ev.EventType == EventTypes.EventGridSubscriptionValidationEvent)
                {
                    var data = (ev.Data as JObject).ToObject<SubscriptionValidationEventData>();
                    var response = new SubscriptionValidationResponse(data.ValidationCode);
                    return Ok(response);
                }
            }
            
            return BadRequest();
        }
    }
}
