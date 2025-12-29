using System.ComponentModel.DataAnnotations;

namespace StudioCore.Application;

public enum ProjectPlatform
{
    [Display(Name = "Undefined")]
    Undefined = 0,

    [Display(Name = "PC")]
    PC = 1, // PC

    [Display(Name = "Playstation 3")]
    PS3 = 2, // Playstation 3

    [Display(Name = "Xbox 360")]
    Xbox360 = 3, // Xbox 360
}