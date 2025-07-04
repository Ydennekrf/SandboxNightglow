using System;
using System.Collections.Generic;

public record AttackDef(string Anim,
                        float Width, float Height,
                        float Forward, float ActiveSecs,
                        int   Dmg);

public record WeaponDef(string Name, AttackDef[] Physical);

public static class WeaponDB
{
    public static readonly Dictionary<string, WeaponDef> All = new()
    {
        ["sword_1"] = new WeaponDef("Bronze Edge", new[]
        {
            new AttackDef("Melee1", 32, 12, 28, 0.10f, 5),
            new AttackDef("Melee2", 32, 12, 28, 0.10f, 7),
        }),
        ["sword_2"] = new WeaponDef("Knightblade", new[]
        {
            new AttackDef("Melee1", 32, 12, 28, 0.10f, 6),
            new AttackDef("Melee2", 32, 12, 28, 0.10f, 8),
            new AttackDef("Melee2", 36, 12, 32, 0.12f, 11),
        }),
    };
}