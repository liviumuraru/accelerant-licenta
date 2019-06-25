using Accelerant.DataLayer.DataCollectors;
using Accelerant.DataLayer.DataProviders;
using Accelerant.DataTransfer.Mapping;
using Accelerant.DataTransfer.Models;
using Accelerant.Services.Collectors;
using Accelerant.Services.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using static Accelerant.DataTransfer.Mapping.MapperConfig;

using DLStatus = Accelerant.DataLayer.Models.TaskData.Status;

namespace Accelerant.Services
{
    public class GraphNode
    {
        public TaskData Task;
        public ICollection<GraphNode> OutNeighbors;
    }

    public interface ITaskGraphService
        : IService<TaskGraph, TaskGraph, Guid>
    {
        TaskNode GetTask(Guid taskGraphId, Guid taskId);
        TaskNode AddTask(DataTransfer.Events.TaskData item, Guid taskGraphId);
        IEnumerable<DataTransfer.Models.TaskGraph> GetAllForWorkspace(Guid UserId, Guid WorkspaceId);
        bool AssignUserToTask(Guid asigneeUserId, Guid taskId, Guid TaskGraphId);
        GraphNode GetGraph(Guid Id);
        bool AddLink(Guid taskGraphId, Guid parentId, Guid childId);
        bool UpdateTaskStatus(Guid taskGraphId, Guid taskId, DLStatus newStatus);
    }

    public class TaskGraphService
        : ITaskGraphService
    {
        private IDataProvider<DataLayer.Models.TaskGraph, Guid> dataProvider;
        private IDataCollector<DataLayer.Models.TaskGraph, DataLayer.Models.TaskGraph, DataLayer.Models.TaskGraph> dataCollector;
        private IDataProvider<DataLayer.Models.Workspace, Guid> WsDataProvider;

        public TaskGraphService(IDataProvider<DataLayer.Models.TaskGraph, Guid> dataProvider,
            IDataCollector<DataLayer.Models.TaskGraph, DataLayer.Models.TaskGraph, DataLayer.Models.TaskGraph> dataCollector,
            IDataProvider<DataLayer.Models.Workspace, Guid> WsDataProvider)
        {
            this.dataCollector = dataCollector;
            this.dataProvider = dataProvider;
            this.WsDataProvider = WsDataProvider;
        }

        public TaskGraph Add(DataTransfer.Events.TaskGraph item)
        {
            var userList = new List<Guid>();
            userList.Add(item.UserId);
            var mappedItem = new DataLayer.Models.TaskGraph
            {
                Id = item.Id,
                Description = item.Description,
                Name = item.Name,
                RootId = null,
                ActiveUsers = userList,
                TaskSetId = null
            };
            var added = dataCollector.Add(mappedItem);

            var ws = ServiceFactory.WorkspaceService.Get(item.WorkspaceId);
            if (ws.TaskGraphIds == null)
                ws.TaskGraphIds = new List<Guid>();
            ws.TaskGraphIds.Add(added.Id);

            var mappedWS = new DataLayer.Models.Workspace
            {
                Description = ws.Description,
                Id = ws.Id,
                Name = ws.Name,
                ActiveUsers = ws.ActiveUsers,
                TaskGraphIds = ws.TaskGraphIds
            };

            DataCollectorFactory.workspaceCollector.Update(mappedWS);

            return new TaskGraph
            {
                Id = added.Id,
                ActiveUsers = added.ActiveUsers,
                Description = added.Description,
                Name = added.Name,
                RootId = added.RootId,
                TaskSetId = added.TaskSetId
            };
        }

        public TaskGraph Get(Guid Id)
        {
            var dalModel = dataProvider.Get(Id);
            return Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<DataLayer.Models.TaskGraph, TaskGraph>(dalModel);
        }

        public IEnumerable<DataTransfer.Models.TaskGraph> GetAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<DataTransfer.Models.TaskGraph> GetMany(IEnumerable<Guid> Ids)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<DataTransfer.Models.TaskGraph> GetAllForWorkspace(Guid UserId, Guid WorkspaceId)
        {
            var taskgraphIds = WsDataProvider.Get(WorkspaceId).TaskGraphIds;
            var taskGraphs = new List<DataTransfer.Models.TaskGraph>();
            foreach(var id in taskgraphIds)
            {
                var item = dataProvider.Get(id);
                var mappedItem = Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<DataLayer.Models.TaskGraph, TaskGraph>(item);
                taskGraphs.Add(mappedItem);
            }
            return taskGraphs;
        }

        public DataTransfer.Models.TaskGraph Update(DataTransfer.Events.TaskGraph item)
        {
            var current = dataProvider.Get(item.Id);

            current.Name = item.Name;
            current.RootId = item.RootId;
            current.Description = item.Description;

            var mappedItem = new DataLayer.Models.TaskGraph
            {
                Id = item.Id,
                Description = item.Description,
                Name = item.Name,
                RootId = item.RootId,
            };

            return Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<DataLayer.Models.TaskGraph, TaskGraph>(dataCollector.Update(current));
        }

        public DataTransfer.Models.TaskNode GetTask(Guid taskGraphId, Guid taskId)
        {
            var taskGraph = Get(taskGraphId);
            var taskSet = ServiceFactory.TaskSetService.Get(taskGraph.TaskSetId.Value);
            return taskSet.Tasks.Where(x => x.Data.Id == taskId).First();
        }

        public DataTransfer.Models.TaskNode AddTask(DataTransfer.Events.TaskData item, Guid taskGraphId)
        {
            var taskGraph = Get(taskGraphId);

            var task = new DataTransfer.Models.TaskNode
            {
                Data = new TaskData
                {
                    Id = item.Id,
                    CurrentStatus = (DataLayer.Models.TaskData.Status)((int)item.CurrentStatus),
                    Description = item.Description,
                    Name = item.Name
                },
                AssignedUser = null,
                EstimatedCompletionTimespan = item.EstimatedCompletionTime,
                OutNeighbors = new List<Guid>(),
                InNeighbors = new List<Guid>(),
                ParentId = null,
                TaskGraphId = taskGraphId,
                WorkspaceId = null,
                IsRoot = false
            };

            if(!taskGraph.TaskSetId.HasValue)
            {
                taskGraph.TaskSetId = Guid.NewGuid();
                var mappedTaskGraph = Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<TaskGraph, DataLayer.Models.TaskGraph>(taskGraph);
                DataCollectorFactory.taskGraphCollector.Update(mappedTaskGraph);
                var taskSet = new TaskSet
                {
                    Id = taskGraph.TaskSetId.Value,
                    Tasks = new List<TaskNode>()
                };
                taskSet.Tasks.Add(task);

                var mappedTaskSet = ConvertTaskSet(taskSet);
                DataCollectorFactory.taskSetCollector.Add(mappedTaskSet);
            }

            if (taskGraph.RootId is null || taskGraph.RootId.Equals(Guid.Empty))
            {
                var taskSet = ServiceFactory.TaskSetService.Get(taskGraph.TaskSetId.Value);
                taskGraph.RootId = task.Data.Id;
                var mappedTaskGraph = Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<TaskGraph, DataLayer.Models.TaskGraph>(taskGraph);
                DataCollectorFactory.taskGraphCollector.Update(mappedTaskGraph);
                task.IsRoot = true;
                task.Data.CurrentStatus = DLStatus.Assignable;
                taskSet.Tasks.Add(task);
                var mappedTaskSet = ConvertTaskSet(taskSet);
                DataCollectorFactory.taskSetCollector.Update(mappedTaskSet);
            }
            else
            {
                var taskSet = ServiceFactory.TaskSetService.Get(taskGraph.TaskSetId.Value);
                taskSet.Tasks.Add(task);
                var mappedTaskSet = ConvertTaskSet(taskSet);
                DataCollectorFactory.taskSetCollector.Update(mappedTaskSet);
            }

            return task;
        }

        public bool AssignUserToTask(Guid asigneeUserId, Guid taskId, Guid TaskGraphId)
        {
            var taskGraph = Get(TaskGraphId);
            var taskSet = ServiceFactory.TaskSetService.Get(taskGraph.TaskSetId.Value);

            var task = taskSet.Tasks.Where(x => x.Data.Id == taskId).First();
            taskSet.Tasks.Remove(task);
            task.AssignedUser = asigneeUserId;
            taskSet.Tasks.Add(task);

            var mappedTaskSet = Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<TaskSet, DataLayer.Models.TaskSet>(taskSet);

            DataCollectorFactory.taskSetCollector.Update(mappedTaskSet);

            return true;
        }

        public GraphNode GetGraph(Guid Id)
        {
            var graph = Get(Id);
            if (!graph.TaskSetId.HasValue) return null;
            var taskSet = ServiceFactory.TaskSetService.Get(graph.TaskSetId.Value);
            var rootTaskData = taskSet.Tasks.Where(x => x.Data.Id == graph.RootId).First();

            var root = new GraphNode
            {
                Task = rootTaskData.Data,
                OutNeighbors = new List<GraphNode>()
            };
            var initRoot = root;

            var queue = new Queue<GraphNode>();
            queue.Enqueue(root);

            while(queue.Count != 0)
            {
                root = queue.Dequeue();

                var taskData = taskSet.Tasks.Where(x => x.Data.Id == root.Task.Id).First();

                foreach(var id in taskData.OutNeighbors)
                {
                    var outNeighbor = new GraphNode
                    {
                        Task = taskSet.Tasks.Where(x => x.Data.Id == id).First().Data,
                        OutNeighbors = new List<GraphNode>()
                    };

                    root.OutNeighbors.Add(outNeighbor);
                    queue.Enqueue(outNeighbor);
                }
            }

            return initRoot;
        }

        public bool AddLink(Guid graphId, Guid parentId, Guid childId)
        {
            var graph = Get(graphId);
            var taskSet = ServiceFactory.TaskSetService.Get(graph.TaskSetId.Value);

            var parent = taskSet.Tasks.Where(x => x.Data.Id == parentId).First();
            taskSet.Tasks.Remove(parent);
            if (parent.OutNeighbors.Contains(childId))
                return false;
            parent.OutNeighbors.Add(childId);
            taskSet.Tasks.Add(parent);

            // check if cycles would be introduced
            var childRoot = taskSet.Tasks.Where(x => x.Data.Id == childId).First();

            var child = childRoot;

            if (!RespectsRules(taskSet, graph.RootId.Value))
                return false;

            var root = new GraphNode
            {
                Task = childRoot.Data,
                OutNeighbors = new List<GraphNode>()
            };
            var initRoot = root;

            var queue = new Queue<GraphNode>();
            queue.Enqueue(root);

            while (queue.Count != 0)
            {
                root = queue.Dequeue();

                var taskData = taskSet.Tasks.Where(x => x.Data.Id == root.Task.Id).First();

                if (root.Task.Id.Equals(parentId))
                    return false;

                foreach (var id in taskData.OutNeighbors)
                {
                    var outNeighbor = new GraphNode
                    {
                        Task = taskSet.Tasks.Where(x => x.Data.Id == id).First().Data,
                        OutNeighbors = new List<GraphNode>()
                    };

                    root.OutNeighbors.Add(outNeighbor);
                    queue.Enqueue(outNeighbor);
                }
            }

            var mappedTaskSet = ConvertTaskSet(taskSet);
            DataCollectorFactory.taskSetCollector.Update(mappedTaskSet);
            return true;
        }

        public TaskGraph Update(TaskGraph item)
        {
            throw new NotImplementedException();
        }

        public static bool RespectsRules(TaskSet ts, Guid rootNodeId)
        {
            var rootNode = ts.Tasks.Where(x => x.Data.Id == rootNodeId).First();
            var root = new GraphNode
            {
                Task = rootNode.Data,
                OutNeighbors = new List<GraphNode>()
            };

            var queue = new Queue<GraphNode>();
            queue.Enqueue(root);

            while (queue.Count != 0)
            {
                root = queue.Dequeue();

                var taskData = ts.Tasks.Where(x => x.Data.Id == root.Task.Id).First();


                foreach (var id in taskData.OutNeighbors)
                {
                    var childData = ts.Tasks.Where(x => x.Data.Id == id).First().Data;
                    var outNeighbor = new GraphNode
                    {
                        Task = childData,
                        OutNeighbors = new List<GraphNode>()
                    };

                    if (root.Task.CurrentStatus == DLStatus.Blocked)
                    {
                        if (childData.CurrentStatus != DLStatus.Blocked)
                            return false;
                    }
                    else if (root.Task.CurrentStatus == DLStatus.Assignable)
                    {
                        if (childData.CurrentStatus != DLStatus.Blocked)
                            return false;
                    }
                    else if (root.Task.CurrentStatus == DLStatus.Assigned)
                    {
                        if (childData.CurrentStatus != DLStatus.Blocked || childData.CurrentStatus != DLStatus.Assigned || childData.CurrentStatus != DLStatus.Assignable)
                            return false;
                    }

                    root.OutNeighbors.Add(outNeighbor);
                    queue.Enqueue(outNeighbor);
                }
            }

            return true;
        }

        public TaskGraph Add(TaskGraph item)
        {
            var model = Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<TaskGraph, DataLayer.Models.TaskGraph>(item);
            var ws = ServiceFactory.WorkspaceService.Get(item.WorkspaceId);
            var taskSet = new DataLayer.Models.TaskSet
            {
                Id = Guid.NewGuid(),
                Tasks = new List<DataLayer.Models.TaskNode>()
            };
            model.TaskSetId = taskSet.Id;
            DataCollectorFactory.taskSetCollector.Add(taskSet);
            var userList = new List<Guid>(ws.ActiveUsers);
            model.ActiveUsers = userList;
            var retModel = dataCollector.Add(model);
            var retDTModel = Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<DataLayer.Models.TaskGraph, TaskGraph>(retModel);

            
            if (ws.TaskGraphIds == null)
                ws.TaskGraphIds = new List<Guid>();
            ws.TaskGraphIds.Add(item.Id);

            var mappedWS = new DataLayer.Models.Workspace
            {
                Description = ws.Description,
                Id = ws.Id,
                Name = ws.Name,
                ActiveUsers = ws.ActiveUsers,
                TaskGraphIds = ws.TaskGraphIds
            };

            DataCollectorFactory.workspaceCollector.Update(mappedWS);

            return retDTModel;
        }

        public IEnumerable<TaskGraph> GetAllForUser(Guid UserId)
        {
            throw new NotImplementedException();
        }

        public static DataLayer.Models.TaskSet ConvertTaskSet(TaskSet taskSet)
        {
            var mappedTaskSet = new DataLayer.Models.TaskSet
            {
                Id = taskSet.Id,
                Tasks = new List<DataLayer.Models.TaskNode>()
            };

            foreach (var taskNodeItem in taskSet.Tasks)
            {
                var mappedItem = new DataLayer.Models.TaskNode
                {
                    AssignedUser = taskNodeItem.AssignedUser,
                    EstimatedCompletionTimespan = taskNodeItem.EstimatedCompletionTimespan ?? 0,
                    OutNeighbors = new List<Guid>(taskNodeItem.OutNeighbors),
                    InNeighbors = new List<Guid>(taskNodeItem.InNeighbors),
                    ParentId = taskNodeItem.ParentId,
                    TaskGraphId = taskNodeItem.TaskGraphId ?? Guid.Empty,
                    WorkspaceId = taskNodeItem.WorkspaceId ?? Guid.Empty,
                    IsRoot = taskNodeItem.IsRoot,
                    Task = new DataLayer.Models.TaskData
                    {
                        CurrentStatus = taskNodeItem.Data.CurrentStatus,
                        Description = taskNodeItem.Data.Description,
                        Id = taskNodeItem.Data.Id,
                        Name = taskNodeItem.Data.Name
                    }
                };
                mappedTaskSet.Tasks.Add(mappedItem);
            }
            return mappedTaskSet;
        }

        public bool UpdateTaskStatus(Guid taskGraphId, Guid taskId, DLStatus newStatus)
        {
            var graph = Get(taskGraphId);
            var taskSet = ServiceFactory.TaskSetService.Get(graph.TaskSetId.Value);

            var task = taskSet.Tasks.Where(x => x.Data.Id == taskId).First();
            taskSet.Tasks.Remove(task);
            task.Data.CurrentStatus = newStatus;
            taskSet.Tasks.Add(task);

            var mappedTaskSet = ConvertTaskSet(taskSet);

            DataCollectorFactory.taskSetCollector.Update(mappedTaskSet);
            return true;
        }
    }
}
