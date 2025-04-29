using Microsoft.AspNetCore.Mvc;
using GameDeliveryPaaS.API.Services;
using GameDeliveryPaaS.API.Models;

namespace GameDeliveryPaaS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly GameService _gameService;

        public GamesController(GameService gameService)
        {
            _gameService = gameService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGame([FromBody] Game newGame)
        {
            if (newGame == null)
            {
                return BadRequest("Game data is null");
            }

            await _gameService.AddGameAsync(newGame);
            return CreatedAtAction(nameof(CreateGame), new { id = newGame.Id }, newGame);
        }
    }
}
