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
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGame(string id, [FromBody] Game updatedGame)
        {
            updatedGame.Id = id; // Id’yi sağlamlaştır (gelen nesneye set et)

            var updated = await _gameService.UpdateGameAsync(id, updatedGame);
            if (!updated)
            {
                return NotFound($"Game with ID {id} not found.");
            }

            return NoContent(); // 204: başarıyla güncellendi, içerik dönmüyoruz
        }
        [HttpPatch("{id}/disable-feedback")]
        public async Task<IActionResult> DisableFeedback(string id)
        {
            var updated = await _gameService.DisableFeedbackAsync(id);
            if (!updated)
            {
                return NotFound($"Game with ID {id} not found.");
            }

            return NoContent();
        }
        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(string id, [FromBody] string comment)
        {
            var updated = await _gameService.AddCommentAsync(id, comment);
            if (!updated)
                return BadRequest("Game not found or feedback is disabled.");

            return Ok("Comment added.");
        }
        [HttpPost("{id}/ratings")]
        public async Task<IActionResult> AddRating(string id, [FromBody] int rating)
        {
            var success = await _gameService.AddRatingAsync(id, rating);
            if (!success)
                return BadRequest("Invalid rating or game not found / feedback disabled.");

            return Ok("Rating added.");
        }

    }
}
