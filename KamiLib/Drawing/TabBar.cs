﻿using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using KamiLib.Interfaces;

namespace KamiLib.Drawing;

public class TabBar : IDrawable
{
    private readonly List<ITabItem> tabs = new();
    private readonly string tabBarID;
    private readonly Vector2 childSize;

    public TabBar(string id, Vector2? size = null)
    {
        tabBarID = id;
        childSize = size ?? Vector2.Zero;
    }
    
    public void AddTab(ITabItem tab) => tabs.Add(tab);
    public void AddTab(IEnumerable<ITabItem> multipleTabs) => tabs.AddRange(multipleTabs);

    public void Draw()
    {
        ImGui.PushID(tabBarID);
        
        if (ImGui.BeginTabBar($"###{KamiCommon.PluginName}TabBar", ImGuiTabBarFlags.NoTooltip))
        {
            foreach (var tab in tabs)
            {
                if(tab.Enabled == false) continue;

                if (ImGui.BeginTabItem(tab.TabName))
                {
                    if (ImGui.BeginChild($"###{KamiCommon.PluginName}TabBarChild", childSize, false, ImGuiWindowFlags.NoScrollbar)) 
                    {
                        ImGui.PushID(tab.TabName);
                        tab.Draw();
                        ImGui.PopID();
                    }
                    ImGui.EndChild();

                    ImGui.EndTabItem();
                }
            }
        }
        
        ImGui.PopID();
    }
}