using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Models.Particulas;

public class Particle
{
    protected Vector3 _initialPos;
    protected Vector3 _currentPos;
    protected Vector3 Velocity;
    protected float speed;
    protected float genTime = 0f; // tiempo en el que nacio
    protected float maxTime; // tiempo maximo antes de morir (en segundos)
    protected bool _isAlive = false;
    public bool IsAlive => _isAlive;
    protected float _alpha = 1f;
    protected float _size;
    protected Color _color;    
    protected Random random = new();

    // 40 particulas * 6 vertices cada una (2 triangulos)
    protected VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[40 * 6];

    public void Draw(Matrix View, Matrix Projection, Effect effect, Texture2D texture)
    {
        if (!_isAlive) return;
        var technique = effect.Techniques["Particles"];
        effect.CurrentTechnique = technique;

        Vector3 right = new Vector3(View.M11, View.M21, View.M31);
        Vector3 up = new Vector3(View.M12, View.M22, View.M32);

        int vertexCount = 0;

        var col = new Color(_color, _alpha);
        var halfsize = _size / 2;
        Vector3 topLeft = _currentPos + (-right + up) * halfsize;
        Vector3 topRight = _currentPos + (right + up) * halfsize;
        Vector3 bottomLeft = _currentPos + (-right - up) * halfsize;
        Vector3 bottomRight = _currentPos + (right - up) * halfsize;

        // triangulo 1 del quad
        _vertices[vertexCount++] = new VertexPositionColorTexture(topLeft, col, new Vector2(0, 0));
        _vertices[vertexCount++] = new VertexPositionColorTexture(topRight, col, new Vector2(1, 0));
        _vertices[vertexCount++] = new VertexPositionColorTexture(bottomLeft, col, new Vector2(0, 1));
        // triangulo 2 del quad
        _vertices[vertexCount++] = new VertexPositionColorTexture(topRight, col, new Vector2(1, 0));
        _vertices[vertexCount++] = new VertexPositionColorTexture(bottomRight, col, new Vector2(1, 1));
        _vertices[vertexCount++] = new VertexPositionColorTexture(bottomLeft, col, new Vector2(0, 1));


        if (vertexCount == 0) return;
        effect.Parameters["View"]?.SetValue(View);
        effect.Parameters["Projection"]?.SetValue(Projection);
        effect.Parameters["ParticleTexture"]?.SetValue(texture);

        var gd = effect.GraphicsDevice;
        gd.BlendState = BlendState.AlphaBlend;
        gd.DepthStencilState = DepthStencilState.DepthRead;
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            gd.DrawUserPrimitives(
            PrimitiveType.TriangleList,
            _vertices,
            0,
            vertexCount / 3
        );
        }
        gd.BlendState = BlendState.Opaque;
        gd.DepthStencilState = DepthStencilState.Default;
    }

    // resetea la particula a sus valores iniciales para que desocupe en el manager
    public void Reset()
    {
        _isAlive = false;
        _initialPos = Vector3.Zero;
        _currentPos = Vector3.Zero;
        genTime = 0f;
        _alpha = 1f;
    }
}