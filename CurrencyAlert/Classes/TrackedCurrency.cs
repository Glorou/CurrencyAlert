﻿using System;
using System.Linq;
using System.Text.Json.Serialization;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;

namespace CurrencyAlert.Classes;

public enum CurrencyType {
    Item,
    HighQualityItem,
    Collectable,
    NonLimitedTomestone,
    LimitedTomestone,
}

public unsafe class TrackedCurrency {
    private uint? iconId;
    private uint? itemId;
    private string? label;

    public required CurrencyType Type { get; init; }

    [JsonIgnore] public IDalamudTextureWrap Icon => Service.TextureProvider.GetFromGameIcon(new GameIconLookup {
        HiRes = true, ItemHq = Type is CurrencyType.HighQualityItem, IconId = IconId,
    }).GetWrapOrEmpty();

    public uint ItemId {
        get => GetItemId();
        init => itemId = value;
    }

    public uint IconId {
        get => iconId ??= Service.DataManager.GetExcelSheet<Item>()!.GetRow(ItemId)?.Icon ?? 0;
        set => iconId = value;
    }

    public required int Threshold;

    public bool Enabled = true;

    public bool ChatWarning;

    public bool ShowInOverlay;

    public bool ShowItemName = true;

    public bool Invert;

    public SeString OverlayWarningText = "Above Threshold";

    [JsonIgnore] public string Name => label ??= Service.DataManager.GetExcelSheet<Item>()!.GetRow(ItemId)?.Name ?? "Unable to read name";

    [JsonIgnore] public bool CanRemove => Type is not (CurrencyType.LimitedTomestone or CurrencyType.NonLimitedTomestone);

    [JsonIgnore] public int CurrentCount => InventoryManager.Instance()->GetInventoryItemCount(ItemId, Type is CurrencyType.HighQualityItem, false, false);

    [JsonIgnore] public bool HasWarning => Invert ? CurrentCount < Threshold : CurrentCount > Threshold;

    private uint GetItemId() {
        itemId ??= Type switch {
            CurrencyType.NonLimitedTomestone => Service.DataManager.GetExcelSheet<TomestonesItem>()!.First(item => item.Tomestones.Row is 2).Item.Row,
            CurrencyType.LimitedTomestone => Service.DataManager.GetExcelSheet<TomestonesItem>()!.FirstOrDefault(item => item.Tomestones.Row is 3)?.Item.Row ?? 0,
            _ => throw new Exception($"ItemId not initialized for type: {Type}"),
        };

        return itemId.Value;
    }
}