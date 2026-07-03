using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP;

/// <summary>
///     Configuracion centralizada de escalas, dimensiones y parametros físicos.
///     1 unidad del mundo = 1 metro
/// </summary>
public static class GameConfig
{
    // Enum para identificar las clases de tanques en todo el juego
    public enum TankClass { Scout, Medium, Heavy }

    // ESCALA GLOBAL
    public const float WorldUnitsPerMeter = 1f;
    public const float MetersPerWorldUnit = 1f / WorldUnitsPerMeter;

    // CONVERSION (util si en el futuro cambiamos la escala base)
    public static float ToMeters(float worldUnits) => worldUnits * MetersPerWorldUnit;
    public static float ToWorldUnits(float meters) => meters * WorldUnitsPerMeter;
    public static Vector3 ToMeters(Vector3 worldPos) => new Vector3(
        ToMeters(worldPos.X), ToMeters(worldPos.Y), ToMeters(worldPos.Z));
    public static Vector3 ToWorldUnits(Vector3 metersPos) => new Vector3(
        ToWorldUnits(metersPos.X), ToWorldUnits(metersPos.Y), ToWorldUnits(metersPos.Z));

    // =========================================================================
    // CONFIGURACION UNIFICADA DE CLASES DE TANQUES
    // =========================================================================
    public static class TankClasses
    {
        public static class Scout
        {
            public const float MaxSpeed = 120f;       // m/s
            public const float MotorForce = 160000f;  //
            public const float TurnSpeed = 1.5f;      //
            public const float AttackDamage = 0.8f;   //
            public const float CannonPitch = 1.00f;   // adimensional, modifica el tono hacia agudo
            public const float Cooldown = 0.5f;       // segundo

            // HPs segun sea Scout player o Scout NPC
            public const float PlayerHealth = 15f;
            public const float EnemyHealth = 4f;
        }

        public static class Medium
        {
            // Identidad compartida
            public const float MaxSpeed = 90f;       // m/s
            public const float MotorForce = 150000f; //
            public const float TurnSpeed = 1.2f;     //
            public const float AttackDamage = 1f;
            public const float CannonPitch = 0.0f;   // adimensional, no modifica el tono
            public const float Cooldown = 0.75f;      // segundo

            // HPs segun sea Medium player o Medium NPC
            public const float PlayerHealth = 20f;
            public const float EnemyHealth = 6f;
        }

        public static class Heavy
        {
            // Identidad compartida
            public const float MaxSpeed = 70f;        // m/s
            public const float MotorForce = 130000f;  //
            public const float TurnSpeed = 0.9f;      //
            public const float AttackDamage = 2.0f;
            public const float CannonPitch = -1.00f;  // adimensional, modifica el tono hacia grave
            public const float Cooldown = 1.5f;       // segundo

            // HPs segun sea Heavy player o Heavy NPC
            public const float PlayerHealth = 30f;
            public const float EnemyHealth = 8f;
        }
    }

    // =========================================================================
    // CONFIGURACION GENERAL (Comun a todos los tanques, sin importar la clase)
    // =========================================================================
    public static class Tank
    {
        public const float TankScale = 1f;      // metros
        public const float Length = 2f;         // metros
        public const float Width = 2f;          // metros
        public const float Height = 2.25f;      // metros
        public const float ChassisMass = 2000f; // kg (tanque real ~60t, ya lo vamos a ir ajustando)
        public const float TurretMass = 500f;   // kg
        public const float VerticalSpeed = 25f; // m/s (~90 km/h) para God Mode
        public const float Acceleration = 40f;  // m/s²
        public const float Friction = 0.95f;    // coeficiente por frame
        public const float SpawnZMargin = 7f;   // metros, el tanque spawnea esta altura por encima del terreno
        public const float MaxFuel = 100f;              // litros
        public const float FuelConsumptionRate = 1f;    // litros
        public const float AngularVelocityClampX = 0.5f;    // radianes/segundo
        public const float AngularVelocityClampZ = 0.3f;    // radianes/segundo
        public const float AngularDampingXZ = 0.88f;        // adimensional
        public const float AngularDampingY = 0.98f;         // adimensional
        public const float VisualOffsetY = -0.35f;          // metros

        public const float CannonMuzzleOffsetY = 1.5f;  // metros
        public const float CannonMuzzleOffsetZ = 2.0f;  // metros
        public const float MinCannonPitch = -10f;  // grados
        public const float MaxCannonPitch = 25f;   // grados
        public const float CannonSpawnOffsetForward = 1f;   // factor multiplicador
        public const float CannonSpawnOffsetUp = 0f;        // factor de suma

        public const float ImpactRadius = 0.65f; // metros, radio de deformacion por impacto
        public const float ImpactDepth = 0.3f;   // metros

        public static string TankModelPath = "tanques/tank v7";
        public static string TankTracksTexture = "tracks_3_252525";

        public static class Stabilizer
        {
            public const float Width = 2.2f;    // metros
            public const float Height = 0.3f;   // metros
            public const float Length = 2.2f;   // metros
            public const float Mass = 6000f;    // kg
            public const float YOffset = -0.9f; // metros
        }

        //Bepu
        public const float PhysicsChassisWidth = 2f;    // metros
        public const float PhysicsChassisLength = 2f;   // metros
        public const float PhysicsChassisHeight = 1.2f; // metros
        public const float PhysicsTurretWidth = 1.4f;   // metros
        public const float PhysicsTurretLength = 1.4f;  // metros
        public const float PhysicsTurretHeight = 1f;    // metros
        public const float PhysicsTurretOffsetY = PhysicsChassisHeight;     // justo encima del chasis
        public const float ForwardDrag = 5000f;         // Coeficiente de arrastre (para velocidad terminal = MotorForce / Drag)
        public const float LateralDrag = 250000f;       // evitar derrape
    }

    // =========================================================================
    // CONFIGURACION DE ENEMIGOS (Solo lo que NO depende de la clase del tanque)
    // =========================================================================
    public static class Enemies
    {
        public const int EnemiesCount = 20;         // cantidad de enemigos en el mapa
        public const int KillsToWin = 10;           // cantidad de enemigos a matar para ganar
        public const float AttackRadius = 50f;      // metros
        public const float Cooldown = 1.0f;         // segundos

        public const float AttackStopDistance = 8f;         // metros
        public const float AttackFireDistance = 5f;         // metros
        public const float CannonSpawnOffsetForward = 3f;   // metros
        public const float CannonSpawnOffsetUp = 2f;        // metros
        public const float SpawnMapMargin = 20f;            // metros
    }

    public static class CannonBall
    {
        public const float Radius = 0.2f;           // metros
        public const float LifetimeSeconds = 5f;    // segundos
        public const float InitialVelocity = 25f;   // metros/segundo
        public const float VisualScale = 0.010f;    // adimensional
    }

    // TERRENO
    public static class Terrain
    {
        public const float CellSizeMeters = 1f;     // 1 pixel del heightmap = 1 CellSizeMeters (en metros)
        public const float TextureTileSize = 20f;   // metros, tamano de la textura en el terreno
        public const float MaxHeightMeters = 35f;   // relieve maximo
        public const float PhysicsMargin = 0.2f;    // margen de seguridad para Bepu
        public const int PhysicsSubsampleStep = 4;  // cuanto dividir la resolucion del heightmap para el mesh de Bepu , (1, 2, 4, 8, ...)
                                                    // PhysicsSubsampleStep = 1 --> 512x512 quads (~500.000 _triangulos_)
    }

    // CAMARA
    public static class Camera
    {
        // --- DISTANCIAS Y POSICION ---
        public const float DefaultDistance = 10f;           // metros, distancia inicial de la camara al tanque
        public const float MinDistance = 1.5f;              // metros, zoom maximo (vista comandante)
        public const float MaxDistance = 35f;               // metros, zoom mínimo (vista mas lejana)

        // --- ALTURA DE LA CAMARA ---
        public const float DefaultHeightOffset = 4.5f;      // metros, altura sobre el tanque en vista normal
        public const float HatchHeightOffset = 2.4f;        // metros, altura en modo escotilla (comandante)

        public const float HatchPositionForwardOffset = 0.8f; // metros, poner la camara adelantada hacia el canon

        // --- PUNTO DE MIRA (LOOK AT) ---
        public const float DefaultLookAtHeight = 3.0f;      // metros, altura a la que mira en vista normal
        public const float HatchLookAtForward = 3.5f;       // metros, desplazamiento hacia adelante en modo escotilla

        // --- TRANSICION A MODO ESCOTILLA ---
        public const float HatchTransitionDistance = 4.0f;  // metros, umbral donde empieza la transicion a escotilla

        // --- ZOOM Y SUAVIZADO ---
        public const float ZoomSensitivity = 1.5f;          // metros/scroll, sensibilidad de la rueda del mouse
        public const float Smoothness = 10f;                // 1/segundos, factor de suavizado del movimiento (Lerp)

        // --- PROYECCION ---
        public const float NearPlaneDist = 0.1f;            // metros, plano de corte cercano (evita clipping del canon)
        public const float FarPlaneDist = 250f;             // metros, plano de corte lejano

        // --- EFECTOS ---
        public const float ShakeIntensity = 1.025f;         // metros, amplitud del camera-shake al recibir daño
        public const float ShakeDuration = 0.100f;          // segundos, duracion del camera-shake

        // --- LIMITES ---
        public const float TerrainClampOffset = 1.0f;       // metros, altura minima de la cámara sobre el terreno
    }

    // ASSETS (CASAS, DECORACIONES)
    public static class Assets
    {
        public const float DefaultScale = 1f;       // exportar 1:1 desde Blender
        public const float MinSpacingMeters = 10f;
        public const float HouseScale = 2f;
        public const float HouseChamberScale = 0.1f;
        public const float DecorationScale = 1.5f;
        public const float DecorationChamberScale = DecorationScale / 100f;
        public const float DynamicSpawnOffset = 0.5f;
    }

    // POWERUPS - BARRIL DE COMBUSTIBLE
    public static class FuelBarrel
    {
        public const int SpawnCount = 30;               // cantidad de barriles en el mapa
        public const float FuelAmount = 25f;            // litros
        public const float RechargeDuration = 1f;       // segundos
        public const float CollectionDistance = 2.5f;   // metros
        public const float Radius = 0.5f;               // metros
        public const float Height = 2f;                 // metros
    }

    // CONFIGURACION DE AUDIO (Volumenes en escala del 1 al 100)
    // El SoundManager divide este valor por 100f para obtener el rango 0.0f - 1.0f nativo de MonoGame
    public static class Audio
    {
        public static class Music
        {
            public const float Menu = 50f;
            public const float Level1 = 5f;
        }

        public static class Sfx
        {
            public const float CannonFire = 30f;
            public const float ColisionCasa = 50f;
            public const float ImpactoMedianaEscala = 50f;
            public const float AgarrarCombustible1 = 90f;
            public const float AgarrarCombustible2 = 50f;
            public const float Viento = 50f;
            public const float CooldownNotReady = 50f;
            public const float Escalera = 50f;
            public const float EnemyCannonFire = 50f;
            public const float PlantaRodadora = 50f;
            public const float CarroceriaAvanzando1 = 50f;
            public const float CarroceriaAvanzando2 = 50f;
            public const float CarroceriaAvanzando3 = 50f;
            public const float CarroceriaAvanzando4 = 50f;
            public const float BajoCombustible1 = 50f;
            public const float BajoCombustible2 = 50f;
            public const float RotarTorreta = 50f;
            public const float Klaxon = 50f;
            public const float Fuego = 50f;
            public const float GolpearArbol = 50f;
            public const float GolpearRoca = 50f;
            public const float PlayerMuere = 50f;
        }
    }
}