using Accelerant.DataTransfer.Models;
using Accelerant.Services.Collectors;
using System;

namespace Accelerant.Services.Mongo
{
    public static class ServiceFactory
    {
        static ServiceFactory()
        {
            TaskGraphService = new TaskGraphService(DataProviderFactory.taskGraphProvider, DataCollectorFactory.taskGraphCollector, DataProviderFactory.workspaceProvider);
            WorkspaceService = new WorkspaceService(DataProviderFactory.workspaceProvider, DataCollectorFactory.workspaceCollector);
            TaskSetService = new TaskSetService(DataProviderFactory.taskSetProvider, DataCollectorFactory.taskSetCollector);
            UsersService = new UsersService(DataProviderFactory.userProvider, DataCollectorFactory.userCollector);
        }

        public static IService<Workspace, Workspace, Guid> WorkspaceService { get; set; }

        public static ITaskGraphService TaskGraphService { get; set; }

        public static ITaskSetService TaskSetService { get; set; }

        public static IUsersService UsersService { get; set; }
    }
}
