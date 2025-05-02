using GameDeliveryPaaS.API.Models;
using GameDeliveryPaaS.API.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

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

        [HttpPut("UpdateUserCommentPermission")]
        public async Task<IActionResult> UpdateUserCommentPermission(string userId, bool canComment)
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User could not find");
            }

            var updatedUser = user.CanComment = canComment;

            var result = await _userService.UpdateAsync(user);

            if (result == null)
            {
                return BadRequest("Permission could not be updated.");
            }

            return Ok(result);
        }
        [HttpPut("UpdateUserRatePermission")]
        public async Task<IActionResult> UpdateUserRatePermission(string userId, bool canRate)
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User could not find");
            }

            var updatedUser = user.CanRate = canRate;

            var result = await _userService.UpdateAsync(user);

            if (result == null)
            {
                return BadRequest("Permission could not be updated.");
            }

            return Ok(result);
        }

        [HttpGet("by-id/{id}")]
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
        public async Task<IActionResult> Delete(string id, [FromServices] IMongoDatabase database)
        {
            var gameCollection = database.GetCollection<Game>("Games");

            var deleted = await _userService.DeleteAsync(id, gameCollection);
            if (!deleted)
                return NotFound("User not found or could not be deleted.");

            return NoContent(); // 204
        }

        [HttpPost("{userId}/play/{gameId}")]
        public async Task<IActionResult> PlayGame(string userId, string gameId, [FromQuery] int minutes, [FromServices] IMongoDatabase database)
        {
            var userCollection = database.GetCollection<User>("Users");
            var gameCollection = database.GetCollection<Game>("Games");

            var user = await userCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) return NotFound("User not found.");

            var game = await gameCollection.Find(g => g.Id == gameId).FirstOrDefaultAsync();
            if (game == null) return NotFound("Game not found.");

            // Kullanıcının PlayedGameIds listesine ekle (varsa atla)
            if (!user.PlayedGameIds.Contains(gameId))
                user.PlayedGameIds.Add(gameId);

            // Kullanıcının PlayedGames listesinde bu oyun varsa güncelle
            var existingUserPlay = user.PlayedGames.FirstOrDefault(p => p.GameId == gameId);
            if (existingUserPlay != null)
            {
                existingUserPlay.Minutes += minutes;
                existingUserPlay.PlayTimeHours = existingUserPlay.Minutes / 60;
            }
            else
            {
                user.PlayedGames.Add(new UserGamePlay
                {
                    GameId = gameId,
                    UserId = userId,
                    Minutes = minutes,
                    PlayTimeHours = minutes / 60
                });
            }

            await _userService.AddPlayTimeMinutesAsync(userId, gameId, minutes);


            // Game tarafındaki PlayedUsers güncellemesi
            var existingGamePlay = game.PlayedUsers?.FirstOrDefault(p => p.UserId == userId);
            if (existingGamePlay != null)
            {
                var update = Builders<Game>.Update
                    .Inc("PlayedUsers.$[elem].Minutes", minutes)
                    .Set("PlayedUsers.$[elem].PlayTimeHours", (existingGamePlay.Minutes + minutes) / 60);

                var result = await gameCollection.UpdateOneAsync(
                    g => g.Id == gameId,
                    update,
                    new UpdateOptions
                    {
                        ArrayFilters = new List<ArrayFilterDefinition>
                        {
                    new BsonDocumentArrayFilterDefinition<BsonDocument>(
                        new BsonDocument("elem.UserId", new ObjectId(userId)))
                        }
                    });

                return Ok("Play time updated.");
            }

            // Eğer Game.PlayedUsers içinde kayıt yoksa yeni bir tane ekle
            var newGamePlay = new UserGamePlay
            {
                UserId = userId,
                GameId = gameId,
                Minutes = minutes,
                PlayTimeHours = minutes / 60
            };

            await gameCollection.UpdateOneAsync(
                g => g.Id == gameId,
                Builders<Game>.Update.Push(g => g.PlayedUsers, newGamePlay)
            );

            return Ok("Play time inserted.");
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
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User newUser)
        {
            if (string.IsNullOrEmpty(newUser.Username))
                return BadRequest("Username is required.");

            var exists = await _userService.UsernameExistsAsync(newUser.Username);
            if (exists)
                return Conflict("Username already exists.");

            await _userService.AddAsync(newUser);
            return CreatedAtAction(nameof(GetById), new { id = newUser.Id }, newUser);
        }

        [HttpGet("login")]
        public async Task<IActionResult> Login([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest("Username is required.");

            var user = await _userService.GetByUsernameAsync(username);
            if (user == null)
                return NotFound("User not found.");

            return Ok(user); // Kullanıcıyı başarılı şekilde döndür
        }
        [HttpGet("{id}/summary")]
        public async Task<IActionResult> GetUserSummary(string id, [FromServices] IMongoDatabase database)
        {
            var gameCollection = database.GetCollection<Game>("Games");
            var summary = await _userService.GetUserSummaryAsync(id, gameCollection);

            if (summary == null)
                return NotFound("User or games not found.");

            return Ok(summary);
        }
        [HttpGet("test-insert")]
        public async Task<IActionResult> TestInsert([FromServices] IMongoDatabase database)
        {
            var games = database.GetCollection<Game>("Games");

            var testGame = new Game
            {
                Name = "TEST GAME",
                Genre = "TEST",
                PlayedUsers = new List<UserGamePlay>
        {
            new UserGamePlay
            {
                UserId = "u123",
                GameId = "g123",
                Minutes = 90,
                PlayTimeHours = 1
            }
        }
            };

            await games.InsertOneAsync(testGame);

            return Ok("Inserted test game.");
        }
       

    }
}
