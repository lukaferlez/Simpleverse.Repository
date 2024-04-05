﻿using System;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IQueryGet<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : class
		where TOptions : QueryOptions, new()
	{
		Task<TModel> GetAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null);

		Task<T> GetAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null);
	}
}
