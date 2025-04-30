namespace GameDeliveryPaaS.API.Models
{
    public class UserGamePlay
    {
        public string? GameId { get; set; }
        public int PlayTimeHours { get; set; }
        public string? UserId { get; set; }
        public int Minutes { get; set; }
    }
}
