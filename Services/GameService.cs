using GameDeliveryPaaS.API.Models;
using GameDeliveryPaaS.API.Settings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GameDeliveryPaaS.API.Services
{
    public class GameService
    {
        private readonly IMongoCollection<Game> _games;

        public GameService(IMongoDatabase database)
        {
            _games = database.GetCollection<Game>("Games");
        }

        public async Task AddGameAsync(Game game)
        {
            await _games.InsertOneAsync(game);
        }

        public async Task<List<Game>> GetAllGamesAsync()
        {
            return await _games.Find(_ => true).ToListAsync();
        }
        public async Task<bool> DeleteGameAsync(string id)
        {
            var objectId = ObjectId.Parse(id);
            var result = await _games.DeleteOneAsync(game => game.Id == id);
            return result.DeletedCount > 0;
        }
        public async Task<Game?> GetGameByIdAsync(string id)
        {
            return await _games.Find(game => game.Id == id).FirstOrDefaultAsync();
        }
        public async Task<bool> UpdateGameAsync(string id, Game updatedGame)
        {
            var result = await _games.ReplaceOneAsync(game => game.Id == id, updatedGame);
            return result.ModifiedCount > 0;
        }
        public async Task<bool> DisableFeedbackAsync(string id)
        {
            var update = Builders<Game>.Update.Set(g => g.IsFeedbackEnabled, false);
            var result = await _games.UpdateOneAsync(game => game.Id == id, update);
            return result.ModifiedCount > 0;
        }
        public async Task<bool> AddCommentAsync(string gameId, string userId, string content)
        {
            // Kullanıcının oyunu en az 60 dakika oynayıp oynamadığını kontrol et
            var game = await _games.Find(g => g.Id == gameId).FirstOrDefaultAsync();
            if (game == null || !game.IsFeedbackEnabled) return false;

            var play = game.PlayedUsers?.FirstOrDefault(p => p.UserId == userId);
            if (play == null || play.Minutes < 60) return false;

            var newComment = new UserComment
            {
                UserId = userId,
                Content = content
            };

            var update = Builders<Game>.Update.Push(g => g.Comments, newComment);
            var result = await _games.UpdateOneAsync(g => g.Id == gameId, update);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> AddGamePlayTimeAsync(string gameId, int hours)
        {
            var update = Builders<Game>.Update.Inc(g => g.TotalPlayTime, hours);
            var result = await _games.UpdateOneAsync(g => g.Id == gameId, update);
            return result.ModifiedCount > 0;
        }
        public async Task<bool> UpdateAverageRatingAsync(string gameId, IMongoCollection<User> userCollection)
        {
            var game = await _games.Find(g => g.Id == gameId).FirstOrDefaultAsync();
            if (game == null) return false;

            var ratingsCount = game.Ratings.Count;
            var totalRating = game.Ratings.Sum(r => r.Rating);

            double average = totalRating / ratingsCount;

            var update = Builders<Game>.Update.Set(g => g.AverageRating, average);
            var result = await _games.UpdateOneAsync(g => g.Id == gameId, update);

            return true;
        }
        public async Task<bool> RemoveRatingAsync(string gameId, string userId)
        {
            var game = await _games.Find(g => g.Id == gameId).FirstOrDefaultAsync();
            if (game == null)
                return false;

            var ratingToRemove = game.Ratings.FirstOrDefault(r => r.UserId == userId);
            if (ratingToRemove == null)
                return false;

            game.Ratings.Remove(ratingToRemove);

            game.AverageRating = game.Ratings.Count > 0
                ? game.Ratings.Average(r => r.Score)
                : 0;

            var result = await _games.ReplaceOneAsync(g => g.Id == gameId, game);
            return result.ModifiedCount > 0;
        }
        public async Task<bool> RemoveCommentAsync(string gameId, string userId)
        {
            var filter = Builders<UserComment>.Filter.Eq(c => c.UserId, userId);
            var update = Builders<Game>.Update.PullFilter(g => g.Comments, filter);

            var result = await _games.UpdateOneAsync(g => g.Id == gameId, update);
            return result.ModifiedCount > 0;
        }
        public async Task<List<GameFullDto>> GetFullGamesAsync()
        {
            var games = await _games.Find(_ => true).ToListAsync();

            return games.Select(game => new GameFullDto
            {
                Name = game.Name,
                Genre = game.Genre,
                AverageRating = game.AverageRating,
                TotalPlayTime = game.TotalPlayTime,
                Ratings = game.Ratings,
                Comments = game.Comments,
                PlayedUsers = game.PlayedUsers,
                Description = game.Description
            }).ToList();
        }
        public async Task<bool> RateGameAsync(string gameId, string userId, int score)
        {
            if (score < 1 || score > 5) return false;

            var game = await _games.Find(g => g.Id == gameId).FirstOrDefaultAsync();
            if (game == null || !game.IsFeedbackEnabled) return false;

            var play = game.PlayedUsers?.FirstOrDefault(p => p.UserId == userId);
            if (play == null || play.Minutes < 60) return false;

            var filter = Builders<Game>.Filter.Eq(g => g.Id, gameId);

            // Eğer daha önce oy verdiyse: güncelle
            var existing = game.Ratings?.FirstOrDefault(r => r.UserId == userId);
            if (existing != null)
            {
                var update = Builders<Game>.Update.Set("Ratings.$[elem].Score", score);
                var arrayFilter = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("elem.UserId", userId))
        };
                var options = new UpdateOptions { ArrayFilters = arrayFilter };

                var result = await _games.UpdateOneAsync(filter, update, options);
                return result.ModifiedCount > 0;
            }

            // İlk defa oy veriyorsa: ekle
            var newRating = new UserRating { UserId = userId, Score = score };
            var push = Builders<Game>.Update.Push(g => g.Ratings, newRating);
            var res = await _games.UpdateOneAsync(filter, push);
            return res.ModifiedCount > 0;
        }
        public async Task<GameFullDto?> GetFullGameDtoAsync(string id)
        {
            var game = await _games.Find(g => g.Id == id).FirstOrDefaultAsync();
            if (game == null)
                return null;

            // DTO'ya mapleme
            return new GameFullDto
            {
                Name = game.Name,
                Genre = game.Genre,
                AverageRating = game.AverageRating,
                TotalPlayTime = game.TotalPlayTime,
                Img = game.Img, // ✅ Burası önemli
                Ratings = game.Ratings,
                Comments = game.Comments,
                PlayedUsers = game.PlayedUsers,
                Description = game.Description
            };
        }
        public async Task<Game?> GetByIdAsync(string id)
        {
            return await _games.Find(g => g.Id == id).FirstOrDefaultAsync();
        }
        public async Task UpdateAsync(string id, Game updatedGame)
        {
            await _games.ReplaceOneAsync(g => g.Id == id, updatedGame);
        }
    }
}
