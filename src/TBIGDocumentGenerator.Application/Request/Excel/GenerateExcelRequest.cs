using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBIGDocumentGenerator.Application.Request.Excel
{
    public class GenerateExcelRequest
    {
        public string ReportCode { get; set; } // Wajib: Kode Report di DB

        // Parameter SQL Statis (misal @StartDate, @EndDate)
        // Key: Nama param (@p1), Value: Nilainya
        public Dictionary<string, object>? Parameters { get; set; }

        // Pilihan Kolom (SELECT ...)
        // Jika null, ambil semua default kolom
        public List<string>? SelectColumns { get; set; }

        // Dynamic Filter (WHERE ...)
        public List<FilterDefinition>? Filters { get; set; }

        // Dynamic Sort (ORDER BY ...)
        public List<SortDefinition>? Sorts { get; set; }

        public NotificationConfig? Notification { get; set; }
    }
}
