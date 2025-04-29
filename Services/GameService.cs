using GameDeliveryPaaS.API.Models;
using GameDeliveryPaaS.API.Settings;
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
    }
}
