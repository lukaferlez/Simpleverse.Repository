using Microsoft.AspNetCore.Mvc;
using Simpleverse.Repository.Db;
using Simpleverse.Repository.Db.Entity;

namespace webapi.Entities.MultiModel
{
	public class ParentModelController : Controller
	{
		private readonly IEntity<ParentModel, IParentModel, DbQueryOptions> _parentEntity;

		public ParentModelController(IEntity<ParentModel, IParentModel, DbQueryOptions> modelEntity)
		{
			_parentEntity = modelEntity;
		}

		public async Task<ActionResult<ParentModel>> GetAsync(int id)
		{
			var model = await _parentEntity.GetAsync(filter => filter.Id = id);
			if (model == null)
				return NotFound();

			return Ok(model);
		}

		public async Task<ActionResult<IEnumerable<ParentModel>>> GetAsync(string name)
		{
			var models = await _parentEntity.ListAsync(filter => filter.Name = name);
			return Ok(models);
		}

		public async Task<ActionResult<ParentModel>> PostAsync([FromBody] ParentModel model)
		{
			var count = await _parentEntity.AddAsync(model);

			return Ok(model);
		}

		public async Task<ActionResult<IEnumerable<ParentModel>>> PostAsync([FromBody] IEnumerable<ParentModel> models)
		{
			var count = await _parentEntity.AddAsync(models);

			return Ok(models);
		}

		public async Task<ActionResult> PutAsync(ParentModel model)
		{
			var count = await _parentEntity.UpdateAsync(model);

			return Ok();
		}

		public async Task<ActionResult> UpdateOnlySomeAsync(ParentModel model)
		{
			var count = await _parentEntity.UpdateAsync(
				update =>
				{
					update.Name = model.Name;
					update.Description = model.Description;
				},
				filter => filter.Id = model.Id
			);

			return Ok();
		}

		public async Task<ActionResult> DeleteAsync(int id)
		{
			var count = await _parentEntity.DeleteAsync(filter => filter.Id = id);
			return Ok();
		}
	}
}
