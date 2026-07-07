using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{//Dinamico - Esfera
    public class Plant : Dinamic
    {
        private float _radius;
        private readonly Random _random = new();

        public Plant(Vector3 position, string path) : base(position, path)
        {
            _destructible = true;
        } //Decoration ya hace lo necesario

        //CARGO EL CONTENIDO (Modificacion de la funcion en DECORATION)
        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
        {
            base.LoadContent(content, simulation, effect);
            // Calculo de escala (Usando una funcion auxiliar para obtener vertices)
            _radius = Math.Max(_dimensions.X, Math.Max(_dimensions.Y, _dimensions.Z)) / 2f;
            _visualScale = 1f;

            // Creo el cuerpo en Bepu (Es la configuracion de la fisica)
            var shape = new Sphere(_radius);
            var shapeIndex = simulation.Shapes.Add(shape);

            //Calculo de la inercia (masa de 1kg porque si) - en el caso de la esfera la inercia se distribuye igual en todos los lados
            var inertia = shape.ComputeInertia(1f);

            // Posicion inicial, se ajusta el centro (Bepu usa el centro, MonoGame la base)
            //Uso la posicion del modelo visual para definir donde ubico el modelo fisico al inicio, pero la altura no por lo del pivote (el centro del modelo)
            var initialPos = new BepuVector3(_position.X, _position.Y + _radius, _position.Z);
            
            //Añado el cuerpo dinamico a la simulacion
            bodyHandle = simulation.Bodies.Add(BodyDescription.CreateDynamic(
                initialPos, inertia, shapeIndex, 0.01f));

            // Le doy una identidad al cuerpo para reconocerlo en colisiones
            simulation.Bodies[bodyHandle].Collidable.Continuity = ContinuousDetection.Passive;
        }

        //ACTUALIZO (Modificacion de la funcion en DECORATION)
        public override void Update(Simulation simulation)
        {
            if (IsDead) return;

            base.Update(simulation);

            // Tomo la posicion actual en la simulacion
            var bodyReference = simulation.Bodies[bodyHandle];
            var pose = bodyReference.Pose;

            //Reviso la velocidad del objeto, si su velocidad disminuyo hasta ser menor a 5 le doy un empujoncito
            if (bodyReference.Velocity.Linear.Length() < 5f) 
            {
                //Use el random para que el empuje sea hacia distintos lados (Ahora estan locas)
                float impulseX = _random.Next(-5, 6);
                float impulseZ = _random.Next(-5, 6);
                bodyReference.ApplyLinearImpulse(new BepuVector3(impulseX, 0, impulseZ)); // Empujoncito medianamente diagonal

                float angularX = _random.NextSingle() * 2f - 1f;
                float angularZ = _random.NextSingle() * 2f - 1f;
                bodyReference.ApplyAngularImpulse(new BepuVector3(angularX, 0, angularZ)); // Esto hace que la planta ruede, impulsando a que siga moviendose luego del empujoncito
            }

            // Convierto la orientacion (uso Quaternianos para que gire como se debe) y pose de Bepu a Monogame
            Matrix rotation = Matrix.CreateFromQuaternion(new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W));
            Vector3 position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);

            // Calculo de la matriz de mundo
            modificarMatrixWorld(rotation, position);
        }
        
        //DIBUJO LAS COLISIONES (Modificacion de la funcion en DECORATION)
        public override void DrawCollisionChamber(Gizmo gizmos, Simulation simulation)
        {   
            if (IsDead) return; //Si el modelo desaparece no hay que dibujarlo

            // Tomo solo la rotacion y posicion que vienen de la Pose de Bepu (el modelo se supone que ya concuerda con el modelo fisico).            
            var pose = simulation.Bodies[bodyHandle].Pose;
            
            Vector3 position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);

            Vector3 sphereSize = new Vector3(_radius);

            gizmos.DrawSphere(position, sphereSize, Color.Green);
        }

    }
}