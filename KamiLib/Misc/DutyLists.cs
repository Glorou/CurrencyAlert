﻿using System.Collections.Generic;
using System.Linq;
using Dalamud;
using KamiLib.Caching;
using Lumina.Excel.GeneratedSheets;

namespace KamiLib.Misc;

public enum DutyType
{
    Savage,
    Ultimate,
    ExtremeUnreal,
    Criterion,
    Alliance,
    None,
}

public class DutyLists
{
    private List<uint> Savage { get; }
    private List<uint> Ultimate { get; }
    private List<uint> ExtremeUnreal { get; }
    private List<uint> Criterion { get; }
    private List<uint> Alliance { get; }

    private static DutyLists? _instance;
    public static DutyLists Instance => _instance ??= new DutyLists();

    private DutyLists()
    {
        // ContentType.Row 5 == Raids
        Savage = LuminaCache<ContentFinderCondition>.Instance.OfLanguage(ClientLanguage.English)
            .Where(t => t.ContentType.Row == 5)
            .Where(t => t.Name.RawString.Contains("Savage"))
            .Select(r => r.TerritoryType.Row)
            .ToList();
        
        // ContentType.Row 28 == Ultimate Raids
        Ultimate = LuminaCache<ContentFinderCondition>.Instance
            .Where(t => t.ContentType.Row == 28)
            .Select(t => t.TerritoryType.Row)
            .ToList();
        
        // ContentType.Row 4 == Trials
        ExtremeUnreal = LuminaCache<ContentFinderCondition>.Instance.OfLanguage(ClientLanguage.English)
            .Where(t => t.ContentType.Row == 4)
            .Where(t => t.Name.RawString.Contains("Extreme") || t.Name.RawString.Contains("Unreal") || t.Name.RawString.Contains("The Minstrel"))
            .Select(t => t.TerritoryType.Row)
            .ToList();

        Criterion = LuminaCache<ContentFinderCondition>.Instance
            .Where(row => row.ContentType.Row is 30)
            .Select(row => row.TerritoryType.Row)
            .ToList();
        
        Alliance = LuminaCache<TerritoryType>.Instance
            .Where(r => r.TerritoryIntendedUse is 8)
            .Select(r => r.RowId)
            .ToList();
    }

    private DutyType GetDutyType(uint dutyId)
    {
        if (Savage.Contains(dutyId)) return DutyType.Savage;
        if (Ultimate.Contains(dutyId)) return DutyType.Ultimate;
        if (ExtremeUnreal.Contains(dutyId)) return DutyType.ExtremeUnreal;
        if (Criterion.Contains(dutyId)) return DutyType.Criterion;
        if (Alliance.Contains(dutyId)) return DutyType.Alliance;

        return DutyType.None;
    }

    public bool IsType(uint dutyId, DutyType type) => GetDutyType(dutyId) == type;
    public bool IsType(uint dutyId, IEnumerable<DutyType> types) => types.Any(type => IsType(dutyId, type));
}