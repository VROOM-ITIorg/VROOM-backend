// ViewModels/Feedback/FeedbackRequest.cs
using System.ComponentModel.DataAnnotations;

namespace ViewModels.Feedback
{
    public class FeedbackRequest
    {
        [Required]
        public string RiderId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [MaxLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
        public string Message { get; set; }

        public string? sendCustomerId { get; set; }
    }
}