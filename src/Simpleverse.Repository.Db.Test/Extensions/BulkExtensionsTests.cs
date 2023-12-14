using Simpleverse.Repository.Db.SqlServer;
using System.Collections.Generic;
using Xunit;

namespace Simpleverse.Repository.Db.Test.Extensions
{
	public class BulkExtensionsTests
	{
		[Fact]
		public void ToDataTable_WhenProvidedNullableData_ReturnsDataTable()
		{
			var entities = new List<Identity>
			{
				 new Identity
				 {
					 Id = 1,
					 Name = null
				 }
			};
			var dataTable = BulkExtensions.ToDataTable(entities, typeof(Identity).GetProperties());
			Assert.NotNull(dataTable);
			Assert.NotEmpty(dataTable.Columns);
			Assert.NotEmpty(dataTable.Rows);
		}
	}
}
