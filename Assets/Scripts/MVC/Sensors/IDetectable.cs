namespace Game.Sensors
{
    public enum DetectableType
    {
        Aliado,
        Enemigo,
        Interactuable,
        Proyectil
    }

    public interface IDetectable
    {
        string GetName();
        DetectableType GetDetectableType();
        UnityEngine.Transform GetTransform();
    }
}
