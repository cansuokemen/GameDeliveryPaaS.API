namespace GameDeliveryPaaS.API.Models
{
    public class UserRating
    {
        public string GameId { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int Score { get; set; }
    }
}
