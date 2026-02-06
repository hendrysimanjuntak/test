using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBIGDocumentGenerator.Infrastructure.Data
{
    public static class DbContextExtensions
    {
        public static IEnumerable<T> MapToList<T>(this DbContext context, DbDataReader reader) where T : class, new()
        {
            var objList = new List<T>();
            var props = typeof(T).GetProperties();

            while (reader.Read())
            {
                T obj = new T();
                foreach (var prop in props)
                {
                    if (!reader.HasColumn(prop.Name) || reader[prop.Name] == DBNull.Value)
                        continue;

                    prop.SetValue(obj, reader[prop.Name]);
                }
                objList.Add(obj);
            }

            return objList;
        }

        public static bool HasColumn(this DbDataReader reader, string columnName)
        {
            for (var i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }

}
