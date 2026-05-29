using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class Transport : BaseEntity
    {
        public string RouteName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;
        public int Capacity { get; set; }
    }
}
