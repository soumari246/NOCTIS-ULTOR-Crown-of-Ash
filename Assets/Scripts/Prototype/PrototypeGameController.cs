using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NoctisUltor.Prototype
{
    public sealed class PrototypeGameController : MonoBehaviour
    {
        private const int BoardSize = 8;
        private const int InventoryLimit = 3;
        private const int RandomSkillSlotCount = 2;
        private const int MaxActingEnemiesPerTurn = 6;
        private const int CurseDamage = 2;

        private static readonly Color BackgroundColor = new(0.08f, 0.08f, 0.09f);
        private static readonly Color PanelColor = new(0.12f, 0.12f, 0.13f, 0.96f);
        private static readonly Color PanelStrongColor = new(0.18f, 0.18f, 0.20f, 1f);
        private static readonly Color TileDarkColor = new(0.15f, 0.15f, 0.17f);
        private static readonly Color TileLightColor = new(0.23f, 0.23f, 0.25f);
        private static readonly Color TileSelectedColor = new(0.56f, 0.56f, 0.58f);
        private static readonly Color TileMoveSafeColor = new(0.80f, 0.80f, 0.82f);
        private static readonly Color TileMoveDangerColor = new(0.45f, 0.45f, 0.47f);
        private static readonly Color TileTargetColor = new(0.70f, 0.70f, 0.72f);
        private static readonly Color TextColor = new(0.95f, 0.95f, 0.95f);
        private static readonly Color MutedTextColor = new(0.72f, 0.72f, 0.74f);
        private static readonly Color PlayerTextColor = new(0.10f, 0.10f, 0.10f);
        private static readonly Color EnemyTextColor = new(0.96f, 0.96f, 0.96f);

        private static PrototypeGameController instance;

        private readonly Button[] boardButtons = new Button[BoardSize * BoardSize];
        private readonly Image[] boardTileImages = new Image[BoardSize * BoardSize];
        private readonly Text[] boardTileTexts = new Text[BoardSize * BoardSize];
        private readonly Button[] skillButtons = new Button[4];
        private readonly Text[] skillButtonTexts = new Text[4];
        private readonly Button[] itemButtons = new Button[InventoryLimit];
        private readonly Text[] itemButtonTexts = new Text[InventoryLimit];
        private readonly List<string> battleLogLines = new();
        private readonly HashSet<Vector2Int> validMoveTiles = new();
        private readonly HashSet<Vector2Int> dangerTiles = new();
        private readonly HashSet<Vector2Int> targetableTiles = new();

        private PrototypeSaveData saveData;
        private PrototypeBattleRun run;
        private System.Random random;
        private Sprite whiteSprite;
        private Font defaultFont;

        private RectTransform menuRoot;
        private RectTransform menuContentRoot;
        private RectTransform battleRoot;
        private RectTransform battleBoardRoot;
        private RectTransform overlayRoot;
        private RectTransform overlayCardRoot;
        private RectTransform overlayButtonRoot;
        private Text titleText;
        private Text subtitleText;
        private Text battleStageText;
        private Text battleTurnText;
        private Text battleInstructionText;
        private Text battleStatsText;
        private Text battleSkillSummaryText;
        private Text battleLogText;
        private Text overlayTitleText;
        private Text overlayBodyText;
        private Button battleConfirmActionButton;
        private Button battleEndActionButton;
        private Button battleCancelTargetButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototypeExists()
        {
            if (FindAnyObjectByType<PrototypeGameController>() != null)
            {
                return;
            }

            var host = new GameObject("PrototypeGameController");
            host.AddComponent<PrototypeGameController>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            random = new System.Random();
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            whiteSprite = BuildWhiteSprite();
            saveData = PrototypePersistence.Load();

            EnsureEventSystem();
            BuildUi();
            ShowLobby();
        }

        private void OnDestroy()
        {
            if (whiteSprite == null)
            {
                return;
            }

            if (whiteSprite.texture != null)
            {
                Destroy(whiteSprite.texture);
            }

            Destroy(whiteSprite);
        }

        private void BuildUi()
        {
            var existingCanvas = FindAnyObjectByType<Canvas>();
            if (existingCanvas != null)
            {
                Destroy(existingCanvas.gameObject);
            }

            var canvasObject = new GameObject("PrototypeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var background = CreatePanel(canvasObject.transform, "Background", BackgroundColor);
            Stretch(background);

            var header = CreatePanel(background.transform, "Header", Color.clear);
            SetAnchors(header, 0f, 0.92f, 1f, 1f, 0f, 0f, 0f, 0f);

            titleText = CreateText(header.transform, "Title", 34, FontStyle.Bold, TextAnchor.UpperLeft);
            SetAnchors((RectTransform)titleText.transform, 0.03f, 0.18f, 0.60f, 1f, 0f, 0f, 0f, 0f);

            subtitleText = CreateText(header.transform, "Subtitle", 18, FontStyle.Normal, TextAnchor.LowerLeft, MutedTextColor);
            SetAnchors((RectTransform)subtitleText.transform, 0.03f, 0f, 0.75f, 0.48f, 0f, 0f, 0f, 0f);

            menuRoot = CreatePanel(background.transform, "MenuRoot", Color.clear);
            SetAnchors(menuRoot, 0.03f, 0.05f, 0.97f, 0.90f, 0f, 0f, 0f, 0f);

            var menuScroll = new GameObject("MenuScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
            menuScroll.SetParent(menuRoot, false);
            Stretch(menuScroll);
            var menuScrollImage = menuScroll.GetComponent<Image>();
            menuScrollImage.sprite = whiteSprite;
            menuScrollImage.type = Image.Type.Sliced;
            menuScrollImage.color = PanelColor;
            menuScroll.GetComponent<Mask>().showMaskGraphic = false;

            menuContentRoot = new GameObject("MenuContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            menuContentRoot.SetParent(menuScroll, false);
            menuContentRoot.anchorMin = new Vector2(0f, 1f);
            menuContentRoot.anchorMax = new Vector2(1f, 1f);
            menuContentRoot.pivot = new Vector2(0.5f, 1f);
            menuContentRoot.offsetMin = new Vector2(24f, 0f);
            menuContentRoot.offsetMax = new Vector2(-24f, 0f);

            var menuLayout = menuContentRoot.GetComponent<VerticalLayoutGroup>();
            menuLayout.spacing = 18f;
            menuLayout.padding = new RectOffset(0, 0, 24, 24);
            menuLayout.childControlHeight = true;
            menuLayout.childControlWidth = true;
            menuLayout.childForceExpandHeight = false;
            menuLayout.childForceExpandWidth = true;

            var fitter = menuContentRoot.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = menuScroll.GetComponent<ScrollRect>();
            scrollRect.content = menuContentRoot;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.viewport = menuScroll;

            BuildBattleUi(background);
            BuildOverlayUi(background);

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.backgroundColor = BackgroundColor;
            }
        }

        private void BuildBattleUi(RectTransform parent)
        {
            battleRoot = CreatePanel(parent, "BattleRoot", Color.clear);
            SetAnchors(battleRoot, 0.02f, 0.04f, 0.98f, 0.90f, 0f, 0f, 0f, 0f);

            var boardPanel = CreatePanel(battleRoot, "BoardPanel", PanelColor);
            SetAnchors(boardPanel, 0f, 0f, 0.64f, 1f, 0f, 0f, -18f, 0f);

            battleStageText = CreateText(boardPanel.transform, "StageText", 24, FontStyle.Bold, TextAnchor.UpperLeft);
            SetAnchors((RectTransform)battleStageText.transform, 0.04f, 0.91f, 0.76f, 0.98f, 0f, 0f, 0f, 0f);

            battleTurnText = CreateText(boardPanel.transform, "TurnText", 18, FontStyle.Bold, TextAnchor.UpperRight, MutedTextColor);
            SetAnchors((RectTransform)battleTurnText.transform, 0.76f, 0.91f, 0.96f, 0.98f, 0f, 0f, 0f, 0f);

            battleInstructionText = CreateText(boardPanel.transform, "InstructionText", 18, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor);
            SetAnchors((RectTransform)battleInstructionText.transform, 0.04f, 0.84f, 0.96f, 0.90f, 0f, 0f, 0f, 0f);

            var gridFrame = CreatePanel(boardPanel.transform, "GridFrame", PanelStrongColor);
            SetAnchors(gridFrame, 0.04f, 0.05f, 0.96f, 0.82f, 0f, 0f, 0f, 0f);

            battleBoardRoot = new GameObject("BoardGrid", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            battleBoardRoot.SetParent(gridFrame, false);
            Stretch(battleBoardRoot, 12f, 12f, 12f, 12f);

            var grid = battleBoardRoot.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(88f, 88f);
            grid.spacing = new Vector2(6f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = BoardSize;

            for (int y = BoardSize - 1; y >= 0; y--)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    var tilePosition = new Vector2Int(x, y);
                    var index = ToIndex(tilePosition);
                    var button = CreateButton(battleBoardRoot, $"Tile_{x}_{y}", string.Empty, () => OnBoardTilePressed(tilePosition));
                    var image = button.GetComponent<Image>();
                    image.sprite = whiteSprite;
                    image.type = Image.Type.Sliced;

                    var label = CreateText(button.transform, "Label", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
                    Stretch((RectTransform)label.transform, 6f, 6f, 6f, 6f);

                    boardButtons[index] = button;
                    boardTileImages[index] = image;
                    boardTileTexts[index] = label;
                }
            }

            var sidePanel = CreatePanel(battleRoot, "SidePanel", PanelColor);
            SetAnchors(sidePanel, 0.66f, 0f, 1f, 1f, 0f, 0f, 0f, 0f);

            battleStatsText = CreateText(sidePanel.transform, "StatsText", 18, FontStyle.Bold, TextAnchor.UpperLeft);
            SetAnchors((RectTransform)battleStatsText.transform, 0.05f, 0.77f, 0.95f, 0.98f, 0f, 0f, 0f, 0f);

            battleSkillSummaryText = CreateText(sidePanel.transform, "SkillSummaryText", 16, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor);
            SetAnchors((RectTransform)battleSkillSummaryText.transform, 0.05f, 0.63f, 0.95f, 0.75f, 0f, 0f, 0f, 0f);

            var skillPanel = CreatePanel(sidePanel.transform, "SkillPanel", PanelStrongColor);
            SetAnchors(skillPanel, 0.05f, 0.39f, 0.95f, 0.61f, 0f, 0f, 0f, 0f);

            var skillLayout = skillPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            skillLayout.spacing = 8f;
            skillLayout.padding = new RectOffset(14, 14, 14, 14);
            skillLayout.childControlHeight = true;
            skillLayout.childControlWidth = true;
            skillLayout.childForceExpandHeight = false;
            skillLayout.childForceExpandWidth = true;

            for (int i = 0; i < skillButtons.Length; i++)
            {
                var slotIndex = i;
                skillButtons[slotIndex] = CreateButton(skillPanel, $"SkillButton_{slotIndex}", $"Skill {slotIndex}", () => OnSkillButtonPressed(slotIndex));
                skillButtonTexts[slotIndex] = skillButtons[slotIndex].GetComponentInChildren<Text>();
            }

            var itemPanel = CreatePanel(sidePanel.transform, "ItemPanel", PanelStrongColor);
            SetAnchors(itemPanel, 0.05f, 0.22f, 0.95f, 0.36f, 0f, 0f, 0f, 0f);
            var itemTitle = CreateText(itemPanel.transform, "ItemTitle", 16, FontStyle.Bold, TextAnchor.UpperLeft);
            itemTitle.text = "所持アイテム";
            SetAnchors((RectTransform)itemTitle.transform, 0.05f, 0.74f, 0.95f, 0.94f, 0f, 0f, 0f, 0f);

            for (int i = 0; i < InventoryLimit; i++)
            {
                var slotIndex = i;
                var button = CreateButton(itemPanel, $"ItemButton_{slotIndex}", $"スロット {slotIndex + 1}\n空き", () => OnItemButtonPressed(slotIndex));
                SetAnchors((RectTransform)button.transform, 0.05f + (slotIndex * 0.31f), 0.08f, 0.31f + (slotIndex * 0.31f), 0.68f, 0f, 0f, 0f, 0f);
                itemButtons[slotIndex] = button;
                itemButtonTexts[slotIndex] = button.GetComponentInChildren<Text>();
            }

            battleEndActionButton = CreateButton(sidePanel, "EndActionButton", "行動終了", EndPlayerActionCycle);
            SetAnchors((RectTransform)battleEndActionButton.transform, 0.05f, 0.14f, 0.45f, 0.20f, 0f, 0f, 0f, 0f);

            battleCancelTargetButton = CreateButton(sidePanel, "CancelTargetButton", "選択解除", OnBattleSecondaryButtonPressed);
            SetAnchors((RectTransform)battleCancelTargetButton.transform, 0.55f, 0.14f, 0.95f, 0.20f, 0f, 0f, 0f, 0f);

            battleConfirmActionButton = CreateButton(sidePanel, "ConfirmActionButton", "攻撃確定", OnBattleConfirmActionPressed);
            SetAnchors((RectTransform)battleConfirmActionButton.transform, 0.05f, 0.14f, 0.33f, 0.20f, 0f, 0f, 0f, 0f);
            SetAnchors((RectTransform)battleEndActionButton.transform, 0.36f, 0.14f, 0.64f, 0.20f, 0f, 0f, 0f, 0f);
            SetAnchors((RectTransform)battleCancelTargetButton.transform, 0.67f, 0.14f, 0.95f, 0.20f, 0f, 0f, 0f, 0f);

            var logPanel = CreatePanel(sidePanel.transform, "LogPanel", PanelStrongColor);
            SetAnchors(logPanel, 0.05f, 0.02f, 0.95f, 0.12f, 0f, 0f, 0f, 0f);
            battleLogText = CreateText(logPanel.transform, "LogText", 14, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor);
            Stretch((RectTransform)battleLogText.transform, 12f, 12f, 12f, 12f);
        }

        private void BuildOverlayUi(RectTransform parent)
        {
            overlayRoot = CreatePanel(parent, "OverlayRoot", new Color(0f, 0f, 0f, 0.78f));
            Stretch(overlayRoot);

            var card = CreatePanel(overlayRoot, "OverlayCard", PanelColor);
            SetAnchors(card, 0.16f, 0.12f, 0.84f, 0.88f, 0f, 0f, 0f, 0f);

            overlayTitleText = CreateText(card.transform, "OverlayTitle", 28, FontStyle.Bold, TextAnchor.UpperCenter);
            SetAnchors((RectTransform)overlayTitleText.transform, 0.08f, 0.90f, 0.92f, 0.98f, 0f, 0f, 0f, 0f);

            overlayBodyText = CreateText(card.transform, "OverlayBody", 18, FontStyle.Normal, TextAnchor.UpperCenter, MutedTextColor);
            SetAnchors((RectTransform)overlayBodyText.transform, 0.08f, 0.80f, 0.92f, 0.90f, 0f, 0f, 0f, 0f);

            overlayCardRoot = CreatePanel(card.transform, "OverlayCardRoot", Color.clear);
            SetAnchors(overlayCardRoot, 0.05f, 0.17f, 0.95f, 0.78f, 0f, 0f, 0f, 0f);

            overlayButtonRoot = CreatePanel(card.transform, "OverlayButtonRoot", Color.clear);
            SetAnchors(overlayButtonRoot, 0.15f, 0.04f, 0.85f, 0.14f, 0f, 0f, 0f, 0f);
            var buttonLayout = overlayButtonRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 16f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlHeight = true;
            buttonLayout.childControlWidth = true;
            buttonLayout.childForceExpandHeight = false;
            buttonLayout.childForceExpandWidth = true;

            overlayRoot.gameObject.SetActive(false);
        }

        private void ShowLobby()
        {
            titleText.text = "NOCTIS ULTOR : Crown of Ash";
            subtitleText.text = "Unity プロトタイプ";
            SetView(true, false, false);
            ClearChildren(menuContentRoot);

            CreateInfoBlock(menuContentRoot, "現在の永続データ",
                $"トークン: {saveData.TokenCount}\n" +
                $"契約精霊: {GetSpiritName((SpiritType)saveData.SelectedSpirit)}\n" +
                $"攻撃力レベル: {saveData.AttackUpgradeLevel}\n" +
                $"開始SP: {saveData.StartingSkillPointUpgradeLevel}\n" +
                $"HPレベル: {PrototypeBalanceTables.GetPermanentMaxHp(saveData)}\n" +
                $"装備中の王の証: {GetSkillName((SkillId)saveData.EquippedSeal)}\n" +
                $"終わりのない戦い最高到達: {Mathf.Max(0, saveData.EndlessBestStage)} ステージ");

            CreateActionBlock(
                menuContentRoot,
                "メニュー",
                ("キャラクター強化", ShowEnhancementScreen),
                ("戦場へ向かう", ShowStageSelectScreen),
                ("終わりのない戦い", ShowEndlessLobby),
                ("セーブを初期化", ResetPermanentSave));
        }

        private void ShowEnhancementScreen()
        {
            titleText.text = "キャラクター強化";
            subtitleText.text = "永続データ";
            SetView(true, false, false);
            ClearChildren(menuContentRoot);

            CreateInfoBlock(menuContentRoot, "トークン所持数", $"{saveData.TokenCount}");

            CreateSelectionBlock(
                menuContentRoot,
                "契約精霊",
                Enum.GetValues(typeof(SpiritType)).Cast<SpiritType>().Select(
                    spirit => (GetSpiritName(spirit) + (((SpiritType)saveData.SelectedSpirit) == spirit ? " (選択中)" : string.Empty),
                        new Action(() =>
                        {
                            saveData.SelectedSpirit = (int)spirit;
                            PrototypePersistence.Save(saveData);
                            ShowEnhancementScreen();
                        }))).ToArray());

            CreateSelectionBlock(
                menuContentRoot,
                "永続強化",
                ($"攻撃力を強化 ({saveData.AttackUpgradeLevel} -> {Mathf.Min(6, saveData.AttackUpgradeLevel + 1)}) / コスト {FormatCost(PrototypeBalanceTables.GetAttackUpgradeCost(saveData))}",
                    new Action(() => TryUpgradePermanentStat(PermanentUpgrade.Attack))),
                ($"開始SPを強化 ({saveData.StartingSkillPointUpgradeLevel} -> {Mathf.Min(5, saveData.StartingSkillPointUpgradeLevel + 1)}) / コスト {FormatCost(PrototypeBalanceTables.GetStartingSkillPointUpgradeCost(saveData))}",
                    new Action(() => TryUpgradePermanentStat(PermanentUpgrade.StartingSp))),
                ($"HPを強化 ({PrototypeBalanceTables.GetPermanentMaxHp(saveData)} -> {Mathf.Min(5, PrototypeBalanceTables.GetPermanentMaxHp(saveData) + 1)}) / コスト {FormatCost(PrototypeBalanceTables.GetHpUpgradeCost(saveData))}",
                    new Action(() => TryUpgradePermanentStat(PermanentUpgrade.Hp))));

            var sealOptions = new List<(string, Action)>
            {
                ("王の証なし" + ((SkillId)saveData.EquippedSeal == SkillId.None ? " (選択中)" : string.Empty), () =>
                {
                    saveData.EquippedSeal = (int)SkillId.None;
                    PrototypePersistence.Save(saveData);
                    ShowEnhancementScreen();
                }),
            };

            foreach (var unlockedSeal in saveData.UnlockedSeals.Select(value => (SkillId)value).OrderBy(value => value))
            {
                var sealLabel = GetSkillName(unlockedSeal) + (((SkillId)saveData.EquippedSeal) == unlockedSeal ? " (選択中)" : string.Empty);
                sealOptions.Add((sealLabel, () =>
                {
                    saveData.EquippedSeal = (int)unlockedSeal;
                    PrototypePersistence.Save(saveData);
                    ShowEnhancementScreen();
                }));
            }

            CreateSelectionBlock(menuContentRoot, "装備する王の証", sealOptions.ToArray());

            var selectedSpirit = (SpiritType)saveData.SelectedSpirit;
            CreateInfoBlock(
                menuContentRoot,
                "現在の基本スキル",
                $"固定スキル1: {GetSkillName(PrototypeBalanceTables.GetPrimarySkill(selectedSpirit))}\n" +
                $"固定スキル2: {GetSkillName((SkillId)saveData.EquippedSeal)}\n" +
                $"ランダムスキル: 戦闘中に最大2つ習得");

            CreateActionBlock(menuContentRoot, "戻る", ("ロビーへ戻る", ShowLobby));
        }

        private void ShowStageSelectScreen()
        {
            titleText.text = "面選択";
            subtitleText.text = "全5面 / 各3ステージ";
            SetView(true, false, false);
            ClearChildren(menuContentRoot);

            foreach (var realm in Enum.GetValues(typeof(RealmId)).Cast<RealmId>())
            {
                var reward = PrototypeBalanceTables.GetRealmClearReward(realm);
                CreateActionBlock(
                    menuContentRoot,
                    $"{PrototypeBalanceTables.GetRealmName(realm)}",
                    ($"開始する / クリア報酬: {reward.x}トークン + {GetSkillName((SkillId)reward.y)}", () => StartRealmRun(realm)));
            }

            CreateActionBlock(menuContentRoot, "戻る", ("ロビーへ戻る", ShowLobby));
        }

        private void ShowEndlessLobby()
        {
            titleText.text = "終わりのない戦い";
            subtitleText.text = "15ステージ循環 / 永続報酬なし";
            SetView(true, false, false);
            ClearChildren(menuContentRoot);

            CreateInfoBlock(
                menuContentRoot,
                "進行ルール",
                "1面から5面までの15ステージを連続で攻略します。\n" +
                "16ステージ以降はポーンが1体ずつ増加します。\n" +
                "32ステージ以降は全駒のHPが毎ステージ+2されます。\n" +
                $"最高到達記録: {saveData.EndlessBestStage} ステージ");

            CreateActionBlock(
                menuContentRoot,
                "開始",
                ("終わりのない戦いを始める", StartEndlessRun),
                ("ロビーへ戻る", ShowLobby));
        }

        private void ShowPermanentResultScreen(string title, string body)
        {
            titleText.text = title;
            subtitleText.text = "リザルト";
            SetView(true, false, false);
            ClearChildren(menuContentRoot);
            CreateInfoBlock(menuContentRoot, title, body);
            CreateActionBlock(menuContentRoot, "次の操作", ("ロビーへ戻る", ShowLobby), ("面選択へ", ShowStageSelectScreen));
        }

        private void StartRealmRun(RealmId realm)
        {
            run = CreateBaseRun(false, realm);
            LoadCurrentStage();
            EnterBattleScreen();
        }

        private void StartEndlessRun()
        {
            run = CreateBaseRun(true, RealmId.KnightKing);
            LoadCurrentStage();
            EnterBattleScreen();
        }

        private PrototypeBattleRun CreateBaseRun(bool endlessMode, RealmId realm)
        {
            var created = new PrototypeBattleRun
            {
                EndlessMode = endlessMode,
                Realm = realm,
                RealmStageIndex = 0,
                EndlessStageNumber = 1,
                TurnNumber = 1,
                Spirit = (SpiritType)saveData.SelectedSpirit,
                PrimarySkill = PrototypeBalanceTables.GetPrimarySkill((SpiritType)saveData.SelectedSpirit),
                EquippedSeal = (SkillId)saveData.EquippedSeal,
                EquippedSealUsesRemaining = (SkillId)saveData.EquippedSeal == SkillId.None ? 0 : 1,
                Attack = PrototypeBalanceTables.GetPermanentAttack(saveData),
                MaxHp = PrototypeBalanceTables.GetPermanentMaxHp(saveData),
                Hp = PrototypeBalanceTables.GetPermanentMaxHp(saveData),
                SkillPoints = PrototypeBalanceTables.GetPermanentStartingSkillPoints(saveData),
                Coins = 0,
                Level = 1,
                Experience = 0,
                NextLevelExperience = 1,
                SpawnPosition = new Vector2Int(3, 0),
                PlayerPosition = new Vector2Int(3, 0),
                BattleState = BattleState.PlayerTurn,
                BannerMessage = "あなたのターン",
            };
            created.RandomSkills = new List<SkillId>();
            created.Inventory = new List<ItemId>();
            return created;
        }

        private void EnterBattleScreen()
        {
            titleText.text = "対局画面";
            subtitleText.text = run.EndlessMode ? "終わりのない戦い" : PrototypeBalanceTables.GetRealmName(run.Realm);
            SetView(false, true, false);
            PreparePlayerTurn(false);
            RefreshBattleUi();
        }

        private void LoadCurrentStage()
        {
            PrototypeStageDefinition stageDefinition;
            if (run.EndlessMode)
            {
                stageDefinition = PrototypeBalanceTables.BuildEndlessStage(run.EndlessStageNumber);
                run.Realm = stageDefinition.Realm;
                run.RealmStageIndex = stageDefinition.RealmStageIndex;
            }
            else
            {
                stageDefinition = PrototypeBalanceTables.GetStageDefinition(run.Realm, run.RealmStageIndex);
            }

            run.Enemies = stageDefinition.Spawns
                .Select(
                    (spawn, index) => new PrototypeEnemyState
                    {
                        RuntimeId = index + 1,
                        PieceType = spawn.PieceType,
                        Position = spawn.Position,
                        MaxHp = PrototypeBalanceTables.GetPieceHp(spawn.PieceType, run.Realm, run.EndlessMode ? run.EndlessStageNumber : 0),
                        Hp = PrototypeBalanceTables.GetPieceHp(spawn.PieceType, run.Realm, run.EndlessMode ? run.EndlessStageNumber : 0),
                    })
                .ToList();

            run.FieldEffects = new List<PrototypeFieldEffectState>();
            run.EquippedSealUsesRemaining = run.EquippedSeal == SkillId.None ? 0 : 1;
            run.PreviewEnemyRuntimeIds = new List<int>();
            run.PlayerPosition = run.SpawnPosition;
            run.BerserkMode = false;
            run.TurnState.HeroRewardDoubleStageFlag = 0;
            battleLogLines.Clear();
            AddBattleLog($"ステージ開始: {stageDefinition.DisplayName}");
            run.BannerMessage = "あなたのターン";
        }

        private void PreparePlayerTurn(bool isExtraTurn)
        {
            run.BattleState = BattleState.PlayerTurn;
            run.PendingSkillTarget = SkillId.None;
            run.PendingItemTarget = ItemId.None;
            run.PendingItemSlotIndex = -1;
            run.HasPendingTargetConfirmation = false;
            run.PendingTargetTile = run.PlayerPosition;
            run.PendingReplacementSkill = SkillId.None;
            targetableTiles.Clear();
            validMoveTiles.Clear();
            dangerTiles.Clear();

            run.TurnState.PreActionUsed = false;
            run.TurnState.PostActionUsed = false;
            run.TurnState.Moved = false;
            run.TurnState.MoveOriginPosition = run.PlayerPosition;
            run.TurnState.HasPendingMove = false;
            run.TurnState.PendingMovePosition = run.PlayerPosition;

            if (!isExtraTurn)
            {
                if (run.TurnState.PendingAttackDoubleTurns > 0)
                {
                    run.TurnState.ActiveAttackDoubleTurns = run.TurnState.PendingAttackDoubleTurns;
                    run.TurnState.PendingAttackDoubleTurns = 0;
                }

                if (run.TurnState.PendingHealOnKillTurns > 0)
                {
                    run.TurnState.ActiveHealOnKillTurns = run.TurnState.PendingHealOnKillTurns;
                    run.TurnState.PendingHealOnKillTurns = 0;
                }

                SelectPreviewEnemies();
                run.BannerMessage = $"{run.TurnNumber}ターン目 / あなたのターン";
            }
            else
            {
                run.BannerMessage = "エクストラターン";
            }

            foreach (var tile in GetPlayerValidMoveTiles())
            {
                validMoveTiles.Add(tile);
            }

            foreach (var dangerTile in GetDangerTilesForPreview())
            {
                dangerTiles.Add(dangerTile);
            }

            TryOpenLevelUp();
        }

        private void SelectPreviewEnemies()
        {
            run.PreviewEnemyRuntimeIds = run.Enemies.Where(enemy => enemy.FreezeTurns <= 0 && GetEnemyValidMoveTiles(enemy).Count > 0).Select(enemy => enemy.RuntimeId).ToList();
            if (run.PreviewEnemyRuntimeIds.Count > MaxActingEnemiesPerTurn)
            {
                run.PreviewEnemyRuntimeIds = run.PreviewEnemyRuntimeIds.OrderBy(_ => random.Next()).Take(MaxActingEnemiesPerTurn).ToList();
            }

            foreach (var enemy in run.Enemies)
            {
                enemy.WillActNextTurn = run.PreviewEnemyRuntimeIds.Contains(enemy.RuntimeId);
            }
        }

        private IEnumerable<Vector2Int> GetDangerTilesForPreview()
        {
            foreach (var runtimeId in run.PreviewEnemyRuntimeIds)
            {
                var enemy = GetEnemyByRuntimeId(runtimeId);
                if (enemy == null)
                {
                    continue;
                }

                foreach (var tile in GetEnemyValidMoveTiles(enemy))
                {
                    yield return tile;
                }
            }
        }

        private void RefreshBattleUi()
        {
            if (run == null)
            {
                return;
            }

            var stageDefinition = run.EndlessMode
                ? PrototypeBalanceTables.BuildEndlessStage(run.EndlessStageNumber)
                : PrototypeBalanceTables.GetStageDefinition(run.Realm, run.RealmStageIndex);

            battleStageText.text = $"{stageDefinition.DisplayName}\n{stageDefinition.Objective}";
            battleTurnText.text = run.BannerMessage;
            battleInstructionText.text = GetCurrentBattleInstructionText();
            battleStatsText.text =
                $"HP {run.Hp}/{run.MaxHp}\n" +
                $"攻撃 {GetCurrentAttackValue()} (基本 {run.Attack})\n" +
                $"SP {run.SkillPoints}/{20}\n" +
                $"コイン {run.Coins}\n" +
                $"Lv {run.Level} / EXP {run.Experience}/{run.NextLevelExperience}\n" +
                $"敵残数 {run.Enemies.Count}\n" +
                $"状態 {(run.BerserkMode ? "バーサーク" : "通常")}";
            battleSkillSummaryText.text =
                $"固定1: {GetSkillName(run.PrimarySkill)}\n" +
                $"固定2: {GetSkillName(run.EquippedSeal)}{(run.EquippedSeal != SkillId.None ? $" ({run.EquippedSealUsesRemaining}回)" : string.Empty)}\n" +
                $"習得スキル: {(run.RandomSkills.Count == 0 ? "なし" : string.Join(" / ", run.RandomSkills.Select(GetSkillName)))}";
            battleLogText.text = string.Join("\n", battleLogLines.TakeLast(8));

            RefreshSkillButtons();
            RefreshItemButtons();
            RefreshBoard();
            var hasPendingTargetConfirmation = HasPendingTargetConfirmation();
            battleConfirmActionButton.gameObject.SetActive(hasPendingTargetConfirmation);
            battleConfirmActionButton.interactable = run.BattleState == BattleState.PlayerTurn && hasPendingTargetConfirmation;
            battleEndActionButton.interactable = run.BattleState == BattleState.PlayerTurn && !hasPendingTargetConfirmation;
            var secondaryButtonText = battleCancelTargetButton.GetComponentInChildren<Text>();
            if (run.BattleState == BattleState.Targeting)
            {
                battleCancelTargetButton.gameObject.SetActive(true);
                secondaryButtonText.text = "選択解除";
            }
            else if (hasPendingTargetConfirmation)
            {
                battleCancelTargetButton.gameObject.SetActive(true);
                secondaryButtonText.text = "攻撃取消";
            }
            else if (HasPendingMoveSelection())
            {
                battleCancelTargetButton.gameObject.SetActive(true);
                secondaryButtonText.text = "移動取消";
            }
            else
            {
                battleCancelTargetButton.gameObject.SetActive(false);
            }
        }

        private string GetBattleInstructionText()
        {
            return run.BattleState switch
            {
                BattleState.PlayerTurn => HasPendingMoveSelection()
                    ? "仮移動中です。候補内なら何度でも選び直せます。スキル、アイテム、行動終了で移動が確定します。"
                    : "移動前と移動後に1回ずつスキルかアイテムが使えます。行動を終えたら「行動終了」を押してください。",
                BattleState.Targeting => GetTargetingInstructionText(),
                BattleState.Shop => "ショップで購入後、「次のステージへ」で進行します。",
                BattleState.LevelUp => "3つの候補から1つ選んでください。",
                BattleState.ReplaceSkill => "置き換えるランダムスキルを選んでください。",
                _ => "進行中です。",
            };
        }

        private string GetCurrentBattleInstructionText()
        {
            if (run.BattleState == BattleState.PlayerTurn)
            {
                if (HasPendingTargetConfirmation())
                {
                    return "攻撃対象を仮選択中です。攻撃確定で移動と攻撃をまとめて発動します。移動先を変えると対象は解除されます。";
                }

                return HasPendingMoveSelection()
                    ? "仮移動中です。候補内なら何度でも選び直せます。スキル、アイテム、行動終了で移動が確定します。"
                    : "移動後に1回だけスキルかアイテムを使えます。行動を終えたら、行動終了を押してください。";
            }

            return GetBattleInstructionText();
        }

        private string GetTargetingInstructionText()
        {
            if (run.PendingSkillTarget != SkillId.None)
            {
                return $"{GetSkillName(run.PendingSkillTarget)} の対象を盤面から選択してください。";
            }

            if (run.PendingItemTarget != ItemId.None)
            {
                return $"{GetItemName(run.PendingItemTarget)} の対象を盤面から選択してください。";
            }

            return "対象を選択してください。";
        }

        private void RefreshSkillButtons()
        {
            var skills = new List<SkillId> { run.PrimarySkill, run.EquippedSeal };
            skills.AddRange(run.RandomSkills);
            while (skills.Count < 4)
            {
                skills.Add(SkillId.None);
            }

            for (int i = 0; i < skillButtons.Length; i++)
            {
                var skillId = skills[i];
                skillButtonTexts[i].text = skillId == SkillId.None
                    ? $"スキル枠 {i + 1}\n空き"
                    : $"{GetSkillName(skillId)}\nSP {PrototypeBalanceTables.GetSkill(skillId).SkillPointCost}";
                skillButtons[i].interactable = skillId != SkillId.None && run.BattleState == BattleState.PlayerTurn && CanUseSupportActionNow() && !HasPendingTargetConfirmation();
            }
        }

        private void RefreshItemButtons()
        {
            for (int i = 0; i < InventoryLimit; i++)
            {
                if (i < run.Inventory.Count)
                {
                    var item = PrototypeBalanceTables.GetItem(run.Inventory[i]);
                    itemButtonTexts[i].text = $"{item.DisplayName}\n{item.Cost}c";
                    itemButtons[i].interactable = run.BattleState == BattleState.PlayerTurn && CanUseSupportActionNow() && !HasPendingTargetConfirmation();
                }
                else
                {
                    itemButtonTexts[i].text = $"スロット {i + 1}\n空き";
                    itemButtons[i].interactable = false;
                }
            }
        }

        private void RefreshBoard()
        {
            var displayedPlayerPosition = GetDisplayedPlayerPosition();
            for (int y = BoardSize - 1; y >= 0; y--)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    var tilePosition = new Vector2Int(x, y);
                    var index = ToIndex(tilePosition);
                    var image = boardTileImages[index];
                    var text = boardTileTexts[index];

                    image.color = (x + y) % 2 == 0 ? TileLightColor : TileDarkColor;
                    if (tilePosition == displayedPlayerPosition)
                    {
                        image.color = TileSelectedColor;
                    }
                    else if (targetableTiles.Contains(tilePosition))
                    {
                        image.color = TileTargetColor;
                    }
                    else if (validMoveTiles.Contains(tilePosition))
                    {
                        image.color = dangerTiles.Contains(tilePosition) ? TileMoveDangerColor : TileMoveSafeColor;
                    }

                    var enemy = GetEnemyAt(tilePosition);
                    if (tilePosition == displayedPlayerPosition)
                    {
                        text.text = $"王女\nHP {run.Hp}";
                        text.color = PlayerTextColor;
                        continue;
                    }

                    if (enemy != null)
                    {
                        var markers = enemy.WillActNextTurn ? "!" : string.Empty;
                        var status = BuildEnemyStatusSuffix(enemy);
                        text.text = $"{markers}{GetPieceShortName(enemy.PieceType)}\n{enemy.Hp}/{enemy.MaxHp}{status}";
                        text.color = EnemyTextColor;
                        continue;
                    }

                    text.text = $"{(char)('A' + x)}{y + 1}";
                    text.color = MutedTextColor;
                }
            }
        }

        private string BuildEnemyStatusSuffix(PrototypeEnemyState enemy)
        {
            var result = string.Empty;
            if (enemy.FreezeTurns > 0)
            {
                result += "\n凍結";
            }

            if (enemy.ShockTurns > 0)
            {
                result += "\n感電";
            }

            if (enemy.HasCurse)
            {
                result += "\n呪い";
            }

            return result;
        }

        private void OnSkillButtonPressed(int slotIndex)
        {
            if (run == null || run.BattleState != BattleState.PlayerTurn)
            {
                return;
            }

            var skillId = GetSkillFromSlot(slotIndex);
            if (skillId == SkillId.None)
            {
                return;
            }

            var skill = PrototypeBalanceTables.GetSkill(skillId);
            if (!CanUseSupportActionNow())
            {
                AddBattleLog("このタイミングではスキルを使えません。");
                RefreshBattleUi();
                return;
            }

            if (skill.IsSingleUsePerStage && run.EquippedSealUsesRemaining <= 0)
            {
                AddBattleLog("この王の証は今ステージでは使い切りました。");
                RefreshBattleUi();
                return;
            }

            if (!CanAffordSkillPoints(skill))
            {
                AddBattleLog("SPが足りません。");
                RefreshBattleUi();
                return;
            }

            if (skill.RequiresTarget)
            {
                run.PendingSkillTarget = skillId;
                run.PendingItemTarget = ItemId.None;
                run.PendingItemSlotIndex = -1;
                run.HasPendingTargetConfirmation = false;
                run.BattleState = BattleState.Targeting;
                targetableTiles.Clear();
                foreach (var tile in GetTargetableTilesForSkill(skillId))
                {
                    targetableTiles.Add(tile);
                }

                RefreshBattleUi();
                return;
            }

            ResolveSkill(skillId, null);
        }

        private void OnItemButtonPressed(int slotIndex)
        {
            if (run == null || run.BattleState != BattleState.PlayerTurn || slotIndex >= run.Inventory.Count)
            {
                return;
            }

            if (!CanUseSupportActionNow())
            {
                AddBattleLog("このタイミングではアイテムを使えません。");
                RefreshBattleUi();
                return;
            }

            var itemId = run.Inventory[slotIndex];
            var item = PrototypeBalanceTables.GetItem(itemId);
            if (item.RequiresTarget)
            {
                run.PendingItemTarget = itemId;
                run.PendingSkillTarget = SkillId.None;
                run.PendingItemSlotIndex = slotIndex;
                run.HasPendingTargetConfirmation = false;
                run.BattleState = BattleState.Targeting;
                targetableTiles.Clear();
                foreach (var tile in GetTargetableTilesForItem(itemId))
                {
                    targetableTiles.Add(tile);
                }

                RefreshBattleUi();
                return;
            }

            ResolveItem(itemId, slotIndex, null);
        }

        private void CancelTargetSelection()
        {
            if (run == null)
            {
                return;
            }

            ClearPendingTargetAction();
            run.BattleState = BattleState.PlayerTurn;
            RefreshBattleUi();
        }

        private void OnBattleSecondaryButtonPressed()
        {
            if (run == null)
            {
                return;
            }

            if (run.BattleState == BattleState.Targeting)
            {
                CancelTargetSelection();
                return;
            }

            if (HasPendingTargetConfirmation())
            {
                CancelPendingTargetConfirmation();
                return;
            }

            if (HasPendingMoveSelection())
            {
                CancelPendingMoveSelection();
            }
        }

        private void CancelPendingMoveSelection()
        {
            if (run == null || !HasPendingMoveSelection())
            {
                return;
            }

            ClearPendingTargetAction();
            run.TurnState.HasPendingMove = false;
            run.TurnState.PendingMovePosition = run.PlayerPosition;
            run.TurnState.Moved = false;
            UpdateMoveAndDangerTiles();
            RefreshBattleUi();
        }

        private void OnBoardTilePressed(Vector2Int tilePosition)
        {
            if (run == null)
            {
                return;
            }

            if (run.BattleState == BattleState.Targeting)
            {
                if (!targetableTiles.Contains(tilePosition))
                {
                    AddBattleLog("対象にできないマスです。");
                    RefreshBattleUi();
                    return;
                }

                if (run.PendingSkillTarget != SkillId.None)
                {
                    if (ShouldDelayTargetedSkillResolution(run.PendingSkillTarget))
                    {
                        QueuePendingTargetConfirmation(tilePosition);
                    }
                    else
                    {
                        ResolveSkill(run.PendingSkillTarget, tilePosition);
                    }
                }
                else if (run.PendingItemTarget != ItemId.None)
                {
                    if (ShouldDelayTargetedItemResolution(run.PendingItemTarget))
                    {
                        QueuePendingTargetConfirmation(tilePosition);
                    }
                    else
                    {
                        ResolveItem(run.PendingItemTarget, run.PendingItemSlotIndex, tilePosition);
                    }
                }

                return;
            }

            if (run.BattleState != BattleState.PlayerTurn)
            {
                return;
            }

            if (run.TurnState.Moved && !HasPendingMoveSelection())
            {
                return;
            }

            if (!validMoveTiles.Contains(tilePosition))
            {
                return;
            }

            if (HasPendingTargetConfirmation() && (!HasPendingMoveSelection() || tilePosition != run.TurnState.PendingMovePosition))
            {
                AddBattleLog("移動先を変更したため、仮選択していた攻撃対象を解除しました。");
                ClearPendingTargetAction();
            }

            ExecutePlayerMove(tilePosition);
        }

        private void ExecutePlayerMove(Vector2Int destination)
        {
            run.TurnState.PendingMovePosition = destination;
            run.TurnState.HasPendingMove = true;
            run.TurnState.Moved = true;

            UpdateMoveAndDangerTiles();
            RefreshBattleUi();
        }

        private void EndPlayerActionCycle()
        {
            if (run == null || run.BattleState != BattleState.PlayerTurn)
            {
                return;
            }

            if (!run.TurnState.Moved && validMoveTiles.Count > 0)
            {
                AddBattleLog("このターンはまだ移動していません。");
                RefreshBattleUi();
                return;
            }

            if (HasPendingTargetConfirmation())
            {
                AddBattleLog("攻撃を確定するか、攻撃取消でやり直してください。");
                RefreshBattleUi();
                return;
            }

            if (!CommitPendingMoveIfNeeded())
            {
                RefreshBattleUi();
                return;
            }

            if (run.TurnState.ExtraTurnsRemaining > 0)
            {
                run.TurnState.ExtraTurnsRemaining--;
                PreparePlayerTurn(true);
                RefreshBattleUi();
                return;
            }

            ExecuteEnemyTurn();
        }

        private bool HasPendingMoveSelection()
        {
            return run != null && run.TurnState.HasPendingMove;
        }

        private bool HasPendingTargetConfirmation()
        {
            return run != null &&
                run.HasPendingTargetConfirmation &&
                (run.PendingSkillTarget != SkillId.None || run.PendingItemTarget != ItemId.None);
        }

        private Vector2Int GetDisplayedPlayerPosition()
        {
            return HasPendingMoveSelection() ? run.TurnState.PendingMovePosition : run.PlayerPosition;
        }

        private Vector2Int GetEnemyTargetPlayerPosition()
        {
            return HasPendingMoveSelection() ? run.TurnState.PendingMovePosition : run.PlayerPosition;
        }

        private void OnBattleConfirmActionPressed()
        {
            if (run == null || run.BattleState != BattleState.PlayerTurn || !HasPendingTargetConfirmation())
            {
                return;
            }

            var skillId = run.PendingSkillTarget;
            var itemId = run.PendingItemTarget;
            var itemSlotIndex = run.PendingItemSlotIndex;
            var targetTile = run.PendingTargetTile;
            ClearPendingTargetAction();

            if (skillId != SkillId.None)
            {
                ResolveSkill(skillId, targetTile);
                return;
            }

            if (itemId != ItemId.None)
            {
                ResolveItem(itemId, itemSlotIndex, targetTile);
            }
        }

        private void CancelPendingTargetConfirmation()
        {
            if (run == null || !HasPendingTargetConfirmation())
            {
                return;
            }

            ClearPendingTargetAction();
            UpdateMoveAndDangerTiles();
            RefreshBattleUi();
        }

        private void ClearPendingTargetAction()
        {
            if (run == null)
            {
                return;
            }

            run.PendingSkillTarget = SkillId.None;
            run.PendingItemTarget = ItemId.None;
            run.PendingItemSlotIndex = -1;
            run.HasPendingTargetConfirmation = false;
            run.PendingTargetTile = GetDisplayedPlayerPosition();
            targetableTiles.Clear();
        }

        private void QueuePendingTargetConfirmation(Vector2Int tilePosition)
        {
            run.HasPendingTargetConfirmation = true;
            run.PendingTargetTile = tilePosition;
            run.BattleState = BattleState.PlayerTurn;
            targetableTiles.Clear();
            targetableTiles.Add(tilePosition);
            RefreshBattleUi();
        }

        private bool ShouldDelayTargetedSkillResolution(SkillId skillId)
        {
            if (!HasPendingMoveSelection())
            {
                return false;
            }

            var skill = PrototypeBalanceTables.GetSkill(skillId);
            return skill.RequiresTarget && (skill.DealsDamage || skill.InstantKill || skill.AppliesFreeze || skill.AppliesShock || skill.AppliesCurse);
        }

        private bool ShouldDelayTargetedItemResolution(ItemId itemId)
        {
            if (!HasPendingMoveSelection())
            {
                return false;
            }

            var item = PrototypeBalanceTables.GetItem(itemId);
            return item.RequiresTarget && (item.DealsDamage || item.InstantKill || item.AppliesFreeze || item.AppliesShock || item.AppliesCurse);
        }

        private bool CommitPendingMoveIfNeeded()
        {
            if (!HasPendingMoveSelection())
            {
                return true;
            }

            var destination = run.TurnState.PendingMovePosition;
            var enemy = GetEnemyAt(destination);
            run.PlayerPosition = destination;
            run.TurnState.HasPendingMove = false;
            run.TurnState.PendingMovePosition = destination;

            if (enemy != null)
            {
                AddBattleLog($"{ToBoardLabel(destination)}で{GetPieceName(enemy.PieceType)}を撃破しました。");
                DefeatEnemy(enemy, "移動撃破", true);
            }
            else
            {
                AddBattleLog($"{ToBoardLabel(destination)}へ移動しました。");
            }

            if (run.TurnState.NextMoveAnywhere)
            {
                run.TurnState.NextMoveAnywhere = false;
            }

            if (run.PlayerPosition.y == BoardSize - 1 && !run.BerserkMode)
            {
                run.BerserkMode = true;
                AddBattleLog("敵陣最奥へ到達し、バーサークモードが発動しました。");
            }

            UpdateMoveAndDangerTiles();
            CheckBattleProgress();
            if (run != null && (run.BattleState == BattleState.PlayerTurn || run.BattleState == BattleState.Targeting))
            {
                TryOpenLevelUp();
            }

            return run != null && (run.BattleState == BattleState.PlayerTurn || run.BattleState == BattleState.Targeting);
        }

        private void ExecuteEnemyTurn()
        {
            run.BattleState = BattleState.EnemyTurn;
            run.BannerMessage = "敵のターン";
            ApplyCurseDamageAtEnemyTurnStart();
            if (run.BattleState == BattleState.GameOver || run.BattleState == BattleState.RealmResult)
            {
                return;
            }

            CheckBattleProgress();
            if (run.BattleState != BattleState.EnemyTurn)
            {
                UpdatePlayerTurnBuffDurationsAfterEnemyTurn();
                run.TurnNumber++;
                RefreshBattleUi();
                return;
            }

            foreach (var runtimeId in run.PreviewEnemyRuntimeIds.ToList())
            {
                var enemy = GetEnemyByRuntimeId(runtimeId);
                if (enemy == null)
                {
                    continue;
                }

                if (enemy.FreezeTurns > 0)
                {
                    enemy.FreezeTurns--;
                    AddBattleLog($"{GetPieceName(enemy.PieceType)}は凍結中で動けません。");
                    if (enemy.ShockTurns > 0)
                    {
                        enemy.ShockTurns--;
                    }

                    continue;
                }

                var validMoves = GetEnemyValidMoveTiles(enemy);
                if (validMoves.Count == 0)
                {
                    if (enemy.ShockTurns > 0)
                    {
                        enemy.ShockTurns--;
                    }

                    continue;
                }

                var chosenMove = ChooseEnemyMove(enemy, validMoves);
                enemy.Position = chosenMove;
                AddBattleLog($"{GetPieceName(enemy.PieceType)}が{ToBoardLabel(chosenMove)}へ移動しました。");

                if (chosenMove == run.PlayerPosition)
                {
                    ApplyDamageToPlayer(1, enemy);
                    if (run.BattleState == BattleState.GameOver || run.BattleState == BattleState.RealmResult)
                    {
                        RefreshBattleUi();
                        return;
                    }
                }

                ApplyFieldEffectsOnEnemyEntry(enemy);
                ApplyShockDamageAfterEnemyMove(enemy);
                CheckBattleProgress();
                if (run.BattleState != BattleState.EnemyTurn)
                {
                    UpdatePlayerTurnBuffDurationsAfterEnemyTurn();
                    run.TurnNumber++;
                    RefreshBattleUi();
                    return;
                }
            }

            UpdatePlayerTurnBuffDurationsAfterEnemyTurn();
            run.TurnNumber++;
            PreparePlayerTurn(false);
            RefreshBattleUi();
        }

        private void ApplyCurseDamageAtEnemyTurnStart()
        {
            foreach (var enemy in run.Enemies.ToList())
            {
                if (!enemy.HasCurse)
                {
                    continue;
                }

                DamageEnemy(enemy, CurseDamage, "呪い");
                if (run.BattleState == BattleState.RealmResult || run.BattleState == BattleState.GameOver)
                {
                    return;
                }
            }
        }

        private void ApplyFieldEffectsOnEnemyEntry(PrototypeEnemyState enemy)
        {
            foreach (var fieldEffect in run.FieldEffects.ToList())
            {
                if (!fieldEffect.Tiles.Contains(enemy.Position))
                {
                    continue;
                }

                switch (fieldEffect.FieldEffectType)
                {
                    case FieldEffectType.ThinIce:
                        if (random.NextDouble() <= 0.7d)
                        {
                            enemy.FreezeTurns += 2;
                            AddBattleLog($"{GetPieceName(enemy.PieceType)}は薄氷で凍結しました。");
                        }
                        break;

                    case FieldEffectType.Tempest:
                        enemy.ShockTurns += 1;
                        AddBattleLog($"{GetPieceName(enemy.PieceType)}は嵐で感電しました。");
                        break;
                }
            }
        }

        private void ApplyShockDamageAfterEnemyMove(PrototypeEnemyState enemy)
        {
            if (enemy.ShockTurns <= 0)
            {
                return;
            }

            DamageEnemy(enemy, 1, "感電");
            enemy.ShockTurns = Mathf.Max(0, enemy.ShockTurns - 1);
        }

        private void UpdatePlayerTurnBuffDurationsAfterEnemyTurn()
        {
            if (run.TurnState.ActiveBarrierEnemyTurns > 0)
            {
                run.TurnState.ActiveBarrierEnemyTurns--;
            }

            if (run.TurnState.ActiveAttackDoubleTurns > 0)
            {
                run.TurnState.ActiveAttackDoubleTurns--;
            }

            if (run.TurnState.ActiveHealOnKillTurns > 0)
            {
                run.TurnState.ActiveHealOnKillTurns--;
            }

            if (run.TurnState.ActiveExpDoubleTurns > 0)
            {
                run.TurnState.ActiveExpDoubleTurns--;
            }

            if (run.TurnState.ActiveCoinDoubleTurns > 0)
            {
                run.TurnState.ActiveCoinDoubleTurns--;
            }

            if (run.TurnState.MadSealTurnsRemaining > 0)
            {
                run.TurnState.MadSealTurnsRemaining--;
            }

            foreach (var fieldEffect in run.FieldEffects.ToList())
            {
                fieldEffect.RemainingEnemyTurns--;
                if (fieldEffect.RemainingEnemyTurns <= 0)
                {
                    run.FieldEffects.Remove(fieldEffect);
                }
            }
        }

        private void ApplyDamageToPlayer(int damage, PrototypeEnemyState attacker)
        {
            if (run.TurnState.IgnoreDamageCharges > 0)
            {
                run.TurnState.IgnoreDamageCharges--;
                AddBattleLog("不死の霊薬がダメージを無効化しました。");
                TriggerThunderWall(attacker);
                return;
            }

            if (run.TurnState.ActiveBarrierEnemyTurns > 0)
            {
                AddBattleLog("クリスタルエスクードがダメージを防ぎました。");
                TriggerThunderWall(attacker);
                return;
            }

            run.Hp -= damage;
            AddBattleLog($"{GetPieceName(attacker.PieceType)}に取られました。HP -{damage}");
            TriggerThunderWall(attacker);

            if (run.Hp <= 0)
            {
                FinalizeGameOver();
                return;
            }

            run.PlayerPosition = FindRespawnPosition();
            AddBattleLog($"{ToBoardLabel(run.PlayerPosition)}にリスポーンしました。");
        }

        private void TriggerThunderWall(PrototypeEnemyState attacker)
        {
            if (!run.TurnState.ThunderWallArmed)
            {
                return;
            }

            run.TurnState.ThunderWallArmed = false;
            if (attacker != null && run.Enemies.Contains(attacker))
            {
                AddBattleLog("サンダーウォールが発動し、敵を倒しました。");
                DefeatEnemy(attacker, "サンダーウォール", false);
            }
        }

        private void ResolveSkill(SkillId skillId, Vector2Int? selectedTile)
        {
            var skill = PrototypeBalanceTables.GetSkill(skillId);
            if (!CommitPendingMoveIfNeeded())
            {
                ClearPendingTargetAction();
                RefreshBattleUi();
                return;
            }

            if (!TrySpendSkillPoints(skill, out var usedFreeSkillCharge))
            {
                AddBattleLog("SPが足りません。");
                run.BattleState = BattleState.PlayerTurn;
                ClearPendingTargetAction();
                RefreshBattleUi();
                return;
            }

            var resolved = false;

            switch (skillId)
            {
                case SkillId.ColdLance:
                    resolved = ResolveLineSkill(skillId, selectedTile, enemy =>
                    {
                        DamageEnemy(enemy, GetCurrentAttackValue(), skill.DisplayName);
                        enemy.FreezeTurns += 2;
                    });
                    break;

                case SkillId.AbsoluteZero:
                    foreach (var enemy in run.Enemies)
                    {
                        enemy.FreezeTurns += 2;
                    }

                    AddBattleLog("すべての敵を凍結させました。");
                    resolved = true;
                    break;

                case SkillId.FrostRain:
                    resolved = ResolveAreaSkill(selectedTile, GetSquareTwoByTwoTiles, enemy =>
                    {
                        DamageEnemy(enemy, GetCurrentAttackValue(), skill.DisplayName);
                        if (random.NextDouble() <= 0.5d)
                        {
                            enemy.FreezeTurns += 2;
                        }
                    });
                    break;

                case SkillId.CrystalEscudo:
                    run.TurnState.ActiveBarrierEnemyTurns = Math.Max(run.TurnState.ActiveBarrierEnemyTurns, 1);
                    AddBattleLog("次の敵ターン終了までダメージ無効です。");
                    resolved = true;
                    break;

                case SkillId.ThinIce:
                    resolved = ResolveFieldSkill(selectedTile, FieldEffectType.ThinIce, 2);
                    break;

                case SkillId.ShatterIce:
                    resolved = ResolveSingleEnemySkill(selectedTile, enemy =>
                    {
                        var damage = GetCurrentAttackValue();
                        if (enemy.FreezeTurns > 0)
                        {
                            damage *= 3;
                        }

                        DamageEnemy(enemy, damage, skill.DisplayName);
                    });
                    break;

                case SkillId.FlameSlash:
                    resolved = ResolveDirectionalAreaSkill(selectedTile, GetFlameSlashTiles, enemy => DamageEnemy(enemy, GetCurrentAttackValue(), skill.DisplayName));
                    break;

                case SkillId.SunWheel:
                    foreach (var enemy in run.Enemies.ToList())
                    {
                        if (GetAroundTwentyFourTiles(run.PlayerPosition).Contains(enemy.Position))
                        {
                            DamageEnemy(enemy, GetCurrentAttackValue(), skill.DisplayName);
                        }
                    }

                    resolved = true;
                    break;

                case SkillId.AzureFlame:
                    resolved = ResolveDirectionalAreaSkill(selectedTile, GetAzureFlameTiles, enemy => DamageEnemy(enemy, GetCurrentAttackValue() * 3, skill.DisplayName));
                    break;

                case SkillId.Hellfire:
                    run.Hp = Mathf.Max(1, run.Hp - 1);
                    foreach (var enemy in run.Enemies.ToList())
                    {
                        DamageEnemy(enemy, GetCurrentAttackValue(), skill.DisplayName);
                    }

                    resolved = true;
                    break;

                case SkillId.Inferno:
                    resolved = ResolveDirectionalAreaSkill(selectedTile, GetLineThreeTiles, enemy => DamageEnemy(enemy, GetCurrentAttackValue() * 2, skill.DisplayName));
                    break;

                case SkillId.BurningHeart:
                    run.TurnState.PendingAttackDoubleTurns = Math.Max(run.TurnState.PendingAttackDoubleTurns, 1);
                    AddBattleLog("次のターンの攻撃力が2倍になります。");
                    resolved = true;
                    break;

                case SkillId.ThunderShock:
                    resolved = ResolveSingleEnemySkill(selectedTile, enemy =>
                    {
                        DamageEnemy(enemy, GetCurrentAttackValue(), skill.DisplayName);
                        enemy.ShockTurns += 2;
                    }, enemy => GetAroundTwentyFourTiles(run.PlayerPosition).Contains(enemy.Position));
                    break;

                case SkillId.Tempest:
                    resolved = ResolveFieldSkill(selectedTile, FieldEffectType.Tempest, 3);
                    break;

                case SkillId.AzureLightningThorn:
                    resolved = ResolveDirectionalAreaSkill(selectedTile, GetLineThreeTiles, enemy =>
                    {
                        DamageEnemy(enemy, GetCurrentAttackValue(), skill.DisplayName);
                        enemy.ShockTurns += 3;
                    });
                    break;

                case SkillId.Resonance:
                    foreach (var enemy in run.Enemies.Where(target => target.ShockTurns > 0).ToList())
                    {
                        DamageEnemy(enemy, GetCurrentAttackValue(), skill.DisplayName);
                    }

                    resolved = true;
                    break;

                case SkillId.BlitzCreek:
                    run.TurnState.ExtraTurnsRemaining++;
                    AddBattleLog("エクストラターンを獲得しました。");
                    resolved = true;
                    break;

                case SkillId.ThunderWall:
                    run.TurnState.ThunderWallArmed = true;
                    AddBattleLog("次に敵に取られた時、相手を倒します。");
                    resolved = true;
                    break;

                case SkillId.WolfStep:
                    run.TurnState.NextMoveAnywhere = true;
                    AddBattleLog("次の移動で好きな場所に移動できます。");
                    resolved = true;
                    break;

                case SkillId.Gluttony:
                    run.TurnState.PendingHealOnKillTurns = Math.Max(run.TurnState.PendingHealOnKillTurns, 1);
                    AddBattleLog("次のターン、撃破ごとにHPを1回復します。");
                    resolved = true;
                    break;

                case SkillId.Kingslayer:
                    resolved = ResolveSingleEnemySkill(selectedTile, enemy => DamageEnemy(enemy, GetCurrentAttackValue(), skill.DisplayName), enemy => GetAdjacentEightTiles(run.PlayerPosition).Contains(enemy.Position));
                    break;

                case SkillId.HomingShot:
                    resolved = ResolveSingleEnemySkill(selectedTile, enemy => DamageEnemy(enemy, GetCurrentAttackValue(), skill.DisplayName));
                    break;

                case SkillId.CurseStrike:
                    resolved = ResolveSingleEnemySkill(selectedTile, enemy =>
                    {
                        enemy.HasCurse = true;
                        AddBattleLog($"{GetPieceName(enemy.PieceType)}に呪いを付与しました。");
                    });
                    break;

                case SkillId.GoldenTouch:
                    resolved = ResolveSingleEnemySkill(selectedTile, enemy =>
                    {
                        var damage = Mathf.FloorToInt(run.Coins * 0.2f);
                        DamageEnemy(enemy, damage, skill.DisplayName);
                    });
                    break;

                case SkillId.SealKnightKing:
                    resolved = ResolveLineSkill(skillId, selectedTile, enemy =>
                    {
                        var damage = Mathf.CeilToInt(enemy.Hp / 2f);
                        DamageEnemy(enemy, damage, skill.DisplayName);
                    });
                    break;

                case SkillId.SealLionKing:
                    resolved = ResolveSingleEnemySkill(selectedTile, enemy => InstantKillEnemy(enemy, skill.DisplayName), enemy => enemy.PieceType != PieceType.King && enemy.PieceType != PieceType.Queen);
                    break;

                case SkillId.SealMadKing:
                    run.TurnState.MadSealTurnsRemaining = 5;
                    AddBattleLog("5ターンの間、撃破ごとにHPを1回復します。");
                    resolved = true;
                    break;

                case SkillId.SealHeroKing:
                    run.TurnState.HeroRewardDoubleStageFlag = 1;
                    AddBattleLog("ステージ終了まで撃破報酬が2倍になります。");
                    resolved = true;
                    break;

                case SkillId.SealAncestorDragonKing:
                    run.TurnState.FreeSkillUsesRemaining += 2;
                    AddBattleLog("次の2回のスキル消費が0になります。");
                    resolved = true;
                    break;
            }

            if (!resolved)
            {
                RestoreSkillPointSpend(skill, usedFreeSkillCharge);
                AddBattleLog("対象を正しく選択できませんでした。");
                run.BattleState = BattleState.PlayerTurn;
                ClearPendingTargetAction();
                RefreshBattleUi();
                return;
            }

            if (skill.IsSingleUsePerStage)
            {
                run.EquippedSealUsesRemaining = Mathf.Max(0, run.EquippedSealUsesRemaining - 1);
            }

            ConsumeSupportActionSlot();
            run.BattleState = BattleState.PlayerTurn;
            ClearPendingTargetAction();
            UpdateMoveAndDangerTiles();
            CheckBattleProgress();
            if (run != null && run.BattleState == BattleState.PlayerTurn)
            {
                TryOpenLevelUp();
            }

            RefreshBattleUi();
        }

        private void ResolveItem(ItemId itemId, int inventoryIndex, Vector2Int? selectedTile)
        {
            if (inventoryIndex < 0 || inventoryIndex >= run.Inventory.Count)
            {
                AddBattleLog("そのアイテムは所持していません。");
                CancelTargetSelection();
                return;
            }

            var item = PrototypeBalanceTables.GetItem(itemId);
            if (!CommitPendingMoveIfNeeded())
            {
                ClearPendingTargetAction();
                RefreshBattleUi();
                return;
            }

            var resolved = false;
            var consumeItem = true;

            switch (itemId)
            {
                case ItemId.HealingPotion:
                case ItemId.GreaterHealingPotion:
                    run.Hp = Mathf.Min(run.MaxHp, run.Hp + item.HealAmount);
                    AddBattleLog($"{item.DisplayName}でHPを{item.HealAmount}回復しました。");
                    resolved = true;
                    break;

                case ItemId.BerserkDrug:
                    run.TurnState.PendingAttackDoubleTurns = Math.Max(run.TurnState.PendingAttackDoubleTurns, 1);
                    AddBattleLog("次のターンの攻撃力が2倍になります。");
                    resolved = true;
                    break;

                case ItemId.StarFragment:
                case ItemId.NightFragment:
                    GainSkillPoints(item.SkillPointAmount);
                    AddBattleLog($"{item.DisplayName}でSPを{item.SkillPointAmount}回復しました。");
                    resolved = true;
                    break;

                case ItemId.DuplicationEye:
                    run.Coins = Mathf.FloorToInt(run.Coins * 1.2f);
                    AddBattleLog("所持コインを1.2倍にしました。");
                    resolved = true;
                    break;

                case ItemId.RiskDrug:
                    run.Hp = Mathf.Max(1, run.Hp - 1);
                    run.TurnState.ExtraTurnsRemaining++;
                    AddBattleLog("HPを1消費してエクストラターンを得ました。");
                    resolved = true;
                    break;

                case ItemId.MagicBullet:
                    resolved = ResolveSingleEnemyItem(selectedTile, item, enemy => DamageEnemy(enemy, GetCurrentAttackValue(), item.DisplayName));
                    break;

                case ItemId.MartialGuide:
                    run.TurnState.ActiveExpDoubleTurns = Math.Max(run.TurnState.ActiveExpDoubleTurns, 2);
                    AddBattleLog("2ターンの間、取得経験値が2倍になります。");
                    resolved = true;
                    break;

                case ItemId.CoinScavengeGuide:
                    run.TurnState.ActiveCoinDoubleTurns = Math.Max(run.TurnState.ActiveCoinDoubleTurns, 2);
                    AddBattleLog("2ターンの間、取得コインが2倍になります。");
                    resolved = true;
                    break;

                case ItemId.IceStone:
                    foreach (var enemy in run.Enemies.Where(target => target.FreezeTurns > 0))
                    {
                        enemy.FreezeTurns += 1;
                    }

                    AddBattleLog("凍結中の敵の凍結ターンを延長しました。");
                    resolved = true;
                    break;

                case ItemId.FireStone:
                    foreach (var enemy in run.Enemies.ToList())
                    {
                        DamageEnemy(enemy, 1, item.DisplayName);
                    }

                    resolved = true;
                    break;

                case ItemId.ThunderStone:
                    foreach (var enemy in run.Enemies.Where(target => target.ShockTurns > 0))
                    {
                        enemy.ShockTurns += 1;
                    }

                    AddBattleLog("感電中の敵の感電ターンを延長しました。");
                    resolved = true;
                    break;

                case ItemId.CursedJar:
                    resolved = ResolveSingleEnemyItem(selectedTile, item, enemy =>
                    {
                        enemy.HasCurse = true;
                        AddBattleLog($"{GetPieceName(enemy.PieceType)}に呪いを付与しました。");
                    });
                    break;

                case ItemId.MirrorStaff:
                    run.TurnState.NextMoveAnywhere = true;
                    AddBattleLog("次の移動で好きな場所に移動できます。");
                    resolved = true;
                    break;

                case ItemId.Mjolnir:
                    resolved = ResolveAreaItem(selectedTile, GetSquareThreeByThreeTiles, enemy =>
                    {
                        enemy.ShockTurns += 2;
                        AddBattleLog($"{GetPieceName(enemy.PieceType)}を2ターン感電させました。");
                    });
                    break;

                case ItemId.GaeBolg:
                    resolved = ResolveLineItem(selectedTile, item, enemy => DamageEnemy(enemy, GetCurrentAttackValue(), item.DisplayName));
                    if (resolved)
                    {
                        consumeItem = random.NextDouble() > 0.5d;
                        if (!consumeItem)
                        {
                            AddBattleLog("神器ゲイボルグが戻ってきました。");
                        }
                    }
                    break;

                case ItemId.Sverl:
                    foreach (var enemy in run.Enemies.Where(target => GetAroundTwentyFourTiles(run.PlayerPosition).Contains(target.Position)))
                    {
                        enemy.FreezeTurns += 2;
                    }

                    AddBattleLog("周囲24マスの敵を凍結させました。");
                    resolved = true;
                    break;

                case ItemId.Excalibur:
                    resolved = ResolveSingleEnemyItem(selectedTile, item, enemy => InstantKillEnemy(enemy, item.DisplayName), enemy => enemy.PieceType != PieceType.King && enemy.PieceType != PieceType.Queen);
                    break;

                case ItemId.ImmortalElixir:
                    run.TurnState.IgnoreDamageCharges++;
                    AddBattleLog("次の被ダメージを1回だけ無効化します。");
                    resolved = true;
                    break;
            }

            if (!resolved)
            {
                AddBattleLog("対象を正しく選択できませんでした。");
                CancelTargetSelection();
                return;
            }

            if (consumeItem)
            {
                run.Inventory.RemoveAt(inventoryIndex);
            }

            ConsumeSupportActionSlot();
            run.BattleState = BattleState.PlayerTurn;
            ClearPendingTargetAction();
            UpdateMoveAndDangerTiles();
            CheckBattleProgress();
            if (run != null && run.BattleState == BattleState.PlayerTurn)
            {
                TryOpenLevelUp();
            }

            RefreshBattleUi();
        }

        private bool ResolveSingleEnemyItem(Vector2Int? selectedTile, ItemDefinition item, Action<PrototypeEnemyState> resolver, Func<PrototypeEnemyState, bool> extraValidator = null)
        {
            if (!selectedTile.HasValue)
            {
                return false;
            }

            var enemy = GetEnemyAt(selectedTile.Value);
            if (enemy == null)
            {
                return false;
            }

            if (extraValidator != null && !extraValidator(enemy))
            {
                return false;
            }

            resolver(enemy);
            return true;
        }

        private bool ResolveLineItem(Vector2Int? selectedTile, ItemDefinition item, Action<PrototypeEnemyState> resolver)
        {
            return ResolveLineTarget(selectedTile, resolver);
        }

        private bool ResolveAreaItem(Vector2Int? selectedTile, Func<Vector2Int, List<Vector2Int>> selector, Action<PrototypeEnemyState> resolver)
        {
            if (!selectedTile.HasValue)
            {
                return false;
            }

            var targets = selector(selectedTile.Value);
            foreach (var enemy in run.Enemies.Where(target => targets.Contains(target.Position)).ToList())
            {
                resolver(enemy);
            }

            return true;
        }

        private bool ResolveSingleEnemySkill(Vector2Int? selectedTile, Action<PrototypeEnemyState> resolver, Func<PrototypeEnemyState, bool> extraValidator = null)
        {
            if (!selectedTile.HasValue)
            {
                return false;
            }

            var enemy = GetEnemyAt(selectedTile.Value);
            if (enemy == null)
            {
                return false;
            }

            if (extraValidator != null && !extraValidator(enemy))
            {
                return false;
            }

            resolver(enemy);
            return true;
        }

        private bool ResolveLineSkill(SkillId skillId, Vector2Int? selectedTile, Action<PrototypeEnemyState> resolver)
        {
            return ResolveLineTarget(selectedTile, resolver);
        }

        private bool ResolveLineTarget(Vector2Int? selectedTile, Action<PrototypeEnemyState> resolver)
        {
            if (!selectedTile.HasValue)
            {
                return false;
            }

            if (!TryGetDirection(run.PlayerPosition, selectedTile.Value, out var direction))
            {
                return false;
            }

            var cursor = run.PlayerPosition + direction;
            while (IsInsideBoard(cursor))
            {
                var enemy = GetEnemyAt(cursor);
                if (enemy != null)
                {
                    resolver(enemy);
                    return true;
                }

                cursor += direction;
            }

            return false;
        }

        private bool ResolveAreaSkill(Vector2Int? selectedTile, Func<Vector2Int, List<Vector2Int>> selector, Action<PrototypeEnemyState> resolver)
        {
            if (!selectedTile.HasValue)
            {
                return false;
            }

            var targets = selector(selectedTile.Value);
            foreach (var enemy in run.Enemies.Where(target => targets.Contains(target.Position)).ToList())
            {
                resolver(enemy);
            }

            return true;
        }

        private bool ResolveFieldSkill(Vector2Int? selectedTile, FieldEffectType fieldEffectType, int duration)
        {
            if (!selectedTile.HasValue)
            {
                return false;
            }

            run.FieldEffects.Add(
                new PrototypeFieldEffectState
                {
                    FieldEffectType = fieldEffectType,
                    RemainingEnemyTurns = duration,
                    Tiles = GetSquareThreeByThreeTiles(selectedTile.Value),
                });
            AddBattleLog(fieldEffectType == FieldEffectType.ThinIce ? "薄氷を展開しました。" : "テンペストを展開しました。");
            return true;
        }

        private bool ResolveDirectionalAreaSkill(Vector2Int? selectedTile, Func<Vector2Int, List<Vector2Int>> selector, Action<PrototypeEnemyState> resolver)
        {
            if (!selectedTile.HasValue)
            {
                return false;
            }

            var areaTiles = selector(selectedTile.Value);
            if (areaTiles.Count == 0)
            {
                return false;
            }

            foreach (var enemy in run.Enemies.Where(target => areaTiles.Contains(target.Position)).ToList())
            {
                resolver(enemy);
            }

            return true;
        }

        private bool CanAffordSkillPoints(SkillDefinition skill)
        {
            return run.TurnState.FreeSkillUsesRemaining > 0 || run.SkillPoints >= skill.SkillPointCost;
        }

        private bool TrySpendSkillPoints(SkillDefinition skill, out bool usedFreeCharge)
        {
            if (run.TurnState.FreeSkillUsesRemaining > 0)
            {
                run.TurnState.FreeSkillUsesRemaining--;
                usedFreeCharge = true;
                return true;
            }

            if (run.SkillPoints < skill.SkillPointCost)
            {
                usedFreeCharge = false;
                return false;
            }

            run.SkillPoints -= skill.SkillPointCost;
            usedFreeCharge = false;
            return true;
        }

        private void RestoreSkillPointSpend(SkillDefinition skill, bool usedFreeCharge)
        {
            if (usedFreeCharge)
            {
                run.TurnState.FreeSkillUsesRemaining++;
                return;
            }

            run.SkillPoints = Mathf.Min(20, run.SkillPoints + skill.SkillPointCost);
        }

        private void ConsumeSupportActionSlot()
        {
            if (!run.TurnState.Moved)
            {
                run.TurnState.PreActionUsed = true;
                return;
            }

            run.TurnState.PostActionUsed = true;
        }

        private bool CanUseSupportActionNow()
        {
            if (run.TurnState.Moved)
            {
                return !run.TurnState.PostActionUsed;
            }

            return !run.TurnState.PreActionUsed;
        }

        private void DamageEnemy(PrototypeEnemyState enemy, int damage, string source)
        {
            damage = Mathf.Max(0, damage);
            enemy.Hp -= damage;
            AddBattleLog($"{source}で{GetPieceName(enemy.PieceType)}に{damage}ダメージ。");
            if (enemy.Hp <= 0)
            {
                DefeatEnemy(enemy, source, false);
            }
        }

        private void InstantKillEnemy(PrototypeEnemyState enemy, string source)
        {
            AddBattleLog($"{source}で{GetPieceName(enemy.PieceType)}を撃破しました。");
            DefeatEnemy(enemy, source, false);
        }

        private void DefeatEnemy(PrototypeEnemyState enemy, string source, bool movedCapture)
        {
            if (!run.Enemies.Contains(enemy))
            {
                return;
            }

            run.Enemies.Remove(enemy);
            var reward = PrototypeBalanceTables.GetPieceReward(enemy.PieceType);
            var expReward = reward.x;
            var coinReward = reward.y;

            if (run.TurnState.HeroRewardDoubleStageFlag > 0)
            {
                expReward *= 2;
                coinReward *= 2;
            }

            if (run.TurnState.ActiveExpDoubleTurns > 0)
            {
                expReward *= 2;
            }

            if (run.TurnState.ActiveCoinDoubleTurns > 0)
            {
                coinReward *= 2;
            }

            run.Experience += expReward;
            run.Coins += coinReward;
            GainSkillPoints(run.BerserkMode ? 2 : 1);
            AddBattleLog($"+{expReward} EXP / +{coinReward} コイン / +{(run.BerserkMode ? 2 : 1)} SP");

            var healCount = 0;
            if (run.TurnState.ActiveHealOnKillTurns > 0)
            {
                healCount++;
            }

            if (run.TurnState.MadSealTurnsRemaining > 0)
            {
                healCount++;
            }

            if (healCount > 0)
            {
                run.Hp = Mathf.Min(run.MaxHp, run.Hp + healCount);
                AddBattleLog($"撃破時効果でHPを{healCount}回復しました。");
            }
        }

        private void TryOpenLevelUp()
        {
            if (run == null)
            {
                return;
            }

            if (run.BattleState != BattleState.PlayerTurn && run.BattleState != BattleState.Targeting)
            {
                return;
            }

            if (run.Experience < run.NextLevelExperience)
            {
                return;
            }

            run.Experience -= run.NextLevelExperience;
            run.NextLevelExperience++;
            run.Level++;
            BuildLevelChoices();
            ShowLevelUpOverlay();
        }

        private void BuildLevelChoices()
        {
            run.LevelChoices = new List<LevelChoiceDefinition>();
            var bonusChoices = new List<LevelChoiceDefinition>
            {
                new LevelChoiceDefinition { Kind = LevelChoiceKind.Coin, Title = "コイン+5", Description = "コインを5獲得する。" },
                new LevelChoiceDefinition { Kind = LevelChoiceKind.SkillPointOne, Title = "SP+1", Description = "スキルポイントを1回復する。" },
                new LevelChoiceDefinition { Kind = LevelChoiceKind.SkillPointTwo, Title = "SP+2", Description = "スキルポイントを2回復する。" },
            };

            var equippedSkills = new HashSet<SkillId>(run.RandomSkills) { run.PrimarySkill, run.EquippedSeal };
            var availableSkills = PrototypeBalanceTables.GetRandomSkillPool(run.Spirit).Where(skillId => !equippedSkills.Contains(skillId)).OrderBy(_ => random.Next()).ToList();

            if (run.RandomSkills.Count < RandomSkillSlotCount && availableSkills.Count > 0)
            {
                var skillId = availableSkills[0];
                run.LevelChoices.Add(new LevelChoiceDefinition { Kind = LevelChoiceKind.Skill, SkillId = skillId, Title = GetSkillName(skillId), Description = PrototypeBalanceTables.GetSkill(skillId).Description });
                run.LevelChoices.AddRange(bonusChoices.OrderBy(_ => random.Next()).Take(2));
            }
            else if (availableSkills.Count > 0)
            {
                var skillId = availableSkills[0];
                run.LevelChoices.Add(new LevelChoiceDefinition { Kind = LevelChoiceKind.Skill, SkillId = skillId, Title = GetSkillName(skillId), Description = "現在のランダムスキル1つと入れ替える。" });
                run.LevelChoices.AddRange(bonusChoices.OrderBy(_ => random.Next()).Take(2));
            }
            else
            {
                run.LevelChoices.AddRange(bonusChoices.OrderBy(_ => random.Next()).Take(3));
            }
        }

        private void ShowLevelUpOverlay()
        {
            run.BattleState = BattleState.LevelUp;
            ShowOverlay("レベルアップ", "3つの候補から1つ選んでください。");
            ClearChildren(overlayCardRoot);
            ClearChildren(overlayButtonRoot);

            var layout = overlayCardRoot.GetComponent<HorizontalLayoutGroup>() ?? overlayCardRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            foreach (var choice in run.LevelChoices)
            {
                var card = CreatePanel(overlayCardRoot, "ChoiceCard", PanelStrongColor);
                var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                cardLayout.spacing = 10f;
                cardLayout.padding = new RectOffset(12, 12, 12, 12);
                cardLayout.childControlHeight = true;
                cardLayout.childControlWidth = true;
                cardLayout.childForceExpandHeight = false;
                cardLayout.childForceExpandWidth = true;

                var title = CreateText(card.transform, "Title", 20, FontStyle.Bold, TextAnchor.UpperLeft);
                title.text = choice.Title;
                var description = CreateText(card.transform, "Description", 16, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor);
                description.text = choice.Description;
                CreateButton(card.transform, "ChooseButton", "選択", () => ResolveLevelChoice(choice));
            }
        }

        private void ResolveLevelChoice(LevelChoiceDefinition choice)
        {
            switch (choice.Kind)
            {
                case LevelChoiceKind.Skill:
                    if (run.RandomSkills.Count < RandomSkillSlotCount)
                    {
                        run.RandomSkills.Add(choice.SkillId);
                        AddBattleLog($"{GetSkillName(choice.SkillId)}を習得しました。");
                        CloseOverlayToBattle();
                    }
                    else
                    {
                        run.PendingReplacementSkill = choice.SkillId;
                        ShowReplacementOverlay();
                    }
                    break;

                case LevelChoiceKind.Coin:
                    run.Coins += 5;
                    AddBattleLog("コインを5獲得しました。");
                    CloseOverlayToBattle();
                    break;

                case LevelChoiceKind.SkillPointOne:
                    GainSkillPoints(1);
                    AddBattleLog("SPを1回復しました。");
                    CloseOverlayToBattle();
                    break;

                case LevelChoiceKind.SkillPointTwo:
                    GainSkillPoints(2);
                    AddBattleLog("SPを2回復しました。");
                    CloseOverlayToBattle();
                    break;
            }

            RefreshBattleUi();
        }

        private void ShowReplacementOverlay()
        {
            run.BattleState = BattleState.ReplaceSkill;
            ShowOverlay("スキル入れ替え", $"{GetSkillName(run.PendingReplacementSkill)} と入れ替えるスキルを選んでください。");
            ClearChildren(overlayCardRoot);
            ClearChildren(overlayButtonRoot);

            var layout = overlayCardRoot.GetComponent<HorizontalLayoutGroup>() ?? overlayCardRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            for (int i = 0; i < run.RandomSkills.Count; i++)
            {
                var skillId = run.RandomSkills[i];
                var replaceIndex = i;
                var card = CreatePanel(overlayCardRoot, "ReplaceCard", PanelStrongColor);
                var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                cardLayout.spacing = 10f;
                cardLayout.padding = new RectOffset(12, 12, 12, 12);
                cardLayout.childControlHeight = true;
                cardLayout.childControlWidth = true;
                cardLayout.childForceExpandHeight = false;
                cardLayout.childForceExpandWidth = true;

                var title = CreateText(card.transform, "Title", 20, FontStyle.Bold, TextAnchor.UpperLeft);
                title.text = GetSkillName(skillId);
                var description = CreateText(card.transform, "Description", 16, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor);
                description.text = PrototypeBalanceTables.GetSkill(skillId).Description;
                CreateButton(card.transform, "ReplaceButton", "これと入れ替える", () => ReplaceRandomSkill(replaceIndex));
            }
        }

        private void ReplaceRandomSkill(int index)
        {
            var oldSkill = run.RandomSkills[index];
            run.RandomSkills[index] = run.PendingReplacementSkill;
            AddBattleLog($"{GetSkillName(oldSkill)} を {GetSkillName(run.PendingReplacementSkill)} に入れ替えました。");
            run.PendingReplacementSkill = SkillId.None;
            CloseOverlayToBattle();
            RefreshBattleUi();
        }

        private void CloseOverlayToBattle()
        {
            overlayRoot.gameObject.SetActive(false);
            ClearChildren(overlayCardRoot);
            ClearChildren(overlayButtonRoot);
            ClearPendingTargetAction();
            run.BattleState = BattleState.PlayerTurn;
            UpdateMoveAndDangerTiles();
            TryOpenLevelUp();
        }

        private void CheckBattleProgress()
        {
            if (run == null)
            {
                return;
            }

            if (run.BattleState != BattleState.PlayerTurn &&
                run.BattleState != BattleState.Targeting &&
                run.BattleState != BattleState.EnemyTurn)
            {
                return;
            }

            if (CurrentStageRequiresKingDefeat())
            {
                if (run.Enemies.All(enemy => enemy.PieceType != PieceType.King))
                {
                    HandleStageClear();
                }
            }
            else if (run.Enemies.Count == 0)
            {
                HandleStageClear();
            }
        }

        private bool CurrentStageRequiresKingDefeat()
        {
            if (run.EndlessMode)
            {
                return PrototypeBalanceTables.BuildEndlessStage(run.EndlessStageNumber).RequiresKingDefeat;
            }

            return PrototypeBalanceTables.GetStageDefinition(run.Realm, run.RealmStageIndex).RequiresKingDefeat;
        }

        private void HandleStageClear()
        {
            run.HighestRealmProgress = Mathf.Max(run.HighestRealmProgress, run.RealmStageIndex + 1);
            if (run.EndlessMode)
            {
                if (run.RealmStageIndex == 2)
                {
                    run.BattleState = BattleState.StageResult;
                    run.LastResultTitle = "ステージクリア";
                    run.LastResultBody = "次の面へ進みます。";
                    ShowStageResultOverlay("ステージクリア", "次の面へ進みます。", AdvanceAfterStageResult);
                    return;
                }

                run.BattleState = BattleState.StageResult;
                run.LastResultTitle = "ステージクリア";
                run.LastResultBody = "ショップへ移動します。";
                ShowStageResultOverlay("ステージクリア", "ショップへ移動します。", EnterShop);
                return;
            }

            if (run.RealmStageIndex < 2)
            {
                run.BattleState = BattleState.StageResult;
                run.LastResultTitle = "ステージクリア";
                run.LastResultBody = "ショップへ移動します。";
                ShowStageResultOverlay("ステージクリア", "ショップへ移動します。", EnterShop);
                return;
            }

            FinalizeRealmClear();
        }

        private void ShowStageResultOverlay(string title, string body, Action continueAction)
        {
            ShowOverlay(title, body);
            ClearChildren(overlayCardRoot);
            ClearChildren(overlayButtonRoot);
            var button = CreateButton(overlayButtonRoot, "ContinueButton", "続ける", continueAction);
            Stretch((RectTransform)button.transform);
        }

        private void EnterShop()
        {
            run.BattleState = BattleState.Shop;
            run.ShopOffers = PrototypeBalanceTables.CreateShopOffers(random);
            ShowOverlay("ショップ", "最大3つまで所持できます。購入後に次のステージへ進みます。");
            ClearChildren(overlayCardRoot);
            ClearChildren(overlayButtonRoot);

            var layout = overlayCardRoot.GetComponent<HorizontalLayoutGroup>() ?? overlayCardRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            foreach (var offer in run.ShopOffers)
            {
                var localOffer = offer;
                var card = CreatePanel(overlayCardRoot, "ShopCard", PanelStrongColor);
                var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                cardLayout.spacing = 10f;
                cardLayout.padding = new RectOffset(12, 12, 12, 12);
                cardLayout.childControlHeight = true;
                cardLayout.childControlWidth = true;
                cardLayout.childForceExpandHeight = false;
                cardLayout.childForceExpandWidth = true;

                var title = CreateText(card.transform, "Title", 18, FontStyle.Bold, TextAnchor.UpperLeft);
                title.text = localOffer.DisplayName;
                var description = CreateText(card.transform, "Description", 15, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor);
                description.text = localOffer.Description;
                var price = CreateText(card.transform, "Price", 16, FontStyle.Bold, TextAnchor.UpperLeft);
                price.text = $"{localOffer.Cost} コイン";
                var buyButton = CreateButton(card.transform, "BuyButton", "購入", () => BuyShopItem(localOffer.Id));
                buyButton.interactable = CanBuyItem(localOffer.Id, localOffer.Cost);
            }

            CreateButton(overlayButtonRoot, "RerollButton", "リロール (-4)", RerollShop);
            CreateButton(overlayButtonRoot, "NextButton", "次のステージへ", AdvanceAfterStageResult);
        }

        private void BuyShopItem(ItemId itemId)
        {
            var item = PrototypeBalanceTables.GetItem(itemId);
            if (!CanBuyItem(itemId, item.Cost))
            {
                AddBattleLog("購入できません。");
                RefreshBattleUi();
                return;
            }

            run.Coins -= item.Cost;
            run.Inventory.Add(itemId);
            AddBattleLog($"{item.DisplayName}を購入しました。");
            EnterShop();
            RefreshBattleUi();
        }

        private bool CanBuyItem(ItemId itemId, int cost)
        {
            if (run.Coins < cost)
            {
                return false;
            }

            if (run.Inventory.Count >= InventoryLimit)
            {
                return false;
            }

            return !run.Inventory.Contains(itemId);
        }

        private void RerollShop()
        {
            if (run.Coins < 4)
            {
                AddBattleLog("コインが足りません。");
                RefreshBattleUi();
                return;
            }

            run.Coins -= 4;
            AddBattleLog("ショップをリロールしました。");
            EnterShop();
            RefreshBattleUi();
        }

        private void AdvanceAfterStageResult()
        {
            overlayRoot.gameObject.SetActive(false);
            ClearChildren(overlayCardRoot);
            ClearChildren(overlayButtonRoot);

            if (run.BattleState == BattleState.Shop)
            {
                if (run.EndlessMode)
                {
                    run.EndlessStageNumber++;
                    LoadCurrentStage();
                }
                else
                {
                    run.RealmStageIndex++;
                    LoadCurrentStage();
                }

                PreparePlayerTurn(false);
                RefreshBattleUi();
                return;
            }

            if (run.EndlessMode)
            {
                run.EndlessStageNumber++;
                LoadCurrentStage();
                PreparePlayerTurn(false);
                RefreshBattleUi();
                return;
            }

            run.RealmStageIndex++;
            LoadCurrentStage();
            PreparePlayerTurn(false);
            RefreshBattleUi();
        }

        private void FinalizeRealmClear()
        {
            var reward = PrototypeBalanceTables.GetRealmClearReward(run.Realm);
            run.RewardTokenCount = reward.x;
            run.RewardSeal = (SkillId)reward.y;
            saveData.TokenCount += reward.x;
            if (!saveData.UnlockedSeals.Contains((int)run.RewardSeal))
            {
                saveData.UnlockedSeals.Add((int)run.RewardSeal);
            }

            PrototypePersistence.Save(saveData);
            ShowPermanentResultScreen(
                "面クリア",
                $"{PrototypeBalanceTables.GetRealmName(run.Realm)} を制覇しました。\n" +
                $"獲得トークン: {reward.x}\n" +
                $"解放スキル: {GetSkillName((SkillId)reward.y)}");
            run = null;
        }

        private void FinalizeGameOver()
        {
            var tokens = run.EndlessMode ? 0 : PrototypeBalanceTables.GetFailureRewardTokens(run.HighestRealmProgress);
            if (!run.EndlessMode)
            {
                saveData.TokenCount += tokens;
            }

            if (run.EndlessMode)
            {
                saveData.EndlessBestStage = Mathf.Max(saveData.EndlessBestStage, run.EndlessStageNumber);
            }

            PrototypePersistence.Save(saveData);
            var body = run.EndlessMode
                ? $"到達ステージ: {run.EndlessStageNumber}\n最高記録: {saveData.EndlessBestStage}"
                : $"進行度報酬: {tokens} トークン";
            ShowPermanentResultScreen("ゲームオーバー", body);
            run = null;
        }

        private void TryUpgradePermanentStat(PermanentUpgrade upgrade)
        {
            var cost = upgrade switch
            {
                PermanentUpgrade.Attack => PrototypeBalanceTables.GetAttackUpgradeCost(saveData),
                PermanentUpgrade.StartingSp => PrototypeBalanceTables.GetStartingSkillPointUpgradeCost(saveData),
                PermanentUpgrade.Hp => PrototypeBalanceTables.GetHpUpgradeCost(saveData),
                _ => -1,
            };

            if (cost < 0 || saveData.TokenCount < cost)
            {
                ShowEnhancementScreen();
                return;
            }

            saveData.TokenCount -= cost;
            switch (upgrade)
            {
                case PermanentUpgrade.Attack:
                    saveData.AttackUpgradeLevel++;
                    break;

                case PermanentUpgrade.StartingSp:
                    saveData.StartingSkillPointUpgradeLevel++;
                    break;

                case PermanentUpgrade.Hp:
                    saveData.HpUpgradeLevel++;
                    break;
            }

            PrototypePersistence.Save(saveData);
            ShowEnhancementScreen();
        }

        private void ResetPermanentSave()
        {
            PrototypePersistence.Reset();
            saveData = PrototypePersistence.Load();
            ShowLobby();
        }

        private void GainSkillPoints(int amount)
        {
            run.SkillPoints = Mathf.Clamp(run.SkillPoints + amount, 0, 20);
        }

        private int GetCurrentAttackValue()
        {
            return run.Attack * (run.TurnState.ActiveAttackDoubleTurns > 0 ? 2 : 1);
        }

        private void UpdateMoveAndDangerTiles()
        {
            validMoveTiles.Clear();
            if (!run.TurnState.Moved || HasPendingMoveSelection())
            {
                foreach (var tile in GetPlayerValidMoveTiles())
                {
                    validMoveTiles.Add(tile);
                }
            }

            dangerTiles.Clear();
            foreach (var dangerTile in GetDangerTilesForPreview())
            {
                dangerTiles.Add(dangerTile);
            }
        }

        private List<Vector2Int> GetPlayerValidMoveTiles()
        {
            if (run == null)
            {
                return new List<Vector2Int>();
            }

            var origin = run.TurnState.MoveOriginPosition;

            if (run.TurnState.NextMoveAnywhere)
            {
                var anywhere = new List<Vector2Int>();
                for (int y = 0; y < BoardSize; y++)
                {
                    for (int x = 0; x < BoardSize; x++)
                    {
                        var candidate = new Vector2Int(x, y);
                        if (candidate != origin)
                        {
                            anywhere.Add(candidate);
                        }
                    }
                }

                return anywhere;
            }

            var moves = new List<Vector2Int>();
            var directions = new[]
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right,
                new Vector2Int(1, 1),
                new Vector2Int(-1, 1),
                new Vector2Int(1, -1),
                new Vector2Int(-1, -1),
            };

            foreach (var direction in directions)
            {
                var cursor = origin + direction;
                while (IsInsideBoard(cursor))
                {
                    moves.Add(cursor);
                    if (!run.BerserkMode && GetEnemyAt(cursor) != null)
                    {
                        break;
                    }

                    cursor += direction;
                }
            }

            return moves;
        }

        private List<Vector2Int> GetEnemyValidMoveTiles(PrototypeEnemyState enemy)
        {
            return enemy.PieceType switch
            {
                PieceType.Pawn => GetPawnMoves(enemy),
                PieceType.Bishop => GetSlidingMoves(enemy.Position, new[] { new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1) }),
                PieceType.Rook => GetSlidingMoves(enemy.Position, new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right }),
                PieceType.Queen => GetSlidingMoves(enemy.Position, new[]
                {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                    new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1),
                }),
                PieceType.King => GetStepMoves(enemy.Position, new[]
                {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                    new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1),
                }),
                PieceType.Knight => GetKnightMoves(enemy.Position),
                _ => new List<Vector2Int>(),
            };
        }

        private List<Vector2Int> GetPawnMoves(PrototypeEnemyState enemy)
        {
            var moves = new List<Vector2Int>();
            var targetPlayerPosition = GetEnemyTargetPlayerPosition();
            var forward = enemy.Position + Vector2Int.down;
            if (IsInsideBoard(forward) && GetEnemyAt(forward) == null)
            {
                moves.Add(forward);
            }

            foreach (var offset in new[] { new Vector2Int(-1, -1), new Vector2Int(1, -1) })
            {
                var captureTile = enemy.Position + offset;
                if (IsInsideBoard(captureTile) && captureTile == targetPlayerPosition)
                {
                    moves.Add(captureTile);
                }
            }

            return moves;
        }

        private List<Vector2Int> GetSlidingMoves(Vector2Int origin, IEnumerable<Vector2Int> directions)
        {
            var moves = new List<Vector2Int>();
            var targetPlayerPosition = GetEnemyTargetPlayerPosition();
            foreach (var direction in directions)
            {
                var cursor = origin + direction;
                while (IsInsideBoard(cursor))
                {
                    if (GetEnemyAt(cursor) != null)
                    {
                        break;
                    }

                    moves.Add(cursor);
                    if (cursor == targetPlayerPosition)
                    {
                        break;
                    }

                    cursor += direction;
                }
            }

            return moves;
        }

        private List<Vector2Int> GetStepMoves(Vector2Int origin, IEnumerable<Vector2Int> directions)
        {
            var moves = new List<Vector2Int>();
            foreach (var direction in directions)
            {
                var candidate = origin + direction;
                if (IsInsideBoard(candidate) && GetEnemyAt(candidate) == null)
                {
                    moves.Add(candidate);
                }
            }

            return moves;
        }

        private List<Vector2Int> GetKnightMoves(Vector2Int origin)
        {
            var candidates = new[]
            {
                new Vector2Int(1, 2), new Vector2Int(2, 1), new Vector2Int(2, -1), new Vector2Int(1, -2),
                new Vector2Int(-1, -2), new Vector2Int(-2, -1), new Vector2Int(-2, 1), new Vector2Int(-1, 2),
            };

            return candidates
                .Select(offset => origin + offset)
                .Where(candidate => IsInsideBoard(candidate) && GetEnemyAt(candidate) == null)
                .ToList();
        }

        private Vector2Int ChooseEnemyMove(PrototypeEnemyState enemy, List<Vector2Int> validMoves)
        {
            var targetPlayerPosition = GetEnemyTargetPlayerPosition();
            foreach (var move in validMoves)
            {
                if (move == targetPlayerPosition)
                {
                    return move;
                }
            }

            return validMoves
                .OrderBy(move => Vector2Int.Distance(move, targetPlayerPosition))
                .ThenBy(move => move.y)
                .ThenBy(move => Mathf.Abs(move.x - targetPlayerPosition.x))
                .First();
        }

        private IEnumerable<Vector2Int> GetTargetableTilesForSkill(SkillId skillId)
        {
            switch (skillId)
            {
                case SkillId.ColdLance:
                case SkillId.SealKnightKing:
                    return GetLineTargetableTiles();

                case SkillId.FrostRain:
                    return GetTopLeftSelectableTilesForSquareTwoByTwo();

                case SkillId.ThinIce:
                case SkillId.Tempest:
                    return GetCenterSelectableTilesForSquareThreeByThree();

                case SkillId.FlameSlash:
                case SkillId.AzureFlame:
                case SkillId.Inferno:
                case SkillId.AzureLightningThorn:
                    return GetDirectionalSelectableTiles();

                case SkillId.Kingslayer:
                    return run.Enemies.Where(enemy => GetAdjacentEightTiles(GetDisplayedPlayerPosition()).Contains(enemy.Position)).Select(enemy => enemy.Position);

                case SkillId.ThunderShock:
                    return run.Enemies.Where(enemy => GetAroundTwentyFourTiles(GetDisplayedPlayerPosition()).Contains(enemy.Position)).Select(enemy => enemy.Position);

                default:
                    return GetSelectableEnemyPositions();
            }
        }

        private IEnumerable<Vector2Int> GetTargetableTilesForItem(ItemId itemId)
        {
            return itemId switch
            {
                ItemId.Mjolnir => GetCenterSelectableTilesForSquareThreeByThree(),
                ItemId.GaeBolg => GetLineTargetableTiles(),
                _ => GetSelectableEnemyPositions(),
            };
        }

        private IEnumerable<Vector2Int> GetSelectableEnemyPositions()
        {
            var displayedPlayerPosition = GetDisplayedPlayerPosition();
            return run.Enemies
                .Select(enemy => enemy.Position)
                .Where(position => position != displayedPlayerPosition);
        }

        private IEnumerable<Vector2Int> GetLineTargetableTiles()
        {
            var result = new List<Vector2Int>();
            var origin = GetDisplayedPlayerPosition();
            foreach (var direction in new[]
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1),
            })
            {
                var tilesInDirection = new List<Vector2Int>();
                var cursor = origin + direction;
                var foundEnemy = false;
                while (IsInsideBoard(cursor))
                {
                    tilesInDirection.Add(cursor);
                    if (GetEnemyAt(cursor) != null)
                    {
                        foundEnemy = true;
                    }

                    cursor += direction;
                }

                if (foundEnemy)
                {
                    result.AddRange(tilesInDirection);
                }
            }

            return result;
        }

        private IEnumerable<Vector2Int> GetDirectionalSelectableTiles()
        {
            var result = new List<Vector2Int>();
            var origin = GetDisplayedPlayerPosition();
            foreach (var direction in new[]
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1),
            })
            {
                var cursor = origin + direction;
                while (IsInsideBoard(cursor))
                {
                    result.Add(cursor);
                    cursor += direction;
                }
            }

            return result;
        }

        private IEnumerable<Vector2Int> GetCenterSelectableTilesForSquareThreeByThree()
        {
            var result = new List<Vector2Int>();
            for (int y = 1; y < BoardSize - 1; y++)
            {
                for (int x = 1; x < BoardSize - 1; x++)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }

            return result;
        }

        private IEnumerable<Vector2Int> GetTopLeftSelectableTilesForSquareTwoByTwo()
        {
            var result = new List<Vector2Int>();
            for (int y = 0; y < BoardSize - 1; y++)
            {
                for (int x = 0; x < BoardSize - 1; x++)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }

            return result;
        }

        private List<Vector2Int> GetSquareThreeByThreeTiles(Vector2Int center)
        {
            var result = new List<Vector2Int>();
            for (int y = center.y - 1; y <= center.y + 1; y++)
            {
                for (int x = center.x - 1; x <= center.x + 1; x++)
                {
                    var tile = new Vector2Int(x, y);
                    if (IsInsideBoard(tile))
                    {
                        result.Add(tile);
                    }
                }
            }

            return result;
        }

        private List<Vector2Int> GetSquareTwoByTwoTiles(Vector2Int topLeft)
        {
            var result = new List<Vector2Int>();
            for (int y = topLeft.y; y <= topLeft.y + 1; y++)
            {
                for (int x = topLeft.x; x <= topLeft.x + 1; x++)
                {
                    var tile = new Vector2Int(x, y);
                    if (IsInsideBoard(tile))
                    {
                        result.Add(tile);
                    }
                }
            }

            return result;
        }

        private List<Vector2Int> GetAroundTwentyFourTiles(Vector2Int center)
        {
            var result = new List<Vector2Int>();
            for (int y = center.y - 2; y <= center.y + 2; y++)
            {
                for (int x = center.x - 2; x <= center.x + 2; x++)
                {
                    var tile = new Vector2Int(x, y);
                    if (tile == center || !IsInsideBoard(tile))
                    {
                        continue;
                    }

                    result.Add(tile);
                }
            }

            return result;
        }

        private List<Vector2Int> GetAdjacentEightTiles(Vector2Int center)
        {
            return new[]
            {
                center + Vector2Int.up,
                center + Vector2Int.down,
                center + Vector2Int.left,
                center + Vector2Int.right,
                center + new Vector2Int(1, 1),
                center + new Vector2Int(-1, 1),
                center + new Vector2Int(1, -1),
                center + new Vector2Int(-1, -1),
            }.Where(IsInsideBoard).ToList();
        }

        private List<Vector2Int> GetLineThreeTiles(Vector2Int targetTile)
        {
            var origin = GetDisplayedPlayerPosition();
            if (!TryGetDirection(origin, targetTile, out var direction))
            {
                return new List<Vector2Int>();
            }

            var result = new List<Vector2Int>();
            var cursor = origin + direction;
            for (int i = 0; i < 3 && IsInsideBoard(cursor); i++)
            {
                result.Add(cursor);
                cursor += direction;
            }

            return result;
        }

        private List<Vector2Int> GetFlameSlashTiles(Vector2Int targetTile)
        {
            var origin = GetDisplayedPlayerPosition();
            if (!TryGetCardinalDirection(origin, targetTile, out var direction))
            {
                return new List<Vector2Int>();
            }

            var result = new List<Vector2Int>();
            if (direction == Vector2Int.up || direction == Vector2Int.down)
            {
                var sideOffset = new[] { 0, -1 };
                for (int length = 1; length <= 5; length++)
                {
                    foreach (var offset in sideOffset)
                    {
                        var tile = new Vector2Int(origin.x + offset, origin.y + (direction.y * length));
                        if (IsInsideBoard(tile))
                        {
                            result.Add(tile);
                        }
                    }
                }
            }
            else
            {
                var sideOffset = new[] { 0, 1 };
                for (int length = 1; length <= 5; length++)
                {
                    foreach (var offset in sideOffset)
                    {
                        var tile = new Vector2Int(origin.x + (direction.x * length), origin.y + offset);
                        if (IsInsideBoard(tile))
                        {
                            result.Add(tile);
                        }
                    }
                }
            }

            return result.Distinct().ToList();
        }

        private List<Vector2Int> GetAzureFlameTiles(Vector2Int targetTile)
        {
            var origin = GetDisplayedPlayerPosition();
            if (!TryGetDirection(origin, targetTile, out var direction))
            {
                return new List<Vector2Int>();
            }

            var result = new List<Vector2Int>();
            var forwardOne = origin + direction;
            var forwardTwo = forwardOne + direction;
            var forwardThree = forwardTwo + direction;
            foreach (var tile in new[] { forwardOne, forwardTwo, forwardThree })
            {
                if (IsInsideBoard(tile))
                {
                    result.Add(tile);
                }
            }

            var perpendiculars = GetPerpendicularDirections(direction);
            foreach (var perpendicular in perpendiculars)
            {
                var tile = forwardThree + perpendicular;
                if (IsInsideBoard(tile))
                {
                    result.Add(tile);
                }
            }

            return result;
        }

        private Vector2Int[] GetPerpendicularDirections(Vector2Int direction)
        {
            if (direction.x == 0)
            {
                return new[] { Vector2Int.left, Vector2Int.right };
            }

            if (direction.y == 0)
            {
                return new[] { Vector2Int.up, Vector2Int.down };
            }

            if (direction == new Vector2Int(1, 1) || direction == new Vector2Int(-1, -1))
            {
                return new[] { new Vector2Int(-1, 1), new Vector2Int(1, -1) };
            }

            return new[] { new Vector2Int(1, 1), new Vector2Int(-1, -1) };
        }

        private Vector2Int FindRespawnPosition()
        {
            if (GetEnemyAt(run.SpawnPosition) == null)
            {
                return run.SpawnPosition;
            }

            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    var candidate = new Vector2Int(x, y);
                    if (GetEnemyAt(candidate) == null)
                    {
                        return candidate;
                    }
                }
            }

            return run.SpawnPosition;
        }

        private PrototypeEnemyState GetEnemyByRuntimeId(int runtimeId)
        {
            return run.Enemies.FirstOrDefault(enemy => enemy.RuntimeId == runtimeId);
        }

        private PrototypeEnemyState GetEnemyAt(Vector2Int position)
        {
            return run.Enemies.FirstOrDefault(enemy => enemy.Position == position);
        }

        private void AddBattleLog(string message)
        {
            battleLogLines.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            if (battleLogLines.Count > 24)
            {
                battleLogLines.RemoveAt(0);
            }
        }

        private string GetSpiritName(SpiritType spirit)
        {
            return spirit switch
            {
                SpiritType.Ice => "氷の精霊",
                SpiritType.Fire => "炎の精霊",
                SpiritType.Thunder => "雷の精霊",
                _ => "不明",
            };
        }

        private string GetSkillName(SkillId skillId)
        {
            return skillId == SkillId.None ? "なし" : PrototypeBalanceTables.GetSkill(skillId).DisplayName;
        }

        private string GetItemName(ItemId itemId)
        {
            return itemId == ItemId.None ? "なし" : PrototypeBalanceTables.GetItem(itemId).DisplayName;
        }

        private string GetPieceName(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => "ポーン",
                PieceType.Bishop => "ビショップ",
                PieceType.Rook => "ルーク",
                PieceType.Knight => "ナイト",
                PieceType.Queen => "クイーン",
                PieceType.King => "キング",
                _ => "駒",
            };
        }

        private string GetPieceShortName(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => "P",
                PieceType.Bishop => "B",
                PieceType.Rook => "R",
                PieceType.Knight => "N",
                PieceType.Queen => "Q",
                PieceType.King => "K",
                _ => "?",
            };
        }

        private SkillId GetSkillFromSlot(int slotIndex)
        {
            if (slotIndex == 0)
            {
                return run.PrimarySkill;
            }

            if (slotIndex == 1)
            {
                return run.EquippedSeal;
            }

            var randomIndex = slotIndex - 2;
            if (randomIndex < 0 || randomIndex >= run.RandomSkills.Count)
            {
                return SkillId.None;
            }

            return run.RandomSkills[randomIndex];
        }

        private void ShowOverlay(string title, string body)
        {
            overlayRoot.gameObject.SetActive(true);
            overlayTitleText.text = title;
            overlayBodyText.text = body;
        }

        private void SetView(bool showMenu, bool showBattle, bool showOverlay)
        {
            menuRoot.gameObject.SetActive(showMenu);
            battleRoot.gameObject.SetActive(showBattle);
            overlayRoot.gameObject.SetActive(showOverlay);
        }

        private void CreateInfoBlock(RectTransform parent, string title, string body)
        {
            var panel = CreatePanel(parent, "InfoBlock", PanelColor);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var titleTextLocal = CreateText(panel.transform, "InfoTitle", 24, FontStyle.Bold, TextAnchor.UpperLeft);
            titleTextLocal.text = title;
            var bodyTextLocal = CreateText(panel.transform, "InfoBody", 18, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor);
            bodyTextLocal.text = body;
        }

        private void CreateActionBlock(RectTransform parent, string title, params (string label, Action action)[] actions)
        {
            var panel = CreatePanel(parent, "ActionBlock", PanelColor);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var titleTextLocal = CreateText(panel.transform, "ActionTitle", 24, FontStyle.Bold, TextAnchor.UpperLeft);
            titleTextLocal.text = title;

            foreach (var action in actions)
            {
                CreateButton(panel.transform, "ActionButton", action.label, () => action.action());
            }
        }

        private void CreateSelectionBlock(RectTransform parent, string title, params (string label, Action action)[] actions)
        {
            CreateActionBlock(parent, title, actions);
        }

        private string FormatCost(int cost)
        {
            return cost < 0 ? "MAX" : cost.ToString();
        }

        private static bool IsInsideBoard(Vector2Int position)
        {
            return position.x >= 0 && position.x < BoardSize && position.y >= 0 && position.y < BoardSize;
        }

        private static string ToBoardLabel(Vector2Int position)
        {
            return $"{(char)('A' + position.x)}{position.y + 1}";
        }

        private static int ToIndex(Vector2Int position)
        {
            return ((BoardSize - 1 - position.y) * BoardSize) + position.x;
        }

        private bool TryGetDirection(Vector2Int from, Vector2Int to, out Vector2Int direction)
        {
            var delta = to - from;
            if (delta == Vector2Int.zero)
            {
                direction = Vector2Int.zero;
                return false;
            }

            if (delta.x == 0 || delta.y == 0 || Mathf.Abs(delta.x) == Mathf.Abs(delta.y))
            {
                direction = new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));
                return true;
            }

            direction = Vector2Int.zero;
            return false;
        }

        private bool TryGetCardinalDirection(Vector2Int from, Vector2Int to, out Vector2Int direction)
        {
            var delta = to - from;
            if (delta.x == 0 && delta.y != 0)
            {
                direction = new Vector2Int(0, Math.Sign(delta.y));
                return true;
            }

            if (delta.y == 0 && delta.x != 0)
            {
                direction = new Vector2Int(Math.Sign(delta.x), 0);
                return true;
            }

            direction = Vector2Int.zero;
            return false;
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        private static Sprite BuildWhiteSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }

        private RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(parent, false);
            var image = panel.GetComponent<Image>();
            image.sprite = whiteSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return panel;
        }

        private Text CreateText(Transform parent, string name, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color? color = null)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = defaultFont;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color ?? TextColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button CreateButton(Transform parent, string name, string label, Action onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.sprite = whiteSprite;
            image.type = Image.Type.Sliced;
            image.color = PanelStrongColor;

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = PanelStrongColor;
            colors.highlightedColor = TileMoveSafeColor;
            colors.pressedColor = TileSelectedColor;
            colors.selectedColor = TileSelectedColor;
            colors.disabledColor = new Color(0.18f, 0.18f, 0.18f, 0.55f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick());

            var text = CreateText(buttonObject.transform, "Text", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.text = label;
            Stretch((RectTransform)text.transform, 8f, 8f, 8f, 8f);

            var layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = 68f;
            return button;
        }

        private static void Stretch(RectTransform rectTransform, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void SetAnchors(RectTransform rectTransform, float minX, float minY, float maxX, float maxY, float offsetMinX, float offsetMinY, float offsetMaxX, float offsetMaxY)
        {
            rectTransform.anchorMin = new Vector2(minX, minY);
            rectTransform.anchorMax = new Vector2(maxX, maxY);
            rectTransform.offsetMin = new Vector2(offsetMinX, offsetMinY);
            rectTransform.offsetMax = new Vector2(offsetMaxX, offsetMaxY);
        }

        private static void ClearChildren(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }

        }

        private enum PermanentUpgrade
        {
            Attack,
            StartingSp,
            Hp,
        }
    }
}
