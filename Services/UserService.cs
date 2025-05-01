using GameDeliveryPaaS.API.Models;
using MongoDB.Driver;

namespace GameDeliveryPaaS.API.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Game> _games;

        public UserService(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("Users");
            _games = database.GetCollection<Game>("Games");
        }

        public async Task<User> UpdateAsync(User userToUpdate)
        {
            if (userToUpdate == null)
                throw new ArgumentNullException(nameof(userToUpdate));

            if (string.IsNullOrEmpty(userToUpdate.Id))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userToUpdate.Id));

            var filter = Builders<User>.Filter.Eq(u => u.Id, userToUpdate.Id);

            // ReplaceOneAsync tüm belgeyi değiştirir
            var result = await _users.ReplaceOneAsync(filter, userToUpdate);

            if (result.IsAcknowledged && result.ModifiedCount > 0)
            {
                return userToUpdate;
            }

            // Eğer kayıt bulunamadıysa veya bir hata olduysa null döndür
            return null;
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

        public async Task<bool> DeleteAsync(string id, IMongoCollection<Game> gameCollection)
        {
            var user = await GetByIdAsync(id);
            if (user == null) return false;

            // Kullanıcının etkilediği tüm oyunları bul
            var games = await gameCollection.Find(_ => true).ToListAsync();

            foreach (var game in games)
            {
                bool modified = false;

                // Yorumları temizle
                var pullComments = Builders<Game>.Update.PullFilter(
                    g => g.Comments,
                    Builders<UserComment>.Filter.Eq(c => c.UserId, id)
                );

                // Puanları temizle
                var pullRatings = Builders<Game>.Update.PullFilter(
                    g => g.Ratings,
                    Builders<UserRating>.Filter.Eq(r => r.UserId, id)
                );

                // Oynama verisini temizle
                var pullPlayData = Builders<Game>.Update.PullFilter(
                    g => g.PlayedUsers,
                    Builders<UserGamePlay>.Filter.Eq(p => p.UserId, id)
                );

                await gameCollection.UpdateOneAsync(g => g.Id == game.Id, pullComments);
                await gameCollection.UpdateOneAsync(g => g.Id == game.Id, pullRatings);
                await gameCollection.UpdateOneAsync(g => g.Id == game.Id, pullPlayData);

                // Ortalama puanı ve toplam süresini yeniden hesapla (isteğe bağlı)
                var updatedGame = await gameCollection.Find(g => g.Id == game.Id).FirstOrDefaultAsync();
                if (updatedGame != null)
                {
                    double newAvg = updatedGame.Ratings.Count > 0
                        ? updatedGame.Ratings.Average(r => r.Score)
                        : 0;

                    int newTotalTime = updatedGame.PlayedUsers.Sum(p => p.Minutes);

                    var updateFields = Builders<Game>.Update
                        .Set(g => g.AverageRating, newAvg)
                        .Set(g => g.TotalPlayTime, newTotalTime);

                    await gameCollection.UpdateOneAsync(g => g.Id == game.Id, updateFields);
                }
            }

            // Son olarak kullanıcıyı sil
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
            // Kullanıcı ve oyun belgelerini bul
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) return false;

            var game = await _games.Find(g => g.Id == gameId).FirstOrDefaultAsync();
            if (game == null) return false;

            // Kullanıcının bu oyunu oynayıp oynamadığını kontrol et (en az 1 saat)
            var playedGame = game.PlayedUsers?.FirstOrDefault(p => p.UserId == userId);
            if (playedGame == null || playedGame.PlayTimeHours < 1)
                return false;

            // Kullanıcı belgesindeki derecelendirmeyi kontrol et ve güncelle
            var existingUserRating = user.RatedGames?.FirstOrDefault(r => r.GameId == gameId);
            if (existingUserRating != null)
            {
                existingUserRating.Rating = rating;
            }
            else
            {
                if (user.RatedGames == null)
                    user.RatedGames = new List<UserRating>();

                user.RatedGames.Add(new UserRating { GameId = gameId, Rating = rating });
            }

            // Oyun belgesindeki derecelendirmeyi kontrol et ve güncelle
            var existingGameRating = game.Ratings?.FirstOrDefault(r => r.UserId == userId);
            if (existingGameRating != null)
            {
                existingGameRating.Rating = rating;
            }
            else
            {
                if (game.Ratings == null)
                    game.Ratings = new List<UserRating>();

                game.Ratings.Add(new UserRating { UserId = userId, Rating = rating });
            }

            // Kullanıcı belgesini güncelle
            var userUpdate = Builders<User>.Update
                .Set(u => u.RatedGames, user.RatedGames);
            var userResult = await _users.UpdateOneAsync(u => u.Id == userId, userUpdate);

            // Oyun belgesini güncelle
            var gameUpdate = Builders<Game>.Update
                .Set(g => g.Ratings, game.Ratings);
            var gameResult = await _games.UpdateOneAsync(g => g.Id == gameId, gameUpdate);

            return true;
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
        public async Task<UserSummaryDto?> GetUserSummaryAsync(string userId, IMongoCollection<Game> gameCollection)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return null;

            var userRatings = new List<(int score, int playTime)>();
            int totalPlayTime = 0;
            string? mostPlayedGame = null;
            int mostPlayMinutes = 0;
            var commentList = new List<CommentDto>();

            foreach (var gameId in user.PlayedGameIds)
            {
                var game = await gameCollection.Find(g => g.Id == gameId).FirstOrDefaultAsync();
                if (game == null) continue;

                var rating = game.Ratings?.FirstOrDefault(r => r.UserId == userId);
                var play = game.PlayedUsers?.FirstOrDefault(p => p.UserId == userId);
                var comment = game.Comments?.FirstOrDefault(c => c.UserId == userId);

                if (play != null)
                {
                    totalPlayTime += play.Minutes;
                    if (play.Minutes > mostPlayMinutes)
                    {
                        mostPlayMinutes = play.Minutes;
                        mostPlayedGame = game.Name;
                    }
                }

                if (rating != null && play != null)
                {
                    userRatings.Add((rating.Score, play.Minutes));
                }

                if (comment != null)
                {
                    commentList.Add(new CommentDto
                    {
                        GameName = game.Name,
                        Content = comment.Content
                    });
                }
            }

            double avgRating = userRatings.Count > 0
                ? userRatings.Sum(r => r.score) / (double)userRatings.Count
                : 0;

            return new UserSummaryDto
            {
                Username = user.Username,
                AverageRating = Math.Round(avgRating, 2),
                TotalPlayTime = totalPlayTime,
                MostPlayedGame = mostPlayedGame,
                Comments = commentList
            };
        }

    }
}
