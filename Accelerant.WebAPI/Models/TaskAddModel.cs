using System;

namespace Accelerant.WebAPI.Models
{
    public class TaskAddModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DataTransfer.Events.TaskData.Status CurrentStatus { get; set; }
        public uint EstimatedCompletionTime { get; set; }
    }
}
