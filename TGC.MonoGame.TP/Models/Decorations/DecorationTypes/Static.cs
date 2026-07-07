using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using TGC.MonoGame.TP.Gizmos;
using System;
using System.IO;

namespace TGC.MonoGame.TP.Models.Decorations
{
    public class Static : Decoration
    {
        public Matrix WorldMatrix => _world;
        public string ModelPath;
        protected StaticHandle _staticHandle;
        public StaticHandle StaticHandle => _staticHandle;

        public Static(Vector3 position, string path) : base(position, path)
        {
            ModelPath = path;
        }

        //CARGO EL CONTENIDO (Modificacion de la funcion en DECORATION)
        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
        {
            base.LoadContent(content, simulation, effect);
            var objectSize = Math.Max(_dimensions.X, Math.Max(_dimensions.Y, _dimensions.Z));
            _normalOffsetScale = MathHelper.Clamp(objectSize * 0.02f, 0.03f, 0.6f);
        }

        //ACTUALIZO (Modificacion de la funcion en DECORATION)
        public override void Update(Simulation simulation)
        {
            base.Update(simulation);
        }

        //DIBUJO EL MODELO (Modificacion de la funcion en DECORATION)
        public override void Draw (Matrix view, Matrix projection)
        {
            base.Draw(view, projection);
        }
        
        //DIBUJO LAS COLISIONES (Modificacion de la funcion en DECORATION)
        public override void DrawCollisionChamber(Gizmo gizmos, Simulation simulation)
        {   
            base.DrawCollisionChamber(gizmos, simulation);
        }

        //Estaticos
        public void modificarMatrixWorld(Matrix rotation, float yOffset = 0f){
            _world = Matrix.CreateTranslation(-_modelCenter)
                    * Matrix.CreateScale(_visualScale)
                    * rotation 
                    * Matrix.CreateTranslation(_position + Vector3.Up * yOffset);
            RecalculateWorldBoundingBox();
        }
    }
}