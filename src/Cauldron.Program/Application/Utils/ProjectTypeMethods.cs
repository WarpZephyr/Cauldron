using Andre.Core;
using SoulsFormats;

namespace StudioCore.Application;
public static class ProjectTypeMethods
{
    public static BHD5.Game? AsBhdGame(this ProjectType p)
        => p switch
        {
            ProjectType.ACV => BHD5.Game.DarkSouls1,
            ProjectType.ACVD => BHD5.Game.DarkSouls1,
            _ => null
        };

    public static Game? AsAndreGame(this ProjectType p)
        => p switch
        {
            ProjectType.ACFA => Game.ACFA,
            ProjectType.ACV => Game.ACV,
            ProjectType.ACVD => Game.ACVD,
            _ => null
        };

    public static bool IsLooseGame(this ProjectType p)
    {
        switch (p)
        {
            case ProjectType.ACFA:
                return true;
        }

        return false;
    }

    public static bool IsPackedGame(this ProjectType p)
    {
        switch (p)
        {
            case ProjectType.ACV:
            case ProjectType.ACVD:
                return true;
        }

        return false;
    }
}