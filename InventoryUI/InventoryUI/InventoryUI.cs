using ImGuiNET;
using System;
using System.Numerics;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Service;
using UtilityBelt.Service.Views;

namespace InventoryUI;
internal class InventoryUI : IDisposable
{
    /// <summary>
    /// The UBService Hud
    /// </summary>
    readonly Hud hud;
    readonly Game g = new();

    Vector2 MIN_SIZE = new(200, 200);
    Vector2 MAX_SIZE = new(1000, 900);

    readonly InventoryHud backpack;

    public InventoryUI()
    {
        // Create a new UBService Hud
        hud = UBService.Huds.CreateHud("InventoryUI");
        hud.WindowSettings = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoScrollbar;

        // set to show our icon in the UBService HudBar
        hud.ShowInBar = true;
        hud.Visible = false;

        // subscribe to the hud render event so we can draw some controls
        hud.OnPreRender += Hud_OnPreRender;
        hud.OnRender += Hud_OnRender;

        backpack = new(hud);
    }

    private void Hud_OnPreRender(object sender, EventArgs e)
    {
        ImGui.SetNextWindowSizeConstraints(MIN_SIZE, MAX_SIZE);
    }

    /// <summary>
    /// Called every time the ui is redrawing.
    /// </summary>
    private void Hud_OnRender(object sender, EventArgs e)
    {
        try
        {
            backpack.Render();
        }
        catch (Exception ex)
        {
            PluginCore.Log(ex);
        }
    }

    public void Dispose()
    {
        hud.Dispose();
        backpack?.Dispose();
    }
}
