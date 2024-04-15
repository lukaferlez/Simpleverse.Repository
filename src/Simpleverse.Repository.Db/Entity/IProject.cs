namespace Simpleverse.Repository.Db.Entity
{
	public interface IProject<out T>
	{
		public T Model { get; }
	}
}
