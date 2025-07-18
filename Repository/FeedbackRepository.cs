using VROOM.Data;
using VROOM.Models;
using Microsoft.EntityFrameworkCore;
using ViewModels.Feedback;

namespace VROOM.Repositories
{
    public class FeedbackRepository : BaseRepository<Feedback>
    {
        private readonly VroomDbContext _context;

        public FeedbackRepository(VroomDbContext context) : base (context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Feedback> GetByCustomerAndRiderAsync(string userId, string riderId)
        {
            return await _context.Feedbacks
                .FirstOrDefaultAsync(f => f.UserId == userId && f.RiderID == riderId && !f.IsDeleted);
        }

        public async Task<bool> AddFeedbackAsync(string customerId, FeedbackRequest feedbackRequest)
        {
            var feedback = new Feedback
            {
                RiderID = feedbackRequest.RiderId,
                UserId = customerId,
                Rating = feedbackRequest.Rating,
                Message = feedbackRequest.Message,
                ModifiedBy = customerId,
                ModifiedAt = DateTime.Now,
                IsDeleted = false
            };

            await _context.Feedbacks.AddAsync(feedback);

            // Update rider's average rating
            var rider = await _context.Riders.FindAsync(feedbackRequest.RiderId);
            if (rider != null)
            {
                var feedbackRatings = await _context.Feedbacks
                    .Where(f => f.RiderID == feedbackRequest.RiderId && !f.IsDeleted)
                    .Select(f => (float)f.Rating) // Convert int to float for Rider.Rating
                    .ToListAsync();
                rider.Rating = feedbackRatings.Any() ? feedbackRatings.Average() : (float)feedbackRequest.Rating;
                _context.Riders.Update(rider); // Update only Rating, Lastupdated remains unchanged
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}