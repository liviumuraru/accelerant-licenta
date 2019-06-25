using System;

namespace Accelerant.WebAPI.Models
{
    public class WorkspaceAddModel
    {
        public string Description { get; set; }
        public string Name { get; set; }
        public Guid UserId { get; set; }
    }
}
