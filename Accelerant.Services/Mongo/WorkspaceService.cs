using Accelerant.DataLayer.DataCollectors;
using Accelerant.DataLayer.DataProviders;
using System;
using System.Collections.Generic;

using WorkspaceTransferModel = Accelerant.DataTransfer.Models.Workspace;
using WorkspaceDataModel = Accelerant.DataLayer.Models.Workspace;
using static Accelerant.DataTransfer.Mapping.MapperConfig;
using System.Linq;

namespace Accelerant.Services
{
    public class WorkspaceService
        : IService<WorkspaceTransferModel, WorkspaceTransferModel, Guid>
    {
        private IDataProvider<WorkspaceDataModel, Guid> dataProvider;
        private IDataCollector<WorkspaceDataModel, WorkspaceDataModel, WorkspaceDataModel> dataCollector;

        public WorkspaceService(IDataProvider<WorkspaceDataModel, Guid> dataProvider, IDataCollector<WorkspaceDataModel, WorkspaceDataModel, WorkspaceDataModel> dataCollector)
        {
            this.dataProvider = dataProvider;
            this.dataCollector = dataCollector;
        }

        public WorkspaceTransferModel Get(Guid Id)
        {
            return Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<WorkspaceDataModel, WorkspaceTransferModel>(dataProvider.Get(Id));
        }

        public IEnumerable<WorkspaceTransferModel> GetMany(IEnumerable<Guid> Ids)
        {
            throw new NotImplementedException();
        }

        public WorkspaceTransferModel Update(WorkspaceTransferModel item)
        {
            //var mappedItem = new DataTransfer.Models.Workspace
            //{
            //    Description = item.Description,
            //    Name = item.Name,
            //    Id = item.Id,
            //    TaskGraphIds = item.TaskGraphIds.ToList(),
            //    UserId = item.UserId
            //};

            var mappedItem = Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<WorkspaceTransferModel, WorkspaceDataModel>(item);

            return Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<WorkspaceDataModel, WorkspaceTransferModel>(dataCollector.Update(mappedItem));
        }

        public WorkspaceTransferModel Add(WorkspaceTransferModel item)
        {
            item.TaskGraphIds = new List<Guid>();

            //var mappedItem = new Workspace
            //{
            //    Description = item.Description,
            //    Name = item.Name,
            //    Id = item.Id,
            //    TaskGraphIds = new List<Guid>(),
            //    UserId = item.UserId
            //};

            var mappedItem = Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<WorkspaceTransferModel, WorkspaceDataModel>(item);

            return Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<WorkspaceDataModel, WorkspaceTransferModel>(dataCollector.Add(mappedItem));
        }
        
        public IEnumerable<WorkspaceTransferModel> GetAllForUser(Guid Id)
        {
            return dataProvider.GetAllForUser(Id).Select(x => Mappers[Tuple.Create(Layer.Data, Layer.DataTransfer)].Map<WorkspaceDataModel, WorkspaceTransferModel>(x));
        }
    }
}
