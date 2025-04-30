using GameDeliveryPaaS.API.Models;
using GameDeliveryPaaS.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameDeliveryPaaS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly GameService _gameService;

        public UsersController(UserService userService, GameService gameService)
        {
            _userService = userService;
            _gameService = gameService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found.");

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            await _userService.AddAsync(user);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var deleted = await _userService.DeleteAsync(id);
            if (!deleted)
                return NotFound($"User with ID {id} not found.");

            return NoContent();
        }
        [HttpPost("{userId}/play/{gameId}")]
        public async Task<IActionResult> PlayGame(string userId, string gameId, [FromQuery] int hours)
        {
            var userUpdated = await _userService.AddPlayTimeAsync(userId, gameId, hours);
            if (!userUpdated) return NotFound("User not found.");

            var gameUpdated = await _gameService.AddGamePlayTimeAsync(gameId, hours);
            if (!gameUpdated) return NotFound("Game not found.");

            return Ok("Play time added successfully.");
        }
        [HttpPost("{userId}/rate/{gameId}")]
        public async Task<IActionResult> RateGame(string userId, string gameId, [FromQuery] int rating)
        {
            if (rating < 1 || rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            var updated = await _userService.AddOrUpdateRatingAsync(userId, gameId, rating);
            if (!updated)
                return NotFound("User not found, or user hasn't played the game for at least 1 hour.");

            // GameService içinde averageRating güncelle
            var updatedAvg = await _gameService.UpdateAverageRatingAsync(gameId, _userService.GetUserCollection());
            if (!updatedAvg)
                return NotFound("Game not found.");

            return Ok("Rating submitted and average updated.");
        }
    }
}
