using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Models.Particulas;

public class DustParticle : Particle
{
    public void Initialize(Vector3 position, Vector3 tankRight, float time)
    {
        genTime = time;
        _currentPos = position;
        _isAlive = true;
        _size = 0.3f;
        _color = Color.Goldenrod;
        maxTime = 0.8f;
        float randomSide = (random.NextSingle() - 0.5f) * 2f;
        Velocity = (tankRight * randomSide + Vector3.Down * 0.5f) * 2f;
    }

    public void Update(GameTime gameTime)
    {
        var totaltime = (float)gameTime.TotalGameTime.TotalSeconds;
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (!_isAlive) return;
        var currentTime = totaltime - genTime;
        if (currentTime >= maxTime)
        {
            Reset();
            return;
        }
        var lifePercent = currentTime / maxTime;
        _alpha = 1f - lifePercent;
        _size = MathHelper.Lerp(0.3f, 1.5f, lifePercent);

        Velocity *= (1f - 3f * dt);
        Velocity += Vector3.Up * 0.3f * dt;

        _currentPos += Velocity * dt;
    }
}