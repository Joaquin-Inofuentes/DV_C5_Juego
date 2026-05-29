namespace Game.Squad
{
    /// <summary>
    /// Interfaz base para todos los estados de la FSM de un soldado (SOLID - OCP).
    /// </summary>
    public interface ISoldierState
    {
        void Enter(SoldierController controller);
        void Update(SoldierController controller);
        void FixedUpdate(SoldierController controller);
        void Exit(SoldierController controller);
    }
}
