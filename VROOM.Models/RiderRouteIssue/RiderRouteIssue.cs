using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models.RiderRouteIssue
{
    class RiderRouteIssue
    {
        int Id;
        int RiderID;
        int RouteID;
        int IssueID;
        string Description;
        DateTime ReportedAt;
        string Severity;
        string Status;
    }
}
