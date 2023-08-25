using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using Simpleverse.Repository.Db.Meta;

namespace Simpleverse.Repository.Db.SqlServer.Merge
{
	public class MergeActionOptions<T> : MergeKeyOptions
	{
		public string Query { get; set; }
		public string Condition { get; set; }
		public MergeAction Action { get; set; }

		public MergeActionOptions<T> Insert()
		{
			var typeMeta = TypeMeta.Get<T>();

			Action = MergeAction.Insert;
			ColumnsByPropertyInfo(typeMeta.PropertiesExceptKeyAndComputed);

			return this;
		}

		public MergeActionOptions<T> Update()
		{
			var typeMeta = TypeMeta.Get<T>();
			Action = MergeAction.Update;
			ColumnsByPropertyInfo(typeMeta.PropertiesExceptKeyAndComputed);
			CheckConditionOnColumns();

			return this;
		}

		public MergeActionOptions<T> Delete()
		{
			Action = MergeAction.Delete;
			return this;
		}

		public MergeActionOptions<T> Raw(string query)
		{
			Action = MergeAction.Query;
			Query = query;
			return this;
		}

		public MergeActionOptions<T> CheckConditionOnColumns()
		{
			return CheckCondition(Columns.ColumnListDifferenceCheck());
		}

		public MergeActionOptions<T> CheckCondition(string condition)
		{
			Condition = condition;
			return this;
		}

		public MergeActionOptions<T> ExcludeColumns(params string[] columNames)
		{
			return ExcludeColumns((IEnumerable<string>)columNames);
		}

		public MergeActionOptions<T> ExcludeColumns(IEnumerable<string> columNames)
		{
			if (Columns == null)
				return this;

			if (columNames == null)
				return this;

			Columns = Columns.Except(columNames);
			return this;
		}

		public void Format(StringBuilder sb)
		{
			switch (Action)
			{
				case MergeAction.Insert:
					sb.AppendFormat("INSERT({0})", Columns.ColumnList());
					sb.AppendLine();
					sb.AppendFormat("VALUES({0})", Columns.ColumnList("Source"));
					sb.AppendLine();
					break;
				case MergeAction.Update:
					sb.AppendLine("UPDATE SET");
					sb.AppendLine(Columns.ColumnListEquals(", ", leftPrefix: string.Empty));
					break;
				case MergeAction.Delete:
					sb.AppendLine("DELETE");
					break;
				case MergeAction.Query:
					sb.AppendLine(Query);
					break;
			}
		}
	}

	public class MergeKeyOptions
	{
		public IEnumerable<string> Columns { get; set; }

		public void ColumnsByName(params string[] columnNames)
		{
			Columns = columnNames;
		}

		public void ColumnsByPropertyInfo(IEnumerable<PropertyInfo> properties)
		{
			if (properties == null)
			{
				Columns = null;
				return;
			}

			Columns = properties.Select(x => x.Name);
		}
	}
}
