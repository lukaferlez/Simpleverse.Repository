using System.Collections.Generic;
using System.Reflection;
using System;

namespace Simpleverse.Repository.Db.SqlServer.Merge
{
	public class OutputOptions<T>
	{
		public Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> Map { get; set; }

		/// <summary>
		/// When true (the default) matched entities are only updated, and therefore only mapped, if their
		/// columns actually differ. Set to false to update and map every matched entity, including ones
		/// that are unchanged.
		/// </summary>
		public bool MapChangedOnly { get; set; } = true;
	}
}
