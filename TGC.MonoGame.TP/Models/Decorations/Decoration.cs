using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations;
/// <summary>
/// Decoraciones dentro de escenario: rocas, arboles, cactus, etc.
/// </summary>
public class Decoration
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderTextures = "Textures/";

    protected Model _model;
    protected Texture2D _texture;
    protected Matrix _world;
    protected Vector3 _position; //Vector3 de monogame por si en la terminal me vuelve a tirar "qui ni sibi bujuju"
    protected float _visualScale;
    protected string _path;
    protected BoundingBox _boundingBox; //la cajita xd
    protected BoundingBox _worldBoundingBox; //caja transformada a espacio mundo, la que se usa para frustum culling
    public BoundingBox BoundingBox => _worldBoundingBox;
    protected Vector3 _dimensions; //guarda el ancho, alto y largo del modelo
    protected Vector3 _modelCenter; //ubicacion del pivote

    protected Effect _effect;

    public Vector3 Position => _position; //Es la variable de solo lectura de la posicion
    private static readonly Vector3[] _cornersCache = new Vector3[8];

    protected float _normalOffsetScale;

    public Decoration(Vector3 position, string path)
    {
        _position = position;
        _path = path;
        _visualScale = 1f; //Hasta delimitar el tamaño de cada modelo con el modelo fisico
    }

    //CARGA DE CONTENIDO (Modificable)
    public virtual void LoadContent(ContentManager content, Simulation simulation, Effect effect)
    {
        _model = content.Load<Model>(ContentFolder3D + _path);
        _texture = content.Load<Texture2D>(ContentFolderTextures + "paleta_256x512"); //Aprovechando que todos usan la misma imagen
        _effect = effect.Clone(); //Como lo clono en vez de usar el mismo comparto el codigo pero no el parametro world ni view que varian de modelo a modelo
        //Para cada malla de mi coleccion de mallas del modelo

        foreach (var mesh in _model.Meshes) 
        {
           //Para cada parte de la malla de mi coleccion de partes de la malla
            foreach (var meshPart in mesh.MeshParts)
            {
                // Reemplazamos el efecto por defecto del modelo por el nuestro
                meshPart.Effect = _effect;
            } 
        }

        _boundingBox = BoundingVolumesUtils.CreateBoundingBox(_model);
        _dimensions = _boundingBox.Max - _boundingBox.Min; //tomo el punto maximo y el punto minimo de mi caja y luego calculo la diferencia para saber la distancia, se usa el Min porque el modelo puede estar un poquito mal posicionado y no lo voy andar corrigiendo 80 veces en blender, ya lo intente
        _modelCenter = (_boundingBox.Max + _boundingBox.Min) / 2f; //ajustamos el pivote que originalmente esta en los pies del modelo visual para que concuerde con el del modelo fisico que es en el centro
        var objectSize = Math.Max(_dimensions.X, Math.Max(_dimensions.Y, _dimensions.Z));
        _normalOffsetScale = MathHelper.Clamp(objectSize * 0.02f, 0.03f, 0.6f);
        //_normalOffsetScale = 0.02f;
        _worldBoundingBox = _boundingBox;
    }

    //ACTUALIZO (Modificable)
    public virtual void Update(Simulation simulation) { } //Varia de modelo a modelo

    //DIBUJO LAS COLISIONES (Modificable)
    public virtual void DrawCollisionChamber(Gizmo gizmos, Simulation simulation) {}

    protected void RecalculateWorldBoundingBox()
    {
        _boundingBox.GetCorners(_cornersCache); //genera los 8 vertices de la caja local
        for (int i = 0; i < _cornersCache.Length; i++)
        {
            _cornersCache[i] = Vector3.Transform(_cornersCache[i], _world);
        }
        _worldBoundingBox = BoundingBox.CreateFromPoints(_cornersCache);
    }

    //DIBUJO (Modificable)
    public virtual void Draw(Matrix view, Matrix projection)
    {
        if (_model == null || _effect == null) return;

        var smm = TGCGame.Instance.ShadowMapManager;

        var technique = _effect.Techniques["DrawShadowedHibrido"];
        if (technique == null)
        {
            if (_effect.Techniques.Count > 0)
                _effect.CurrentTechnique = _effect.Techniques[0];
        }
        else
        {
            _effect.CurrentTechnique = technique;
        }

        _effect.Parameters["View"]?.SetValue(view);
        _effect.Parameters["Projection"]?.SetValue(projection);
        _effect.Parameters["ModelTexture"]?.SetValue(_texture);
        _effect.Parameters["World"]?.SetValue(_world);
        _effect.Parameters["DiffuseColor"]?.SetValue(Vector3.One); 
        _effect.Parameters["InverseTransposeWorld"]?.SetValue(Matrix.Transpose(Matrix.Invert(_world)));
        _effect.Parameters["normalOffsetScale"]?.SetValue(_normalOffsetScale);
        _effect.Parameters["Shininess"]?.SetValue(16f);
        _effect.Parameters["IsDeformable"]?.SetValue(0);

        if (smm != null)
        {
            _effect.Parameters["LightViewProjection"]?.SetValue(smm.LightViewProjection);
            _effect.Parameters["lightPosition"]?.SetValue(smm.LightPosition);
            
            _effect.Parameters["shadowMapStatic"]?.SetValue(smm.StaticShadowRenderTarget);
            _effect.Parameters["shadowMapDynamic"]?.SetValue(smm.DynamicShadowRenderTarget);
            
            if (smm.StaticShadowRenderTarget != null)
            {
                _effect.Parameters["shadowMapSize"]?.SetValue(new Vector2(smm.StaticShadowRenderTarget.Width, smm.StaticShadowRenderTarget.Height));
            }
        }

        var gd = _effect.GraphicsDevice;

        foreach (var mesh in _model.Meshes)
        {
            foreach (var meshPart in mesh.MeshParts)
            {
                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    
                    gd.SetVertexBuffer(meshPart.VertexBuffer);
                    gd.Indices = meshPart.IndexBuffer;
                    gd.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList, 
                        meshPart.VertexOffset, 
                        meshPart.StartIndex, 
                        meshPart.PrimitiveCount
                    );
                }
            }
        }
    }

    public virtual void DrawDepth(Matrix lightViewProjection)
    {
        if (_model == null || _effect == null) return;

        var technique = _effect.Techniques["DepthPass"];
        if (technique != null)
        {
            _effect.CurrentTechnique = technique;
        }

        _effect.Parameters["World"]?.SetValue(_world);
        _effect.Parameters["LightViewProjection"]?.SetValue(lightViewProjection);
        //_effect.Parameters["normalOffsetScale"]?.SetValue(_normalOffsetScale);
        _effect.Parameters["IsDeformable"]?.SetValue(0);

        var gd = _effect.GraphicsDevice;

        foreach (var mesh in _model.Meshes)
        {
            foreach (var meshPart in mesh.MeshParts)
            {
                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    
                    gd.SetVertexBuffer(meshPart.VertexBuffer);
                    gd.Indices = meshPart.IndexBuffer;
                    gd.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList, 
                        meshPart.VertexOffset, 
                        meshPart.StartIndex, 
                        meshPart.PrimitiveCount
                    );
                }
            }
        }
    }
    
}