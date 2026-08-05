public interface ISystem
{
    void Initialize(EcsWorld world);
    void Tick(float deltaTime);
    void Dispose();
}
