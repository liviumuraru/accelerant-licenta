using Accelerant.DataTransfer.Models;
using Accelerant.Services.Mongo;
using Accelerant.WebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Accelerant.WebAPI.Controllers
{
    [Route("graph")]
    [ApiController]
    [Authorize]
    public class TaskGraphController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get([FromQuery]Guid TaskGraphId, [FromQuery]Guid UserId)
        {
            var taskGraph = ServiceFactory.TaskGraphService.Get(TaskGraphId);
            if (!taskGraph.ActiveUsers.Contains(UserId))
                return new NotFoundResult();
            return Ok(taskGraph);
        }

        [HttpGet]
        [Route("graph")]
        public IActionResult GetGraphModel([FromQuery]Guid TaskGraphId, [FromQuery]Guid UserId)
        {
            var taskGraph = ServiceFactory.TaskGraphService.Get(TaskGraphId);
            if (!taskGraph.ActiveUsers.Contains(UserId))
                return new NotFoundResult();
            var taskGraphModel = ServiceFactory.TaskGraphService.GetGraph(TaskGraphId);
            if (taskGraphModel == null) return new NotFoundResult();
            return Ok(taskGraphModel);
        }

        [HttpGet]
        [Route("nodes")]
        public IActionResult GetGraphNodes([FromQuery]Guid TaskGraphId, [FromQuery]Guid UserId)
        {
            var taskGraph = ServiceFactory.TaskGraphService.Get(TaskGraphId);
            if (!taskGraph.ActiveUsers.Contains(UserId) || taskGraph.TaskSetId == null)
                return new NotFoundResult();
            var tasks = ServiceFactory.TaskSetService.Get(taskGraph.TaskSetId.Value);
            return Ok(tasks.Tasks);
        }

        [HttpGet]
        [Route("edges")]
        public IActionResult GetGraphEdges([FromQuery]Guid TaskGraphId, [FromQuery]Guid UserId)
        {
            var taskGraph = ServiceFactory.TaskGraphService.Get(TaskGraphId);
            if (!taskGraph.ActiveUsers.Contains(UserId) || taskGraph.TaskSetId == null)
                return new NotFoundResult();
            var tasks = ServiceFactory.TaskSetService.Get(taskGraph.TaskSetId.Value).Tasks;

            var edgeList = new List<Tuple<Guid, Guid>>();

            foreach(var task in tasks)
            {
                foreach(var dest in task.OutNeighbors)
                {
                    edgeList.Add(Tuple.Create(task.Data.Id, dest));
                }
            }

            return Ok(edgeList);
        }

        [HttpGet]
        [Route("all")]
        public IActionResult GetAllForWorkspace([FromQuery]Guid UserId, [FromQuery]Guid WorkspaceId)
        {
            return Ok(ServiceFactory.TaskGraphService.GetAllForWorkspace(UserId, WorkspaceId));
        }

        [HttpPost]
        [Route("add")]
        public IActionResult Add([FromBody]TaskGraphAddModel taskGraph)
        {
            var userList = new List<Guid>();
            userList.Add(taskGraph.UserId);
            var tgModel = new TaskGraph
            {
                Id = Guid.NewGuid(),
                Description = taskGraph.Description,
                Name = taskGraph.Name,
                RootId = null,
                TaskSetId = null,
                ActiveUsers = userList,
                WorkspaceId = taskGraph.WorkspaceId
            };

            return Ok(ServiceFactory.TaskGraphService.Add(tgModel));
        }
    }
}