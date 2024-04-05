using System.Collections.Generic;
using System.Linq;

namespace Simpleverse.Repository.ChangeTracking
{
	public class ChangeTrack : IChangeTrack
	{
		private HashSet<string> _changed;

		public bool IsChanged
			=> _changed.Count > 0;

		public IEnumerable<string> Changed
			=> _changed.ToArray();

		public ChangeTrack()
		{
			Clear();
		}

		public void SetChanged(string name)
			=> _changed.Add(name);

		public void Clear()
			=> _changed = new HashSet<string>();
	}
}
