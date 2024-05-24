using Microsoft.AspNetCore.Mvc;
using Simpleverse.Repository.Db.Entity;

namespace webapi.Entities.Basic
{
	public class BasicModelController : Controller
	{
		private readonly IEntity<BasicModel> _basicEntity;

		public BasicModelController(IEntity<BasicModel> modelEntity)
		{
			_basicEntity = modelEntity;
		}

		public async Task<ActionResult<BasicModel>> GetAsync(int id)
		{
			var model = await _basicEntity.GetAsync(filter => filter.Id = id);
			if (model == null)
				return NotFound();

			return Ok(model);
		}

		public async Task<ActionResult<IEnumerable<BasicModel>>> GetAsync(string name)
		{
			var models = await _basicEntity.ListAsync(filter => filter.Name = name);
			return Ok(models);
		}

		public async Task<ActionResult<BasicModel>> PostAsync([FromBody] BasicModel model)
		{
			var count = await _basicEntity.AddAsync(model);

			return Ok(model);
		}

		public async Task<ActionResult<IEnumerable<BasicModel>>> PostAsync([FromBody] IEnumerable<BasicModel> models)
		{
			var count = await _basicEntity.AddAsync(models);

			return Ok(models);
		}

		public async Task<ActionResult> PutAsync(BasicModel model)
		{
			var count = await _basicEntity.UpdateAsync(model);

			return Ok();
		}

		public async Task<ActionResult> UpdateOnlySomeAsync(BasicModel model)
		{
			var count = await _basicEntity.UpdateAsync(
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
			var count = await _basicEntity.DeleteAsync(filter => filter.Id = id);
			return Ok();
		}
	}
}
