using GameDeliveryPaaS.API.Models;
using MongoDB.Driver;

namespace GameDeliveryPaaS.API.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("Users");
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _users.Find(_ => true).ToListAsync();
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task AddAsync(User user)
        {
            await _users.InsertOneAsync(user);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _users.DeleteOneAsync(u => u.Id == id);
            return result.DeletedCount > 0;
        }
        public async Task<bool> AddPlayTimeAsync(string userId, string gameId, int hours)
        {
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) return false;

            // Oyun daha önce oynanmamışsa ekle, oynanmışsa süresini artır
            var existingPlay = user.PlayedGames.FirstOrDefault(p => p.GameId == gameId);
            if (existingPlay == null)
            {
                user.PlayedGames.Add(new UserGamePlay
                {
                    GameId = gameId,
                    PlayTimeHours = hours
                });
            }
            else
            {
                existingPlay.PlayTimeHours += hours;
            }

            // Güncellenmiş kullanıcıyı veritabanına kaydet
            var result = await _users.ReplaceOneAsync(u => u.Id == userId, user);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> AddOrUpdateRatingAsync(string userId, string gameId, int rating)
        {
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) return false;

            // Kullanıcının bu oyunu oynayıp oynamadığını kontrol et (en az 1 saat)
            var playedGame = user.PlayedGames.FirstOrDefault(p => p.GameId == gameId);
            if (playedGame == null || playedGame.PlayTimeHours < 1)
                return false;

            // Daha önce puan verilmişse güncelle, yoksa ekle
            var existingRating = user.RatedGames.FirstOrDefault(r => r.GameId == gameId);
            if (existingRating != null)
            {
                existingRating.Rating = rating;
            }
            else
            {
                user.RatedGames.Add(new UserRating { GameId = gameId, Rating = rating });
            }

            var update = Builders<User>.Update
                .Set(u => u.RatedGames, user.RatedGames);

            var result = await _users.UpdateOneAsync(u => u.Id == userId, update);
            return result.ModifiedCount > 0;
        }
        public IMongoCollection<User> GetUserCollection()
        {
            return _users;
        }
        public async Task<bool> UsernameExistsAsync(string username)
        {
            var user = await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
            return user != null;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        }
    }
}
