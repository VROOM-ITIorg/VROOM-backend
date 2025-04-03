using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Data;
using VROOM.Models;

namespace VROOM.Repositories
{
    public class FeedbackRepository : BaseRepository<Feedback>
    {
        public FeedbackRepository(MyDbContext context) : base(context) { }
    }
}
