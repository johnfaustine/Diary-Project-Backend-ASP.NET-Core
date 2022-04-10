using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryProject.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string RToken { get; set; }
        public DateTime RTExpirationDate { get; set; }
        public ApplicationUser User { get; set; }
    }
}
