namespace Game.Sensors
{
    public enum DetectableType
    {
        Aliado,
        Enemigo,
        Interactuable,
        Proyectil,
        Invisible   // Usado por soldados caídos — los detectores enemigos lo ignoran
    }

    public interface IDetectable
    {
        string GetName();
        DetectableType GetDetectableType();
        UnityEngine.Transform GetTransform();
    }
}
