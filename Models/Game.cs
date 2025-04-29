using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GameDeliveryPaaS.API.Models
{
    public class Game
    {
        [BsonId]  // MongoDB için anahtar alan
        [BsonRepresentation(BsonType.ObjectId)] // string yerine ObjectId kullanılsın
        [BsonIgnoreIfDefault] // eğer boş bırakılırsa Mongo kendi üretir
        public string? Id { get; set; }

        public string Name { get; set; }

        public string Genre { get; set; }
        public bool IsFeedbackEnabled { get; set; } = true;
        public List<string> Comments { get; set; } = new();

    }
}
