using Simpleverse.Repository.Entity;
using System.Text.Json.Serialization;

namespace webapi.Entities.MultiModel
{
	public class Parent : IProject<ParentModel>
	{
		[JsonIgnore]
		public ParentModel Model { get; set; }

		internal int Id
		{
			get => Model.Id;
			set => Model.Id = value;
		}
		public string Name
		{
			get => Model.Name;
			set => Model.Name = value;
		}
		public IEnumerable<string> Tags
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


		public DateTime Created
		{
			get => Model.Created;
			set => Model.Created = value;
		}

		public Parent()
			: this(new ParentModel())
		{

		}

		public Parent(ParentModel model)
		{
			Model = model;
		}
	}
}
