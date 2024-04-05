using System.Collections.Generic;

namespace Simpleverse.Repository.ChangeTracking
{
	public interface IChangeTrack
	{
		public bool IsChanged { get; }
		public IEnumerable<string> Changed { get; }
		void Clear();
	}
}
