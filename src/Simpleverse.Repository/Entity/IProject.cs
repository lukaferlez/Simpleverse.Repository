namespace Simpleverse.Repository.Entity
{
	public interface IProject<out T>
	{
		public T Model { get; }
	}
}
