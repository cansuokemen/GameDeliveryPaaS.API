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
        public async Task<bool> AddCommentAsync(string id, string comment)
        {
            var update = Builders<Game>.Update.Push(g => g.Comments, comment);
            var result = await _games.UpdateOneAsync(
                game => game.Id == id && game.IsFeedbackEnabled,
                update
            );
            return result.ModifiedCount > 0;
        }
        public async Task<bool> AddRatingAsync(string id, string userId, int score)
        {
            if (score < 1 || score > 5)
                return false;

            var game = await _games.Find(g => g.Id == id && g.IsFeedbackEnabled).FirstOrDefaultAsync();
            if (game == null)
                return false;

            // Kullanıcının daha önce puan verip vermediğini kontrol et
            var existingRating = game.Ratings.FirstOrDefault(r => r.UserId == userId);
            if (existingRating != null)
                return false; // aynı kullanıcı birden fazla puan veremesin (isteğe bağlı)

            // Yeni oyu ekle
            game.Ratings.Add(new UserRating { UserId = userId, Score = score });

            // Ortalama hesapla
            game.AverageRating = game.Ratings.Average(r => r.Score);

            var result = await _games.ReplaceOneAsync(g => g.Id == id, game);
            return result.ModifiedCount > 0;
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

    }
}
