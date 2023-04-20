using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomBuildings
{
    public class Config
    {
        public int LastId { get; set; } = CustomBuildingsMod.MinimumID;
        public List<ReservedIds> Reserved { get; set; } = new List<ReservedIds>();
    }

    public class ReservedIds
    {
        public string ModId { get; set; } = "";

        public int TileId { get; set; } = 0;
    }
}
