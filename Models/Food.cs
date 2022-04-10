using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryProject.Models
{
    public class Food
    {
        public int FoodId { get; set; }
        public string UserId { get; set; }
        public DateTime FoodDateTime { get; set; }
        public string FoodName { get; set; }
        [Range(0, int.MaxValue)]
        public int FoodCalorie { get; set; }
        public ApplicationUser User { get; set; }
    }
}
