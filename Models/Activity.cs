using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryProject.Models
{
    public class Activity
    {
        public int ActivityId { get; set; }
        public string UserId { get; set; }
        public DateTime ActivityDateTime { get; set; }
        public string ActivityName { get; set; }
        public ApplicationUser User { get; set; }
    }
}
