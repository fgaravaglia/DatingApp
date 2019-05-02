namespace DatingApp.API.Models
{
    public class Like
    {
        public int LikerId { get; set; }
        public int LikeeId { get; set; }
        // person that likee is favorite
        public User Liker { get; set; }
        public User Likee { get; set; }
    }
}