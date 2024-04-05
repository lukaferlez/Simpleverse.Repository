using System.Collections.Generic;
using System.Linq;

namespace Simpleverse.Repository.ChangeTracking
{
	public class ChangeTrack : IChangeTrack
	{
		private Dictionary<string, object> _changes;

		public ChangeTrack()
		{
			Clear();
		}

		public void Track(string name, object value)
			=> _changes[name] = value;

		public IEnumerable<(string name, object value)> Changes
			=> _changes.Select(x => (x.Key, x.Value)).ToArray();

		public void Clear()
			=> _changes = new Dictionary<string, object>();
	}
}
