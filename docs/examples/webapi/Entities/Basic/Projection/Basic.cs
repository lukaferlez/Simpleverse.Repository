using Simpleverse.Repository.Entity;
using System.Text.Json.Serialization;

namespace webapi.Entities.Basic.Projection
{
	public class Basic : IProject<BasicModel>
	{
		[JsonIgnore]
		public BasicModel Model { get; set; }

		internal virtual int Id
		{
			get => Model.Id;
			set => Model.Id = value;
		}
		public virtual string Name
		{
			get => Model.Name;
			set => Model.Name = value;
		}
		public virtual IEnumerable<string> Tags
		{
			get
			{
				return Model.Description.Split(",");
			}
			set
			{
				Model.Description = string.Join(",", value);
			}
		}
		public virtual DateTime Created
		{
			get => Model.Created;
			set => Model.Created = value;
		}

		public Basic()
			: this(new BasicModel())
		{

		}

		public Basic(BasicModel model)
		{
			Model = model;
		}
	}
}
