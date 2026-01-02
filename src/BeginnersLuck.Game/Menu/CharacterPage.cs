using System;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Services;

namespace BeginnersLuck.Game.Menu;

public sealed class CharacterPage : ListDetailsPageBase
{
    public override string Title => "CHARACTER";
    public override string FooterHint => "BACK/B: CLOSE";

    private enum Row
    {
        Hp,
        MaxHp,
        Gold,
        Xp,
    }

    protected override int ItemCount(GameServices s) => Enum.GetValues<Row>().Length;

    protected override void GetRow(GameServices s, int index, out string left, out string right)
    {
        var row = (Row)index;
        var p = s.Player;

        left = row switch
        {
            Row.Hp => "HP",
            Row.MaxHp => "MAX HP",
            Row.Gold => "GOLD",
            Row.Xp => "XP",
            _ => "?"
        };

        right = row switch
        {
            Row.Hp => $"{p.Hp}",
            Row.MaxHp => $"{p.MaxHp}",
            Row.Gold => $"{p.Gold}",
            Row.Xp => $"{p.Xp}",
            _ => ""
        };
    }

    protected override void GetDetails(GameServices s, int index, out string title, out string preview, out string body, out string footerLine)
    {
        var row = (Row)index;
        var p = s.Player;

        title = row switch
        {
            Row.Hp => "CURRENT HP",
            Row.MaxHp => "MAX HP",
            Row.Gold => "GOLD",
            Row.Xp => "EXPERIENCE",
            _ => "DETAILS"
        };

        preview = row switch
        {
            Row.Hp => $"HP: {p.Hp}/{p.MaxHp}",
            Row.MaxHp => $"CAP: {p.MaxHp}",
            Row.Gold => $"ON HAND: {p.Gold}",
            Row.Xp => $"TOTAL: {p.Xp}",
            _ => ""
        };

        body = row switch
        {
            Row.Hp => "YOUR CURRENT HEALTH. IF IT HITS ZERO, YOU'RE DOWN.",
            Row.MaxHp => "YOUR HEALTH CAP. POTIONS AND REST HEAL UP TO THIS LIMIT.",
            Row.Gold => "CURRENCY USED FOR SHOPS, INNS, AND FUTURE SERVICES.",
            Row.Xp => "EXPERIENCE EARNED FROM BATTLES. LEVELING COMES NEXT.",
            _ => ""
        };

        footerLine = "READ ONLY";
    }

    // Read-only page: confirm does nothing (but we still consume to prevent spam)
    protected override void OnConfirm(GameServices s, in UpdateContext uc, int index)
    {
        // Optional tiny feedback:
        // SetToast("NOTHING TO USE HERE.", 0.8f);
    }
}
