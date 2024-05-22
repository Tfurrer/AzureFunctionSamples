using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflows.Models
{
    public class Prospect
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsExperienced { get; set; }
        public string CurrentStatus { get; set; }
        public DateTime CurrentStatusStartTime { get; set; } = DateTime.UtcNow;

    }
}
