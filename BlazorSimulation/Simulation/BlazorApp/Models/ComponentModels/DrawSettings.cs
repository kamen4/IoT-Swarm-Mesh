using Microsoft.AspNetCore.Components;

namespace BlazorApp.Models.ComponentModels;

public class DrawSettings
{

    public bool AreVisibilitiesVisible { get; set; } = true;
    public bool AreConnectionsVisible { get; set; } = true;
    public bool ArePacketsVisible { get; set; } = true;
}
