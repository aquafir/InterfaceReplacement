using AcClient;
using ACEditor;
using ImGuiNET;
using System;
using System.IO;
using System.Linq;
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

    Vector2 MIN_SIZE = new(200, 400);
    Vector2 MAX_SIZE = new(1000, 900);

    readonly InventoryHud backpack;

    public InventoryUI()
    {
        //g.World.OnChatInput += World_OnChatInput;
        
        // Create a new UBService Hud
        hud = UBService.Huds.CreateHud("InventoryUI");
        hud.WindowSettings = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoScrollbar;

        // set to show our icon in the UBService HudBar
        hud.ShowInBar = true;
        hud.Visible = true;

        // subscribe to the hud render event so we can draw some controls
        hud.OnPreRender += Hud_OnPreRender;
        hud.OnRender += Hud_OnRender;

        backpack = new(hud);
    }

    //unsafe private void World_OnChatInput(object sender, UtilityBelt.Scripting.Events.ChatInputEventArgs e)
    //{
    //    if (e.Text != "/qd")
    //        return;

    //    C.Chat($"Dropping all");
    //    e.Eat = true;
    //    //foreach (var item in UBService.Scripts.GameState.Character.Weenie.AllItemIds)
    //    foreach (var item in g.Character.Inventory.Select(x => x.Id))
    //    {
    //        using (var stream = new MemoryStream())
    //        using (var writer = new BinaryWriter(stream))
    //        {
    //            writer.Write((uint)0xF7B1); // order header
    //            writer.Write((uint)0x0); // sequence.. ace doesnt verify this
    //            writer.Write((uint)0x001B); // drop item
    //            writer.Write((uint)item);
    //            var bytes = stream.ToArray();
    //            fixed (byte* bytesPtr = bytes)
    //            {
    //                Proto_UI.SendToControl((char*)bytesPtr, bytes.Length);
    //            }
    //        }
    //    }
    //}

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
            backpack.Draw();
        }
        catch (Exception ex)
        {
            PluginCore.Log(ex);
            //PluginCore.Log($"EX:\n{ex.Message}\nInner:{ex.InnerException}\nStack{ex.StackTrace}");
        }
    }

    public void Dispose()
    {
        try
        {
            backpack?.Dispose();
            hud?.Dispose();
        }catch(Exception ex)
        {

        }
    }
}
