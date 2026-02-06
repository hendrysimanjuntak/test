using Castle.Core.Configuration;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TBIGDocumentGenerator.Application.Interfaces.Entities.TBGAPPHFIRE141.TBIGDocumentGenerator;
using TBIGDocumentGenerator.Application.Interfaces.Modules;
using TBIGDocumentGenerator.Application.Request.Excel;
using TBIGDocumentGenerator.Application.Request.PDF;
using TBIGDocumentGenerator.Domain.Entities.TBGAPPHFIRE141.TBIGDocumentGenerator;

namespace TBIGDocumentGenerator.Application.Services.Modules
{
    public class ExcelService : IExcelService
    {
        private readonly IExcelReportService _excelReportService;
        private readonly IExcelWorksheetService _excelWorksheetService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public ExcelService(IExcelReportService excelReportService, IExcelWorksheetService excelWorksheetService, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _excelReportService = excelReportService;
            _excelWorksheetService = excelWorksheetService;
            _configuration = configuration;
        }

        public async Task<byte[]> GenerateExcelAsync(GenerateExcelRequest request)
        {
            var report = (await _excelReportService.GetListAsync(filter: w => w.Code == request.ReportCode).ConfigureAwait(false)).FirstOrDefault();
            
            if (report == null) throw new Exception($"Report Code '{request.ReportCode}' not found.");

            var sheets = await _excelWorksheetService.GetListAsync(filter: w => w.ExcelReportID == report.ID).ConfigureAwait(false);
            if (!sheets.Any()) throw new Exception($"No worksheets configured for '{request.ReportCode}'.");

            using var workbook = new XLWorkbook();

            foreach (var sheet in sheets)
            {
                string connString = _configuration.GetConnectionString(sheet.ConnectionStringName);
                if (string.IsNullOrEmpty(connString))
                {
                    throw new Exception($"Connection string '{sheet.ConnectionStringName}' not configured in appsettings.");
                }

                var (sql, parameters) = BuildSqlQuery(sheet, request);

                DataTable dataTable = await ExecuteQueryAsync(connString, sql, parameters);
                dataTable.TableName = sheet.SheetName;

                // Add to Excel
                var worksheet = workbook.Worksheets.Add(dataTable);
                worksheet.Columns().AdjustToContents();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private async Task<DataTable> ExecuteQueryAsync(string connectionString, string sql, Dictionary<string, object> parameters)
        {
            using var connection = new SqlConnection(connectionString);

            using var command = new SqlCommand(sql, connection);
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    command.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
                }
            }

            var dt = new DataTable();
            var adapter = new SqlDataAdapter(command);

            await Task.Run(() => adapter.Fill(dt));

            return dt;
        }

        private (string sql, Dictionary<string, object> parameters) BuildSqlQuery(ExcelWorksheet sheet, GenerateExcelRequest request)
        {
            var parameters = new Dictionary<string, object>();

            if (request.Parameters != null)
            {
                foreach (var kvp in request.Parameters)
                    parameters.Add(kvp.Key, kvp.Value);
            }

            string selectPart = "*";
            if (request.SelectColumns != null && request.SelectColumns.Any())
            {
                // Validation (Security)
                if (!string.IsNullOrEmpty(sheet.AllowedColumnsJson))
                {
                    var allowed = JsonSerializer.Deserialize<List<string>>(sheet.AllowedColumnsJson);
                    var invalidCols = request.SelectColumns.Except(allowed, StringComparer.OrdinalIgnoreCase);
                    if (invalidCols.Any())
                        throw new Exception($"Columns not allowed: {string.Join(", ", invalidCols)}");
                }

                // Prevent SQL Injection via Column Name
                selectPart = string.Join(", ", request.SelectColumns.Select(c => $"[{c.Replace("]", "")}]"));
            }

            string wherePart = "1=1";
            if (request.Filters != null)
            {
                int idx = 0;
                foreach (var filter in request.Filters)
                {
                    string paramName = $"@f{idx}";
                    string colName = $"[{filter.Field.Replace("]", "")}]";

                    switch (filter.Operator?.ToLower())
                    {
                        case "eq": // Equal
                            wherePart += $" AND {colName} = {paramName}";
                            parameters.Add(paramName, filter.Value);
                            break;
                        case "neq": // Not Equal
                            wherePart += $" AND {colName} <> {paramName}";
                            parameters.Add(paramName, filter.Value);
                            break;
                        case "gt": // Greater Than
                            wherePart += $" AND {colName} > {paramName}";
                            parameters.Add(paramName, filter.Value);
                            break;
                        case "gte": // Greater Than Equal
                            wherePart += $" AND {colName} >= {paramName}";
                            parameters.Add(paramName, filter.Value);
                            break;
                        case "lt": // Less Than
                            wherePart += $" AND {colName} < {paramName}";
                            parameters.Add(paramName, filter.Value);
                            break;
                        case "lte": // Less Than Equal
                            wherePart += $" AND {colName} <= {paramName}";
                            parameters.Add(paramName, filter.Value);
                            break;
                        case "contains": // Like %Value%
                            wherePart += $" AND {colName} LIKE {paramName}";
                            parameters.Add(paramName, $"%{filter.Value}%");
                            break;
                        case "startswith": // Like Value%
                            wherePart += $" AND {colName} LIKE {paramName}";
                            parameters.Add(paramName, $"{filter.Value}%");
                            break;
                        case "endswith": // Like %Value
                            wherePart += $" AND {colName} LIKE {paramName}";
                            parameters.Add(paramName, $"%{filter.Value}");
                            break;
                        default:
                            // Default to Equal if unknown
                            wherePart += $" AND {colName} = {paramName}";
                            parameters.Add(paramName, filter.Value);
                            break;
                    }
                    idx++;
                }
            }

            // Order By
            string orderPart = "";
            if (request.Sorts != null && request.Sorts.Any())
            {
                var sorts = request.Sorts.Select(s => $"[{s.Field.Replace("]", "")}] {(s.Direction?.ToLower() == "desc" ? "DESC" : "ASC")}");
                orderPart = "ORDER BY " + string.Join(", ", sorts);
            }

            // Final Assembly (Wrapper Query)
            string finalSql = $@"
                SELECT {selectPart} 
                FROM ({sheet.BaseQuery}) AS SourceData 
                WHERE {wherePart} 
                {orderPart}";

            return (finalSql, parameters);
        }
    }
}
