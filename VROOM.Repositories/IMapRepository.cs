using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Models.Map;

namespace VROOM.Repositories
{
    public interface IMapRepository
    {
        Task<MapModel> GetCoordinatesAsync(string locationName);
    }
}
