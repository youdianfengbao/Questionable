using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Questionable.Model;
using Questionable.Model.Common;
using Questionable.Model.Questing;

namespace Questionable.Data;

[SuppressMessage("Performance", "CA1822")]
internal sealed class AlliedSocietyData
{
    public ReadOnlyDictionary<ushort, AlliedSocietyMountConfiguration> Mounts { get; } =
        new Dictionary<ushort, AlliedSocietyMountConfiguration>
        {
            { 66, new([1016093, 1016087], EAetheryteLocation.SeaOfCloudsOkZundu) },
            { 79, new([1017031, 1016837, 1016838], EAetheryteLocation.DravanianForelandsAnyxTrine) },
            { 88, new([1017470, 1017432], EAetheryteLocation.ChurningMistsZenith) },
            { 89, new([1017322], EAetheryteLocation.ChurningMistsZenith) },
            { 147, new([1024777,1024912], EAetheryteLocation.FringesPeeringStones) },
            { 369, new([1051798], EAetheryteLocation.KozamaukaDockPoga) },
            { 391, new([1052562], EAetheryteLocation.YakTelMamook) },
            { 24, new([1052562, 1008332], EAetheryteLocation.EastShroudHawthorneHut) }
        }.AsReadOnly();

    public EAlliedSociety GetCommonAlliedSocietyTurnIn(ElementId elementId)
    {
        if (elementId is QuestId questId)
        {
            return questId.Value switch
            {
                >= 1222 and <= 1251 => EAlliedSociety.Amaljaa, //ARR
                >= 1494 and <= 1523 => EAlliedSociety.Ixal, // Ixal also has 1566,1567,1568
                >= 1566 and <= 1568 => EAlliedSociety.Ixal, 
                >= 1325 and <= 1344 => EAlliedSociety.Kobolds, // Kobolds 1364-1373
                >= 1364 and <= 1373 => EAlliedSociety.Kobolds,
                >= 1257 and <= 1286 => EAlliedSociety.Sylphs,
                >= 2171 and <= 2200 => EAlliedSociety.VanuVanu, //HW
                >= 2261 and <= 2280 => EAlliedSociety.Vath,
                >= 2290 and <= 2319 => EAlliedSociety.Moogles,
                >= 3042 and <= 3069 => EAlliedSociety.Ananta, //Storm 
                >= 2979 and <= 3002 => EAlliedSociety.Kojin,
                >= 3103 and <= 3130 => EAlliedSociety.Namazu,
                >= 3902 and <= 3929 => EAlliedSociety.Dwarves, //SB
                >= 3689 and <= 3716 => EAlliedSociety.Pixies,
                >= 3806 and <= 3833 => EAlliedSociety.Qitari,
                >= 4551 and <= 4578 => EAlliedSociety.Arkasodara, //EW
                >= 4687 and <= 4714 => EAlliedSociety.Loporrits,
                >= 4607 and <= 4634 => EAlliedSociety.Omicrons,
                >= 5199 and <= 5226 => EAlliedSociety.Pelupelu, //Dawn
                >= 5261 and <= 5288 => EAlliedSociety.MamoolJa,
                >= 5336 and <= 5363 => EAlliedSociety.YokHuy,
                _ => EAlliedSociety.None,
            };
        }

        return EAlliedSociety.None;
    }

    public void GetCommonAlliedSocietyNpcs(EAlliedSociety alliedSociety, out uint[] normalNpcs, out uint[] mountNpcs)
    {
        if (alliedSociety == EAlliedSociety.VanuVanu)
        {
            normalNpcs = [1016088, 1016091, 1016092];
            mountNpcs = [1016093];
        }
        else if (alliedSociety == EAlliedSociety.Vath)
        {
            normalNpcs = [];
            mountNpcs = [1017031];
        }
        else if (alliedSociety == EAlliedSociety.Moogles)
        {
            normalNpcs = [];
            mountNpcs = [1017322, 1017470, 1017471];
        }
        else if (alliedSociety == EAlliedSociety.Qitari)
        {
            normalNpcs = [];
            mountNpcs = [1032663];
        }
        else if (alliedSociety == EAlliedSociety.Pelupelu)
        {
            normalNpcs = [];
            mountNpcs = [1051798];
        }
        else
        {
            normalNpcs = [];
            mountNpcs = [];
        }
    }
}

public sealed record AlliedSocietyMountConfiguration(IReadOnlyList<uint> IssuerDataIds, EAetheryteLocation ClosestAetheryte);
