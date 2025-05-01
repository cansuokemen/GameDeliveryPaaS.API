using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GameDeliveryPaaS.API.Models
{
    public class Game
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Genre { get; set; }
        public bool IsFeedbackEnabled { get; set; } = true;

        public List<UserComment> Comments { get; set; } = new();
        public double AverageRating { get; set; } = 0;
        public int TotalPlayTime { get; set; } = 0;

        public List<UserRating> Ratings { get; set; } = new();

        [BsonElement("PlayedUsers")]
        public List<UserGamePlay> PlayedUsers { get; set; } = new();
    }
}
