using System.Linq;

namespace Slide;

public class PlayerState
{
    public int   PlayerLevel     { get; set; } = 1;
    public int[] AbilityLevels   { get; }      = new int[5];
    public int   SpentPoints     => AbilityLevels.Sum();
    public int   AvailablePoints => PlayerLevel - SpentPoints;

    public void Reset()
    {
        PlayerLevel = 1;
        System.Array.Fill(AbilityLevels, 0);
    }
}
