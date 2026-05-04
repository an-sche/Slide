namespace Slide;

public interface IEnemyBehavior
{
    void Process(float delta, Enemy enemy);
    void Draw(Enemy enemy) { }
}
