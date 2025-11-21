using System.ComponentModel.DataAnnotations;

namespace UpsaMe_API.DTOs.Posts
{
    public class UpdatePostDto
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Content { get; set; }
    }
}