using System;
using System.Collections.Generic;
using UnityEngine;

namespace NoctisUltor.Prototype
{
    public enum PrototypeScreen
    {
        Lobby,
        Enhancement,
        StageSelect,
        EndlessLobby,
        Battle,
        Result,
    }

    public enum BattleState
    {
        None,
        PlayerTurn,
        Targeting,
        EnemyTurn,
        Shop,
        LevelUp,
        ReplaceSkill,
        StageResult,
        RealmResult,
        GameOver,
    }

    public enum RealmId
    {
        KnightKing,
        LionKing,
        MadKing,
        HeroKing,
        AncestorDragonKing,
    }

    public enum SpiritType
    {
        Ice,
        Fire,
        Thunder,
    }

    public enum PieceType
    {
        Pawn,
        Bishop,
        Rook,
        Knight,
        Queen,
        King,
    }

    public enum SkillId
    {
        None,
        ColdLance,
        AbsoluteZero,
        FrostRain,
        CrystalEscudo,
        ThinIce,
        ShatterIce,
        FlameSlash,
        SunWheel,
        AzureFlame,
        Hellfire,
        Inferno,
        BurningHeart,
        ThunderShock,
        Tempest,
        AzureLightningThorn,
        Resonance,
        BlitzCreek,
        ThunderWall,
        WolfStep,
        Gluttony,
        Kingslayer,
        HomingShot,
        CurseStrike,
        GoldenTouch,
        SealKnightKing,
        SealLionKing,
        SealMadKing,
        SealHeroKing,
        SealAncestorDragonKing,
    }

    public enum ItemId
    {
        None,
        HealingPotion,
        BerserkDrug,
        StarFragment,
        DuplicationEye,
        RiskDrug,
        NightFragment,
        GreaterHealingPotion,
        MagicBullet,
        MartialGuide,
        CoinScavengeGuide,
        IceStone,
        FireStone,
        ThunderStone,
        CursedJar,
        MirrorStaff,
        Mjolnir,
        GaeBolg,
        Sverl,
        Excalibur,
        ImmortalElixir,
    }

    public enum TargetingMode
    {
        None,
        LineToEdge8,
        AnyEnemy,
        AnyTile,
        AdjacentEight,
        AroundTwentyFour,
        LineThreeEight,
        SquareThreeByThree,
        SquareTwoByTwo,
        FlameSlashArea,
        AzureFlameArea,
    }

    public enum FieldEffectType
    {
        ThinIce,
        Tempest,
    }

    public enum LevelChoiceKind
    {
        Skill,
        Coin,
        SkillPointOne,
        SkillPointTwo,
    }

    [Serializable]
    public sealed class PrototypeSaveData
    {
        public int TokenCount;
        public int AttackUpgradeLevel = 1;
        public int StartingSkillPointUpgradeLevel;
        public int HpUpgradeLevel;
        public int SelectedSpirit = (int)SpiritType.Ice;
        public int EquippedSeal = (int)SkillId.None;
        public List<int> UnlockedSeals = new();
        public int EndlessBestStage;
    }

    public sealed class PrototypeStageDefinition
    {
        public RealmId Realm;
        public int RealmStageIndex;
        public string DisplayName;
        public string Objective;
        public bool RequiresKingDefeat;
        public List<PrototypeSpawnDefinition> Spawns = new();
    }

    public sealed class PrototypeSpawnDefinition
    {
        public PieceType PieceType;
        public Vector2Int Position;
    }

    public sealed class SkillDefinition
    {
        public SkillId Id;
        public string DisplayName;
        public string Description;
        public int SkillPointCost;
        public SpiritType? SpiritRestriction;
        public bool IsUniversal;
        public bool IsSeal;
        public bool IsFixedPrimary;
        public bool IsSingleUsePerStage;
        public bool CanTargetKing;
        public bool CanTargetQueen;
        public bool CanTargetSelf;
        public bool DealsDamage;
        public bool DealsAllEnemyDamage;
        public bool RequiresTarget;
        public bool ConsumesPlayerHp;
        public bool AppliesFreeze;
        public bool AppliesShock;
        public bool AppliesCurse;
        public bool GrantsBarrier;
        public bool GrantsExtraTurn;
        public bool GrantsNextMoveAnywhere;
        public bool GrantsAttackDoubleNextTurn;
        public bool GrantsHealOnKillNextTurn;
        public bool GrantsThunderWall;
        public bool GrantsRewardDoubleUntilStageEnd;
        public bool GrantsFreeSkillUses;
        public bool HalvesCurrentHp;
        public bool InstantKill;
        public bool DamagesOnlyFrozenForTriple;
        public bool DamagesOnlyShocked;
        public bool FreezeAllEnemies;
        public bool DamageAllEnemies;
        public bool CreatesThinIce;
        public bool CreatesTempest;
        public int DamageMultiplier = 1;
        public int StatusTurns;
        public int DurationTurns;
        public int FreeSkillUseCount;
        public TargetingMode TargetingMode;
    }

    public sealed class ItemDefinition
    {
        public ItemId Id;
        public string DisplayName;
        public string Description;
        public int Cost;
        public bool RequiresTarget;
        public bool DealsDamage;
        public bool DamageAllEnemies;
        public bool AppliesFreeze;
        public bool AppliesShock;
        public bool AppliesCurse;
        public bool InstantKill;
        public bool CanTargetKing;
        public bool CanTargetQueen;
        public bool GrantsAttackDoubleNextTurn;
        public bool GrantsExtraTurn;
        public bool GrantsNextMoveAnywhere;
        public bool GrantsIgnoreDamageOnce;
        public bool GrantsSkillPoints;
        public bool ExtendsFreeze;
        public bool ExtendsShock;
        public bool DoublesExpTurns;
        public bool DoublesCoinTurns;
        public bool MultipliesCoins;
        public bool CostsHp;
        public int DamageMultiplier = 1;
        public int HealAmount;
        public int SkillPointAmount;
        public int StatusTurns;
        public int DurationTurns;
        public int CoinMultiplierPercent = 100;
        public TargetingMode TargetingMode;
    }

    public sealed class LevelChoiceDefinition
    {
        public LevelChoiceKind Kind;
        public SkillId SkillId;
        public string Title;
        public string Description;
    }

    public sealed class PrototypeEnemyState
    {
        public int RuntimeId;
        public PieceType PieceType;
        public Vector2Int Position;
        public int Hp;
        public int MaxHp;
        public int FreezeTurns;
        public int ShockTurns;
        public bool HasCurse;
        public bool WillActNextTurn;
    }

    public sealed class PrototypeFieldEffectState
    {
        public FieldEffectType FieldEffectType;
        public List<Vector2Int> Tiles = new();
        public int RemainingEnemyTurns;
    }

    public sealed class PrototypePlayerTurnState
    {
        public bool PreActionUsed;
        public bool PostActionUsed;
        public bool Moved;
        public Vector2Int MoveOriginPosition;
        public bool HasPendingMove;
        public Vector2Int PendingMovePosition;
        public int ExtraTurnsRemaining;
        public int PendingAttackDoubleTurns;
        public int ActiveAttackDoubleTurns;
        public int PendingHealOnKillTurns;
        public int ActiveHealOnKillTurns;
        public int ActiveExpDoubleTurns;
        public int ActiveCoinDoubleTurns;
        public int ActiveBarrierEnemyTurns;
        public int IgnoreDamageCharges;
        public int FreeSkillUsesRemaining;
        public int HeroRewardDoubleStageFlag;
        public int MadSealTurnsRemaining;
        public bool NextMoveAnywhere;
        public bool ThunderWallArmed;
    }

    public sealed class PrototypeBattleRun
    {
        public bool EndlessMode;
        public RealmId Realm;
        public int RealmStageIndex;
        public int EndlessStageNumber = 1;
        public int TurnNumber = 1;
        public int StagesClearedThisRun;
        public int HighestRealmProgress;
        public SpiritType Spirit;
        public SkillId PrimarySkill;
        public SkillId EquippedSeal;
        public int EquippedSealUsesRemaining;
        public int Level = 1;
        public int Experience;
        public int NextLevelExperience = 1;
        public int Attack;
        public int MaxHp;
        public int Hp;
        public int SkillPoints;
        public int Coins;
        public Vector2Int SpawnPosition;
        public Vector2Int PlayerPosition;
        public bool BerserkMode;
        public BattleState BattleState;
        public SkillId PendingSkillTarget = SkillId.None;
        public ItemId PendingItemTarget = ItemId.None;
        public int PendingItemSlotIndex = -1;
        public bool HasPendingTargetConfirmation;
        public Vector2Int PendingTargetTile;
        public SkillId PendingReplacementSkill = SkillId.None;
        public int RewardTokenCount;
        public SkillId RewardSeal = SkillId.None;
        public string LastResultTitle;
        public string LastResultBody;
        public string BannerMessage;
        public List<PrototypeEnemyState> Enemies = new();
        public List<PrototypeFieldEffectState> FieldEffects = new();
        public List<SkillId> RandomSkills = new();
        public List<ItemId> Inventory = new();
        public List<int> PreviewEnemyRuntimeIds = new();
        public List<LevelChoiceDefinition> LevelChoices = new();
        public List<ItemDefinition> ShopOffers = new();
        public PrototypePlayerTurnState TurnState = new();
    }
}
