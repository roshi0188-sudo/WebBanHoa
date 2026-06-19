using System.ComponentModel.DataAnnotations;

namespace WebBanHoa.Models
{
    public class VisitorLog
    {
        [Key]
        public int Id { get; set; }
        public string? IpAddress { get; set; }
        public DateTime AccessTime { get; set; } = DateTime.Now;
        public string? UrlVisited { get; set; }
    }
}