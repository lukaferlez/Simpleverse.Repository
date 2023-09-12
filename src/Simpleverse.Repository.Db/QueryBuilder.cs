using Dapper;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Simpleverse.Repository.Db.Extensions.Dapper;

namespace Simpleverse.Repository.Db
{
	public class QueryBuilder<TTable> : SqlBuilder
	{
		public Table<TTable> Table { get; }

		public QueryBuilder()
			: this(string.Empty)
		{
		}

		public QueryBuilder(string alias)
			: this(new Table<TTable>(alias))
		{
		}

		public QueryBuilder(Table<TTable> tableReference)
		{
			Table = tableReference;
		}

		#region Template

		public Template AsSelect(Action<QueryBuilder<TTable>> builder = null, DbQueryOptions options = null)
		{
			builder?.Invoke(this);
			return this.SelectTemplate(Table, options);
		}

		public Template AsUpdate(Action<QueryBuilder<TTable>> builder = null)
		{
			builder?.Invoke(this);
			return this.UpdateTemplate(Table);
		}

		public Template AsDelete(Action<QueryBuilder<TTable>> builder = null)
		{
			builder?.Invoke(this);
			return this.DeleteTemplate(Table);
		}

		public Template AddTemplate(
			Func<Func<Expression<Func<TTable, object>>, Selector>, string> function
		)
		{
			return AddTemplate(function((expression) => new Selector(Table.Column(expression).ToString(true))));
		}

		#endregion

		#region Select


		public QueryBuilder<TTable> SelectMap<T>(
			string rawSql,
			Expression<Func<TTable, T>> targetColumn
		)
			=> SelectMap(new Selector(rawSql), targetColumn);

		public QueryBuilder<TTable> SelectMap<TSource, T>(
			Table<TSource> source,
			Expression<Func<TSource, T>> column,
			Expression<Func<TTable, T>> targetColumn
		)
			=> SelectMap(source.Column(column), targetColumn);

		public QueryBuilder<TTable> SelectMap<T>(
			Selector column,
			Expression<Func<TTable, T>> targetColumn
		)
		{
			Select(column.As(Table.Column(targetColumn).Column));
			return this;
		}

		public QueryBuilder<TTable> SelectMap<T>(
			Expression<Func<TTable, T>> column,
			Expression<Func<TTable, T>> targetColumn
		)
			=> SelectMap(
				column,
				Table.Column(targetColumn).Column
			);

		public QueryBuilder<TTable> SelectMap<T>(
			Expression<Func<TTable, int>> column,
			Expression<Func<TTable, T>> targetColumn
		)
			where T : struct, Enum
			=> SelectMap(
				column,
				Table.Column(targetColumn).Column
			);

		public QueryBuilder<TTable> SelectMap<T>(
			Expression<Func<TTable, T>> column,
			string targetColumn
		)
		{
			Select(
				Table.Column(column).As(targetColumn)
			);
			return this;
		}

		public QueryBuilder<TTable> SelectMap<T>(
			Func<Func<Expression<Func<TTable, object>>, Selector>, Selector> selectorBuilder,
			Expression<Func<TTable, T>> targetColumn
		)
		{
			return SelectMap(
				selectorBuilder,
				Table.Column(targetColumn).Column
			);
		}

		public QueryBuilder<TTable> SelectMap(
			Func<Func<Expression<Func<TTable, object>>, Selector>, Selector> selectorBuilder,
			string targetColumn
		)
		{
			Select(
				selectorBuilder((column) => Table.Column(column)).As(targetColumn)
			);
			return this;
		}

		public QueryBuilder<TTable> SelectMap<TSource, TMap>(
			Expression<Func<TTable, TSource>> column,
			Action<Selector> selectorOptions,
			Expression<Func<TTable, TMap>> targetColumn
		)
		{
			return Select(
				column,
				x =>
				{
					selectorOptions(x);
					x.As(Table.Column(targetColumn).Column);
				}
			);
		}

		public QueryBuilder<TTable> Select<T>(
			Expression<Func<TTable, T>> column,
			Action<Selector> selectorOptions
		)
		{
			this.Select(Table.Column(column), selectorOptions);
			return this;
		}

		public QueryBuilder<TTable> Select<TSource, T>(
			Table<TSource> tableReference,
			Expression<Func<TSource, T>> column
		)
		{
			Select(tableReference.Column(column));
			return this;
		}

		public QueryBuilder<TTable> Select<T>(
			Expression<Func<TTable, T>> column
		)
		{
			Select(Table.Column(column));
			return this;
		}

		public QueryBuilder<TTable> Select(
			Func<Func<Expression<Func<TTable, object>>, Selector>, Selector> selectorBuilder
		)
		{
			Select(
				selectorBuilder((expression) => Table.Column(expression))
			);

			return this;
		}

		public QueryBuilder<TTable> Select(
			Func<Func<Expression<Func<TTable, object>>, Selector>, string> selectorBuilder
		)
		{
			Select(
				selectorBuilder((expression) => Table.Column(expression))
			);

			return this;
		}

		public QueryBuilder<TTable> Select(
			Selector selector
		)
		{
			Select(selector.ToString(true));
			return this;
		}

		public QueryBuilder<TTable> SelectMap<T>(
			Template template,
			Expression<Func<TTable, T>> column
		)
			=> SelectMap(template, Table.Column(column).ToString(false));

		public QueryBuilder<TTable> SelectMap(
			Template template,
			string targetColumn
		)
		{
			Select(
				$@"(
					{template.RawSql}
				) AS {targetColumn}",
				template.Parameters
			);
			return this;
		}

		public QueryBuilder<TTable> SelectAll()
		{
			this.SelectAll(Table);
			return this;
		}

		#endregion

		#region Where

		#region Value

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T>> column, T? value, string condition = "=") where T : struct
		{
			this.Where(Table.Column(column), value, condition: condition);
			return this;
		}

		#region Enumerable

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T?>> column, IEnumerable<T> values, bool not = false) where T : struct
		{
			this.Where(Table.Column(column), values, not: not);
			return this;
		}

		public QueryBuilder<TTable> WhereNot<T>(Expression<Func<TTable, T?>> column, IEnumerable<T> values) where T : struct
			=> Where(column, values, not: true);

		#endregion

		#region WhereBetween

		public QueryBuilder<TTable> WhereBetween<T>(Expression<Func<TTable, T>> column, T? valueFrom, T? valueTo) where T : struct
		{
			this.WhereBetween(Table.Column(column), valueFrom, valueTo);
			return this;
		}

		#endregion

		#endregion

		#region String

		public QueryBuilder<TTable> Where(Expression<Func<TTable, string>> column, string value, string condition = "=")
		{
			this.Where(Table.Column(column), value, condition: condition);
			return this;
		}

		public QueryBuilder<TTable> Where(Expression<Func<TTable, string>> column, IEnumerable<string> values, bool not = false)
		{
			this.Where(Table.Column(column), values, not: not);
			return this;
		}

		public QueryBuilder<TTable> WhereNot(Expression<Func<TTable, string>> column, IEnumerable<string> values)
			=> Where(column, values, not: true);

		public QueryBuilder<TTable> WhereStarts(Expression<Func<TTable, string>> column, string value)
		{
			this.WhereStarts(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> WhereEnds(Expression<Func<TTable, string>> column, string value)
		{
			this.WhereEnds(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> WhereContains(Expression<Func<TTable, string>> column, string value)
		{
			this.WhereContains(Table.Column(column), value);
			return this;
		}

		#endregion

		#region References

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T>> column, T value, string condition = "=")
		{
			this.Where(Table.Column(column), value, condition: condition);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T>> column, IEnumerable<T> values, bool not = false)
		{
			this.Where(Table.Column(column), values, not: not);
			return this;
		}

		public QueryBuilder<TTable> WhereNot<T>(Expression<Func<TTable, T>> column, IEnumerable<T> values)
			=> Where(column, values, not: true);

		#endregion

		#region DateTime

		public QueryBuilder<TTable> Where(Expression<Func<TTable, DateTime>> column, IEnumerable<DateTime> values, bool not = false)
		{
			this.Where(Table.Column(column), values, not: not);
			return this;
		}

		public QueryBuilder<TTable> WhereNot(Expression<Func<TTable, DateTime>> column, IEnumerable<DateTime> values)
			=> Where(column, values, not: true);

		#endregion

		#region Enum

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T?>> column, T? value) where T : struct, Enum
		{
			this.Where(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T?>> column, IEnumerable<T?> value) where T : struct, Enum
		{
			this.Where(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T>> column, T? value) where T : struct, Enum
		{
			this.Where(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T>> column, IEnumerable<T?> value) where T : struct, Enum
		{
			this.Where(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T?>> column, T value) where T : struct, Enum
		{
			this.Where(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T?>> column, IEnumerable<T> value) where T : struct, Enum
		{
			this.Where(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T>> column, T value) where T : struct, Enum
		{
			this.Where(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T>> column, IEnumerable<T> value) where T : struct, Enum
		{
			this.Where(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, int>> column, T value) where T : struct, Enum
		{
			this.Where(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, int>> column, T? value) where T : struct, Enum
		{
			this.Where(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, int>> column, IEnumerable<T> value) where T : struct, Enum
		{
			this.Where(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, int>> column, IEnumerable<T?> value) where T : struct, Enum
		{
			this.Where(Table.Column(column), value);
			return this;
		}

		#endregion

		#region WhereNull

		public QueryBuilder<TTable> WhereNull<T>(Expression<Func<TTable, T>> column, bool? value)
		{
			this.WhereNull(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> WhereNull<T>(Expression<Func<TTable, T>> column)
		{
			this.WhereNull(Table.Column(column));
			return this;
		}

		#endregion

		#region WhereNotNull

		public QueryBuilder<TTable> WhereNotNull<T>(Expression<Func<TTable, T>> column, bool? value)
		{
			this.WhereNotNull(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> WhereNotNull<T>(Expression<Func<TTable, T>> column)
		{
			this.WhereNotNull(Table.Column(column));
			return this;
		}

		#endregion

		#region Selector

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T>> column, Action<Selector> selectorAction)
		{
			var selector = Table.Column(column);
			selectorAction?.Invoke(selector);
			this.Where(selector);
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T>> column, Func<Selector, Selector> selectorFunc)
		{
			var selector = Table.Column(column);
			this.Where(selectorFunc?.Invoke(selector));
			return this;
		}

		public QueryBuilder<TTable> Where<T>(Expression<Func<TTable, T>> column, Func<Selector, string> selectorFunc)
		{
			var selector = Table.Column(column);
			Where(selectorFunc?.Invoke(selector));
			return this;
		}

		public QueryBuilder<TTable> Where<T>(
			Func<Func<Expression<Func<TTable, object>>, Selector>, string> selectorBuilder
		)
		{
			Where(selectorBuilder((column) => Table.Column(column)));
			return this;
		}

		#endregion

		#endregion

		#region Set

		public QueryBuilder<TTable> Set<T>(Expression<Func<TTable, T>> column, Selector selector)
		{
			this.Set(Table.Column(column), selector);
			return this;
		}

		public QueryBuilder<TTable> Set<T>(Expression<Func<TTable, T>> column, T? value) where T : struct
		{
			this.Set(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Set<T>(Expression<Func<TTable, T?>> column, T? value) where T : struct
		{
			this.Set(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Set<T>(Expression<Func<TTable, int>> column, T value) where T : struct, Enum
		{
			this.Set(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Set<T>(Expression<Func<TTable, int>> column, T? value) where T : struct, Enum
		{
			this.Set(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Set(Expression<Func<TTable, string>> column, string value)
		{
			this.Set(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> Set<T>(Expression<Func<TTable, T>> column, T value)
		{
			this.Set(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> SetNull<T>(Expression<Func<TTable, T>> column, bool value)
		{
			this.SetNull(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> SetNull<T>(Expression<Func<TTable, T>> column, bool? value)
		{
			this.SetNull(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> SetNullOnFalse<T>(Expression<Func<TTable, T>> column, bool value)
		{
			this.SetNullOnFalse(Table.Column(column), value);
			return this;
		}

		public QueryBuilder<TTable> SetNullOnFalse<T>(Expression<Func<TTable, T>> column, bool? value)
		{
			this.SetNullOnFalse(Table.Column(column), value);
			return this;
		}

		#endregion

		#region Join

		public QueryBuilder<TTable> Join<TTarget, TColumn>(
			Table<TTarget> targetReference,
			Expression<Func<TTable, TColumn>> source,
			Expression<Func<TTarget, TColumn>> target
		)
		{
			this.Join(Table, targetReference, source, target);
			return this;
		}

		public QueryBuilder<TTable> Join<TTarget>(
			Table<TTarget> targetReference,
			Func<Table<TTable>, Table<TTarget>, string> joinCondition
		)
		{
			this.Join(Table, targetReference, joinCondition);
			return this;
		}

		public QueryBuilder<TTable> InnerJoin<TTarget, TColumn>(
			Table<TTarget> targetReference,
			Expression<Func<TTable, TColumn>> source,
			Expression<Func<TTarget, TColumn>> target
		)
		{
			this.InnerJoin(Table, targetReference, source, target);
			return this;
		}

		public QueryBuilder<TTable> InnerJoin<TTarget>(
			Table<TTarget> targetReference,
			Func<Table<TTable>, Table<TTarget>, string> joinCondition
		)
		{
			this.InnerJoin(Table, targetReference, joinCondition);
			return this;
		}

		public QueryBuilder<TTable> LeftJoin<TTarget, TColumn>(
			Table<TTarget> targetReference,
			Expression<Func<TTable, TColumn>> source,
			Expression<Func<TTarget, TColumn>> target
		)
		{
			this.LeftJoin(Table, targetReference, source, target);
			return this;
		}

		public QueryBuilder<TTable> LeftJoin<TTarget>(
			Table<TTarget> targetReference,
			Func<Table<TTable>, Table<TTarget>, string> joinCondition
		)
		{
			this.LeftJoin(Table, targetReference, joinCondition);
			return this;
		}

		public QueryBuilder<TTable> RightJoin<TTarget, TColumn>(
			Table<TTarget> targetReference,
			Expression<Func<TTable, TColumn>> source,
			Expression<Func<TTarget, TColumn>> target
		)
		{
			this.RightJoin(Table, targetReference, source, target);
			return this;
		}

		public QueryBuilder<TTable> RightJoin<TTarget>(
			Table<TTarget> targetReference,
			Func<Table<TTable>, Table<TTarget>, string> joinCondition
		)
		{
			this.RightJoin(Table, targetReference, joinCondition);
			return this;
		}

		#endregion

		#region GroupBy

		public QueryBuilder<TTable> GroupBy<T>(Expression<Func<TTable, T>> column)
		{
			this.GroupBy(Table.Column(column));
			return this;
		}

		#endregion

		#region OrderBy

		public QueryBuilder<TTable> OrderBy<TSource, T>(
			Table<TSource> source,
			Expression<Func<TSource, T>> column,
			OrderDirection orderDirection = OrderDirection.Ascending
		)
		{
			this.OrderBy(source.Column(column), orderDirection);
			return this;
		}

		public QueryBuilder<TTable> OrderBy<T>(
			Expression<Func<TTable, T>> column,
			OrderDirection orderDirection = OrderDirection.Ascending
		)
		{
			this.OrderBy(Table.Column(column), orderDirection);
			return this;
		}

		#endregion
	}
}
