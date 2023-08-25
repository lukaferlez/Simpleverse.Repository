using Dapper;
using Simpleverse.Repository.Db.Meta;

namespace Simpleverse.Repository.Db.Extensions.Dapper
{
	public static class TemplateExtensions
	{
		#region Select

		public static SqlBuilder.Template SelectTemplate<T>(this SqlBuilder sqlBuilder, DbQueryOptions options, string tableAlias = null)
			=> sqlBuilder.SelectTemplate(DbRepository.TableReference(TypeMeta.Get<T>().TableName, tableAlias), options);

		public static SqlBuilder.Template SelectTemplate<T>(this SqlBuilder sqlBuilder, Table<T> source, DbQueryOptions options)
			=> sqlBuilder.SelectTemplate(source.ToString(), options);

		public static SqlBuilder.Template SelectTemplate(this SqlBuilder sqlBuilder, string tableReference, DbQueryOptions options)
		{
			return sqlBuilder.AddTemplate($@"
					SELECT {options.TopCondition} /**select**/
					FROM {tableReference}
						{options.LockCondition}
						/**join**/
						/**innerjoin**/
						/**leftjoin**/
						/**rightjoin**/
					/**where**/
					/**groupby**/
					/**orderby**/
				"
			);
		}

		#endregion

		#region Delete

		public static SqlBuilder.Template DeleteTemplate<T>(this SqlBuilder sqlBuilder, string tableAlias = null)
			=> sqlBuilder.DeleteTemplate(Table<T>.Get(tableAlias));

		public static SqlBuilder.Template DeleteTemplate<T>(this SqlBuilder sqlBuilder, Table<T> source)
			=> sqlBuilder.DeleteTemplate(source.Meta.TableName, source.Alias);

		public static SqlBuilder.Template DeleteTemplate(this SqlBuilder sqlBuilder, string tableName, string alias)
		{
			return sqlBuilder.AddTemplate(
				$@"
					DELETE {(string.IsNullOrEmpty(alias) ? tableName : alias)}
					FROM
						{DbRepository.TableReference(tableName, alias)}
						/**join**/
						/**innerjoin**/
						/**leftjoin**/
						/**rightjoin**/
					/**where**/
				"
			);
		}

		#endregion

		#region Update

		public static SqlBuilder.Template UpdateTemplate<T>(this SqlBuilder sqlBuilder, string tableAlias = null)
			=> sqlBuilder.UpdateTemplate(Table<T>.Get(tableAlias));

		public static SqlBuilder.Template UpdateTemplate<T>(this SqlBuilder sqlBuilder, Table<T> source)
			=> sqlBuilder.UpdateTemplate(source.Meta.TableName, source.Alias);

		public static SqlBuilder.Template UpdateTemplate(this SqlBuilder sqlBuilder, string tableName, string alias)
		{
			return sqlBuilder.AddTemplate(
				$@"
					UPDATE {(string.IsNullOrEmpty(alias) ? tableName : alias)}
					/**set**/
					FROM
						{DbRepository.TableReference(tableName, alias)}
						/**join**/
						/**innerjoin**/
						/**leftjoin**/
						/**rightjoin**/
					/**where**/
				"
			);
		}

		#endregion
	}
}
