﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageMagicMod
{
    public class WitchOvenHeatRenderer : IRenderer
    {
        BlockPos pos;
        ICoreClientAPI api;

        MeshRef cubeModelRef;
        int textureId;
        float voxelHeight;
        public int glowLevel;
        protected Matrixf ModelMat = new Matrixf();

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
            textureId = api.Render.GetOrLoadTexture(new AssetLocation("block/coal/orecoalmix.png"));
        }


        public void SetFillLevel(float voxelHeight)
        {
            if (this.voxelHeight == voxelHeight && cubeModelRef != null) return;

            this.voxelHeight = voxelHeight;

            cubeModelRef?.Dispose();

            if (voxelHeight == 0) return;

            MeshData modeldata = CubeMeshUtil.GetCube(8 / 32f, voxelHeight / 32f, new Vec3f(0, 0, 0));
            modeldata.Flags = new int[6 * 4];

            cubeModelRef = api.Render.UploadMesh(modeldata);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (voxelHeight == 0) return;

            IStandardShaderProgram prog = api.Render.PreparedStandardShader(pos.X, pos.Y, pos.Z, new Vec4f(1 + glowLevel / 128f, 1 + glowLevel / 128f, 1 + glowLevel / 512f, 1));
            prog.ExtraGlow = 610; // glowLevel;

            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;

            rpi.BindTexture2d(textureId);

            prog.ModelMatrix = ModelMat
                .Identity()
                //.Translate(8 / 16f + pos.X - camPos.X, pos.Y - camPos.Y + voxelHeight / 32f, 8 / 16f + pos.Z - camPos.Z)
                .Translate(-0.5 / 16f + 8 / 16f + pos.X - camPos.X, 5 / 16f + pos.Y - camPos.Y + voxelHeight / 32f, 8 / 16f + pos.Z - camPos.Z)
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
