using Andre.Core;

namespace StudioCore.Application;
public static class ProjectPlatformMethods
{
    public static Platform? AsAndrePlatform(this ProjectPlatform p)
        => p switch
        {
            ProjectPlatform.PC => Platform.PC,
            ProjectPlatform.PS3 => Platform.PS3,
            ProjectPlatform.Xbox360 => Platform.Xbox360,
            _ => null
        };
}