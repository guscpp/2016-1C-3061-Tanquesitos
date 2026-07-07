using System.Numerics;
using System.Linq;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;

using TGC.MonoGame.TP;
using TGC.MonoGame.TP.Models.Decorations;
using TGC.MonoGame.TP.Models.Tanks;

public struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    private SpringSettings ContactSpringiness { get; set; }
    private float MaximumRecoveryVelocity { get; set; }
    private float FrictionCoefficient { get; set; }

    public NarrowPhaseCallbacks(SpringSettings contactSpringiness) : this(contactSpringiness, 2f, 1f)
    {
    }

    public NarrowPhaseCallbacks(SpringSettings contactSpringiness, float maximumRecoveryVelocity,
        float frictionCoefficient)
    {
        ContactSpringiness = contactSpringiness;
        MaximumRecoveryVelocity = maximumRecoveryVelocity;
        FrictionCoefficient = frictionCoefficient;
    }

    public void Initialize(Simulation simulation)
    {
        //Use a default if the springiness value wasn't initialized... at least until struct field initializers are supported outside of previews.
        if (ContactSpringiness.AngularFrequency == 0 && ContactSpringiness.TwiceDampingRatio == 0)
        {
            ContactSpringiness = new SpringSettings(30, 1);
            MaximumRecoveryVelocity = 2f;
            FrictionCoefficient = 1f;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b,
        ref float speculativeMargin)
    {
        //While the engine won't even try creating pairs between statics at all, it will ask about kinematic-kinematic pairs.
        //Those pairs cannot emit constraints since both involved bodies have infinite inertia. Since most of the demos don't need
        //to collect information about kinematic-kinematic pairs, we'll require that at least one of the bodies needs to be dynamic.
        return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
    {
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
        out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        pairMaterial.FrictionCoefficient = FrictionCoefficient;
        pairMaterial.MaximumRecoveryVelocity = MaximumRecoveryVelocity;
        pairMaterial.SpringSettings = ContactSpringiness;

        // =========================================================================
        // 1. COLISIÓN DINÁMICA vs DINÁMICA (Tanque/Bala vs Objetos/Enemigos)
        // =========================================================================
        if (pair.A.Mobility == CollidableMobility.Dynamic && pair.B.Mobility == CollidableMobility.Dynamic)
        {
            BodyHandle handleA = pair.A.BodyHandle;
            BodyHandle handleB = pair.B.BodyHandle;
            BodyHandle tankHandle = TGCGame.Instance._tank.TankHandler;

            // --- A. Tanque vs Objetos Dinámicos ---
            if (handleA == tankHandle || handleB == tankHandle)
            {
                BodyHandle obstacleHandle = (handleA == tankHandle) ? handleB : handleA;

                // ✅ BÚSQUEDA O(1) SIN LINQ
                if (TGCGame.Instance._dinamicsManager.DynamicDecorationsByHandle.TryGetValue(obstacleHandle, out var objetoChocado))
                {
                    if (!objetoChocado.IsDead && objetoChocado.esDestruible) objetoChocado.HandleCollision();
                }
            }

            // --- B. Lógica de Balas (Cannonball) ---
            Cannonball cannonball = null;
            if (TGCGame.Instance.CannonballManager.TryGetCannonball(handleA, out var cbA)) cannonball = cbA;
            else if (TGCGame.Instance.CannonballManager.TryGetCannonball(handleB, out var cbB)) cannonball = cbB;

            if (cannonball != null)
            {
                BodyHandle obstacleHandle = (cannonball.BodyHandle == handleA) ? handleB : handleA;

                // Bala vs Tanque
                if ((handleA == tankHandle || handleB == tankHandle) && !cannonball.IsDead)
                {
                    Vector3 bulletPos = TGCGame.Instance.CannonballManager.GetCannonballPosition(cannonball.BodyHandle).ToNumerics();
                    TGCGame.Instance._tank.HandleHealth(cannonball.AttackDamage, bulletPos);
                    cannonball.killCannonball();
                }

                // Bala vs Objetos Dinámicos
                // ✅ BÚSQUEDA O(1) SIN LINQ
                if (TGCGame.Instance._dinamicsManager.DynamicDecorationsByHandle.TryGetValue(obstacleHandle, out var objetoChocadoBala))
                {
                    if (!objetoChocadoBala.IsDead && objetoChocadoBala.esDestruible && !cannonball.IsDead)
                    {
                        objetoChocadoBala.HandleCollision();
                        cannonball.killCannonball();
                    }
                }

                // Bala vs Enemigos
                // ✅ BÚSQUEDA O(1) SIN LINQ
                if (TGCGame.Instance._enemiesManager.EnemiesByHandle.TryGetValue(obstacleHandle, out var enemyChocado))
                {
                    if (!enemyChocado.IsDead && !cannonball.IsDead)
                    {
                        var bulletPos = TGCGame.Instance.CannonballManager.GetCannonballPosition(cannonball.BodyHandle);
                        enemyChocado.HandleHealth(cannonball.AttackDamage, bulletPos);
                        cannonball.killCannonball();
                    }
                }
            }
        }

        // =========================================================================
        // 2. COLISIÓN DINÁMICA vs ESTÁTICA (Sonidos de impacto)
        // =========================================================================
        if ((pair.A.Mobility == CollidableMobility.Dynamic && pair.B.Mobility == CollidableMobility.Static) ||
            (pair.B.Mobility == CollidableMobility.Dynamic && pair.A.Mobility == CollidableMobility.Static))
        {
            BodyHandle dynamicHandle = pair.A.Mobility == CollidableMobility.Dynamic ? pair.A.BodyHandle : pair.B.BodyHandle;
            StaticHandle staticHandle = pair.A.Mobility == CollidableMobility.Static ? pair.A.StaticHandle : pair.B.StaticHandle;

            if (staticHandle == TGCGame.Instance.TerrainHandle)
            {
                // Ignorar terreno para no ensuciar el tracker de sonidos
            }
            else if (dynamicHandle == TGCGame.Instance._tank.TankHandler)
            {
                TGCGame.Instance.CollisionTracker.TryPlay(() =>
                {
                    // ✅ BÚSQUEDA O(1) PARA CASAS
                    if (TGCGame.Instance._housesManager.HousesByHandle.ContainsKey(staticHandle))
                    {
                        TGCGame.Instance.SoundManager.PlaySound("colision_casa");
                        return true;
                    }

                    // ✅ BÚSQUEDA O(1) + PATTERN MATCHING PARA DECORACIÓN ESTÁTICA
                    // Esto reemplaza los 3 "OfType<T>().Any()" que tenías antes
                    if (TGCGame.Instance._staticsManager.StaticsByHandle.TryGetValue(staticHandle, out var staticObj))
                    {
                        if (staticObj is Tree || staticObj is Cactus)
                        {
                            TGCGame.Instance.SoundManager.PlaySound("golpear_arbol");
                            return true;
                        }
                        if (staticObj is Rock)
                        {
                            TGCGame.Instance.SoundManager.PlaySound("golpear_roca");
                            return true;
                        }
                    }
                    return false;
                });
            }
        }

        // =========================================================================
        // 3. BALA vs TERRENO (Estático)
        // =========================================================================
        if (pair.A.Mobility == CollidableMobility.Static || pair.B.Mobility == CollidableMobility.Static)
        {
            var terreno = TGCGame.Instance.TerrainHandle;
            Cannonball cannonball = null;

            if (pair.A.Mobility == CollidableMobility.Static && pair.A.StaticHandle == terreno)
                TGCGame.Instance.CannonballManager.TryGetCannonball(pair.B.BodyHandle, out cannonball);
            if (pair.B.Mobility == CollidableMobility.Static && pair.B.StaticHandle == terreno)
                TGCGame.Instance.CannonballManager.TryGetCannonball(pair.A.BodyHandle, out cannonball);

            if (cannonball != null) cannonball.killCannonball();
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB,
        ref ConvexContactManifold manifold)
    {
        return true;
    }

    public void Dispose()
    {
        //Something to be dispose.
    }
}