using System.Collections.Generic;
using System.Reflection;

namespace Simpleverse.Dapper.SqlServer.Merge
{
	public class MergeActionOptions
	{
		public string Condition { get; set; }
		public MergeAction Action { get; set; }
		public IEnumerable<PropertyInfo> Columns { get; set; }
	}
}
