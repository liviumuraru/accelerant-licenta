using Accelerant.DataLayer.DataCollectors.Mongo;
using Accelerant.DataLayer.Models;
using MongoDB.Driver;
using System.Security.Authentication;

namespace Accelerant.Services.Collectors
{
    public static class DataCollectorFactory
    {
        public const string DB_NAME = "accelerant";
        public const string WORKSPACE_COLLECTION_NAME = "workspaces";
        public const string TASK_GRAPHS_COLLECTION_NAME = "task_graphs";
        public const string TASK_SETS_COLLECTION_NAME = "task_sets";
        public const string USER_COLLECTION_NAME = "users";
        public const string MONGO_CONNECTION = "mongodb+srv://admin:admin@cluster0-nu64h.gcp.mongodb.net/test?retryWrites=true&w=majority";//@"mongodb://accelerant-mongodb-account:qsIgHUg9NFcMw8bKLZZsaFUqq7XJ2vktszhQr86U4Cyez5wQoBJQl0KcEiipQ3nUQvjXzTTE6RJEvtpbp9ZFNQ==@accelerant-mongodb-account.documents.azure.com:10255/?ssl=true&replicaSet=globaldb";

        private static IMongoClient mongoClient;
        private static IMongoDatabase mongoDatabase;

        public static TaskGraphCollector taskGraphCollector { get; set; }
        public static WorkspaceCollector workspaceCollector { get; set; }
        public static TaskSetCollector taskSetCollector { get; set; }
        public static UserCollector userCollector { get; set; }

        static DataCollectorFactory()
        {
            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(MONGO_CONNECTION));
            settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            mongoClient = new MongoClient(MONGO_CONNECTION);
            mongoDatabase = mongoClient.GetDatabase(DB_NAME);

            var taskGraphCollection = mongoDatabase.GetCollection<TaskGraph>(TASK_GRAPHS_COLLECTION_NAME);
            taskGraphCollector = new TaskGraphCollector(taskGraphCollection);

            var workspaceCollection = mongoDatabase.GetCollection<Workspace>(WORKSPACE_COLLECTION_NAME);
            workspaceCollector = new WorkspaceCollector(workspaceCollection);

            var taskSetCollection = mongoDatabase.GetCollection<TaskSet>(TASK_SETS_COLLECTION_NAME);
            taskSetCollector = new TaskSetCollector(taskSetCollection);

            var userCollection = mongoDatabase.GetCollection<Accelerant.DataLayer.Models.User>(USER_COLLECTION_NAME);
            userCollector = new UserCollector(userCollection);
        }
    }
}
