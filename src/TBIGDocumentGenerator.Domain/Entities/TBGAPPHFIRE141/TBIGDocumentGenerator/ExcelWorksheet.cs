using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBIGDocumentGenerator.Domain.Interfaces;

namespace TBIGDocumentGenerator.Domain.Entities.TBGAPPHFIRE141.TBIGDocumentGenerator
{
    [Table("ExcelWorksheet", Schema = "dbo")]
    public class ExcelWorksheet : IAuditableEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [MaxLength(50)]
        public int ExcelReportID { get; set; }

        [Required]
        [MaxLength(30)]
        public string SheetName { get; set; }

        [Required]
        [MaxLength(100)]
        public string ConnectionStringName { get; set; } 

        [Required]
        public string BaseQuery { get; set; }

        public string? AllowedColumnsJson { get; set; }
        public string? ColumnMappingJson { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool? IsActive { get; set; }
    }
}
