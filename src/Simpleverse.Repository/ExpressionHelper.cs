using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Simpleverse.Repository
{
	public static class ExpressionHelper
	{
		public static MemberInfo GetMember<T, R>(Expression<Func<T, R>> expression)
		{
			var lambda = (LambdaExpression)expression;

			MemberExpression memberExpression;
			if (lambda.Body is UnaryExpression unaryExpression)
				memberExpression = (MemberExpression)unaryExpression.Operand;
			else
				memberExpression = (MemberExpression)lambda.Body;

			return memberExpression.Member;
		}

		public static string GetMemberName<T, R>(Expression<Func<T, R>> expression)
			=> GetMember(expression).Name;
	}
}
