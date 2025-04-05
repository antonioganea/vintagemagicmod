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
        public AssetLocation heatShape { get; protected set; } = AssetLocation.Create("block/metal/witchoven/witchoven-heat", VintageMagicModModSystem.Domain);

        BlockPos pos;
        ICoreClientAPI api;

        MeshRef cubeModelRef;
        MultiTextureMeshRef multiTextureMeshRef;
        int hotTextureId;
        int coldTextureId;
        float voxelHeight;
        public int glowLevel;
        protected Matrixf ModelMat = new Matrixf();

        BlockFacing ownFacing;

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

            CreateMeshData();

            this.ownFacing = BlockFacing.FromCode(api.World.BlockAccessor.GetBlock(pos).LastCodePart(0));
        }

        private void CreateMeshData()
        {
            heatShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");

            Shape shape = Vintagestory.API.Common.Shape.TryGet(api, heatShape);

            Block block = api.World.BlockAccessor.GetBlock(pos);
            if (block.BlockId == 0) return;

            api.Tesselator.TesselateShape(block, shape, out var modeldata);

            //modeldata.Flags = new int[6 * 4];


            var modeldata2 = CubeMeshUtil.GetCube(8 / 32f, 16 / 32f, new Vec3f(0, 0, 0));

            modeldata.SetUv(modeldata2.GetUv());

            //modeldata = CubeMeshUtil.GetCube(8 / 32f, voxelHeight / 32f, new Vec3f(0, 0, 0));
            //modeldata = CubeMeshUtil.GetCube(8 / 32f, 8 / 32f, new Vec3f(0, 0, 0));
            //modeldata.Flags = new int[6 * 4];
            

            ITexPositionSource t = new SimpleTextureSource();

            api.Tesselator.TesselateShapeWithJointIds("blabla", shape, out MeshData modeldata4, t, Vec3f.Zero);

            //multiTextureMeshRef = api.Render.UploadMultiTextureMesh(modeldata);

            cubeModelRef = api.Render.UploadMesh(modeldata4);

            //Vec3f dir = this.ownFacing.Normalf;
        }


        public void SetFillLevel(float voxelHeight)
        {
            //if (this.voxelHeight == voxelHeight && cubeModelRef != null) return;
            //this.voxelHeight = voxelHeight;
            //cubeModelRef?.Dispose();
            //if (voxelHeight == 0) return;
            //MeshData modeldata = CubeMeshUtil.GetCube(8 / 32f, voxelHeight / 32f, new Vec3f(0, 0, 0));
            //modeldata.Flags = new int[6 * 4];
            //cubeModelRef = api.Render.UploadMesh(modeldata);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            //if (voxelHeight == 0) return;

            IStandardShaderProgram prog = api.Render.PreparedStandardShader(pos.X, pos.Y, pos.Z, new Vec4f(1 + glowLevel / 128f, 1 + glowLevel / 128f, 1 + glowLevel / 512f, 1));
            prog.ExtraGlow = glowLevel; // glowLevel;

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

            voxelHeight += deltaTime * 225.0f;

            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                .Translate(0.5f, 0.5f, 0.5f)
                .RotateYDeg(-90.0f + 90.0f * ownFacing.HorizontalAngleIndex)
                .Translate(-0.5f, -0.5f, -0.5f)
                //.Translate(8 / 16f + pos.X - camPos.X, pos.Y - camPos.Y + voxelHeight / 32f, 8 / 16f + pos.Z - camPos.Z)
                //.Translate(-0.5 / 16f + 8 / 16f + pos.X - camPos.X, 1+ 5 / 16f + pos.Y - camPos.Y + voxelHeight / 32f, 8 / 16f + pos.Z - camPos.Z)
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
            cubeModelRef?.Dispose();
        }
    }
}
