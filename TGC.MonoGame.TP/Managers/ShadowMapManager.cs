using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TGC.MonoGame.TP.Managers
{
    public class ShadowMapManager : IDisposable
    {
        private GraphicsDevice _graphicsDevice;
    
        private int _resolution;

        public RenderTarget2D StaticShadowRenderTarget { get; private set; }
        public RenderTarget2D DynamicShadowRenderTarget { get; private set; }
        public bool RebajarSombrasEstaticas { get; set; } = true;
        public Matrix LightView { get; private set; }
        public Matrix LightProjection { get; private set; }
        public Matrix LightViewProjection { get; private set; }

        public Matrix StaticLightViewProjection { get; private set; }
        public Matrix DynamicLightViewProjection { get; private set; }

        private Vector3 _lightPosition;
        private Vector3 _lightTarget;

        public Vector3 LightPosition 
        { 
            get => _lightPosition; 
            set 
            { 
                if (_lightPosition != value)
                {
                    _lightPosition = value; 
                    ActualizarMatricesLuz();
                    // Si se mueve el sol, obligatoriamente hay que recrear el mapa estático
                    RebajarSombrasEstaticas = true; 
                }
            } 
        }

        public Vector3 LightTarget 
        { 
            get => _lightTarget; 
            set 
            { 
                if (_lightTarget != value)
                {
                    _lightTarget = value; 
                    ActualizarMatricesLuz();
                    RebajarSombrasEstaticas = true;
                }
            } 
        }

        public ShadowMapManager(GraphicsDevice graphicsDevice, int resolution = 2048)
        {
            _graphicsDevice = graphicsDevice;
            _resolution = resolution;
            _lightPosition = new Vector3(200f, 300f, 150f); 
            _lightTarget = Vector3.Zero;

            StaticShadowRenderTarget = new RenderTarget2D(
                _graphicsDevice, 
                _resolution, 
                _resolution, 
                false, 
                SurfaceFormat.Single, 
                DepthFormat.Depth24
            );
            
            DynamicShadowRenderTarget = new RenderTarget2D(
                _graphicsDevice, 
                _resolution, 
                _resolution, 
                false, 
                SurfaceFormat.Single, 
                DepthFormat.Depth24
            );

            DynamicLightViewProjection = Matrix.Identity;
            StaticLightViewProjection = Matrix.Identity;
            
            ActualizarMatricesLuz();
        }

        public void ActualizarMatricesLuz()
        {
            LightView = Matrix.CreateLookAt(LightPosition, LightTarget, Vector3.Up);
        }

        public void BeginStaticShadowPass()
        {
            _graphicsDevice.SetRenderTarget(StaticShadowRenderTarget);
            _graphicsDevice.Clear(Color.White); 
            
            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public void BeginDynamicShadowPass()
        {
            _graphicsDevice.SetRenderTarget(DynamicShadowRenderTarget);
            _graphicsDevice.Clear(Color.White); 
            
            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public void BeginLightingPass(Effect shadowEffect, RenderTarget2D originalRenderTarget = null)
        {
            _graphicsDevice.SetRenderTarget(originalRenderTarget); 
            
            shadowEffect.Parameters["shadowMapStatic"]?.SetValue(StaticShadowRenderTarget);
            shadowEffect.Parameters["shadowMapDynamic"]?.SetValue(DynamicShadowRenderTarget);
            shadowEffect.Parameters["shadowMapSize"]?.SetValue(new Vector2(_resolution, _resolution));
        }

        public void FitFrustumToScene(Vector3 sceneMin, Vector3 sceneMax)
        {
            Vector3[] corners =
            [
                new Vector3(sceneMin.X, sceneMin.Y, sceneMin.Z),
                new Vector3(sceneMax.X, sceneMin.Y, sceneMin.Z),
                new Vector3(sceneMin.X, sceneMax.Y, sceneMin.Z),
                new Vector3(sceneMax.X, sceneMax.Y, sceneMin.Z),
                new Vector3(sceneMin.X, sceneMin.Y, sceneMax.Z),
                new Vector3(sceneMax.X, sceneMin.Y, sceneMax.Z),
                new Vector3(sceneMin.X, sceneMax.Y, sceneMax.Z),
                new Vector3(sceneMax.X, sceneMax.Y, sceneMax.Z),
            ];

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var corner in corners)
            {
                var lightSpaceCorner = Vector3.Transform(corner, LightView);
                minX = Math.Min(minX, lightSpaceCorner.X);
                maxX = Math.Max(maxX, lightSpaceCorner.X);
                minY = Math.Min(minY, lightSpaceCorner.Y);
                maxY = Math.Max(maxY, lightSpaceCorner.Y);
                minZ = Math.Min(minZ, lightSpaceCorner.Z);
                maxZ = Math.Max(maxZ, lightSpaceCorner.Z);
            }

            LightProjection = Matrix.CreateOrthographicOffCenter(
                minX, maxX,
                minY, maxY,
                -maxZ, -minZ  // near/far invertidos porque Z apunta hacia adentro en view space y si no se ve todo oscuro
            );

            LightViewProjection = LightView * LightProjection;
        }

        public void FitStaticToScene(Vector3 sceneMin, Vector3 sceneMax)
        {
            Vector3[] corners =
            [
                new Vector3(sceneMin.X, sceneMin.Y, sceneMin.Z),
                new Vector3(sceneMax.X, sceneMin.Y, sceneMin.Z),
                new Vector3(sceneMin.X, sceneMax.Y, sceneMin.Z),
                new Vector3(sceneMax.X, sceneMax.Y, sceneMin.Z),
                new Vector3(sceneMin.X, sceneMin.Y, sceneMax.Z),
                new Vector3(sceneMax.X, sceneMin.Y, sceneMax.Z),
                new Vector3(sceneMin.X, sceneMax.Y, sceneMax.Z),
                new Vector3(sceneMax.X, sceneMax.Y, sceneMax.Z),
            ];

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var corner in corners)
            {
                var lc = Vector3.Transform(corner, LightView);
                minX = Math.Min(minX, lc.X); maxX = Math.Max(maxX, lc.X);
                minY = Math.Min(minY, lc.Y); maxY = Math.Max(maxY, lc.Y);
                minZ = Math.Min(minZ, lc.Z); maxZ = Math.Max(maxZ, lc.Z);
            }

            var proj = Matrix.CreateOrthographicOffCenter(minX, maxX, minY, maxY, -maxZ, -minZ);
            LightViewProjection = LightView * proj;
        }

        public void FitDynamicToCamera(Vector3[] cameraCorners)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var corner in cameraCorners)
            {
                var lc = Vector3.Transform(corner, LightView);
                minX = Math.Min(minX, lc.X); maxX = Math.Max(maxX, lc.X);
                minY = Math.Min(minY, lc.Y); maxY = Math.Max(maxY, lc.Y);
                minZ = Math.Min(minZ, lc.Z); maxZ = Math.Max(maxZ, lc.Z);
            }

            var proj = Matrix.CreateOrthographicOffCenter(minX, maxX, minY, maxY, -maxZ, -minZ);
            DynamicLightViewProjection = LightView * proj;
        }

        public void Dispose()
        {
            StaticShadowRenderTarget?.Dispose();
            DynamicShadowRenderTarget?.Dispose();
        }
    }
}