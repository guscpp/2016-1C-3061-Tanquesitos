using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;
using TGC.MonoGame.TP.Gizmos;
namespace TGC.MonoGame.TP.Models.Decorations
{//Dinamico (2 tipos) - Caja
    public class Cart : Dinamic
    {
        private float _width;
        private float _height;
        private float _lenght;

        public Cart(Vector3 position, string path) : base(position, path)
        {
            _destructible = false;
        } //Decoration ya hace lo necesario
        
        //CARGO EL CONTENIDO (Modificacion de la funcion en DECORATION)
        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
        {
            base.LoadContent(content, simulation, effect);
            // Calculo de escala (Usando una funcion auxiliar para obtener vertices)
            _width = _dimensions.X;
            _height = _dimensions.Y;
            _lenght = _dimensions.Z;
            _visualScale = 1f;

            // Creo el cuerpo en Bepu (Es la configuracion de la fisica)
            var shape = new Box(_width, _height, _lenght);
            var shapeIndex = simulation.Shapes.Add(shape);

            //Calculo de la inercia (masa de 1kg porque si)
            var inertia = shape.ComputeInertia(1f);

            // Posicion inicial, se ajusta el centro (Bepu usa el centro, MonoGame la base)
            //Uso la posicion del modelo visual para definir donde ubico el modelo fisico al inicio, pero la altura no por lo del pivote (el centro del modelo)
            var initialPos = new BepuVector3(_position.X, _position.Y, _position.Z);

            //Para que no consuma recursos a lo loco cuando no lo estoy tocando y asegurarme de que se va quedar quieto hasta que el tanque lo choque
            //Primer parametro --> umbral de sueño --> si no se mueve demasiado bepu lo congela
            //Segundo parametro --> estado inicial --> es 0 porque esta dormido
            var activity = new BodyActivityDescription(0.01f, 0);
            
            //Añado el cuerpo dinamico a la simulacion
            bodyHandle = simulation.Bodies.Add(BodyDescription.CreateDynamic(
                initialPos, inertia, shapeIndex, activity)); //si colocamos el 0.01f solamente en vez del activity se verá algunos objetos "temblar"

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
            
            Matrix rotation = Matrix.CreateFromQuaternion(new Quaternion(
                pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W));
            
            Vector3 position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);

            // Matriz --> volumen real de colision en el motor fisico
            // Uso el radio y altura para escalar el cilindro del Gizmo si es necesario
            Matrix gizmoWorld = Matrix.CreateScale(_width, _height, _lenght) 
                                * rotation 
                                * Matrix.CreateTranslation(position);

            gizmos.DrawCube(gizmoWorld, Color.Green);
        }

    }
}