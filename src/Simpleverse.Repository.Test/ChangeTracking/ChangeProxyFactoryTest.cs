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
			Assert.Equal(2, changeTracker.Changed.Count());
			Assert.Single(changeTracker.Changed.Where(x => x == nameof(TestInterface.IntProperty)));
			Assert.Single(changeTracker.Changed.Where(x => x == nameof(TestInterface.IntNullableProperty)));
		}

		[Fact]
		public void CreateClassChangeTracker()
		{
			var proxy = ChangeProxyFactory.Create<TestClass>();

			proxy.IntProperty = 1;
			proxy.IntNullableProperty = null;

			var changeTracker = (IChangeTrack)proxy;
			Assert.Equal(2, changeTracker.Changed.Count());
			Assert.Single(changeTracker.Changed.Where(x => x == nameof(TestInterface.IntProperty)));
			Assert.Single(changeTracker.Changed.Where(x => x == nameof(TestInterface.IntNullableProperty)));
		}

		[Fact]
		public void FailCreateProxyNoParameterlessConstructor()
		{
			Assert.Throws<NotSupportedException>(() => ChangeProxyFactory.Create<TestClassNoParameterlessConstructor>());
		}

		[Fact]
		public void MultipleChange()
		{
			var proxy = ChangeProxyFactory.Create<TestInterface>();

			proxy.IntNullableProperty = null;
			proxy.IntNullableProperty = 2;

			var changeTracker = (IChangeTrack)proxy;

			Assert.Single(changeTracker.Changed);
			Assert.Single(changeTracker.Changed.Where(x => x == nameof(TestInterface.IntNullableProperty)));
		}

		[Fact]
		public void CompositInterfaceProxy()
		{
			var proxy = ChangeProxyFactory.Create<CompositInterface>();

			proxy.IntNullableProperty = null;
			proxy.IntNullableProperty = 2;
			proxy.Int2Property = 1;

			var changeTracker = (IChangeTrack)proxy;

			Assert.Equal(2, changeTracker.Changed.Count());
			Assert.Single(changeTracker.Changed.Where(x => x == nameof(CompositInterface.IntNullableProperty)));
			Assert.Single(changeTracker.Changed.Where(x => x == nameof(CompositInterface.Int2Property)));
		}

		[Fact]
		public void DuplicatePropertyInterfaceProxy()
		{
			var proxy = ChangeProxyFactory.Create<DuplicatePropertyInterface>();

			proxy.IntProperty = 2;

			var changeTracker = (IChangeTrack)proxy;

			Assert.Single(changeTracker.Changed);
			Assert.Single(changeTracker.Changed.Where(x => x == nameof(DuplicatePropertyInterface.IntProperty)));
		}

		[Fact]
		public void NameDuplicatePropertyInterfaceProxy()
		{
			Assert.Throws<NotSupportedException>(() => ChangeProxyFactory.Create<NameDuplicatePropertyInterface>());
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

	public interface CompositInterface : TestInterface
	{
		int Int2Property { get; set; }
	}

	public interface DuplicatePropertyInterface : TestInterface
	{
		int IntProperty { get; set; }
	}

	public interface NameDuplicatePropertyInterface : TestInterface
	{
		string IntProperty { get; set; }
	}
}