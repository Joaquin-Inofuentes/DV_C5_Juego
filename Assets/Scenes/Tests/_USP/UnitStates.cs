namespace Game.Squad
{
    public interface IUnitState
    {
        void Enter(UnitController unit);
        void Update(UnitController unit);
        void FixedUpdate(UnitController unit);
        void Exit(UnitController unit);
    }
}