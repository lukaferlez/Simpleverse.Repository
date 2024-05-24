
namespace webapi.Entities.MultiModel
{
	public interface IParentModel
	{
		int Id { get; set; }
		string Name { get; set; }
		string Description { get; set; }
		DateTime Created { get; set; }
	}
}