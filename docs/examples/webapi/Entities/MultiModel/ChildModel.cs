using System.ComponentModel.DataAnnotations.Schema;

namespace webapi.Entities.MultiModel
{
	[Table("dbo.[BasicChild]")]
	public class ChildModel
	{
		public virtual int Id { get; set; }
		public virtual int ParentId { get; set; }
		public virtual string SomeStringData { get; set; }
		public virtual bool SuperAdditionalData { get; set; }
		public virtual DateTime Created { get; set; }
	}
}
