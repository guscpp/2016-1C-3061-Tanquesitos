namespace TGC.MonoGame.TP.Models.Particulas;

public class ParticlesPool
{
    private int _maxParticles = 30; // size de cada pool - 30 particulas de cada tipo 
    public int MaxParticles => _maxParticles;
    public SmokeParticle[] _particulasHumo { get; private set; }
    public ExplosionParticle[] _particulasExplosion { get; private set; }
    public DustParticle[] _particulasPolvo { get; private set; }

    public enum ParticleType
    {
        HUMO,
        FUEGO,
        POLVO
    };

    public void Initialize()
    {
        // reserva del pool :)
        _particulasExplosion = new ExplosionParticle[_maxParticles];
        _particulasHumo = new SmokeParticle[_maxParticles];
        _particulasPolvo = new DustParticle[_maxParticles];
        for(int i=0; i<_maxParticles; i++)
        {
            _particulasExplosion[i] = new ExplosionParticle();
            _particulasHumo[i] = new SmokeParticle();
            _particulasPolvo[i] = new DustParticle();
        }
    }

    public Particle GetParticle(ParticleType tipo)
    {
        switch(tipo)
        {
            case ParticleType.HUMO:
                {
                    foreach(var particle in _particulasHumo)
                    {
                        if(!particle.IsAlive) return particle;
                    }
                    break;   
                }
            case ParticleType.FUEGO:
                {
                    foreach(var particle in _particulasExplosion
            )
                    {
                        if(!particle.IsAlive) return particle;
                    }
                    break; 
                }
            case ParticleType.POLVO:
                {
                    foreach(var particle in _particulasPolvo)
                    {
                        if(!particle.IsAlive) return particle;
                    }
                    break; 
                }
        }
        return null;
    }
}