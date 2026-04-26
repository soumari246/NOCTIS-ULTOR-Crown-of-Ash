using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NoctisUltor.Prototype
{
    public static class PrototypeBalanceTables
    {
        private static readonly Dictionary<PieceType, int[]> PieceHpByRealm = new()
        {
            { PieceType.Pawn, new[] { 3, 5, 7, 9, 11 } },
            { PieceType.Bishop, new[] { 3, 6, 9, 12, 15 } },
            { PieceType.Rook, new[] { 3, 6, 9, 12, 15 } },
            { PieceType.Knight, new[] { 3, 6, 9, 12, 15 } },
            { PieceType.Queen, new[] { 8, 16, 24, 32, 40 } },
            { PieceType.King, new[] { 10, 20, 30, 40, 50 } },
        };

        private static readonly Dictionary<PieceType, Vector2Int> PieceRewards = new()
        {
            { PieceType.Pawn, new Vector2Int(1, 1) },
            { PieceType.Bishop, new Vector2Int(1, 10) },
            { PieceType.Rook, new Vector2Int(1, 10) },
            { PieceType.Knight, new Vector2Int(1, 10) },
            { PieceType.Queen, new Vector2Int(1, 20) },
            { PieceType.King, new Vector2Int(3, 20) },
        };

        private static readonly Dictionary<RealmId, Vector2Int> RealmClearRewards = new()
        {
            { RealmId.KnightKing, new Vector2Int(4, (int)SkillId.SealKnightKing) },
            { RealmId.LionKing, new Vector2Int(5, (int)SkillId.SealLionKing) },
            { RealmId.MadKing, new Vector2Int(6, (int)SkillId.SealMadKing) },
            { RealmId.HeroKing, new Vector2Int(7, (int)SkillId.SealHeroKing) },
            { RealmId.AncestorDragonKing, new Vector2Int(8, (int)SkillId.SealAncestorDragonKing) },
        };

        private static readonly Dictionary<SkillId, SkillDefinition> SkillDefinitions = BuildSkillDefinitions();
        private static readonly Dictionary<ItemId, ItemDefinition> ItemDefinitions = BuildItemDefinitions();
        private static readonly List<PrototypeStageDefinition> StageDefinitions = BuildStageDefinitions();

        public static IReadOnlyDictionary<SkillId, SkillDefinition> Skills => SkillDefinitions;
        public static IReadOnlyDictionary<ItemId, ItemDefinition> Items => ItemDefinitions;
        public static IReadOnlyList<PrototypeStageDefinition> Stages => StageDefinitions;

        public static string GetRealmName(RealmId realm)
        {
            return realm switch
            {
                RealmId.KnightKing => "騎士王",
                RealmId.LionKing => "獅子王",
                RealmId.MadKing => "狂王",
                RealmId.HeroKing => "英雄王",
                RealmId.AncestorDragonKing => "祖竜王",
                _ => "不明",
            };
        }

        public static string GetStageCategoryName(int stageIndex)
        {
            return stageIndex switch
            {
                0 => "野営地",
                1 => "砦",
                2 => "本城",
                _ => "戦場",
            };
        }

        public static int GetPermanentMaxHp(PrototypeSaveData saveData)
        {
            return 2 + Mathf.Clamp(saveData.HpUpgradeLevel, 0, 3);
        }

        public static int GetPermanentAttack(PrototypeSaveData saveData)
        {
            return Mathf.Clamp(saveData.AttackUpgradeLevel, 1, 6);
        }

        public static int GetPermanentStartingSkillPoints(PrototypeSaveData saveData)
        {
            return Mathf.Clamp(saveData.StartingSkillPointUpgradeLevel, 0, 5);
        }

        public static int GetAttackUpgradeCost(PrototypeSaveData saveData)
        {
            if (saveData.AttackUpgradeLevel >= 6)
            {
                return -1;
            }

            return saveData.AttackUpgradeLevel * 2;
        }

        public static int GetStartingSkillPointUpgradeCost(PrototypeSaveData saveData)
        {
            if (saveData.StartingSkillPointUpgradeLevel >= 5)
            {
                return -1;
            }

            return (saveData.StartingSkillPointUpgradeLevel + 1) * 2;
        }

        public static int GetHpUpgradeCost(PrototypeSaveData saveData)
        {
            if (saveData.HpUpgradeLevel >= 3)
            {
                return -1;
            }

            return (saveData.HpUpgradeLevel + 1) * 2;
        }

        public static SkillId GetPrimarySkill(SpiritType spirit)
        {
            return spirit switch
            {
                SpiritType.Ice => SkillId.ColdLance,
                SpiritType.Fire => SkillId.FlameSlash,
                SpiritType.Thunder => SkillId.ThunderShock,
                _ => SkillId.ColdLance,
            };
        }

        public static List<SkillId> GetRandomSkillPool(SpiritType spirit)
        {
            var result = new List<SkillId>
            {
                SkillId.WolfStep,
                SkillId.Gluttony,
                SkillId.Kingslayer,
                SkillId.HomingShot,
                SkillId.CurseStrike,
                SkillId.GoldenTouch,
            };

            switch (spirit)
            {
                case SpiritType.Ice:
                    result.AddRange(new[]
                    {
                        SkillId.AbsoluteZero,
                        SkillId.FrostRain,
                        SkillId.CrystalEscudo,
                        SkillId.ThinIce,
                        SkillId.ShatterIce,
                    });
                    break;

                case SpiritType.Fire:
                    result.AddRange(new[]
                    {
                        SkillId.SunWheel,
                        SkillId.AzureFlame,
                        SkillId.Hellfire,
                        SkillId.Inferno,
                        SkillId.BurningHeart,
                    });
                    break;

                case SpiritType.Thunder:
                    result.AddRange(new[]
                    {
                        SkillId.Tempest,
                        SkillId.AzureLightningThorn,
                        SkillId.Resonance,
                        SkillId.BlitzCreek,
                        SkillId.ThunderWall,
                    });
                    break;
            }

            return result;
        }

        public static PrototypeStageDefinition GetStageDefinition(RealmId realm, int realmStageIndex)
        {
            return StageDefinitions.First(stage => stage.Realm == realm && stage.RealmStageIndex == realmStageIndex);
        }

        public static PrototypeStageDefinition BuildEndlessStage(int endlessStageNumber)
        {
            var baseIndex = (endlessStageNumber - 1) % StageDefinitions.Count;
            var baseStage = CloneStageDefinition(StageDefinitions[baseIndex]);
            baseStage.DisplayName = $"終わりのない戦い {endlessStageNumber}";

            var extraPawnCount = Mathf.Clamp(endlessStageNumber - 15, 0, 16);
            if (extraPawnCount > 0)
            {
                var occupied = new HashSet<Vector2Int>(baseStage.Spawns.Select(spawn => spawn.Position));
                foreach (var candidate in GetEndlessPawnCandidatePositions())
                {
                    if (extraPawnCount <= 0)
                    {
                        break;
                    }

                    if (occupied.Contains(candidate))
                    {
                        continue;
                    }

                    baseStage.Spawns.Add(
                        new PrototypeSpawnDefinition
                        {
                            PieceType = PieceType.Pawn,
                            Position = candidate,
                        });
                    occupied.Add(candidate);
                    extraPawnCount--;
                }
            }

            return baseStage;
        }

        public static int GetPieceHp(PieceType pieceType, RealmId realm, int endlessStageNumber)
        {
            var realmIndex = (int)realm;
            var baseHp = PieceHpByRealm[pieceType][realmIndex];
            if (endlessStageNumber < 32)
            {
                return baseHp;
            }

            var bonusSteps = endlessStageNumber - 31;
            return baseHp + (bonusSteps * 2);
        }

        public static Vector2Int GetPieceReward(PieceType pieceType)
        {
            return PieceRewards[pieceType];
        }

        public static int GetFailureRewardTokens(int highestRealmProgress)
        {
            return highestRealmProgress switch
            {
                <= 0 => 0,
                1 => 1,
                _ => 2,
            };
        }

        public static Vector2Int GetRealmClearReward(RealmId realm)
        {
            return RealmClearRewards[realm];
        }

        public static List<ItemDefinition> BuildShopPool()
        {
            return ItemDefinitions.Values.Where(item => item.Id != ItemId.None).ToList();
        }

        public static SkillDefinition GetSkill(SkillId skillId)
        {
            return SkillDefinitions[skillId];
        }

        public static ItemDefinition GetItem(ItemId itemId)
        {
            return ItemDefinitions[itemId];
        }

        public static List<ItemDefinition> CreateShopOffers(System.Random random)
        {
            var sixCost = ItemDefinitions.Values.Where(item => item.Cost == 6).ToList();
            var sixOrTwelve = ItemDefinitions.Values.Where(item => item.Cost == 6 || item.Cost == 12).ToList();
            var expensive = ItemDefinitions.Values.Where(item => item.Cost == 12 || item.Cost == 20 || item.Cost == 50).ToList();

            return new List<ItemDefinition>
            {
                CloneItemDefinition(sixCost[random.Next(sixCost.Count)]),
                CloneItemDefinition(sixOrTwelve[random.Next(sixOrTwelve.Count)]),
                CloneItemDefinition(sixOrTwelve[random.Next(sixOrTwelve.Count)]),
                CloneItemDefinition(expensive[random.Next(expensive.Count)]),
                CloneItemDefinition(expensive[random.Next(expensive.Count)]),
            };
        }

        private static Dictionary<SkillId, SkillDefinition> BuildSkillDefinitions()
        {
            return new Dictionary<SkillId, SkillDefinition>
            {
                {
                    SkillId.ColdLance,
                    new SkillDefinition
                    {
                        Id = SkillId.ColdLance,
                        DisplayName = "コールドランス",
                        Description = "直線上の最初の敵に攻撃し、2ターン凍結させる。",
                        SkillPointCost = 5,
                        SpiritRestriction = SpiritType.Ice,
                        IsFixedPrimary = true,
                        DealsDamage = true,
                        AppliesFreeze = true,
                        DamageMultiplier = 1,
                        StatusTurns = 2,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.LineToEdge8,
                    }
                },
                {
                    SkillId.AbsoluteZero,
                    new SkillDefinition
                    {
                        Id = SkillId.AbsoluteZero,
                        DisplayName = "アブソリュート・ゼロ",
                        Description = "すべての敵を2ターン凍結させる。",
                        SkillPointCost = 5,
                        SpiritRestriction = SpiritType.Ice,
                        FreezeAllEnemies = true,
                        AppliesFreeze = true,
                        StatusTurns = 2,
                        RequiresTarget = false,
                    }
                },
                {
                    SkillId.FrostRain,
                    new SkillDefinition
                    {
                        Id = SkillId.FrostRain,
                        DisplayName = "フロストレイン",
                        Description = "選んだ2x2マスに攻撃し、50%で2ターン凍結させる。",
                        SkillPointCost = 5,
                        SpiritRestriction = SpiritType.Ice,
                        DealsDamage = true,
                        AppliesFreeze = true,
                        DamageMultiplier = 1,
                        StatusTurns = 2,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.SquareTwoByTwo,
                    }
                },
                {
                    SkillId.CrystalEscudo,
                    new SkillDefinition
                    {
                        Id = SkillId.CrystalEscudo,
                        DisplayName = "クリスタルエスクード",
                        Description = "次の敵ターン終了まで受けるダメージを無効化する。",
                        SkillPointCost = 5,
                        SpiritRestriction = SpiritType.Ice,
                        GrantsBarrier = true,
                        DurationTurns = 1,
                    }
                },
                {
                    SkillId.ThinIce,
                    new SkillDefinition
                    {
                        Id = SkillId.ThinIce,
                        DisplayName = "薄氷",
                        Description = "選んだ3x3マスに2ターン続く薄氷を設置する。",
                        SkillPointCost = 4,
                        SpiritRestriction = SpiritType.Ice,
                        CreatesThinIce = true,
                        DurationTurns = 2,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.SquareThreeByThree,
                    }
                },
                {
                    SkillId.ShatterIce,
                    new SkillDefinition
                    {
                        Id = SkillId.ShatterIce,
                        DisplayName = "砕氷",
                        Description = "単体攻撃。凍結中の敵には3倍ダメージ。",
                        SkillPointCost = 4,
                        SpiritRestriction = SpiritType.Ice,
                        DealsDamage = true,
                        DamagesOnlyFrozenForTriple = true,
                        DamageMultiplier = 1,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.AnyEnemy,
                    }
                },
                {
                    SkillId.FlameSlash,
                    new SkillDefinition
                    {
                        Id = SkillId.FlameSlash,
                        DisplayName = "フレイムスラッシュ",
                        Description = "選んだ4方向へ2x5マスの範囲攻撃。",
                        SkillPointCost = 5,
                        SpiritRestriction = SpiritType.Fire,
                        IsFixedPrimary = true,
                        DealsDamage = true,
                        DamageMultiplier = 1,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.FlameSlashArea,
                    }
                },
                {
                    SkillId.SunWheel,
                    new SkillDefinition
                    {
                        Id = SkillId.SunWheel,
                        DisplayName = "日輪",
                        Description = "キャラクター周囲24マスの敵を攻撃する。",
                        SkillPointCost = 5,
                        SpiritRestriction = SpiritType.Fire,
                        DealsDamage = true,
                        DamageMultiplier = 1,
                        RequiresTarget = false,
                        TargetingMode = TargetingMode.AroundTwentyFour,
                    }
                },
                {
                    SkillId.AzureFlame,
                    new SkillDefinition
                    {
                        Id = SkillId.AzureFlame,
                        DisplayName = "蒼炎",
                        Description = "選んだ8方向へT字5マスの3倍攻撃。",
                        SkillPointCost = 5,
                        SpiritRestriction = SpiritType.Fire,
                        DealsDamage = true,
                        DamageMultiplier = 3,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.AzureFlameArea,
                    }
                },
                {
                    SkillId.Hellfire,
                    new SkillDefinition
                    {
                        Id = SkillId.Hellfire,
                        DisplayName = "ヘルファイア",
                        Description = "HPを1消費してすべての敵を攻撃する。",
                        SkillPointCost = 5,
                        SpiritRestriction = SpiritType.Fire,
                        ConsumesPlayerHp = true,
                        DealsAllEnemyDamage = true,
                        DamageAllEnemies = true,
                        DamageMultiplier = 1,
                    }
                },
                {
                    SkillId.Inferno,
                    new SkillDefinition
                    {
                        Id = SkillId.Inferno,
                        DisplayName = "インフェルノ",
                        Description = "選んだ8方向へ長さ3マスの2倍攻撃。",
                        SkillPointCost = 4,
                        SpiritRestriction = SpiritType.Fire,
                        DealsDamage = true,
                        DamageMultiplier = 2,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.LineThreeEight,
                    }
                },
                {
                    SkillId.BurningHeart,
                    new SkillDefinition
                    {
                        Id = SkillId.BurningHeart,
                        DisplayName = "バーニングハート",
                        Description = "次のターンの攻撃力を2倍にする。",
                        SkillPointCost = 4,
                        SpiritRestriction = SpiritType.Fire,
                        GrantsAttackDoubleNextTurn = true,
                        DurationTurns = 1,
                    }
                },
                {
                    SkillId.ThunderShock,
                    new SkillDefinition
                    {
                        Id = SkillId.ThunderShock,
                        DisplayName = "サンダーショック",
                        Description = "周囲24マスから1体を攻撃し、2ターン感電させる。",
                        SkillPointCost = 5,
                        SpiritRestriction = SpiritType.Thunder,
                        IsFixedPrimary = true,
                        DealsDamage = true,
                        AppliesShock = true,
                        DamageMultiplier = 1,
                        StatusTurns = 2,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.AnyEnemy,
                    }
                },
                {
                    SkillId.Tempest,
                    new SkillDefinition
                    {
                        Id = SkillId.Tempest,
                        DisplayName = "テンペスト",
                        Description = "選んだ3x3マスに3ターン続く嵐を設置する。",
                        SkillPointCost = 5,
                        SpiritRestriction = SpiritType.Thunder,
                        CreatesTempest = true,
                        DurationTurns = 3,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.SquareThreeByThree,
                    }
                },
                {
                    SkillId.AzureLightningThorn,
                    new SkillDefinition
                    {
                        Id = SkillId.AzureLightningThorn,
                        DisplayName = "碧雷棘",
                        Description = "選んだ8方向へ長さ3マスを攻撃し、3ターン感電させる。",
                        SkillPointCost = 5,
                        SpiritRestriction = SpiritType.Thunder,
                        DealsDamage = true,
                        AppliesShock = true,
                        DamageMultiplier = 1,
                        StatusTurns = 3,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.LineThreeEight,
                    }
                },
                {
                    SkillId.Resonance,
                    new SkillDefinition
                    {
                        Id = SkillId.Resonance,
                        DisplayName = "共鳴",
                        Description = "感電中のすべての敵にダメージを与える。",
                        SkillPointCost = 5,
                        SpiritRestriction = SpiritType.Thunder,
                        DealsDamage = true,
                        DamagesOnlyShocked = true,
                        DamageMultiplier = 1,
                    }
                },
                {
                    SkillId.BlitzCreek,
                    new SkillDefinition
                    {
                        Id = SkillId.BlitzCreek,
                        DisplayName = "ブリッツクリーク",
                        Description = "エクストラターンを1回得る。",
                        SkillPointCost = 4,
                        SpiritRestriction = SpiritType.Thunder,
                        GrantsExtraTurn = true,
                    }
                },
                {
                    SkillId.ThunderWall,
                    new SkillDefinition
                    {
                        Id = SkillId.ThunderWall,
                        DisplayName = "サンダーウォール",
                        Description = "敵に取られた時、その敵を倒す。",
                        SkillPointCost = 4,
                        SpiritRestriction = SpiritType.Thunder,
                        GrantsThunderWall = true,
                    }
                },
                {
                    SkillId.WolfStep,
                    new SkillDefinition
                    {
                        Id = SkillId.WolfStep,
                        DisplayName = "狼のステップ",
                        Description = "次の移動で好きな場所へ移動できる。",
                        SkillPointCost = 5,
                        IsUniversal = true,
                        GrantsNextMoveAnywhere = true,
                    }
                },
                {
                    SkillId.Gluttony,
                    new SkillDefinition
                    {
                        Id = SkillId.Gluttony,
                        DisplayName = "悪食",
                        Description = "次のターン、敵を倒すたびHPを1回復する。",
                        SkillPointCost = 5,
                        IsUniversal = true,
                        GrantsHealOnKillNextTurn = true,
                        DurationTurns = 1,
                    }
                },
                {
                    SkillId.Kingslayer,
                    new SkillDefinition
                    {
                        Id = SkillId.Kingslayer,
                        DisplayName = "王殺し",
                        Description = "周囲8マスの敵1体に攻撃。キングも対象にできる。",
                        SkillPointCost = 10,
                        IsUniversal = true,
                        DealsDamage = true,
                        DamageMultiplier = 1,
                        CanTargetKing = true,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.AdjacentEight,
                    }
                },
                {
                    SkillId.HomingShot,
                    new SkillDefinition
                    {
                        Id = SkillId.HomingShot,
                        DisplayName = "ホーミングショット",
                        Description = "敵1体に攻撃する。",
                        SkillPointCost = 3,
                        IsUniversal = true,
                        DealsDamage = true,
                        DamageMultiplier = 1,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.AnyEnemy,
                    }
                },
                {
                    SkillId.CurseStrike,
                    new SkillDefinition
                    {
                        Id = SkillId.CurseStrike,
                        DisplayName = "呪殺",
                        Description = "敵1体に呪いを付与する。",
                        SkillPointCost = 5,
                        IsUniversal = true,
                        AppliesCurse = true,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.AnyEnemy,
                    }
                },
                {
                    SkillId.GoldenTouch,
                    new SkillDefinition
                    {
                        Id = SkillId.GoldenTouch,
                        DisplayName = "ゴールデンタッチ",
                        Description = "所持コインx0.2のダメージを敵1体に与える。",
                        SkillPointCost = 3,
                        IsUniversal = true,
                        DealsDamage = true,
                        DamageMultiplier = 0,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.AnyEnemy,
                    }
                },
                {
                    SkillId.SealKnightKing,
                    new SkillDefinition
                    {
                        Id = SkillId.SealKnightKing,
                        DisplayName = "騎士王の証",
                        Description = "直線上の最初の敵の現在HPを半分削る。各ステージ1回。",
                        SkillPointCost = 7,
                        IsSeal = true,
                        IsSingleUsePerStage = true,
                        HalvesCurrentHp = true,
                        RequiresTarget = true,
                        TargetingMode = TargetingMode.LineToEdge8,
                    }
                },
                {
                    SkillId.SealLionKing,
                    new SkillDefinition
                    {
                        Id = SkillId.SealLionKing,
                        DisplayName = "獅子王の証",
                        Description = "キングとクイーン以外の敵1体を倒す。各ステージ1回。",
                        SkillPointCost = 7,
                        IsSeal = true,
                        IsSingleUsePerStage = true,
                        InstantKill = true,
                        RequiresTarget = true,
                        CanTargetKing = false,
                        CanTargetQueen = false,
                        TargetingMode = TargetingMode.AnyEnemy,
                    }
                },
                {
                    SkillId.SealMadKing,
                    new SkillDefinition
                    {
                        Id = SkillId.SealMadKing,
                        DisplayName = "狂王の証",
                        Description = "5ターンの間、敵を倒すたびHPを1回復する。各ステージ1回。",
                        SkillPointCost = 7,
                        IsSeal = true,
                        IsSingleUsePerStage = true,
                        GrantsHealOnKillNextTurn = true,
                        DurationTurns = 5,
                    }
                },
                {
                    SkillId.SealHeroKing,
                    new SkillDefinition
                    {
                        Id = SkillId.SealHeroKing,
                        DisplayName = "英雄王の証",
                        Description = "ステージ終了まで撃破報酬が2倍になる。各ステージ1回。",
                        SkillPointCost = 7,
                        IsSeal = true,
                        IsSingleUsePerStage = true,
                        GrantsRewardDoubleUntilStageEnd = true,
                    }
                },
                {
                    SkillId.SealAncestorDragonKing,
                    new SkillDefinition
                    {
                        Id = SkillId.SealAncestorDragonKing,
                        DisplayName = "祖竜王の証",
                        Description = "次の2回のスキル消費を0にする。各ステージ1回。",
                        SkillPointCost = 7,
                        IsSeal = true,
                        IsSingleUsePerStage = true,
                        GrantsFreeSkillUses = true,
                        FreeSkillUseCount = 2,
                    }
                },
            };
        }

        private static Dictionary<ItemId, ItemDefinition> BuildItemDefinitions()
        {
            return new Dictionary<ItemId, ItemDefinition>
            {
                { ItemId.HealingPotion, new ItemDefinition { Id = ItemId.HealingPotion, DisplayName = "回復ポーション", Description = "HPを1回復する。", Cost = 6, HealAmount = 1 } },
                { ItemId.BerserkDrug, new ItemDefinition { Id = ItemId.BerserkDrug, DisplayName = "狂薬", Description = "次のターンの攻撃力を2倍にする。", Cost = 6, GrantsAttackDoubleNextTurn = true, DurationTurns = 1 } },
                { ItemId.StarFragment, new ItemDefinition { Id = ItemId.StarFragment, DisplayName = "星の破片", Description = "SPを2回復する。", Cost = 6, GrantsSkillPoints = true, SkillPointAmount = 2 } },
                { ItemId.DuplicationEye, new ItemDefinition { Id = ItemId.DuplicationEye, DisplayName = "複製の魔眼", Description = "所持コインを1.2倍にする。", Cost = 6, MultipliesCoins = true, CoinMultiplierPercent = 120 } },
                { ItemId.RiskDrug, new ItemDefinition { Id = ItemId.RiskDrug, DisplayName = "劇薬", Description = "HPを1消費してエクストラターンを得る。", Cost = 6, CostsHp = true, GrantsExtraTurn = true } },
                { ItemId.NightFragment, new ItemDefinition { Id = ItemId.NightFragment, DisplayName = "夜の破片", Description = "SPを4回復する。", Cost = 12, GrantsSkillPoints = true, SkillPointAmount = 4 } },
                { ItemId.GreaterHealingPotion, new ItemDefinition { Id = ItemId.GreaterHealingPotion, DisplayName = "大回復ポーション", Description = "HPを2回復する。", Cost = 12, HealAmount = 2 } },
                { ItemId.MagicBullet, new ItemDefinition { Id = ItemId.MagicBullet, DisplayName = "魔弾", Description = "敵1体にダメージを与える。", Cost = 12, RequiresTarget = true, DealsDamage = true, DamageMultiplier = 1, TargetingMode = TargetingMode.AnyEnemy } },
                { ItemId.MartialGuide, new ItemDefinition { Id = ItemId.MartialGuide, DisplayName = "武術の指南書", Description = "2ターンの間、取得経験値が2倍。", Cost = 12, DoublesExpTurns = true, DurationTurns = 2 } },
                { ItemId.CoinScavengeGuide, new ItemDefinition { Id = ItemId.CoinScavengeGuide, DisplayName = "死体漁りの指南書", Description = "2ターンの間、取得コインが2倍。", Cost = 12, DoublesCoinTurns = true, DurationTurns = 2 } },
                { ItemId.IceStone, new ItemDefinition { Id = ItemId.IceStone, DisplayName = "氷の魔石", Description = "凍結中の敵の凍結ターンを1延長する。", Cost = 20, ExtendsFreeze = true } },
                { ItemId.FireStone, new ItemDefinition { Id = ItemId.FireStone, DisplayName = "炎の魔石", Description = "すべての敵に1ダメージ。", Cost = 20, DamageAllEnemies = true, DamageMultiplier = 1 } },
                { ItemId.ThunderStone, new ItemDefinition { Id = ItemId.ThunderStone, DisplayName = "雷の魔石", Description = "感電中の敵の感電ターンを1延長する。", Cost = 20, ExtendsShock = true } },
                { ItemId.CursedJar, new ItemDefinition { Id = ItemId.CursedJar, DisplayName = "怨霊壺", Description = "敵1体に呪いを付与する。", Cost = 20, RequiresTarget = true, AppliesCurse = true, TargetingMode = TargetingMode.AnyEnemy } },
                { ItemId.MirrorStaff, new ItemDefinition { Id = ItemId.MirrorStaff, DisplayName = "鏡の杖", Description = "次の移動で好きな場所へ移動できる。", Cost = 20, GrantsNextMoveAnywhere = true } },
                { ItemId.Mjolnir, new ItemDefinition { Id = ItemId.Mjolnir, DisplayName = "神器ミョルニル", Description = "選んだ3x3マスの敵を2ターン感電させる。", Cost = 50, RequiresTarget = true, AppliesShock = true, StatusTurns = 2, TargetingMode = TargetingMode.SquareThreeByThree } },
                { ItemId.GaeBolg, new ItemDefinition { Id = ItemId.GaeBolg, DisplayName = "神器ゲイボルグ", Description = "直線上の敵を攻撃。50%で戻ってくる。", Cost = 50, RequiresTarget = true, DealsDamage = true, DamageMultiplier = 1, TargetingMode = TargetingMode.LineToEdge8 } },
                { ItemId.Sverl, new ItemDefinition { Id = ItemId.Sverl, DisplayName = "神器スヴェル", Description = "周囲24マスの敵を2ターン凍結させる。", Cost = 50, AppliesFreeze = true, StatusTurns = 2, TargetingMode = TargetingMode.AroundTwentyFour } },
                { ItemId.Excalibur, new ItemDefinition { Id = ItemId.Excalibur, DisplayName = "エクスカリバー", Description = "キング・クイーン以外の敵1体を倒す。", Cost = 50, RequiresTarget = true, InstantKill = true, CanTargetKing = false, CanTargetQueen = false, TargetingMode = TargetingMode.AnyEnemy } },
                { ItemId.ImmortalElixir, new ItemDefinition { Id = ItemId.ImmortalElixir, DisplayName = "不死の霊薬", Description = "次に受けるダメージを1回だけ無効化する。", Cost = 50, GrantsIgnoreDamageOnce = true } },
            };
        }

        private static List<PrototypeStageDefinition> BuildStageDefinitions()
        {
            var definitions = new List<PrototypeStageDefinition>();
            AddStage(definitions, RealmId.KnightKing, 0, "騎士王 / 野営地", "敵をすべて倒す", false, new[]
            {
                Spawn(PieceType.Pawn, 0, 6), Spawn(PieceType.Pawn, 1, 6), Spawn(PieceType.Pawn, 2, 6),
                Spawn(PieceType.Pawn, 4, 6), Spawn(PieceType.Pawn, 5, 6), Spawn(PieceType.Pawn, 7, 6),
            });
            AddStage(definitions, RealmId.KnightKing, 1, "騎士王 / 砦", "敵をすべて倒す", false, StandardPawns());
            AddStage(definitions, RealmId.KnightKing, 2, "騎士王 / 本城", "キングを倒す", true, StandardPawnsWith(PieceType.Queen, 3, 7, PieceType.King, 4, 7));

            AddStage(definitions, RealmId.LionKing, 0, "獅子王 / 野営地", "敵をすべて倒す", false, StandardPawns());
            AddStage(definitions, RealmId.LionKing, 1, "獅子王 / 砦", "敵をすべて倒す", false, StandardPawnsWith(PieceType.Bishop, 2, 7, PieceType.Bishop, 5, 7));
            AddStage(definitions, RealmId.LionKing, 2, "獅子王 / 本城", "キングを倒す", true, StandardPawnsWith(PieceType.Bishop, 2, 7, PieceType.Queen, 3, 7, PieceType.King, 4, 7, PieceType.Bishop, 5, 7));

            AddStage(definitions, RealmId.MadKing, 0, "狂王 / 野営地", "敵をすべて倒す", false, StandardPawnsWith(PieceType.Bishop, 2, 7, PieceType.Bishop, 5, 7));
            AddStage(definitions, RealmId.MadKing, 1, "狂王 / 砦", "敵をすべて倒す", false, StandardPawnsWith(PieceType.Rook, 0, 7, PieceType.Bishop, 2, 7, PieceType.Bishop, 5, 7, PieceType.Rook, 7, 7));
            AddStage(definitions, RealmId.MadKing, 2, "狂王 / 本城", "キングを倒す", true, StandardPawnsWith(PieceType.Rook, 0, 7, PieceType.Bishop, 2, 7, PieceType.Queen, 3, 7, PieceType.King, 4, 7, PieceType.Bishop, 5, 7, PieceType.Rook, 7, 7));

            AddStage(definitions, RealmId.HeroKing, 0, "英雄王 / 野営地", "敵をすべて倒す", false, StandardPawnsWith(PieceType.Rook, 0, 7, PieceType.Bishop, 2, 7, PieceType.Bishop, 5, 7, PieceType.Rook, 7, 7));
            AddStage(definitions, RealmId.HeroKing, 1, "英雄王 / 砦", "敵をすべて倒す", false, StandardWithoutKingAndQueen());
            AddStage(definitions, RealmId.HeroKing, 2, "英雄王 / 本城", "キングを倒す", true, FullStandardBoard());

            AddStage(definitions, RealmId.AncestorDragonKing, 0, "祖竜王 / 野営地", "敵をすべて倒す", false, StandardWithoutKingAndQueen());
            AddStage(definitions, RealmId.AncestorDragonKing, 1, "祖竜王 / 砦", "敵をすべて倒す", false, StandardWithKnightOnRoyalSquares());
            AddStage(definitions, RealmId.AncestorDragonKing, 2, "祖竜王 / 本城", "キングを倒す", true, FullStandardBoard());

            return definitions;
        }

        private static PrototypeStageDefinition CloneStageDefinition(PrototypeStageDefinition source)
        {
            return new PrototypeStageDefinition
            {
                Realm = source.Realm,
                RealmStageIndex = source.RealmStageIndex,
                DisplayName = source.DisplayName,
                Objective = source.Objective,
                RequiresKingDefeat = source.RequiresKingDefeat,
                Spawns = source.Spawns.Select(spawn => new PrototypeSpawnDefinition { PieceType = spawn.PieceType, Position = spawn.Position }).ToList(),
            };
        }

        private static ItemDefinition CloneItemDefinition(ItemDefinition source)
        {
            return new ItemDefinition
            {
                Id = source.Id,
                DisplayName = source.DisplayName,
                Description = source.Description,
                Cost = source.Cost,
                RequiresTarget = source.RequiresTarget,
                DealsDamage = source.DealsDamage,
                DamageAllEnemies = source.DamageAllEnemies,
                AppliesFreeze = source.AppliesFreeze,
                AppliesShock = source.AppliesShock,
                AppliesCurse = source.AppliesCurse,
                InstantKill = source.InstantKill,
                CanTargetKing = source.CanTargetKing,
                CanTargetQueen = source.CanTargetQueen,
                GrantsAttackDoubleNextTurn = source.GrantsAttackDoubleNextTurn,
                GrantsExtraTurn = source.GrantsExtraTurn,
                GrantsNextMoveAnywhere = source.GrantsNextMoveAnywhere,
                GrantsIgnoreDamageOnce = source.GrantsIgnoreDamageOnce,
                GrantsSkillPoints = source.GrantsSkillPoints,
                ExtendsFreeze = source.ExtendsFreeze,
                ExtendsShock = source.ExtendsShock,
                DoublesExpTurns = source.DoublesExpTurns,
                DoublesCoinTurns = source.DoublesCoinTurns,
                MultipliesCoins = source.MultipliesCoins,
                CostsHp = source.CostsHp,
                DamageMultiplier = source.DamageMultiplier,
                HealAmount = source.HealAmount,
                SkillPointAmount = source.SkillPointAmount,
                StatusTurns = source.StatusTurns,
                DurationTurns = source.DurationTurns,
                CoinMultiplierPercent = source.CoinMultiplierPercent,
                TargetingMode = source.TargetingMode,
            };
        }

        private static IEnumerable<Vector2Int> GetEndlessPawnCandidatePositions()
        {
            var candidates = new List<Vector2Int>();
            for (int y = 5; y >= 2; y--)
            {
                for (int x = 0; x < 8; x++)
                {
                    candidates.Add(new Vector2Int(x, y));
                }
            }

            return candidates;
        }

        private static PrototypeSpawnDefinition Spawn(PieceType pieceType, int x, int y)
        {
            return new PrototypeSpawnDefinition
            {
                PieceType = pieceType,
                Position = new Vector2Int(x, y),
            };
        }

        private static List<PrototypeSpawnDefinition> StandardPawns()
        {
            return new List<PrototypeSpawnDefinition>
            {
                Spawn(PieceType.Pawn, 0, 6), Spawn(PieceType.Pawn, 1, 6), Spawn(PieceType.Pawn, 2, 6), Spawn(PieceType.Pawn, 3, 6),
                Spawn(PieceType.Pawn, 4, 6), Spawn(PieceType.Pawn, 5, 6), Spawn(PieceType.Pawn, 6, 6), Spawn(PieceType.Pawn, 7, 6),
            };
        }

        private static List<PrototypeSpawnDefinition> StandardPawnsWith(params object[] pieces)
        {
            var spawns = StandardPawns();
            for (var i = 0; i < pieces.Length; i += 3)
            {
                spawns.Add(Spawn((PieceType)pieces[i], (int)pieces[i + 1], (int)pieces[i + 2]));
            }

            return spawns;
        }

        private static List<PrototypeSpawnDefinition> StandardWithoutKingAndQueen()
        {
            return new List<PrototypeSpawnDefinition>(FullStandardBoard().Where(spawn => spawn.PieceType != PieceType.King && spawn.PieceType != PieceType.Queen));
        }

        private static List<PrototypeSpawnDefinition> StandardWithKnightOnRoyalSquares()
        {
            var result = StandardWithoutKingAndQueen();
            result.Add(Spawn(PieceType.Knight, 3, 7));
            result.Add(Spawn(PieceType.Knight, 4, 7));
            return result;
        }

        private static List<PrototypeSpawnDefinition> FullStandardBoard()
        {
            return new List<PrototypeSpawnDefinition>
            {
                Spawn(PieceType.Rook, 0, 7), Spawn(PieceType.Knight, 1, 7), Spawn(PieceType.Bishop, 2, 7), Spawn(PieceType.Queen, 3, 7),
                Spawn(PieceType.King, 4, 7), Spawn(PieceType.Bishop, 5, 7), Spawn(PieceType.Knight, 6, 7), Spawn(PieceType.Rook, 7, 7),
                Spawn(PieceType.Pawn, 0, 6), Spawn(PieceType.Pawn, 1, 6), Spawn(PieceType.Pawn, 2, 6), Spawn(PieceType.Pawn, 3, 6),
                Spawn(PieceType.Pawn, 4, 6), Spawn(PieceType.Pawn, 5, 6), Spawn(PieceType.Pawn, 6, 6), Spawn(PieceType.Pawn, 7, 6),
            };
        }

        private static void AddStage(List<PrototypeStageDefinition> definitions, RealmId realm, int stageIndex, string displayName, string objective, bool kingDefeat, IEnumerable<PrototypeSpawnDefinition> spawns)
        {
            definitions.Add(
                new PrototypeStageDefinition
                {
                    Realm = realm,
                    RealmStageIndex = stageIndex,
                    DisplayName = displayName,
                    Objective = objective,
                    RequiresKingDefeat = kingDefeat,
                    Spawns = spawns.ToList(),
                });
        }
    }
}
