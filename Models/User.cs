using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GameDeliveryPaaS.API.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;

        public List<string> PlayedGameIds { get; set; } = new();
        public List<UserGamePlay> PlayedGames { get; set; } = new();
        public List<UserRating> RatedGames { get; set; } = new();
    }
}
