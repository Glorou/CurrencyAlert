﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Utility;
using ImGuiNET;
using KamiLib.Caching;
using KamiLib.Configuration;
using KamiLib.Drawing;
using KamiLib.Extensions;
using KamiLib.Localization;
using Lumina.Excel.GeneratedSheets;

namespace KamiLib.Blacklist;

public static class BlacklistDraw
{
    private static readonly List<uint> EntriesToRemove = new();
    private static readonly List<uint> EntriesToAdd = new();
    private static string _searchString = string.Empty;
    private static List<SearchResult>? _searchResults = new();
    
    public static void DrawBlacklist(Setting<List<uint>> blacklistedAreas)
    {
        InfoBox.Instance
            .AddTitle(Strings.Blacklist_CurrentlyBlacklisted, out var innerWidth, 1.0f)
            .AddDummy(5.0f)
            .AddAction(() => BlacklistedAreasList(blacklistedAreas))
            .AddDisabledButton(EntriesToRemove.Count == 0 ? Strings.Blacklist_ClearBlacklist : Strings.Blacklist_RemoveSelectedAreas.Format(EntriesToRemove.Count), () =>
            {
                if (EntriesToRemove.Count == 0)
                {
                    blacklistedAreas.Value.Clear();
                    KamiCommon.SaveConfiguration();
                }
                else
                {
                    blacklistedAreas.Value.RemoveAll(entry => EntriesToRemove.Contains(entry));
                    EntriesToRemove.Clear();
                    KamiCommon.SaveConfiguration();
                }
            }, !ImGui.GetIO().KeyShift, Strings.DisabledButton_HoldShift, innerWidth)
            .Draw();
    }

    public static void DrawAddRemoveHere(Setting<List<uint>> blacklistedZones)
    {
        InfoBox.Instance
            .AddTitle(Strings.Blacklist_AddRemoveZone, 1.0f)
            .AddAction(() => LuminaCache<TerritoryType>.Instance.GetRow(Service.ClientState.TerritoryType)?.DrawLabel())
            .BeginTable()
            .BeginRow()
            .AddDisabledButton(Strings.Common_Add, () =>
                { 
                    Add(blacklistedZones, Service.ClientState.TerritoryType);
                }, blacklistedZones.Value.Contains(Service.ClientState.TerritoryType), buttonSize: InfoBox.Instance.InnerWidth / 2.0f - 5.0f * ImGuiHelpers.GlobalScale)
            .AddDisabledButton(Strings.Common_Remove, () =>
                {
                    Remove(blacklistedZones, Service.ClientState.TerritoryType);
                }, !blacklistedZones.Value.Contains(Service.ClientState.TerritoryType), buttonSize: InfoBox.Instance.InnerWidth / 2.0f - 5.0f * ImGuiHelpers.GlobalScale)
            .EndRow()
            .EndTable()
            .Draw();
    }

    public static void DrawTerritorySearch(Setting<List<uint>> blacklistedZones)
    {
        InfoBox.Instance
            .AddTitle(Strings.Blacklist_ZoneSearch, out var innerWidth, 1.0f)
            .AddAction(() =>
            {
                ImGui.PushItemWidth(InfoBox.Instance.InnerWidth);
                if (ImGui.InputTextWithHint("###TerritorySearch", Strings.Blacklist_Search, ref _searchString, 60, ImGuiInputTextFlags.AutoSelectAll))
                {
                    _searchResults = Search(_searchString, 5);
                    PluginLog.Debug("Updating TerritorySearch Results");
                }
            })
            .AddAction(() => DisplayResults(_searchResults))
            .AddDisabledButton(Strings.Blacklist_AddSelectedAreas.Format(EntriesToAdd.Count), () =>
            {
                blacklistedZones.Value.AddRange(EntriesToAdd);
                EntriesToAdd.Clear();
                KamiCommon.SaveConfiguration();
                
            }, !EntriesToAdd.Any(), Strings.Blacklist_SelectZones, innerWidth)
            .Draw();
    }

    public static void PrimeSearch()
    {
        _searchResults = Search("", 5);
    }
    
    private static List<SearchResult> Search(string searchTerms, int numResults)
    {
        return Service.DataManager.GetExcelSheet<TerritoryType>()!
            .Where(territory => territory.PlaceName.Row is not 0)
            .Where(territory => territory.PlaceName.Value is not null)
            .GroupBy(territory => territory.PlaceName.Value!.Name.ToDalamudString().TextValue)
            .Select(territory => territory.First())
            .Where(territory => territory.PlaceName.Value!.Name.ToDalamudString().TextValue.ToLower().Contains(searchTerms.ToLower()))
            .Select(territory => new SearchResult {
                TerritoryID = territory.RowId
            })
            .OrderBy(searchResult => searchResult.TerritoryName)
            .Take(numResults)
            .ToList();
    }

    private static void DisplayResults(List<SearchResult>? results)
    {
        if (results is null) return; 
        
        if (ImGui.BeginChild("###SearchResultsChild", new Vector2(InfoBox.Instance.InnerWidth, 21.0f * 5 * ImGuiHelpers.GlobalScale )))
        {
            foreach (var result in results)
            {
                if (ImGui.Selectable($"###SearchResult{result.TerritoryID}", EntriesToAdd.Contains(result.TerritoryID)))
                {
                    if (!EntriesToAdd.Contains(result.TerritoryID))
                    {
                        EntriesToAdd.Add(result.TerritoryID);
                    }
                    else
                    {
                        EntriesToAdd.Remove(result.TerritoryID);
                    }
                }
                    
                ImGui.SameLine();
                LuminaCache<TerritoryType>.Instance.GetRow(result.TerritoryID)?.DrawLabel();
            }
        }
        ImGui.EndChild();
    }

    private static void BlacklistedAreasList(Setting<List<uint>> blacklistedAreas)
    {
        var itemCount = Math.Min(blacklistedAreas.Value.Count, 10);
        var listHeight = itemCount * ImGuiHelpers.GlobalScale * 21.0f;
        var minHeight = 21.0f * ImGuiHelpers.GlobalScale;

        var size = new Vector2(InfoBox.Instance.InnerWidth, MathF.Max(listHeight, minHeight));
        
        if(ImGui.BeginChild("###BlacklistFrame", size, false))
        {
            if (!blacklistedAreas.Value.Any())
            {
                ImGui.SetCursorPos(ImGui.GetCursorPos() with { X = ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize(Strings.Blacklist_Empty).X / 2.0f});
                ImGui.TextColored(Colors.Orange, Strings.Blacklist_Empty);
            }
            else
            {
                DrawBlacklistedAreas(blacklistedAreas);
            }
        }
        ImGui.EndChild();
    }

    private static void DrawBlacklistedAreas(Setting<List<uint>> blacklistedAreas)
    {
        var territories = blacklistedAreas.Value
            .Select(area => LuminaCache<TerritoryType>.Instance.GetRow(area))
            .OfType<TerritoryType>()
            .OrderBy(territory => territory.GetPlaceNameString());
        
        foreach (var territory in territories)
        {
            ImGui.PushItemWidth(InfoBox.Instance.InnerWidth);
            if (ImGui.Selectable($"###{territory}", EntriesToRemove.Contains(territory.RowId)))
            {
                if (!EntriesToRemove.Contains(territory.RowId))
                {
                    EntriesToRemove.Add(territory.RowId);
                }
                else
                {
                    EntriesToRemove.Remove(territory.RowId);
                }
            }
            
            ImGui.SameLine();
            territory.DrawLabel();
        }
    }
    
    private static void Add(Setting<List<uint>> zones, uint id)
    {
        if (!zones.Value.Contains(id))
        {
            zones.Value.Add(id);
            KamiCommon.SaveConfiguration();
        }
    }

    private static void Remove(Setting<List<uint>> zones, uint id)
    {
        if (zones.Value.Contains(id))
        {
            zones.Value.Remove(id);
            KamiCommon.SaveConfiguration();
        }
    }
}