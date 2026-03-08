using System;
using System.Collections.Generic;

namespace Feniks.Web.Models
{
    public class Estimate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ConstructionObjectId { get; set; }
        public string Status { get; set; } = "Черновик";
        public decimal TotalCost { get; set; }
        public decimal CustomerPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerEmail { get; set; }
    }
}
