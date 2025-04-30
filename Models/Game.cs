using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using GameDeliveryPaaS.API.Models;
using System.Linq;


namespace GameDeliveryPaaS.API.Models
{
    public class Game
    {
        [BsonId]  // MongoDB için anahtar alan
        [BsonRepresentation(BsonType.ObjectId)] // string yerine ObjectId kullanılsın
        [BsonIgnoreIfDefault] // eğer boş bırakılırsa Mongo kendi üretir
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Genre { get; set; }
        public bool IsFeedbackEnabled { get; set; } = true;
        public List<string> Comments { get; set; } = new();
        public double AverageRating { get; set; } = 0;
        public int TotalPlayTime { get; set; } = 0; // saat cinsinden


    }
}
