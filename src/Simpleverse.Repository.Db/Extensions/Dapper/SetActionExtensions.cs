using Dapper;
using Simpleverse.Repository.Db.Extensions.Dapper;

namespace Simpleverse.Repository.Db.Extensions.Dapper
{
	public static class SetActionExtensions
	{
		#region Value

		public static SqlBuilder Set<T>(this SqlBuilder sqlBuilder, string name, T? value, string alias) where T : struct
			=> sqlBuilder.Set(new Selector(name, alias), value);

		public static SqlBuilder Set<T>(this SqlBuilder sqlBuilder, Selector column, T? value) where T : struct
		{
			if (value.HasValue)
				sqlBuilder.SetInternal(column, value.Value);

			return sqlBuilder;
		}

		#endregion

		#region String

		public static SqlBuilder Set(this SqlBuilder sqlBuilder, string name, string value, string alias)
			=> sqlBuilder.Set(new Selector(name, alias), value);

		public static SqlBuilder Set(this SqlBuilder sqlBuilder, Selector column, string value)
		{
			if (!string.IsNullOrEmpty(value))
				sqlBuilder.SetInternal(column, value);

			return sqlBuilder;
		}

		#endregion

		#region Reference

		public static SqlBuilder Set<T>(this SqlBuilder sqlBuilder, string name, T value, string alias)
			=> sqlBuilder.Set(new Selector(name, alias), value);

		public static SqlBuilder Set<T>(this SqlBuilder sqlBuilder, Selector column, T value)
		{
			return sqlBuilder.SetInternal(column, value);
		}

		#endregion

		public static SqlBuilder Set(this SqlBuilder sqlBuilder, Selector column, Selector value)
		{
			return sqlBuilder.Set($"{column} = {value}");
		}

		private static SqlBuilder SetInternal<T>(this SqlBuilder sqlBuilder, Selector column, T value, string parameterName = null)
		{
			if (string.IsNullOrEmpty(parameterName))
				parameterName = column.Column;

			parameterName = DbRepository.ParameterName(parameterName, alias: "Set_" + column.TableAlias);

			return sqlBuilder.Set($"{column} = @{parameterName}", WhereExtensions.Parameter(parameterName, value));
		}

		#region Null

		public static void SetNull(this SqlBuilder sqlBuilder, string name, bool? value, string alias)
			=> sqlBuilder.SetNull(new Selector(name, alias), value);

		public static void SetNull(this SqlBuilder sqlBuilder, Selector column, bool? value)
		{
			if (!value.HasValue)
				return;

			sqlBuilder.SetNull(column, value.Value);
		}

		public static void SetNullOnFalse(this SqlBuilder sqlBuilder, string name, bool? value, string alias)
			=> sqlBuilder.SetNullOnFalse(new Selector(name, alias), value);

		public static void SetNullOnFalse(this SqlBuilder sqlBuilder, Selector column, bool? value)
		{
			if (!value.HasValue)
				return;

			sqlBuilder.SetNull(column, !value.Value);
		}

		private static void SetNull(this SqlBuilder sqlBuilder, Selector column, bool value)
		{
			if (!value)
				return;

			sqlBuilder.Set($"{column} = NULL");
		}

		#endregion
	}
}
