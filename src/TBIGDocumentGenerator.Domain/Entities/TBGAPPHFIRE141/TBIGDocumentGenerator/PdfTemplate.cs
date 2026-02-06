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
    [Table("PdfTemplate", Schema = "dbo")]
    public class PdfTemplate : IAuditableEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [MaxLength(50)]
        public string Code { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public string HtmlContent { get; set; }

        [MaxLength(20)]
        public string PaperFormat { get; set; } = "A4";

        public bool Landscape { get; set; } = false;

        [MaxLength(10)]
        public string MarginTop { get; set; } = "20mm";
        [MaxLength(10)]
        public string MarginBottom { get; set; } = "20mm";
        [MaxLength(10)]
        public string MarginLeft { get; set; } = "15mm";
        [MaxLength(10)]
        public string MarginRight { get; set; } = "15mm";

        public string? HeaderHtml { get; set; }
        public string? FooterHtml { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool? IsActive { get; set; }
    }
}
