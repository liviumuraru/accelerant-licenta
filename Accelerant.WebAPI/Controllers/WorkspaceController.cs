using Accelerant.Services.Mongo;
using Accelerant.WebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using Accelerant.Services.Collectors;
using static Accelerant.DataTransfer.Mapping.MapperConfig;
using System.Linq;

namespace Accelerant.WebAPI.Controllers
{
    public class AddUserToWorkspaceModel
    {
        public Guid workspaceId;
        public string newUserName;
        public Guid thisUserId;
    }

    public class RemoveUserFromWorkspaceModel
    {
        public Guid workspaceId;
        public string userName;
        public Guid thisUserId;
    }


    [Route("workspace")]
    [ApiController]
    [Authorize]
    public class WorkspaceController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get([FromQuery]Guid WorkspaceId, [FromQuery]Guid UserId)
        {
            var workspace = ServiceFactory.WorkspaceService.Get(WorkspaceId);
            if (!workspace.ActiveUsers.Contains(UserId))
                return new NotFoundResult();

            var newItem = new { data = workspace, users = new List<Object>() };
            foreach (var userId in workspace.ActiveUsers)
            {
                var userData = ServiceFactory.UsersService.Get(userId);
                newItem.users.Add(new { name = userData.Name, id = userData.Id });
            }

            return Ok(newItem);
        }

        [HttpPost]
        [Route("users/remove")]
        public IActionResult RemoveUser([FromBody]RemoveUserFromWorkspaceModel removeUserModel)
        {
            var ws = ServiceFactory.WorkspaceService.Get(removeUserModel.workspaceId);
            if (!ws.ActiveUsers.Contains(removeUserModel.thisUserId))
            {
                return Unauthorized();
            }

            var user = ServiceFactory.UsersService.GetByName(removeUserModel.userName);

            if (user == null)
                return NotFound();
            if (!ws.ActiveUsers.Contains(user.Id.Value))
            {
                return BadRequest();
            }
            ((List<Guid>)ws.ActiveUsers).RemoveAll(usr => user.Id.Value.Equals(usr));
            ServiceFactory.WorkspaceService.Update(ws);
            foreach (var tgId in ws.TaskGraphIds)
            {
                var tg = ServiceFactory.TaskGraphService.Get(tgId);
                if (!tg.ActiveUsers.Contains(user.Id.Value))
                {
                    return BadRequest();
                }
                var newUserList = new List<Guid>(tg.ActiveUsers);
                newUserList.RemoveAll(usr => user.Id.Value.Equals(usr));
                tg.ActiveUsers = newUserList;
                var tgModel = Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<DataTransfer.Models.TaskGraph, DataLayer.Models.TaskGraph>(tg);
                DataCollectorFactory.taskGraphCollector.Update(tgModel);
            }
            return Ok(true);
        }

        [HttpPost]
        [Route("users/add")]
        public IActionResult AddUser([FromBody]AddUserToWorkspaceModel addUserModel)
        {
            var ws = ServiceFactory.WorkspaceService.Get(addUserModel.workspaceId);
            if(!ws.ActiveUsers.Contains(addUserModel.thisUserId))
            {
                return Unauthorized();
            }
            
            var user = ServiceFactory.UsersService.GetByName(addUserModel.newUserName);

            if (user == null)
                return NotFound();
            if (ws.ActiveUsers.Contains(user.Id.Value))
            {
                return BadRequest();
            }
            ws.ActiveUsers.Add(user.Id.Value);
            ServiceFactory.WorkspaceService.Update(ws);
            foreach(var tgId in ws.TaskGraphIds)
            {
                var tg = ServiceFactory.TaskGraphService.Get(tgId);
                if (tg.ActiveUsers.Contains(user.Id.Value))
                {
                    return BadRequest();
                }
                var newUserList = new List<Guid>(tg.ActiveUsers);
                newUserList.Add(user.Id.Value);
                tg.ActiveUsers = newUserList;
                var tgModel = Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<DataTransfer.Models.TaskGraph, DataLayer.Models.TaskGraph>(tg);
                DataCollectorFactory.taskGraphCollector.Update(tgModel);
            }
            return Ok(true);
        }

        [HttpGet]
        [Route("all")]
        public IActionResult GetAllForUser([FromQuery]Guid UserId)
        {
            return Ok(ServiceFactory.WorkspaceService.GetAllForUser(UserId));
        }

        [HttpPost]
        [Route("add")] 
        public IActionResult Add([FromBody]WorkspaceAddModel workSpace)
        {
            var userList = new List<Guid>();
            userList.Add(workSpace.UserId);

            var workSpaceData = new DataTransfer.Models.Workspace
            {
                Description = workSpace.Description,
                Id = Guid.NewGuid(),
                Name = workSpace.Name,
                TaskGraphIds = null,
                ActiveUsers = userList
            };

            return Ok(ServiceFactory.WorkspaceService.Add(workSpaceData));
        }
    }
}
