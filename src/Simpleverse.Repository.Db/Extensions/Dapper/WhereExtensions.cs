using Dapper;
using EnumsNET;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace Simpleverse.Repository.Db.Extensions.Dapper
{
	public static class WhereExtensions
	{
		#region Value

		public static SqlBuilder Where<T>(this SqlBuilder sqlBuilder, string name, T? value, string alias, string condition = "=") where T : struct
			=> sqlBuilder.Where(new Selector(name, alias), value, condition: condition);

		public static SqlBuilder Where<T, TTable>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, T>> column, T? value, string condition = "=") where T : struct
			=> sqlBuilder.Where(table.Column(column), value, condition: condition);

		public static SqlBuilder Where<T>(this SqlBuilder sqlBuilder, Selector column, T? value, string condition = "=") where T : struct
		{
			if (value.HasValue)
				sqlBuilder.WhereInternal(column, value.Value, condition: condition);

			return sqlBuilder;
		}

		#region WhereBetween

		public static SqlBuilder WhereBetween<T>(this SqlBuilder sqlBuilder, string name, T? valueFrom, T? valueTo, string alias) where T : struct
			=> sqlBuilder.WhereBetween(new Selector(name, alias), valueFrom, valueTo);

		public static SqlBuilder WhereBetween<T, TTable>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, T>> column, T? valueFrom, T? valueTo) where T : struct
			=> sqlBuilder.WhereBetween(table.Column(column), valueFrom, valueTo);

		public static SqlBuilder WhereBetween<T, TTable>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, T?>> column, T? valueFrom, T? valueTo) where T : struct
			=> sqlBuilder.WhereBetween(table.Column(column), valueFrom, valueTo);

		public static SqlBuilder WhereBetween<T>(this SqlBuilder sqlBuilder, Selector column, T? valueFrom, T? valueTo) where T : struct
		{
			if (valueFrom.HasValue)
				sqlBuilder.WhereInternal(
					new Selector(column.Column, column.TableAlias), valueFrom, condition: ">=", parameterName: column.Column + "From"
				);

			if (valueTo.HasValue)
				sqlBuilder.WhereInternal(
					new Selector(column.Column, column.TableAlias), valueTo, condition: "<=", parameterName: column.Column + "To"
				);

			return sqlBuilder;
		}

		#endregion

		#endregion

		#region String

		public static SqlBuilder Where(this SqlBuilder sqlBuilder, string name, string value, string alias, string condition = "=")
		{
			return sqlBuilder.Where(new Selector(name, alias), value, condition: condition);
		}

		public static SqlBuilder Where<TTable>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, string>> column, string value, string condition = "=")
			=> sqlBuilder.Where(table.Column(column), value, condition: condition);

		public static SqlBuilder Where(this SqlBuilder sqlBuilder, Selector column, string value, string condition = "=")
		{
			if (!string.IsNullOrEmpty(value))
				sqlBuilder.WhereInternal(column, value, condition: condition);

			return sqlBuilder;
		}

		public static SqlBuilder WhereStarts(this SqlBuilder sqlBuilder, Selector column, string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				sqlBuilder.WhereInternal(
					column,
					value,
					(column, resolvedParamName) => column.StartWith($"@{resolvedParamName}", true)
				);
			}

			return sqlBuilder;
		}

		public static SqlBuilder WhereEnds(this SqlBuilder sqlBuilder, Selector column, string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				sqlBuilder.WhereInternal(
					column,
					value,
					(column, resolvedParamName) => column.EndsWith($"@{resolvedParamName}", true)
				);
			}

			return sqlBuilder;
		}

		public static SqlBuilder WhereContains(this SqlBuilder sqlBuilder, Selector column, string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				sqlBuilder.WhereInternal(
					column,
					value,
					(column, resolvedParamName) => column.Contains($"@{resolvedParamName}", true)
				);
			}

			return sqlBuilder;
		}

		#region Enumerable

		public static SqlBuilder Where(this SqlBuilder sqlBuilder, string name, IEnumerable<string> values, string alias, bool not = false)
		{
			return sqlBuilder.Where(new Selector(name, alias), values, not: not);
		}

		public static SqlBuilder Where<TTable>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, string>> column, IEnumerable<string> values, bool not = false)
			=> sqlBuilder.Where(table.Column(column), values, not: not);

		public static SqlBuilder Where(this SqlBuilder sqlBuilder, Selector column, IEnumerable<string> values, bool not = false)
		{
			if (values != null && values.Any())
				sqlBuilder.Where(column.In(values, not).ToString());

			return sqlBuilder;
		}

		public static SqlBuilder WhereNot(this SqlBuilder sqlBuilder, string name, IEnumerable<string> values, string alias)
			=> sqlBuilder.Where(name, values, alias, not: true);

		#endregion

		#endregion

		#region Reference

		public static SqlBuilder Where<T>(this SqlBuilder sqlBuilder, string name, T value, string alias, string condition = "=")
			=> sqlBuilder.Where(new Selector(name, alias), value, condition: condition);

		public static SqlBuilder Where<T, TTable>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, T>> column, T value, string condition = "=")
			=> sqlBuilder.Where(table.Column(column), value, condition: condition);

		public static SqlBuilder Where<T>(this SqlBuilder sqlBuilder, Selector column, T value, string condition = "=")
		{
			if (value == null)
				return sqlBuilder;

			return sqlBuilder.WhereInternal(column, value, condition: condition);
		}

		#region Enumerable

		public static SqlBuilder Where<T>(this SqlBuilder sqlBuilder, string name, IEnumerable<T> values, string alias, bool not = false)
		{
			return sqlBuilder.Where(new Selector(name, alias), values, not: not);
		}

		public static SqlBuilder Where<T, TTable>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, T>> column, IEnumerable<T> values, bool not = false)
			=> sqlBuilder.Where(table.Column(column), values, not: not);

		public static SqlBuilder Where<T>(this SqlBuilder sqlBuilder, Selector column, IEnumerable<T> values, bool not = false)
		{
			if (values == null || !values.Any())
				return sqlBuilder;

			return sqlBuilder.Where(column.In(values, not).ToString());
		}

		public static SqlBuilder WhereNot<T>(this SqlBuilder sqlBuilder, string name, IEnumerable<T> values, string alias)
			=> sqlBuilder.Where(name, values, alias, not: true);

		public static SqlBuilder WhereNot<T, TTable>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, T>> column, IEnumerable<T> values)
			=> sqlBuilder.Where(table.Column(column), values, not: true);

		#endregion

		#endregion

		#region DateTime

		public static SqlBuilder Where(this SqlBuilder sqlBuilder, string name, IEnumerable<DateTime> values, string alias, bool not = false)
			=> sqlBuilder.Where(new Selector(name, alias), values, not: not);

		public static SqlBuilder Where<TTable>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, DateTime>> column, IEnumerable<DateTime> values, bool not = false)
			=> sqlBuilder.Where(table.Column(column), values, not: not);

		public static SqlBuilder Where(this SqlBuilder sqlBuilder, Selector column, IEnumerable<DateTime> values, bool not = false)
		{
			if (values == null || !values.Any())
				return sqlBuilder;

			return sqlBuilder.Where(column.In(values, not).ToString());
		}

		public static void WhereNot(this SqlBuilder sqlBuilder, string name, IEnumerable<DateTime> values, string alias)
			=> sqlBuilder.Where(name, values, alias, not: true);

		public static SqlBuilder WhereNot<TTable>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, DateTime>> column, IEnumerable<DateTime> values)
			=> sqlBuilder.Where(table.Column(column), values, not: true);

		#endregion

		#region Enum

		public static SqlBuilder Where<T>(this SqlBuilder sqlBuilder, string name, T? value, string alias) where T : struct, Enum
			=> sqlBuilder.Where(new Selector(name, alias), value);

		public static SqlBuilder Where<T, TTable>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, T>> column, T? value) where T : struct, Enum
			=> sqlBuilder.Where(table.Column(column), value);

		public static SqlBuilder Where<T>(this SqlBuilder sqlBuilder, Selector column, T? value) where T : struct, Enum
		{
			if (!value.HasValue)
				return sqlBuilder;

			if (typeof(T).IsDefined(typeof(FlagsAttribute), false))
			{
				var flags = value.Value.GetFlags();

				for (var iCount = 0; iCount < flags.Count(); iCount++)
				{
					var parameters = new DynamicParameters();

					parameters.Add($"@{column.Column}{iCount}", flags.ElementAt(iCount));
					sqlBuilder.OrWhere($"{column} & @{column.Column}{iCount} <> 0", parameters);
				}
			}
			else
			{
				sqlBuilder.WhereInternal(column, value.Value);
			}

			return sqlBuilder;
		}

		#endregion

		public static SqlBuilder Where(this SqlBuilder builder, Selector selector)
			=> builder.Where(selector.ToString());

		private static SqlBuilder WhereInternal<T>(
			this SqlBuilder sqlBuilder,
			Selector column,
			T value,
			string condition = "=",
			string parameterName = null
		)
			=> sqlBuilder.WhereInternal(
				column,
				value,
				(column, resolvedParamName) => column.Is($"@{resolvedParamName}", condition: condition),
				parameterName: parameterName
			);

		private static SqlBuilder WhereInternal<T>(
			this SqlBuilder sqlBuilder,
			Selector column,
			T value,
			Action<Selector, string> selectorOptions,
			string parameterName = null
		)
		{
			if (string.IsNullOrEmpty(parameterName))
				parameterName = column.Column;

			parameterName = DbRepository.ParameterName(parameterName, alias: column.TableAlias);
			selectorOptions(column, parameterName);

			return sqlBuilder.Where(
				column.ToString(),
				Parameter(parameterName, value)
			);
		}

		#region WhereNull

		public static SqlBuilder WhereNull(this SqlBuilder sqlBuilder, string name, bool? value, string alias)
			=> sqlBuilder.WhereNull(new Selector(name, alias), value);

		public static SqlBuilder WhereNull<TTable, T>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, T>> column, bool? value)
			=> sqlBuilder.WhereNull(table.Column(column), value);

		public static SqlBuilder WhereNull(this SqlBuilder sqlBuilder, Selector column, bool? value)
		{
			if (value.HasValue)
			{
				if (value.Value)
					sqlBuilder.WhereNull(column);
				else
					sqlBuilder.WhereNotNull(column);
			}

			return sqlBuilder;
		}

		public static SqlBuilder WhereNull(this SqlBuilder sqlBuilder, string name, string alias)
			=> sqlBuilder.WhereNull(new Selector(name, alias));

		public static SqlBuilder WhereNull<TTable, T>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, T>> column)
			=> sqlBuilder.WhereNull(table.Column(column));

		public static SqlBuilder WhereNull(this SqlBuilder sqlBuilder, Selector column)
			=> sqlBuilder.Where(column.IsNull().ToString());

		#endregion

		#region WhereNotNull

		public static SqlBuilder WhereNotNull(this SqlBuilder sqlBuilder, string name, bool? value, string alias)
			=> sqlBuilder.WhereNotNull(new Selector(name, alias), value);

		public static SqlBuilder WhereNotNull<TTable, T>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, T>> column, bool? value)
			=> sqlBuilder.WhereNotNull(table.Column(column), value);

		public static SqlBuilder WhereNotNull(this SqlBuilder sqlBuilder, Selector column, bool? value)
		{
			if (value.HasValue)
			{
				if (value.Value)
					sqlBuilder.WhereNotNull(column);
				else
					sqlBuilder.WhereNull(column);
			}

			return sqlBuilder;
		}

		public static SqlBuilder WhereNotNull(this SqlBuilder sqlBuilder, string name, string alias)
			=> sqlBuilder.WhereNotNull(new Selector(name, alias));

		public static SqlBuilder WhereNotNull<TTable, T>(this SqlBuilder sqlBuilder, Table<TTable> table, Expression<Func<TTable, T>> column)
			=> sqlBuilder.WhereNotNull(table.Column(column));

		public static SqlBuilder WhereNotNull(this SqlBuilder sqlBuilder, Selector column)
			=> sqlBuilder.Where(column.IsNotNull().ToString());

		#endregion

		#region WhereExists

		public static SqlBuilder WhereExists(this SqlBuilder builder, SqlBuilder.Template query)
		{
			return builder.Where($"EXISTS ({query.RawSql})", query.Parameters);
		}

		#endregion

		public static IDictionary<string, object> Parameter(string parameterName, object value)
		{
			IDictionary<string, object> parameter = new ExpandoObject();
			parameter[parameterName] = value;
			return parameter;
		}
	}
}
