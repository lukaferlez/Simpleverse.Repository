using Microsoft.AspNetCore.Mvc;
using Simpleverse.Repository.Db.Entity;

namespace webapi.Entities.Basic.Projection
{
	public class BasicController : Controller
	{
		private readonly IProjectedEntity<Basic, BasicModel> _basicEntity;

		public BasicController(IProjectedEntity<Basic, BasicModel> modelEntity)
		{
			_basicEntity = modelEntity;
		}

		[HttpGet()]
		public async Task<ActionResult<IEnumerable<Basic>>> GetAsync()
		{
			var models = await _basicEntity.ListAsync();
			return Ok(models);
		}

		[HttpGet("{name}")]
		public async Task<ActionResult<Basic>> GetAsync(string name)
		{
			var model = await _basicEntity.GetAsync(filter => filter.Name = name);
			if (model == null)
				return NotFound();

			return Ok(model);
		}

		[HttpPost]
		public async Task<ActionResult<Basic>> PostAsync([FromBody] Basic model)
		{
			var count = await _basicEntity.AddAsync(model);

			return Ok(model);
		}

		[HttpPost]
		public async Task<ActionResult<IEnumerable<Basic>>> PostAsync([FromBody] IEnumerable<Basic> models)
		{
			var count = await _basicEntity.AddAsync(models);

			return Ok(models);
		}

		[HttpPut("{id}")]
		public async Task<ActionResult> PutAsync(int id, [FromBody] Basic model)
		{
			model.Id = id;

			var count = await _basicEntity.UpdateAsync(model);

			return Ok();
		}

		[HttpPut("{id}")]
		public async Task<ActionResult> UpdateOnlySomeAsync(int id, [FromBody] Basic model)
		{
			var count = await _basicEntity.UpdateAsync(
				update =>
				{
					update.Name = model.Name;
					update.Description = model.Model.Description;
				},
				filter => filter.Id = id
			);

			return Ok();
		}

		[HttpDelete("{id}")]
		public async Task<ActionResult> DeleteAsync(int id)
		{
			var count = await _basicEntity.DeleteAsync(filter => filter.Id = id);
			return Ok();
		}
	}
}
