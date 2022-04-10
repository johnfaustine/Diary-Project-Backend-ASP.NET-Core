using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryProject.Models
{
    public class Finance
    {
        public int FinanceId { get; set; }
        public string UserId { get; set; }
        public DateTime FinanceDateTime { get; set; }
        public string FinanceName { get; set; }

        [Range(0, (double)decimal.MaxValue)]
        [Column(TypeName = "decimal(19,4)")]
        public Decimal FinanceExpense { get; set; }

        [Range(0, (double)decimal.MaxValue)]
        [Column(TypeName = "decimal(19,4)")]
        public Decimal FinanceIncome { get; set; }
        public ApplicationUser User { get; set; }
    }
}
