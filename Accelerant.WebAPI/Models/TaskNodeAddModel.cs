using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Accelerant.WebAPI.Models
{
    public class TaskNodeAddModel
    {
        public Guid WorkspaceId { get; set; }
        public Guid TaskGraphId { get; set; }
        public TaskAddModel TaskData { get; set; }
    }
}
