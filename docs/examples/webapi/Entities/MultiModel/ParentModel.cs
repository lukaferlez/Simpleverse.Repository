using Dapper.Contrib.Extensions;

namespace webapi.Entities.MultiModel
{
	[Table("dbo.[Basic]")]
	public class ParentModel : IParentModel
	{
		#region IParentModel

		public virtual int Id { get; set; }
		public virtual string Name { get; set; }
		public virtual string Description { get; set; }
		public virtual DateTime Created { get; set; }

		#endregion

		[Write(false)]
		public IEnumerable<ChildModel> Children { get; set; }
	}
}
