using Dapper;
using Simpleverse.Repository.Db.Extensions.Dapper;
using Simpleverse.Repository.Db.Meta;
using System;
using System.Linq.Expressions;

namespace Simpleverse.Repository.Db.Extensions.Dapper
{
	public static class JoinExtensions
	{
		public static SqlBuilder Join<TTarget>(
			this SqlBuilder builder,
			string sourceColumn, string sourceAlias,
			string targetColumn, string targetAlias
		)
		{
			return Join<TTarget>(builder.Join, sourceColumn, sourceAlias, targetColumn, targetAlias);
		}

		public static SqlBuilder InnerJoin<TTarget>(
			this SqlBuilder builder,
			string sourceColumn, string sourceAlias,
			string targetColumn, string targetAlias
		)
		{
			return Join<TTarget>(builder.InnerJoin, sourceColumn, sourceAlias, targetColumn, targetAlias);
		}

		public static SqlBuilder LeftJoin<TTarget>(
			this SqlBuilder builder,
			string sourceColumn, string sourceAlias,
			string targetColumn, string targetAlias
		)
		{
			return Join<TTarget>(builder.LeftJoin, sourceColumn, sourceAlias, targetColumn, targetAlias);
		}

		public static SqlBuilder RightJoin<TTarget>(
			this SqlBuilder builder,
			string sourceColumn, string sourceAlias,
			string targetColumn, string targetAlias
		)
		{
			return Join<TTarget>(builder.RightJoin, sourceColumn, sourceAlias, targetColumn, targetAlias);
		}

		internal static SqlBuilder Join<TTarget>(
			Func<string, dynamic, SqlBuilder> joinFunction,
			string sourceColumn, string sourceAlias,
			string targetColumn, string targetAlias
		)
		{
			var targetTypeMeta = TypeMeta.Get<TTarget>();

			return joinFunction(
				@$"{DbRepository.TableReference(targetTypeMeta.TableName, targetAlias)}
					ON {DbRepository.ColumnReference(sourceColumn, sourceAlias)} = {DbRepository.ColumnReference(targetColumn, targetAlias)}
				",
				null
			);
		}

		public static SqlBuilder Join<TTarget>(
			this SqlBuilder builder,
			string alias,
			Func<Table<TTarget>, string> joinCondition,
			out Table<TTarget> targetReference
		)
		{
			return Join(builder.Join, alias, joinCondition, out targetReference);
		}

		public static SqlBuilder InnerJoin<TTarget>(
			this SqlBuilder builder,
			string alias,
			Func<Table<TTarget>, string> joinCondition,
			out Table<TTarget> targetReference
		)
		{
			return Join(builder.InnerJoin, alias, joinCondition, out targetReference);
		}

		public static SqlBuilder LeftJoin<TTarget>(
			this SqlBuilder builder,
			string alias,
			Func<Table<TTarget>, string> joinCondition,
			out Table<TTarget> targetReference
		)
		{
			return Join(builder.LeftJoin, alias, joinCondition, out targetReference);
		}

		public static SqlBuilder RightJoin<TTarget>(
			this SqlBuilder builder,
			string alias,
			Func<Table<TTarget>, string> joinCondition,
			out Table<TTarget> targetReference
		)
		{
			return Join(builder.RightJoin, alias, joinCondition, out targetReference);
		}

		internal static SqlBuilder Join<TTarget>(
			Func<string, dynamic, SqlBuilder> joinFunction,
			string alias,
			Func<Table<TTarget>, string> joinCondition,
			out Table<TTarget> targetReference
		)
		{
			targetReference = Table<TTarget>.Get(alias);
			return joinFunction(
				$@"
					{targetReference}
					ON {joinCondition(targetReference)}
				",
				null
			);
		}

		public static SqlBuilder Join<TSource, TTarget>(
			this SqlBuilder builder,
			Table<TSource> sourceReference,
			Table<TTarget> targetReference,
			Func<Table<TSource>, Table<TTarget>, string> joinCondition
		)
		{
			return Join(builder.Join, sourceReference, targetReference, joinCondition);
		}

		public static SqlBuilder InnerJoin<TSource, TTarget>(
			this SqlBuilder builder,
			Table<TSource> sourceReference,
			Table<TTarget> targetReference,
			Func<Table<TSource>, Table<TTarget>, string> joinCondition
		)
		{
			return Join(builder.InnerJoin, sourceReference, targetReference, joinCondition);
		}

		public static SqlBuilder LeftJoin<TSource, TTarget>(
			this SqlBuilder builder,
			Table<TSource> sourceReference,
			Table<TTarget> targetReference,
			Func<Table<TSource>, Table<TTarget>, string> joinCondition
		)
		{
			return Join(builder.LeftJoin, sourceReference, targetReference, joinCondition);
		}

		public static SqlBuilder RightJoin<TSource, TTarget>(
			this SqlBuilder builder,
			Table<TSource> sourceReference,
			Table<TTarget> targetReference,
			Func<Table<TSource>, Table<TTarget>, string> joinCondition
		)
		{
			return Join(builder.RightJoin, sourceReference, targetReference, joinCondition);
		}

		internal static SqlBuilder Join<TSource, TTarget>(
			Func<string, dynamic, SqlBuilder> joinFunction,
			Table<TSource> sourceReference,
			Table<TTarget> targetReference,
			Func<Table<TSource>, Table<TTarget>, string> joinCondition
		)
		{
			return joinFunction(
				$@"{targetReference}
					ON {joinCondition(sourceReference, targetReference)}
				",
				null
			);
		}

		public static SqlBuilder Join<TSource, TTarget, TColumn>(
			this SqlBuilder builder,
			Table<TSource> sourceReference,
			Table<TTarget> targetReference,
			Expression<Func<TSource, TColumn>> source,
			Expression<Func<TTarget, TColumn>> target
		)
		{
			return Join(builder.Join, sourceReference, targetReference, source, target);
		}

		public static SqlBuilder InnerJoin<TSource, TTarget, TColumn>(
			this SqlBuilder builder,
			Table<TSource> sourceReference,
			Table<TTarget> targetReference,
			Expression<Func<TSource, TColumn>> source,
			Expression<Func<TTarget, TColumn>> target
		)
		{
			return Join(builder.InnerJoin, sourceReference, targetReference, source, target);
		}

		public static SqlBuilder LeftJoin<TSource, TTarget, TColumn>(
			this SqlBuilder builder,
			Table<TSource> sourceReference,
			Table<TTarget> targetReference,
			Expression<Func<TSource, TColumn>> source,
			Expression<Func<TTarget, TColumn>> target
		)
		{
			return Join(builder.LeftJoin, sourceReference, targetReference, source, target);
		}

		public static SqlBuilder RightJoin<TSource, TTarget, TColumn>(
			this SqlBuilder builder,
			Table<TSource> sourceReference,
			Table<TTarget> targetReference,
			Expression<Func<TSource, TColumn>> source,
			Expression<Func<TTarget, TColumn>> target
		)
		{
			return Join(builder.RightJoin, sourceReference, targetReference, source, target);
		}

		internal static SqlBuilder Join<TSource, TTarget, TColumn>(
			Func<string, dynamic, SqlBuilder> joinFunction,
			Table<TSource> sourceReference,
			Table<TTarget> targetReference,
			Expression<Func<TSource, TColumn>> source,
			Expression<Func<TTarget, TColumn>> target
		)
		{
			return joinFunction(
				$@"{targetReference}
					ON {sourceReference.Column(source)} = {targetReference.Column(target)}
				",
				null
			);
		}
	}
}