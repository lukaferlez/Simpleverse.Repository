using Simpleverse.Repository.Db.Extensions.Dapper;
using Simpleverse.Repository.Db.Meta;
using System;
using System.Linq.Expressions;
using static Dapper.SqlBuilder;

namespace Simpleverse.Repository.Db
{
    public class Table<T>
	{
		public string Alias { get; }

		public TypeMeta Meta { get; }

		public string TableName => Meta.TableName;

		public Table(string alias)
			: this(TypeMeta.Get<T>(), alias)
		{

		}

		public Table(TypeMeta typeMeta, string alias)
		{
			Alias = alias;
			Meta = typeMeta;
		}

		public static Table<T> Get(string alias)
		{
			return new Table<T>(TypeMeta.Get<T>(), alias);
		}

		public Selector Column(string columnName)
		{
			return new Selector(columnName, Alias);
		}

		public Selector Column<R>(Expression<Func<T, R>> expression)
		{
			var lambda = (LambdaExpression)expression;

			MemberExpression memberExpression;
			if (lambda.Body is UnaryExpression unaryExpression)
				memberExpression = (MemberExpression)unaryExpression.Operand;
			else
				memberExpression = (MemberExpression)lambda.Body;

			return Column(memberExpression.Member.Name);
		}

		#region Select

		public Template Select(Action<QueryBuilder<T>> builder = null, Options options = null)
			=> Select(this, builder, options);

		public Template Select(string alias, Action<QueryBuilder<T>> builder = null, Options options = null)
			=> Select(new Table<T>(alias), builder, options);

		public Template Select(Table<T> source, Action<QueryBuilder<T>> builder = null, Options options = null)
			=> Query(
				source,
				templateBuilder =>
				{
					builder?.Invoke(templateBuilder);
					return templateBuilder.SelectTemplate(source, options ?? new Options());
				}
			);

		#endregion

		#region Update

		public Template Update(Action<QueryBuilder<T>> builder = null)
			=> Update(this, builder);

		public Template Update(string alias, Action<QueryBuilder<T>> builder = null)
			=> Update(new Table<T>(alias), builder);

		public Template Update(Table<T> source, Action<QueryBuilder<T>> builder = null)
			=> Query(
				source,
				templateBuilder =>
				{
					builder?.Invoke(templateBuilder);
					return templateBuilder.UpdateTemplate(source);
				}
			);

		#endregion

		#region Delete

		public Template Delete(Action<QueryBuilder<T>> builder = null)
			=> Delete(this, builder);

		public Template Delete(string alias, Action<QueryBuilder<T>> builder = null)
			=> Delete(new Table<T>(alias), builder);

		public Template Delete(Table<T> source, Action<QueryBuilder<T>> builder = null)
			=> Query(
				source,
				templateBuilder =>
				{
					builder?.Invoke(templateBuilder);
					return templateBuilder.DeleteTemplate(source);
				}
			);

		#endregion

		#region Query

		public Template Query(Func<QueryBuilder<T>, Template> templateBuilder)
			=> Query(this, templateBuilder);

		public Template Query(string alias, Func<QueryBuilder<T>, Template> templateBuilder)
			=> Query(new Table<T>(alias), templateBuilder);

		public Template Query(Table<T> source, Func<QueryBuilder<T>, Template> templateBuilder)
		{
			var query = new QueryBuilder<T>(source);
			return templateBuilder(query);
		}

		#endregion

		public QueryBuilder<T> AsQuery()
		{
			return new QueryBuilder<T>(this);
		}

		public override string ToString()
		{
			return DbRepository.TableReference(Meta.TableName, Alias);
		}
	}
}