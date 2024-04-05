using Simpleverse.Repository.ChangeTracking;

namespace Simpleverse.Repository.Test.ChangeTracking
{
	public class ChangeProxyFactoryTest
	{
		[Fact]
		public void CreateInterfaceChangeTracker()
		{
			var proxy = ChangeProxyFactory.Create<TestInterface>();

			proxy.IntProperty = 1;
			proxy.IntNullableProperty = null;

			var changeTracker = (IChangeTrack)proxy;
			Assert.Equal(2, changeTracker.Changes.Count());

			Assert.Single(changeTracker.Changes.Where(x => x.name == nameof(TestInterface.IntProperty)));
			Assert.Equal(1, changeTracker.Changes.First(x => x.name == nameof(TestInterface.IntProperty)).value);

			Assert.Single(changeTracker.Changes.Where(x => x.name == nameof(TestInterface.IntNullableProperty)));
			Assert.Null(changeTracker.Changes.First(x => x.name == nameof(TestInterface.IntNullableProperty)).value);
		}

		[Fact]
		public void CreateClassChangeTracker()
		{
			var proxy = ChangeProxyFactory.Create<TestClass>();

			proxy.IntProperty = 1;
			proxy.IntNullableProperty = null;

			var changeTracker = (IChangeTrack)proxy;
			Assert.Equal(2, changeTracker.Changes.Count());

			Assert.Single(changeTracker.Changes.Where(x => x.name == nameof(TestInterface.IntProperty)));
			Assert.Equal(1, changeTracker.Changes.First(x => x.name == nameof(TestInterface.IntProperty)).value);

			Assert.Single(changeTracker.Changes.Where(x => x.name == nameof(TestInterface.IntNullableProperty)));
			Assert.Null(changeTracker.Changes.First(x => x.name == nameof(TestInterface.IntNullableProperty)).value);
		}

		[Fact]
		public void FailCreateProxyNoParameterlessConstructor()
		{
			Assert.Throws<NotSupportedException>(() => ChangeProxyFactory.Create<TestClassNoParameterlessConstructor>());
		}

		[Fact]
		public void FailCreateProxyNoVirtual()
		{
			Assert.Throws<NotSupportedException>(() => ChangeProxyFactory.Create<TestClassNoVirtual>());
		}

		[Fact]
		public void MultipleChange()
		{
			var proxy = ChangeProxyFactory.Create<TestInterface>();

			proxy.IntNullableProperty = null;
			proxy.IntNullableProperty = 2;

			var changeTracker = (IChangeTrack)proxy;

			Assert.Single(changeTracker.Changes);
			Assert.Single(changeTracker.Changes.Where(x => x.name == nameof(TestInterface.IntNullableProperty)));
			Assert.Equal(2, changeTracker.Changes.First(x => x.name == nameof(TestInterface.IntNullableProperty)).value);
		}
	}

	public interface TestInterface
	{
		int IntProperty { get; set; }
		int? IntNullableProperty { get; set; }
		string StringProperty { get; set; }
		DateTime DateTimeProperty { get; set; }
	}

	public class TestClass
	{
		public virtual int IntProperty { get; set; }
		public virtual int? IntNullableProperty { get; set; }
		public virtual string StringProperty { get; set; }
		public virtual DateTime DateTimeProperty { get; set; }
	}

	public class TestClassNoVirtual
	{
		public virtual int IntProperty { get; set; }
		public virtual int? IntNullableProperty { get; set; }
		public string StringProperty { get; set; }
		public virtual DateTime DateTimeProperty { get; set; }
	}

	public class TestClassNoParameterlessConstructor
	{
		public TestClassNoParameterlessConstructor(int value)
		{
			IntProperty = value;
		}

		public virtual int IntProperty { get; set; }
		public virtual int? IntNullableProperty { get; set; }
		public virtual string StringProperty { get; set; }
		public virtual DateTime DateTimeProperty { get; set; }
	}
}