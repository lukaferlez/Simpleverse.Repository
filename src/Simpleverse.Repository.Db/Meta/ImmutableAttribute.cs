using System;

namespace Simpleverse.Repository.Db.Meta
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ImmutableAttribute : Attribute
	{
	}
}
