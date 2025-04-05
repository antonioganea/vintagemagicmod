using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VintageMagicMod
{
    public class WitchOvenHeatRenderer : IRenderer
    {
        BlockPos pos;
        ICoreClientAPI api;

        int hotTextureId;
        int coldTextureId;

        public int glowLevel;
        protected Matrixf ModelMat = new Matrixf();

        private BlockFacing ownFacing;

        private MeshRef cubeModelRef;

        public double RenderOrder
        {
            get { return 0.5; }
        }

        public int RenderRange
        {
            get { return 24; }
        }

        public WitchOvenHeatRenderer(BlockPos pos, ICoreClientAPI api)
        {
            this.pos = pos;
            this.api = api;

            hotTextureId = api.Render.GetOrLoadTexture(new AssetLocation("vintagemagicmod:block/coal/ember.png"));
            coldTextureId = api.Render.GetOrLoadTexture(new AssetLocation("vintagemagicmod:block/coal/coke.png"));

            this.ownFacing = BlockFacing.FromCode(api.World.BlockAccessor.GetBlock(pos).LastCodePart(0));

            Block block = api.World.BlockAccessor.GetBlock(pos);
            if (block.BlockId == 0) return;

            BlockWitchOven blockWitchOven = block as BlockWitchOven;
            if (blockWitchOven == null) return;
            cubeModelRef = blockWitchOven.GetMeshData(api);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            IStandardShaderProgram prog = api.Render.PreparedStandardShader(pos.X, pos.Y, pos.Z, new Vec4f(1 + glowLevel / 128f, 1 + glowLevel / 128f, 1 + glowLevel / 512f, 1));
            prog.ExtraGlow = glowLevel;

            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;

            if (glowLevel > 50)
            {
                rpi.BindTexture2d(hotTextureId);
            }
            else
            {
                rpi.BindTexture2d(coldTextureId);
            }

            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                .Translate(0.5f, 0.5f, 0.5f)
                .RotateYDeg(-90.0f + 90.0f * ownFacing.HorizontalAngleIndex)
                .Translate(-0.5f, -0.5f, -0.5f)
                .Values
            ;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(cubeModelRef);
            prog.Stop();
        }

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        }
    }
}
