using Dapper;
using System;
using System.Data;

namespace Simpleverse.Repository.Db.Meta
{
	public class SqlMapperDateTimeKindHandler : SqlMapper.TypeHandler<DateTime>
	{
		protected DateTimeKind DateTimeKind { get; }

		public SqlMapperDateTimeKindHandler(DateTimeKind dateTimeKind)
		{
			DateTimeKind = dateTimeKind;
		}

		public override void SetValue(IDbDataParameter parameter, DateTime value)
		{
			parameter.DbType = DbType.DateTime;
			parameter.Value = value;
		}

		public override DateTime Parse(object value)
		{
			return DateTime.SpecifyKind((DateTime)value, DateTimeKind);
		}

		public static void UseUtc()
		{
			SqlMapper.RemoveTypeMap(typeof(DateTime));
			SqlMapper.AddTypeHandler(new SqlMapperDateTimeKindHandler(DateTimeKind.Utc));
		}
	}
}
