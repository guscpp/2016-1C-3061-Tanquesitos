using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TGC.MonoGame.TP.Models;

public class DamageNumber
{
    private Vector2 _screenPosition;
    private float _value;
    private float _timer;
    private const float Duration = 1.0f; //segundos
    private const float RiseSpeed = 50f; //pixeles por segundo

    public bool IsDead => _timer <= 0f;

    public DamageNumber(Vector3 worldPosition, float value, Viewport viewport, Matrix view, Matrix projection)
    {
        _value = value;
        _timer = Duration;

        //proyeccion inicial de 3D a 2D usando la camara actual
        Vector3 projected = viewport.Project(worldPosition, projection, view, Matrix.Identity);
        _screenPosition = new Vector2(projected.X, projected.Y);
    }

    public void Update(float dt)
    {
        _timer -= dt;
        _screenPosition.Y -= RiseSpeed * dt;  //se resta para que suba porque negativo es arriba :*)
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        if (IsDead) return;

        //alpha va de 1 (opaco) a 0 (transparente) durante la duracion
        float alpha = MathHelper.Clamp(_timer / Duration, 0f, 1f);

        Color textColor = new Color(1f, 0.2f, 0.2f, alpha);      // Rojo sangre
        Color shadowColor = new Color(0f, 0f, 0f, alpha * 0.8f); // Sombra negra

        string text = ((int)_value).ToString();
        Vector2 textSize = font.MeasureString(text);
        Vector2 origin = textSize / 2f; //centro el origen por si queremos escalarlo

        //sombra
        spriteBatch.DrawString(font, text, _screenPosition + new Vector2(2, 2), shadowColor, 0f, origin, 1f, SpriteEffects.None, 0f);
        //texto principal
        spriteBatch.DrawString(font, text, _screenPosition, textColor, 0f, origin, 1f, SpriteEffects.None, 0f);
    }
}