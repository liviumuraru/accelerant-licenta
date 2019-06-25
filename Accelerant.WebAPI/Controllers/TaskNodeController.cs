using Accelerant.Services;
using Accelerant.Services.Collectors;
using Accelerant.Services.Mongo;
using Accelerant.WebAPI.Models;
using Accelerant.WebAPI.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using static Accelerant.DataLayer.Models.TaskData;
using static Accelerant.DataTransfer.Mapping.MapperConfig;

namespace Accelerant.WebAPI.Controllers
{
    public class AssignTaskModel
    {
        public Guid SelfUserId;
        public Guid NewUserId;
        public Guid TaskId;
        public Guid TaskGraphId;
    }

    public class TaskNodeSetRootModel
    {
        public Guid userId;
        public Guid TaskId;
        public Guid TaskGraphId;
    }

    public class AddLinkModel
    {
        public Guid ParentId;
        public Guid ChildId;
        public Guid TaskGraphId;
    }

    public class DeleteTaskModel
    {
        public Guid userId;
        public Guid taskId;
        public Guid taskGraphId;
    }

    public class UpdateTaskModel
    {
        public Guid taskId;
        public Guid userId;
        public Guid taskGraphId;
        public string name;
        public string description;
        public Guid? assignedUser;
        public Status status;
    }

    public class StatusUpdateModel
    {
        public Guid userId;
        public Guid taskId;
        public Guid taskGraphId;
        public Status newStatus;
    }

    [Route("node")]
    [ApiController]
    [Authorize]
    public class TaskNodeController : ControllerBase
    {
        private IHubContext<GraphServiceHub> hub;

        public TaskNodeController(IHubContext<GraphServiceHub> hub)
        {
            this.hub = hub;
        }

        [HttpGet]
        public IActionResult Get([FromQuery]Guid TaskGraphId, [FromQuery]Guid taskId)
        {
            var taskGraph = ServiceFactory.TaskGraphService.Get(TaskGraphId);
            
            if (taskGraph.TaskSetId.HasValue)
            {
                var taskSet = ServiceFactory.TaskSetService.Get(taskGraph.TaskSetId.Value);
                var task = taskSet.Tasks.Where(x => x.Data.Id == taskId);
                return Ok(ServiceFactory.TaskSetService.Get(taskGraph.TaskSetId.Value));
            }

            return new NotFoundResult();
        }

        [HttpPost]
        [Route("link")]
        public IActionResult AddLink([FromBody]AddLinkModel addLinkModel)
        {
            var result = ServiceFactory.TaskGraphService.AddLink(addLinkModel.TaskGraphId, addLinkModel.ParentId, addLinkModel.ChildId);
            if (result)
            {
                hub.Clients.Group(addLinkModel.TaskGraphId.ToString()).SendAsync("link_task_node", addLinkModel);
                return Ok(addLinkModel);
            }
            else
                return BadRequest();
        }

        [HttpPatch]
        [Route("update")]
        public IActionResult UpdateTask([FromBody]UpdateTaskModel updateTaskData)
        {
            var tg = ServiceFactory.TaskGraphService.Get(updateTaskData.taskGraphId);
            var ts = ServiceFactory.TaskSetService.Get(tg.TaskSetId.Value);
            if (!tg.ActiveUsers.Contains(updateTaskData.userId))
                return Unauthorized();

            var task = ts.Tasks.Where(x => x.Data.Id == updateTaskData.taskId).First();
            ts.Tasks.Remove(task);
            task.AssignedUser = updateTaskData.assignedUser;
            task.Data.Name = updateTaskData.name;
            task.Data.Description = updateTaskData.description;
            task.Data.CurrentStatus = updateTaskData.status;
            ts.Tasks.Add(task);
            var mappedTaskSet = TaskGraphService.ConvertTaskSet(ts);
            DataCollectorFactory.taskSetCollector.Update(mappedTaskSet);

            hub.Clients.Group(updateTaskData.taskGraphId.ToString()).SendAsync("update_task_node", task);
            return Ok(task);
        }

        [HttpPost]
        [Route("delete")]
        public IActionResult DeleteTask([FromBody]DeleteTaskModel deleteTaskModel)
        {
            var tg = ServiceFactory.TaskGraphService.Get(deleteTaskModel.taskGraphId);
            var ts = ServiceFactory.TaskSetService.Get(tg.TaskSetId.Value);
            if (!tg.ActiveUsers.Contains(deleteTaskModel.userId))
                return Unauthorized();

            ((List<DataTransfer.Models.TaskNode>)ts.Tasks).RemoveAll(x => x.Data.Id.Equals(deleteTaskModel.taskId));
            var taskSetModel = TaskGraphService.ConvertTaskSet(ts);

            foreach(var task in taskSetModel.Tasks)
            {
                ((List<Guid>)task.OutNeighbors).RemoveAll(x => x.Equals(deleteTaskModel.taskId));
            }

            DataCollectorFactory.taskSetCollector.Update(taskSetModel);

            if(tg.RootId.HasValue && tg.RootId.Value.Equals(deleteTaskModel.taskId))
            {
                tg.RootId = null;
                var tgModel = Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<DataTransfer.Models.TaskGraph, DataLayer.Models.TaskGraph>(tg);
                DataCollectorFactory.taskGraphCollector.Update(tgModel);
            }

            hub.Clients.Group(deleteTaskModel.taskGraphId.ToString()).SendAsync("delete_task_node", deleteTaskModel.taskId);
            return Ok(deleteTaskModel.taskId);
        }

        [HttpPost]
        [Route("assign")]
        public IActionResult Assign([FromBody]AssignTaskModel assignTaskData)
        {
            var result = ServiceFactory.TaskGraphService.AssignUserToTask(assignTaskData.NewUserId, assignTaskData.TaskId, assignTaskData.TaskGraphId);
            if (result)
            {
                hub.Clients.Group(assignTaskData.TaskGraphId.ToString()).SendAsync("assign_task_node", assignTaskData);
                return Ok(assignTaskData);
            }
            else
                return BadRequest();
        }

        [HttpPost]
        [Route("statusupdate")]
        public IActionResult ChangeStatus([FromBody]StatusUpdateModel statusUpdateData)
        {
            var result = ServiceFactory.TaskGraphService.UpdateTaskStatus(statusUpdateData.taskGraphId, statusUpdateData.taskId, statusUpdateData.newStatus);
            if (result)
            {
                hub.Clients.Group(statusUpdateData.taskGraphId.ToString()).SendAsync("update_task_node_status", statusUpdateData);
                return Ok(statusUpdateData);
            }
            else
                return BadRequest();
        }

        [HttpPost]
        [Route("add")]
        public IActionResult Add(TaskNodeAddModel taskNodeAddData)
        {
            var taskData = new DataTransfer.Events.TaskData
            {
                CurrentStatus = taskNodeAddData.TaskData.CurrentStatus,
                Description = taskNodeAddData.TaskData.Description,
                Name = taskNodeAddData.TaskData.Name,
                Id = Guid.NewGuid(),
                TaskGraphId = taskNodeAddData.TaskGraphId,
                EstimatedCompletionTime = taskNodeAddData.TaskData.EstimatedCompletionTime
            };

            var added = ServiceFactory.TaskGraphService.AddTask(taskData, taskNodeAddData.TaskGraphId);

            hub.Clients.Group(taskNodeAddData.TaskGraphId.ToString()).SendAsync("add_task_node", added);



            return Ok(added);
        }

        [HttpPost]
        [Route("markroot")]
        public IActionResult SetRoot([FromBody]TaskNodeSetRootModel setRootData)
        {
            var tg = ServiceFactory.TaskGraphService.Get(setRootData.TaskGraphId);
            var oldRootId = tg.RootId;
            var ts = ServiceFactory.TaskSetService.Get(tg.TaskSetId.Value);
            if (!tg.ActiveUsers.Contains(setRootData.userId))
                return Unauthorized();

            var task = ts.Tasks.Where(x => x.Data.Id == setRootData.TaskId).First();
            if (task.Data.CurrentStatus == Status.Blocked) return BadRequest();

            tg.RootId = task.Data.Id;
            var mappedTg = Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<DataTransfer.Models.TaskGraph, DataLayer.Models.TaskGraph>(tg);
            

            var oldTask = ts.Tasks.Where(x => x.Data.Id == oldRootId).First();
            ts.Tasks.Remove(oldTask);
            oldTask.IsRoot = false;
            ts.Tasks.Add(oldTask);

            ts.Tasks.Remove(task);
            task.IsRoot = true;
            ts.Tasks.Add(task);


            if (!TaskGraphService.RespectsRules(ts, oldRootId.Value)) return BadRequest();
            

            var mappedTaskSet = TaskGraphService.ConvertTaskSet(ts);

            var moddedTg = DataCollectorFactory.taskGraphCollector.Update(mappedTg);
            DataCollectorFactory.taskSetCollector.Update(mappedTaskSet);

            hub.Clients.Group(setRootData.TaskGraphId.ToString()).SendAsync("set_root_node", setRootData);
            return Ok(task);
        }
    }
}