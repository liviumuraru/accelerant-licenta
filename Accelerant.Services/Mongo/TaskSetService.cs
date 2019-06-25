using Accelerant.DataLayer.DataCollectors;
using Accelerant.DataLayer.DataProviders;
using System;
using System.Collections.Generic;

using TaskSetTransferModel = Accelerant.DataTransfer.Models.TaskSet;
using TaskSetDataModel = Accelerant.DataLayer.Models.TaskSet;
using TaskNodeDataModel = Accelerant.DataLayer.Models.TaskNode;

using static Accelerant.DataTransfer.Mapping.MapperConfig;
using Accelerant.DataTransfer.Models;

namespace Accelerant.Services
{
    public interface ITaskSetService
        : IService<TaskSetTransferModel, TaskSetTransferModel, Guid>
    {
    }

    public class TaskSetService
        : ITaskSetService
    {
        private IDataProvider<TaskSetDataModel, Guid> dataProvider;
        private IDataCollector<TaskSetDataModel, TaskSetDataModel, TaskSetDataModel> dataCollector;

        public TaskSetService(IDataProvider<TaskSetDataModel, Guid> dataProvider, IDataCollector<TaskSetDataModel, TaskSetDataModel, TaskSetDataModel> dataCollector)
        {
            this.dataCollector = dataCollector;
            this.dataProvider = dataProvider;
        }

        public TaskSetTransferModel Add(TaskSetTransferModel item)
        {
            //var taskList = new List<TaskNodeDataModel>();
            //foreach(var taskNode in item.Tasks)
            //{
            //    var toAdd = new TaskNodeDataModel
            //    {
            //        TaskGraphId = taskNode.
            //    }
            //    task
            //}

            //var mappedItem = new TaskSetDataModel
            //{
            //    Id = item.Id,
            //    Tasks = item.T
            //};

            //return dataCollector.Add(mappedItem);

            throw new NotImplementedException();
        }

        public TaskSetTransferModel Get(Guid Id)
        {
            var unmapped = dataProvider.Get(Id);
            var mapped = new TaskSetTransferModel
            {
                Id = unmapped.Id,
                Tasks = new List<TaskNode>()
            };

            foreach (var item in unmapped.Tasks)
            {
                var mappedItem = new TaskNode
                {
                    AssignedUser = item.AssignedUser,
                    EstimatedCompletionTimespan = item.EstimatedCompletionTimespan,
                    OutNeighbors = new List<Guid>(item.OutNeighbors),
                    InNeighbors = new List<Guid>(item.InNeighbors),
                    ParentId = item.ParentId,
                    TaskGraphId = item.TaskGraphId,
                    WorkspaceId = item.WorkspaceId,
                    IsRoot = item.IsRoot,
                    Data = new TaskData
                    {
                        CurrentStatus = item.Task.CurrentStatus,
                        Description = item.Task.Description,
                        Id = item.Task.Id,
                        Name = item.Task.Name
                    }
                };
                mapped.Tasks.Add(mappedItem);
            }

            return mapped;
        }

        public IEnumerable<TaskSetTransferModel> GetAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TaskSetTransferModel> GetAllForUser(Guid UserId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TaskSetTransferModel> GetMany(IEnumerable<Guid> Ids)
        {
            throw new NotImplementedException();
        }

        public TaskSetTransferModel Update(TaskSetTransferModel item)
        {
            //var mappedItem = new DataTransfer.Models.TaskSet
            //{
            //    Id = item.Id,
            //    Tasks = item.Tasks
            //};

            //return dataCollector.Update(mappedItem);

            throw new NotImplementedException();
        }
    }
}
