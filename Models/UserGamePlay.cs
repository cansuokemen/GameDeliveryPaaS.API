using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GameDeliveryPaaS.API.Models
{
    [BsonIgnoreExtraElements]
    public class UserGamePlay
    {
        [BsonElement("GameId")]
        [BsonRepresentation(BsonType.ObjectId)] // <-- bu önemli
        public string? GameId { get; set; }

        [BsonElement("PlayTimeHours")]
        public int PlayTimeHours { get; set; }

        [BsonElement("UserId")]
        [BsonRepresentation(BsonType.ObjectId)] // <-- bu önemli
        public string? UserId { get; set; }

        [BsonElement("Minutes")]
        public int Minutes { get; set; }
    }
}
