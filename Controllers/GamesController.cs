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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGame(string id)
        {
            var deleted = await _gameService.DeleteGameAsync(id);
            if (!deleted)
            {
                return NotFound($"Game with ID {id} not found.");
            }

            return NoContent();
        }
        [HttpGet]
        public async Task<IActionResult> GetAllGames()
        {
            var games = await _gameService.GetAllGamesAsync();
            return Ok(games);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGameById(string id)
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound($"Game with ID {id} not found.");
            }

            return Ok(game);
        }

    }
}
