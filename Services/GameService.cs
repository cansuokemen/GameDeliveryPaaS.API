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

    }
}
