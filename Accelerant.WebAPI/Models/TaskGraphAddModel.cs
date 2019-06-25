using System;

namespace Accelerant.WebAPI.Models
{
    public class TaskGraphAddModel
    {
        public Guid WorkspaceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid UserId { get; set; }
    }
}
