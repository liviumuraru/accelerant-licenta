using Accelerant.DataLayer.DataProviders.Mongo;
using Accelerant.DataLayer.Models;
using MongoDB.Driver;
using System.Security.Authentication;

namespace Accelerant.Services.Collectors
{
    public static class DataProviderFactory
    {
        public const string DB_NAME = "accelerant";
        public const string WORKSPACE_COLLECTION_NAME = "workspaces";
        public const string TASK_GRAPHS_COLLECTION_NAME = "task_graphs";
        public const string TASK_SETS_COLLECTION_NAME = "task_sets";
        public const string USER_COLLECTION_NAME = "users";
        public const string MONGO_CONNECTION = "mongodb+srv://admin:admin@cluster0-nu64h.gcp.mongodb.net/test?retryWrites=true&w=majority";//@"mongodb://accelerant-mongodb-account:qsIgHUg9NFcMw8bKLZZsaFUqq7XJ2vktszhQr86U4Cyez5wQoBJQl0KcEiipQ3nUQvjXzTTE6RJEvtpbp9ZFNQ==@accelerant-mongodb-account.documents.azure.com:10255/?ssl=true&replicaSet=globaldb";

        private static IMongoClient mongoClient;
        private static IMongoDatabase mongoDatabase;

        public static TaskGraphProvider taskGraphProvider { get; set; }
        public static WorkspaceProvider workspaceProvider { get; set; }
        public static TaskSetProvider taskSetProvider { get; set; }
        public static UserProvider userProvider { get; set; }

        static DataProviderFactory()
        {
            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(MONGO_CONNECTION));
            settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            mongoClient = new MongoClient(MONGO_CONNECTION);
            mongoDatabase = mongoClient.GetDatabase(DB_NAME);

            var taskGraphCollection = mongoDatabase.GetCollection<Accelerant.DataLayer.Models.TaskGraph>(TASK_GRAPHS_COLLECTION_NAME);
            taskGraphProvider = new TaskGraphProvider(taskGraphCollection);

            var workspaceCollection = mongoDatabase.GetCollection<Accelerant.DataLayer.Models.Workspace>(WORKSPACE_COLLECTION_NAME);
            workspaceProvider = new WorkspaceProvider(workspaceCollection);

            var taskSetCollection = mongoDatabase.GetCollection<Accelerant.DataLayer.Models.TaskSet>(TASK_SETS_COLLECTION_NAME);
            taskSetProvider = new TaskSetProvider(taskSetCollection);

            var userCollection = mongoDatabase.GetCollection<Accelerant.DataLayer.Models.User>(USER_COLLECTION_NAME);
            userProvider = new UserProvider(userCollection);
        }
    }
}
