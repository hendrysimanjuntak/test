using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Linq.Expressions;
using System.Globalization;
using TBIGDocumentGenerator.Domain.Models.Datatables;

namespace TBIGDocumentGenerator.Domain.Helpers
{
    public static class DataTablesQueryAdapter<T> where T : class
    {
        private static Expression<Func<T, bool>>? CombineFilters(Expression<Func<T, bool>>? first, Expression<Func<T, bool>>? second)
        {
            if (first == null) return second;
            if (second == null) return first;

            var parameter = Expression.Parameter(typeof(T), "x"); // Gunakan parameter baru yang konsisten
            var visitor = new ParameterUpdateVisitor(second.Parameters.First(), parameter); // Ganti parameter ekspresi kedua
            var secondBody = visitor.Visit(second.Body);

            visitor = new ParameterUpdateVisitor(first.Parameters.First(), parameter); // Ganti parameter ekspresi pertama
            var firstBody = visitor.Visit(first.Body);

            var body = Expression.AndAlso(firstBody, secondBody);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        public static Expression<Func<T, bool>>? BuildFilterExpression(DataTablesRequest dtRequest)
        {
            Expression<Func<T, bool>>? combinedFilter = null;
            var parameter = Expression.Parameter(typeof(T), "x");

            var globalSearchValue = dtRequest.Search?.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(globalSearchValue))
            {
                Expression? globalFilterBody = null;
                foreach (var column in dtRequest.Columns.Where(c => c.Searchable && !string.IsNullOrWhiteSpace(c.Data)))
                {
                    try
                    {
                        var propertyExpr = Expression.Property(parameter, column.Data);
                        Expression? currentColumnExpr = BuildContainsOrEqualsExpression(propertyExpr, globalSearchValue);

                        if (currentColumnExpr != null)
                        {
                            globalFilterBody = globalFilterBody == null
                                ? currentColumnExpr
                                : Expression.OrElse(globalFilterBody, currentColumnExpr);
                        }
                    }
                    catch (ArgumentException) {}
                }
                if (globalFilterBody != null)
                {
                    combinedFilter = Expression.Lambda<Func<T, bool>>(globalFilterBody, parameter);
                }
            }

            foreach (var column in dtRequest.Columns.Where(c => c.Searchable && !string.IsNullOrWhiteSpace(c.Data) && !string.IsNullOrWhiteSpace(c.Search?.Value)))
            {
                string columnSearchValue = column.Search.Value.Trim();
                try
                {
                    var propertyExpr = Expression.Property(parameter, column.Data);
                    Expression? columnFilterBody = BuildContainsOrEqualsExpression(propertyExpr, columnSearchValue);

                    if (columnFilterBody != null)
                    {
                        var columnLambda = Expression.Lambda<Func<T, bool>>(columnFilterBody, parameter);
                        combinedFilter = CombineFilters(combinedFilter, columnLambda); 
                    }
                }
                catch (ArgumentException) {}
            }

            return combinedFilter;
        }
        public static Func<IQueryable<T>, IOrderedQueryable<T>>? BuildOrderByFunc(DataTablesRequest dtRequest)
        {
            if (dtRequest.Order == null || !dtRequest.Order.Any()) return null;

            var orderClauses = new List<string>();
            foreach (var order in dtRequest.Order)
            {
                if (order.Column < 0 || order.Column >= dtRequest.Columns.Count) continue;
                var column = dtRequest.Columns[order.Column];
                if (column.Orderable && !string.IsNullOrWhiteSpace(column.Data))
                {
                    if (typeof(T).GetProperty(column.Data, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) != null)
                    {
                        string direction = order.Dir?.ToLowerInvariant() == "desc" ? "descending" : "ascending";
                        orderClauses.Add($"{column.Data} {direction}");
                    }
                }
            }

            if (orderClauses.Any())
            {
                string orderByString = string.Join(", ", orderClauses);
                return query => query.OrderBy(orderByString);
            }
            return null;
        }

        private static Expression? BuildStringContainsExpression(Expression propertyExpr, string value)
        {
            if (propertyExpr.Type != typeof(string) || string.IsNullOrEmpty(value))
                return null;

            var searchValueConstant = Expression.Constant(value);
            var containsMethodInfo = typeof(string).GetMethod("Contains", new[] { typeof(string) });

            if (containsMethodInfo == null) return null; 

            var nullCheck = Expression.NotEqual(propertyExpr, Expression.Constant(null, typeof(string)));
            var containsCall = Expression.Call(propertyExpr, containsMethodInfo, searchValueConstant);

            return Expression.AndAlso(nullCheck, containsCall);
        }

		private static Expression? BuildContainsOrEqualsExpression(Expression propertyExpr, string value)
		{
			var type = propertyExpr.Type;
			var isNullable = Nullable.GetUnderlyingType(type) != null;
			var actualType = Nullable.GetUnderlyingType(type) ?? type;

			// String
			if (actualType == typeof(string))
			{
				var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
				if (containsMethod == null) return null;

				var nullCheck = Expression.NotEqual(propertyExpr, Expression.Constant(null, typeof(string)));
				var containsCall = Expression.Call(propertyExpr, containsMethod, Expression.Constant(value));
				return Expression.AndAlso(nullCheck, containsCall);
			}

			// Integer
			if (actualType == typeof(int) && int.TryParse(value, out var intValue))
			{
				var constant = Expression.Constant(intValue, type);
				return Expression.Equal(propertyExpr, constant);
			}

			// Decimal
			if (actualType == typeof(decimal) && decimal.TryParse(value, out var decimalValue))
			{
				var constant = Expression.Constant(decimalValue, type);
				return Expression.Equal(propertyExpr, constant);
			}

			if (actualType == typeof(bool))
			{
				bool? parsedBool = null;
				var val = value.Trim().ToLower();

				// Try parse default bool first
				if (bool.TryParse(val, out var boolValue))
				{
					parsedBool = boolValue;
				}
				else
				{
					// Custom string to bool mapping
					if (val == "1" || val == "active" || val == "aktif")
						parsedBool = true;
					else if (val == "0" || val == "false" || val == "inactive" || val == "nonaktif")
						parsedBool = false;
				}

				if (parsedBool.HasValue)
				{
					var constant = Expression.Constant(parsedBool.Value, type);
					return Expression.Equal(propertyExpr, constant);
				}
			}
			// DateTime & Nullable<DateTime>
			if (actualType == typeof(DateTime))
			{
				Expression valueExpr = propertyExpr;

				if (isNullable)
				{
					var hasValue = Expression.Property(propertyExpr, "HasValue");
					valueExpr = Expression.Property(propertyExpr, "Value");

					if (int.TryParse(value, out var year))
					{
						var yearExpr = Expression.Property(valueExpr, nameof(DateTime.Year));
						return Expression.AndAlso(hasValue, Expression.Equal(yearExpr, Expression.Constant(year)));
					}

					if (TryParseMonth(value, out var month))
					{
						var monthExpr = Expression.Property(valueExpr, nameof(DateTime.Month));
						return Expression.AndAlso(hasValue, Expression.Equal(monthExpr, Expression.Constant(month)));
					}

					if (DateTime.TryParse(value, out var dtValue))
					{
						var dateExpr = Expression.Property(valueExpr, nameof(DateTime.Date));
						return Expression.AndAlso(hasValue, Expression.Equal(dateExpr, Expression.Constant(dtValue.Date)));
					}
				}
				else
				{
					if (int.TryParse(value, out var year))
					{
						var yearExpr = Expression.Property(valueExpr, nameof(DateTime.Year));
						return Expression.Equal(yearExpr, Expression.Constant(year));
					}

					if (TryParseMonth(value, out var month))
					{
						var monthExpr = Expression.Property(valueExpr, nameof(DateTime.Month));
						return Expression.Equal(monthExpr, Expression.Constant(month));
					}

					if (DateTime.TryParse(value, out var dtValue))
					{
						var dateExpr = Expression.Property(valueExpr, nameof(DateTime.Date));
						return Expression.Equal(dateExpr, Expression.Constant(dtValue.Date));
					}
				}

				var toStringMethod = typeof(DateTime).GetMethod("ToString", Type.EmptyTypes);
				if (toStringMethod != null)
				{
					var toStringExpr = Expression.Call(valueExpr, toStringMethod);
					var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
					var containsExpr = Expression.Call(toStringExpr, containsMethod!, Expression.Constant(value));

					return isNullable
						? Expression.AndAlso(Expression.Property(propertyExpr, "HasValue"), containsExpr)
						: containsExpr;
				}
			}

			return null;
		}
		private static bool TryParseMonth(string input, out int month)
		{
			month = 0;
			if (string.IsNullOrWhiteSpace(input)) return false;

			input = input.Trim().ToLower();

			var culture = CultureInfo.InvariantCulture;
			var monthNames = culture.DateTimeFormat.MonthNames;
			var abbrevMonthNames = culture.DateTimeFormat.AbbreviatedMonthNames;

			for (int i = 0; i < 12; i++)
			{
				if (input == monthNames[i].ToLower() || input == abbrevMonthNames[i].ToLower())
				{
					month = i + 1;
					return true;
				}
			}

			return false;
		}

		public class ParameterUpdateVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public ParameterUpdateVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return ReferenceEquals(node, _oldParameter) ? _newParameter : base.VisitParameter(node);
            }
        }
    }
}


