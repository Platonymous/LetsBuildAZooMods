using ModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyZoo.Tile_Data;

namespace CustomBuildings
{
    public class Content
    {
        public List<BuildingDefinition> Buildings { get; set; } = new List<BuildingDefinition>();
    }

    public class BuildingDefinition
    {
        private IModHelper _pack = null;

        private int _tileId = -1;

        private TILETYPE _template = TILETYPE.None;

        public string Id { get; set; } = "";

        public string Category { get; set; } = "Floors";

        public string BuildingType { get; set; } = "Floor";

        public int Rotations { get; set; } = 4;

        public bool IsVolumeFloor { get; set; } = false;

        public bool IsCroppedFloor { get; set; } = false;

        public bool IsAboveGroundFloor { get; set; } = false;

        public bool HasFront { get; set; } = false;

        public string Name { get; set; } = "";

        public string Template { get; set; } = "";

        public string Description { get; set; } = "";

        public int TileWidth = 16;

        public int TileHeight = 16;

        public int FrontTileWidth = 16;

        public int FrontTileHeight = 16;

        public string Texture { get; set; } = "spritesheet";

        public int Cost { get; set; } = -1;

        public List<ProductionValue> Productions { get; set; } = new List<ProductionValue>();
        public List<ProductionValue> Consumptions { get; set; } = new List<ProductionValue>();

        public void SetPack(IModHelper pack)
        {
            _pack = pack;
        }

        public IModHelper GetPack()
        {
            return _pack;
        }

        public void SetTileId(int tileId)
        {
            _tileId = tileId;
        }

        public int GetTileId()
        {
            return _tileId;
        }

        public bool TryGetTemplate(out TILETYPE template)
        {
            if(_template == TILETYPE.None && !string.IsNullOrEmpty(Template))
                _template = (TILETYPE) Enum.Parse(typeof(TILETYPE), Template, true);

            template = _template;

            return _template != TILETYPE.None;
        }

        public bool HasThisTemplate(TILETYPE template)
        {
            return TryGetTemplate(out TILETYPE t) && t == template;
        }
    }

    public class ProductionValue
    {
        public int Type { get; set; } = -1;

        public int Volume { get; set; } = -1;
    }

   
}
