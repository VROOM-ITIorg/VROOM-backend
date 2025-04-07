using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models
{
    public enum issueSeverityEnum
    {
        Minimal, Mild, Moderate, Severe
    }


    public enum IssueTypeEnum
    {
        Crash,
        Slowdown,
        Police,
        Construction,
        LaneClosure,
        StalledVehicle,
        ObjectOnRoad,
        Other

    }
}
