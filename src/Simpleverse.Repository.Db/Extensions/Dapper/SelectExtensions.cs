using Dapper;
using Simpleverse.Repository.Db.Extensions.Dapper;
using System;
using System.Linq.Expressions;

namespace Simpleverse.Repository.Db.Extensions.Dapper
{
	public static class SelectExtensions
	{
		public static SqlBuilder SelectMap(
			this SqlBuilder builder,
			string sourceColumn,
			string sourceAlias,
			string targetColumn
		)
			=> builder.Select(
				new Selector(sourceColumn, sourceAlias).As(targetColumn)
			);

		public static SqlBuilder SelectMap<TTable, T>(
			this SqlBuilder builder,
			Table<TTable> tableReference,
			Expression<Func<TTable, T>> column,
			string targetColumn
		)
			=> builder.Select(
				tableReference.Column(column).As(targetColumn)
			);

		public static SqlBuilder SelectMap(
			this SqlBuilder builder,
			SqlBuilder.Template template,
			string targetColumn
		)
		{
			builder.Select(
				$@"(
					{template.RawSql}
				) AS {targetColumn}",
				template.Parameters
			);
			return builder;
		}

		public static SqlBuilder Select(
			this SqlBuilder builder,
			Selector column,
			Action<Selector> selectorOptions
		)
		{
			selectorOptions(column);
			return builder.Select(column);
		}

		public static SqlBuilder Select(
			this SqlBuilder builder,
			Selector selector
		) => builder.Select(selector.ToString(true));

		public static SqlBuilder SelectAll<TTable>(
			this SqlBuilder builder,
			Table<TTable> tableReference
		)
			=> builder.Select(
				tableReference.Column("*").As(string.Empty)
			);
	}
}