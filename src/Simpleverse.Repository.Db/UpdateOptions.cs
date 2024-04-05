using Simpleverse.Repository.Db.Extensions.Dapper;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Simpleverse.Repository.Db
{
	public class UpdateOptions<TTable>
	{
		private readonly List<Action<QueryBuilder<TTable>>> _actions;

		public UpdateOptions()
		{
			_actions = new List<Action<QueryBuilder<TTable>>>();
		}

		public UpdateOptions<TTable> Set<T>(Expression<Func<TTable, T>> column, Selector selector)
		{
			_actions.Add(builder => builder.Set(column, selector));
			return this;
		}

		public UpdateOptions<TTable> Set<T>(Expression<Func<TTable, T?>> column, T? value) where T : struct
		{
			if (value.HasValue)
				_actions.Add(builder => builder.Set(column, value.Value));
			else
				_actions.Add(builder => builder.SetNull(column, true));

			return this;
		}

		public UpdateOptions<TTable> Set<T>(Expression<Func<TTable, int>> column, T value) where T : struct, Enum
		{
			_actions.Add(builder => builder.Set(column, value));
			return this;
		}

		public UpdateOptions<TTable> Set<T>(Expression<Func<TTable, int?>> column, T? value) where T : struct, Enum
		{
			if (value.HasValue)
				_actions.Add(builder => builder.Set(builder.Table.Column(column), value.Value));
			else
				_actions.Add(builder => builder.SetNull(column, true));
			return this;
		}

		public UpdateOptions<TTable> Set(Expression<Func<TTable, string>> column, string value)
		{
			_actions.Add(builder => builder.Set(column, value));
			return this;
		}

		public UpdateOptions<TTable> Set<T>(Expression<Func<TTable, T>> column, T value)
		{
			_actions.Add(builder => builder.Set(column, value));
			return this;
		}

		public UpdateOptions<TTable> SetNull<T>(Expression<Func<TTable, T>> column)
		{
			_actions.Add(builder => builder.SetNull(column, true));
			return this;
		}

		public void Apply(QueryBuilder<TTable> builder)
		{
			foreach (var action in _actions)
			{
				action(builder);
			}
		}
	}
}
