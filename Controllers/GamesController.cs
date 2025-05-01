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
        private readonly UserService _userService;

        public GamesController(GameService gameService, UserService userService)
        {
            _gameService = gameService;
            _userService = userService;
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
        public async Task<IActionResult> AddComment(string id, [FromQuery] string userId, [FromBody] string comment)
        {
            var user = await _userService.GetByIdAsync(userId);

            if (user is null)
            {
                return NotFound("User could not find.");
            }

            if(user.CanComment is false)
            {
                return BadRequest("User does not have comment permission");
            }

            var updated = await _gameService.AddCommentAsync(id, userId, comment);
            if (!updated)
                return BadRequest("Game not found or feedback is disabled.");

            return Ok("Comment added.");
        }

        [HttpDelete("{id}/ratings/{userId}")]
        public async Task<IActionResult> RemoveRating(string id, string userId)
        {
            var removed = await _gameService.RemoveRatingAsync(id, userId);
            if (!removed)
                return NotFound($"Rating by user '{userId}' not found for game {id}.");

            return NoContent(); // 204
        }
        [HttpDelete("{id}/comments")]
        public async Task<IActionResult> RemoveComment(string id, [FromQuery] string userId)
        {
            var removed = await _gameService.RemoveCommentAsync(id, userId);
            if (!removed)
                return NotFound("Comment not found.");

            return NoContent(); // 204
        }
        [HttpGet("full")]
        public async Task<IActionResult> GetFullGames()
        {
            var games = await _gameService.GetFullGamesAsync();
            return Ok(games);
        }
        [HttpPost("{id}/rate")]
        public async Task<IActionResult> RateGame(string id, [FromQuery] string userId, [FromQuery] int score)
        {
            var success = await _gameService.RateGameAsync(id, userId, score);
            if (!success)
                return BadRequest("Rating failed. Make sure the user has played at least 60 minutes and score is 1–5.");

            return Ok("Rating submitted successfully.");
        }
    }
}
