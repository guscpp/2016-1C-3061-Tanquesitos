using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using BepuPhysics.Collidables;
using TGC.MonoGame.TP.Gizmos;
using TGC.MonoGame.TP.Models.Tanks;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;

namespace TGC.MonoGame.TP.Models.Decorations
{
    public class FuelBarrel : Decoration
    {
        private StaticHandle _staticHandle;
        private float _height;

        public bool IsCollected { get; private set; }
        public bool IsRecharging { get; private set; }
        public float RechargeProgress { get; private set; }
        public TankPlayer CollectedBy { get; private set; }

        public FuelBarrel(Vector3 position) : base(position, "powerups/FuelBarrel")
        {
            _visualScale = 1f;
        }

        //Resetea los barriles entre partidas
        public void ResetBarrel(Simulation simulation)
        {
            bool wasCollected = IsCollected;

            //Restaurar las banderas de estado
            IsCollected = false;
            IsRecharging = false;
            CollectedBy = null;
            RechargeProgress = 0f;

            //Restaurar la visibilidad
            _visualScale = 1f;

            //Recrear el cuerpo fisico si fue eliminado al tomarse
            if (wasCollected)
            {
                var shape = new Cylinder(GameConfig.FuelBarrel.Radius, _height);
                var shapeIndex = simulation.Shapes.Add(shape);
                var initialPos = new BepuVector3(_position.X, _position.Y, _position.Z);
                _staticHandle = simulation.Statics.Add(new StaticDescription(initialPos, shapeIndex));
            }
        }

        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
        {
            base.LoadContent(content, simulation, effect);
            //_normalOffsetScale = 0.10f;
            // cache dimensions post-bbox calc para evitar recompute
            _dimensions = _boundingBox.Max - _boundingBox.Min;
            _height = _dimensions.Y;

            // cilindro estatico para colision: radio y altura desde config
            var shape = new Cylinder(GameConfig.FuelBarrel.Radius, _height);
            var shapeIndex = simulation.Shapes.Add(shape);

            // offset y: bepu usa centro geometrico, el terrain.getheight devuelve base
            var initialPos = new BepuVector3(_position.X, _position.Y, _position.Z);
            _staticHandle = simulation.Statics.Add(new StaticDescription(initialPos, shapeIndex));

            // correccion de orientacion: fbx exportado en z-up, mono game usa y-up
            Matrix rotation = Matrix.CreateRotationX(MathHelper.ToRadians(-90));
            modificarMatrixWorld(rotation);
        }

        // intento de recoleccion: valida distancia y estado antes de activar recarga
        public void TryCollect(TankPlayer tank, Simulation simulation)
        {
            if (IsCollected || IsRecharging) return;

            // centro del barril para calculo de distancia (ajustado por altura)
            Vector3 barrelCenter = new Vector3(_position.X, _position.Y + _height / 2f, _position.Z);
            float dist = Vector3.Distance(barrelCenter, tank.Position);

            if (dist < GameConfig.FuelBarrel.CollectionDistance)
            {
                IsCollected = true;
                _visualScale = 0f; // ocultar visual sin eliminar referencia al modelo
                simulation.Statics.Remove(_staticHandle); // liberar cuerpo fisico para evitar colisiones fantasma

                IsRecharging = true;
                RechargeProgress = 0f;
                CollectedBy = tank;

                TGCGame.Instance.SoundManager.PlaySound("agarrar_combustible_1");
            }
        }

        // recarga progresiva frame a frame
        public void UpdateRecharge(float dt)
        {
            if (!IsRecharging || CollectedBy == null) return;

            RechargeProgress += dt;
            // tasa constante: fuel / duracion => litros por segundo
            float fuelPerSecond = GameConfig.FuelBarrel.FuelAmount / GameConfig.FuelBarrel.RechargeDuration;
            CollectedBy.AddFuel(fuelPerSecond * dt);

            // finalizar recarga al completar duracion configurada
            if (RechargeProgress >= GameConfig.FuelBarrel.RechargeDuration)
            {
                IsRecharging = false;
                CollectedBy = null;
            }
            RecalculateWorldBoundingBox();
        }

        // debug visual: dibuja volumen de colision solo si el barril esta activo
        public override void DrawCollisionChamber(Gizmo gizmos, Simulation simulation)
        {
            if (IsCollected) return;
            Matrix gizmoWorld = Matrix.CreateScale(GameConfig.FuelBarrel.Radius, _height, GameConfig.FuelBarrel.Radius)
                * Matrix.CreateTranslation(_position.X, _position.Y, _position.Z);
            gizmos.DrawCylinder(gizmoWorld, Color.Orange);
        }

        public void modificarMatrixWorld(Matrix rotation){
        _world = Matrix.CreateTranslation(-_modelCenter)
                * Matrix.CreateScale(_visualScale)
                * rotation 
                * Matrix.CreateTranslation(_position);

            RecalculateWorldBoundingBox();
    }
    }
}