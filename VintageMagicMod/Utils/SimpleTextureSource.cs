using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace VintageMagicMod.Utils
{
    public class SimpleTextureSource : ITexPositionSource
    {
        private Size2i size;
        private TextureAtlasPosition textureAtlasPosition;

        public SimpleTextureSource()
        {
            size = new Size2i(1, 1);
            textureAtlasPosition = new TextureAtlasPosition();

            textureAtlasPosition.x1 = 0.0f;
            textureAtlasPosition.x2 = 1.0f;

            textureAtlasPosition.y1 = 0.0f;
            textureAtlasPosition.y2 = 1.0f;
        }

        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                return textureAtlasPosition;
            }
        }

        public Size2i AtlasSize
        {
            get
            {
                return size;
            }
        }
    }
}
