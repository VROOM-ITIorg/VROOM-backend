using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    public class FeedbackRepository : BaseRepository<Feedback>
    {
        public FeedbackRepository(MyDbContext context) : base(context) { }
    }
}
