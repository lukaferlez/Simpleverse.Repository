namespace webapi.Entities.Basic
{
	public class BasicModel
	{
		public virtual int Id { get; set; }
		public virtual string Name { get; set; }
		public virtual string Description { get; set; }
		public virtual DateTime Created { get; set; }
	}
}
