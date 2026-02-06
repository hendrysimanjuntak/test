using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBIGDocumentGenerator.Domain.Models.Datatables
{
    public class DataTablesRequest
    {
        // Properti Draw, Start, Length untuk paging
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }

        // Properti Search untuk filter global
        public DataTablesSearch Search { get; set; } = new DataTablesSearch();

        // Properti Order untuk sorting
        public List<DataTablesOrder> Order { get; set; } = new List<DataTablesOrder>();

        // Properti Columns untuk info kolom (termasuk filter per kolom)
        public List<DataTablesColumn> Columns { get; set; } = new List<DataTablesColumn>();
    }
}
