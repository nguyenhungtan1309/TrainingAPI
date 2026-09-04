using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingAPI.Models
{
    [Table("Department")]
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Employee> Employees { get; set; }
    }
}
