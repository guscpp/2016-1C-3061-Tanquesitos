using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Models;

public class Skybox
{
    private GraphicsDevice _graphicsDevice;
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private Effect _effect;
    private TextureCube _cubemap;
    
    public float RotationSpeed { get; set; } = 0.003f; //radianes/segundo
    private float _currentRotation = 0f;

    public Skybox(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        CreateCubeGeometry();
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Effects/Skybox");

        //cargar las 6 caras y crear el TextureCube
        string[] faceNames = { "Right", "Left", "Top", "Bottom", "Front", "Back" };
        Texture2D[] faces = new Texture2D[6];
        for (int i = 0; i < 6; i++)
        {
            faces[i] = content.Load<Texture2D>($"Textures/skybox/Daylight_Box_{faceNames[i]}");
        }

        int size = faces[0].Width;
        _cubemap = new TextureCube(_graphicsDevice, size, false, SurfaceFormat.Color);
        
        Color[] pixelData = new Color[size * size];
        for (int i = 0; i < 6; i++)
        {
            faces[i].GetData(pixelData);
            _cubemap.SetData((CubeMapFace)i, pixelData);
        }
    }

    private void CreateCubeGeometry()
    {
        // ✅ Cubo GIGANTE de 10000x10000x10000 para que siempre llene la pantalla
        float size = 100f;
        float half = size / 2f;

        Vector3[] positions = new Vector3[]
        {
        // Front face (+Z)
        new(-half, -half,  half), new( half, -half,  half), new( half,  half,  half), new(-half,  half,  half),
        // Back face (-Z)
        new( half, -half, -half), new(-half, -half, -half), new(-half,  half, -half), new( half,  half, -half),
        // Top face (+Y)
        new(-half,  half,  half), new( half,  half,  half), new( half,  half, -half), new(-half,  half, -half),
        // Bottom face (-Y)
        new(-half, -half, -half), new( half, -half, -half), new( half, -half,  half), new(-half, -half,  half),
        // Right face (+X)
        new( half, -half,  half), new( half, -half, -half), new( half,  half, -half), new( half,  half,  half),
        // Left face (-X)
        new(-half, -half, -half), new(-half, -half,  half), new(-half,  half,  half), new(-half,  half, -half),
        };

        VertexPosition[] vertices = new VertexPosition[positions.Length];
        for (int i = 0; i < positions.Length; i++)
            vertices[i] = new VertexPosition(positions[i]);

        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPosition), vertices.Length, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);

        ushort[] indices = new ushort[36];
        for (int face = 0; face < 6; face++)
        {
            ushort offset = (ushort)(face * 4);
            int i = face * 6;
            indices[i + 0] = (ushort)(offset + 0);
            indices[i + 1] = (ushort)(offset + 1);
            indices[i + 2] = (ushort)(offset + 2);
            indices[i + 3] = (ushort)(offset + 0);
            indices[i + 4] = (ushort)(offset + 2);
            indices[i + 5] = (ushort)(offset + 3);
        }

        _indexBuffer = new IndexBuffer(_graphicsDevice, typeof(ushort), indices.Length, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices);
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _currentRotation += RotationSpeed * dt;
        
        //evitar que crezca infinitamente
        if (_currentRotation > MathHelper.TwoPi) 
            _currentRotation -= MathHelper.TwoPi;
    }

    public void Draw(Matrix view, Matrix projection, Vector3 cameraPosition)
    {
        if (_effect == null || _cubemap == null) return;

        // 1. CullClockwise: Como estamos dentro del cubo, 
        //    necesitamos ver las caras "desde atras"
        var originalRasterizer = _graphicsDevice.RasterizerState;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;

        // 2. DepthRead: Lee el depth pero no escribe.
        //    Asi el skybox nunca tapa nada, pero el truco .xyww 
        //    del shader ya lo empuja al fondo.
        var originalDepth = _graphicsDevice.DepthStencilState;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        float skyboxOffset = 20f;

        // El skybox sigue a la camara (siempre centrado en ella)
        // y rota lentamente para simular movimiento de nubes
        Matrix world = Matrix.CreateRotationY(_currentRotation)
            * Matrix.CreateTranslation(cameraPosition + Vector3.Down * skyboxOffset);
        
        Matrix wvp = world * view * projection;

        _effect.Parameters["WorldViewProjection"]?.SetValue(wvp);
        _effect.Parameters["SkyboxTexture"]?.SetValue(_cubemap);

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
        }

        //restaurar estados
        _graphicsDevice.RasterizerState = originalRasterizer;
        _graphicsDevice.DepthStencilState = originalDepth;
    }
}
