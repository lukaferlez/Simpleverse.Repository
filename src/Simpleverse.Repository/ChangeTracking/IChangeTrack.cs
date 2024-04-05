using System.Collections.Generic;

namespace Simpleverse.Repository.ChangeTracking
{
	public interface IChangeTrack
	{
		public IEnumerable<(string name, object value)> Changes { get; }
		void Clear();
	}
}
