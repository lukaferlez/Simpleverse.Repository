namespace Simpleverse.Repository.Db
{
	public class DbQueryOptions : QueryOptions
	{
		private bool _lockForUpdate;
		public string LockCondition => _lockForUpdate ? "WITH(UPDLOCK)" : string.Empty;
		public void LockForUpdate()
		{
			_lockForUpdate = true;
		}

		public int? Take { get; set; }
		public OrderDirection Order { get; set; } = OrderDirection.Descending;
		public string TopCondition => Take.HasValue ? $"TOP {Take.Value}" : string.Empty;
	}
}
