namespace GameDeliveryPaaS.API.Models
{
    public class GameFullDto
    {
        public string? Name { get; set; }
        public string? Genre { get; set; }
        public double AverageRating { get; set; }
        public int TotalPlayTime { get; set; }
        public List<UserRating>? Ratings { get; set; }
        public List<UserComment>? Comments { get; set; }
        public List<UserGamePlay>? PlayedUsers { get; set; }
    }
}
