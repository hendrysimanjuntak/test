using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBIGDocumentGenerator.Application.Request
{
    public class NotificationConfig
    {
        public bool Enabled { get; set; } = false;

        // ID Koneksi user yang request (didapat dari SignalR di Frontend)
        public string? ConnectionId { get; set; }

        // Nama Topic/Method SignalR untuk update progress
        // Contoh: "ReceiveProgress" -> Client listen ke method ini
        public string ProgressMethod { get; set; } = "ReceiveProgress";

        // Nama Topic/Method SignalR saat selesai
        // Contoh: "ReceiveDownloadUrl"
        public string FinishMethod { get; set; } = "ReceiveDownloadUrl";
    }
}
