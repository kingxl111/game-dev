#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.AI.Navigation;
using TMPro;

using ReactorBreach.Core;
using ReactorBreach.Data;
using ReactorBreach.Enemies;
using ReactorBreach.Environment;
using ReactorBreach.Player;
using ReactorBreach.ScriptableObjects;
using ReactorBreach.Systems;
using ReactorBreach.Tools;
using ReactorBreach.UI;

namespace ReactorBreach.EditorTools
{
    /// <summary>
    /// Однокнопочный конструктор «Level 1 (Tutorial)»: настраивает теги/слои,
    /// создаёт ScriptableObject-ассеты, перегенерирует InputActions и собирает
    /// рабочую сцену со всеми менеджерами, игроком, UI и тестовыми объектами.
    /// </summary>
    public static class Level1Builder
    {
        private const string ScenePath        = "Assets/Scenes/Level_01_Tutorial.unity";
        private const string SOFolder         = "Assets/ScriptableObjects/Instances";
        private const string InputActionsPath = "Assets/ScriptableObjects/Instances/PlayerActions.inputactions";

        // Размеры и координаты комнат уровня
        private const float WallHeight   = 8.2f;
        private const float WallThick    = 0.4f;
        private const float DoorWidth    = 4f;
        private const float DoorHeight   = 3f;

        // Z-координаты разделителей
        private const float HubBack      = -16f;
        private const float WallHub_A    = -2f;   // вход в зону A
        private const float WallA_B      = 16f;   // зона A → B
        private const float WallB_C      = 34f;   // зона B → C
        private const float WallC_Final  = 52f;   // зона C → финал
        private const float FinalEnd     = 64f;

        // Ширина уровня (X)
        private const float HalfWidth    = 12f;

        private static EnemyConfig _crawlerConfig;
        private static EnemyConfig _bigEnemyConfig;

        [MenuItem("ReactorBreach/Build Level 1 (Tutorial)")]
        public static void Build()
        {
            try
            {
                EnsureFolders();
                EnsureTagsAndLayers();
                var weld         = CreateOrUpdate<ToolConfig>($"{SOFolder}/ToolConfig_Weld.asset",    ConfigureWeld);
                var gravity      = CreateOrUpdate<ToolConfig>($"{SOFolder}/ToolConfig_Gravity.asset", ConfigureGravity);
                var foam         = CreateOrUpdate<ToolConfig>($"{SOFolder}/ToolConfig_Foam.asset",    ConfigureFoam);
                var obj          = CreateOrUpdate<LevelObjective>($"{SOFolder}/LevelObjective_01_Tutorial.asset", ConfigureObjective);
                _crawlerConfig    = CreateOrUpdate<EnemyConfig>($"{SOFolder}/EnemyConfig_Crawler.asset", ConfigureCrawler);
                _bigEnemyConfig   = CreateOrUpdate<EnemyConfig>($"{SOFolder}/EnemyConfig_BigStalker.asset", ConfigureBigStalker);
                var actions      = CreateInputActions();

                BuildScene(weld, gravity, foam, obj, actions);
                AddSceneToBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Workaround: SerializedObject не всегда сохраняет ссылку на
                // ScriptedImporter-ассет (.inputactions) при первом сохранении
                // свежесозданной сцены. Патчим YAML напрямую — гарантированно.
                PatchSceneInputActionsReference();

                EditorUtility.DisplayDialog(
                    "Level 1 готов",
                    "Сцена Level_01_Tutorial.unity создана и добавлена в Build Settings.\n\n" +
                    "Нажми Play. Если шрифты в UI пустые — сделай Window → TextMeshPro → " +
                    "Import TMP Essential Resources.",
                    "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Level1Builder] {e}");
                EditorUtility.DisplayDialog("Ошибка", e.Message, "OK");
            }
        }

        // ── Папки ─────────────────────────────────────────────────────────────

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/ScriptableObjects");
            EnsureFolder("Assets/ScriptableObjects/Instances");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)!.Replace("\\", "/");
            string name   = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        // ── Tags & Layers ─────────────────────────────────────────────────────

        private static readonly string[] RequiredTags =
        {
            "Player", "Enemy", "Interactable", "Surface",
            "BreachPoint", "Breakable", "PressurePlate",
            "FoamBump", "WombMouth", "UIFade",
        };

        private static readonly (int index, string name)[] RequiredLayers =
        {
            (6,  "Player"),
            (7,  "Enemy"),
            (8,  "Interactable"),
            (9,  "Static"),
            (10, "Breakable"),
            (11, "FoamZone"),
            (12, "WeldBridge"),
        };

        private static void EnsureTagsAndLayers()
        {
            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var so = new SerializedObject(tagManagerAsset);

            var tagsProp = so.FindProperty("tags");
            foreach (var tag in RequiredTags)
            {
                bool exists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) { exists = true; break; }
                if (exists) continue;
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            }

            var layersProp = so.FindProperty("layers");
            foreach (var (idx, name) in RequiredLayers)
            {
                var layerProp = layersProp.GetArrayElementAtIndex(idx);
                if (string.IsNullOrEmpty(layerProp.stringValue)) layerProp.stringValue = name;
            }

            so.ApplyModifiedProperties();
        }

        // ── ScriptableObjects ─────────────────────────────────────────────────

        private static T CreateOrUpdate<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            bool created = false;
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                created = true;
            }
            configure(asset);
            EditorUtility.SetDirty(asset);
            if (created) Debug.Log($"[Level1Builder] Created {path}");
            return asset;
        }

        private static void ConfigureWeld(ToolConfig c)
        {
            c.ToolName        = "Weld";
            c.WeldRange       = 8f;
            c.WeldDuration    = 10f;
            c.WeldEnergyCost  = 25f;
            c.MaxEnergy       = 100f;
            c.EnergyRegenRate = 5f;
            c.MaxActiveWelds  = 4;
            c.WeldBreakForce  = 50000f;
        }

        private static void ConfigureGravity(ToolConfig c)
        {
            c.ToolName        = "Gravity";
            c.GravityRange      = 12f;
            c.MassMultiplier    = 30f;
            c.LightMassDivider  = 30f;
            c.GravityDuration  = 4f;
            c.GravityCooldown  = 5f;
            c.MaxTargetVolume  = 1.5f;
        }

        private static void ConfigureFoam(ToolConfig c)
        {
            c.ToolName              = "Foam";
            c.FoamRange             = 10f;
            c.FoamRadius            = 2.5f;
            c.FoamDuration          = 8f;
            c.FoamUsesPerLevel      = 12;
            c.FoamSlowMultiplier    = 0.3f;
            c.StickVelocityThreshold = 0.5f;
        }

        private static void ConfigureObjective(LevelObjective o)
        {
            o.LevelName     = "Жилой модуль «Причал»";
            o.Description   = "Пройдите 3 секции и активируйте главный пульт";
            o.Type          = ObjectiveType.Reach;
            o.SceneName     = "Level_01_Tutorial";
            o.PressureTimer = 0f;
            o.EnemyCount    = 0;
            o.BreachCount   = 0;
            o.BriefingText  = "Кольцо-7. Три зоны, три инструмента. Проложи путь к главному пульту.";
        }

        private static void ConfigureCrawler(EnemyConfig c)
        {
            c.EnemyName      = "Ползун";
            c.MaxHP          = 80f;
            c.MoveSpeed      = 2f;
            c.AttackDamage   = 12f;
            c.AttackCooldown = 1.5f;
            c.DetectionRange = 32f; // вся зона B с порога двери (14 м не хватало)
            c.AttackRange    = 1.6f;
            c.LoseTargetTime = 4f;
            c.CanClimbWalls  = false;
            c.IsStationary   = false;
            c.SpawnInterval  = 0f;
        }

        private static void ConfigureBigStalker(EnemyConfig c)
        {
            c.EnemyName      = "Сталкер";
            c.MaxHP          = 200f;
            c.MoveSpeed      = 1.55f;
            c.AttackDamage   = 25f;
            c.AttackCooldown = 2.0f;
            c.DetectionRange = 18f;
            c.AttackRange    = 1.8f;
            c.LoseTargetTime = 5f;
            c.CanClimbWalls  = false;
            c.IsStationary   = false;
            c.SpawnInterval  = 0f;
        }

        // ── Input Actions ─────────────────────────────────────────────────────

        private static InputActionAsset CreateInputActions()
        {
            // Удаляем старый файл с опечаткой, если остался
            AssetDatabase.DeleteAsset("Assets/ScriptableObjects/Instances/NewInputActopn.inputactions");

            // Если файл уже существует — используем его. Это даёт стабильный
            // GUID/fileID между запусками билдера.
            if (!File.Exists(InputActionsPath))
            {
                string json = BuildPlayerActionsJson();
                File.WriteAllText(InputActionsPath, json);
            }

            // ВАЖНО: используем ТОЛЬКО ImportAsset, без Refresh/SaveAssets!
            // Refresh инвалидирует ВСЕ ранее загруженные ассеты в C#-переменных
            // (weld/gravity/foam/objective), и их ссылки на сцене становятся
            // {fileID: 0}. ImportAsset точечно импортирует только этот файл.
            AssetDatabase.ImportAsset(InputActionsPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            string guid = AssetDatabase.AssetPathToGUID(InputActionsPath);
            if (string.IsNullOrEmpty(guid))
                throw new System.Exception($"No GUID for {InputActionsPath}. Try Reimport All.");

            var imported = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (imported == null)
                throw new System.Exception($"Failed to load {InputActionsPath}. Try Reimport All.");

            Debug.Log($"[Level1Builder] InputActions loaded: guid={guid}, " +
                      $"persistent={EditorUtility.IsPersistent(imported)}, maps={imported.actionMaps.Count}");
            return imported;
        }

        // Собираем .inputactions JSON через временный in-memory объект
        private static string BuildPlayerActionsJson()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "PlayerActions";

            var map = new InputActionMap("Player");
            asset.AddActionMap(map);

            var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up",    "<Keyboard>/w")
                .With("Down",  "<Keyboard>/s")
                .With("Left",  "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            var look = map.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            look.AddBinding("<Mouse>/delta");

            var jump = map.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");

            var sprint = map.AddAction("Sprint", InputActionType.Button);
            sprint.AddBinding("<Keyboard>/leftShift");

            var primary = map.AddAction("PrimaryAction", InputActionType.Button);
            primary.AddBinding("<Mouse>/leftButton");

            var secondary = map.AddAction("SecondaryAction", InputActionType.Button);
            secondary.AddBinding("<Mouse>/rightButton");

            var slot1 = map.AddAction("ToolSlot1", InputActionType.Button);
            slot1.AddBinding("<Keyboard>/1");
            var slot2 = map.AddAction("ToolSlot2", InputActionType.Button);
            slot2.AddBinding("<Keyboard>/2");
            var slot3 = map.AddAction("ToolSlot3", InputActionType.Button);
            slot3.AddBinding("<Keyboard>/3");

            var scroll = map.AddAction("ToolScroll", InputActionType.Value, expectedControlLayout: "Vector2");
            scroll.AddBinding("<Mouse>/scroll");

            var pause = map.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");

            string json = asset.ToJson();
            Object.DestroyImmediate(asset);
            return json;
        }

        // ── Сцена ─────────────────────────────────────────────────────────────

        private static void BuildScene(ToolConfig weld, ToolConfig gravity, ToolConfig foam,
                                       LevelObjective objective, InputActionAsset actions)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ВАЖНО: NewScene + ImportAsset из CreateInputActions могут
            // инвалидировать C#-ссылки на ассеты, созданные ранее. Поэтому
            // перезагружаем все ассеты свежими по их путям прямо здесь.
            weld            = AssetDatabase.LoadAssetAtPath<ToolConfig>($"{SOFolder}/ToolConfig_Weld.asset");
            gravity         = AssetDatabase.LoadAssetAtPath<ToolConfig>($"{SOFolder}/ToolConfig_Gravity.asset");
            foam            = AssetDatabase.LoadAssetAtPath<ToolConfig>($"{SOFolder}/ToolConfig_Foam.asset");
            objective       = AssetDatabase.LoadAssetAtPath<LevelObjective>($"{SOFolder}/LevelObjective_01_Tutorial.asset");
            actions         = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            _crawlerConfig  = AssetDatabase.LoadAssetAtPath<EnemyConfig>($"{SOFolder}/EnemyConfig_Crawler.asset");
            _bigEnemyConfig = AssetDatabase.LoadAssetAtPath<EnemyConfig>($"{SOFolder}/EnemyConfig_BigStalker.asset");

            if (weld == null || gravity == null || foam == null || objective == null || actions == null
                || _crawlerConfig == null || _bigEnemyConfig == null)
                throw new System.Exception(
                    $"[Level1Builder] BuildScene: один из ассетов не загрузился. " +
                    $"weld={weld}, gravity={gravity}, foam={foam}, obj={objective}, actions={actions}, " +
                    $"crawler={_crawlerConfig}, big={_bigEnemyConfig}");

            BuildLighting();
            BuildLevelGeometry();
            BuildManagers(objective);
            var (player, mainCamera) = BuildPlayer(weld, gravity, foam, actions);
            BuildCanvas(player);
            BuildLevelHints();
            BuildRoomA_Weld();
            BuildRoomB_Gravity();
            BuildRoomC_Foam();
            BuildFinalRoom();
            BuildNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        // Тёмная индастриал-палитра в тёплых янтарных тонах
        private static readonly Color FloorColor    = new Color(0.07f, 0.06f, 0.05f);
        private static readonly Color WallColor     = new Color(0.12f, 0.10f, 0.08f);
        private static readonly Color BeamColor     = new Color(0.16f, 0.13f, 0.10f);
        private static readonly Color CrateColor    = new Color(0.55f, 0.30f, 0.10f);
        private static readonly Color PillarColor   = new Color(0.20f, 0.16f, 0.12f);
        private static readonly Color GravityColor  = new Color(0.85f, 0.45f, 0.14f);
        private static readonly Color FoamBoxColor  = new Color(0.45f, 0.32f, 0.16f);
        private static readonly Color PanelBase     = new Color(0.10f, 0.08f, 0.06f);
        private static readonly Color PanelScreen   = new Color(1.00f, 0.55f, 0.18f);
        private static readonly Color BreachColor   = new Color(1.00f, 0.32f, 0.10f);
        private static readonly Color WarmAccent    = new Color(1.00f, 0.62f, 0.32f);
        private static readonly Color WarmDimLight  = new Color(0.95f, 0.55f, 0.25f);

        private static void BuildLighting()
        {
            var sun = new GameObject("Directional Light");
            sun.transform.rotation = Quaternion.Euler(48f, -25f, 0f);
            var light = sun.AddComponent<Light>();
            light.type           = LightType.Directional;
            light.intensity      = 0.30f;
            light.color          = new Color(1.00f, 0.78f, 0.55f);
            light.shadows        = LightShadows.Soft;
            light.shadowStrength = 0.85f;

            // Лампы по одной над центром каждой комнаты
            CreatePointLight("Lamp_Hub",   new Vector3(0, 4.5f, -9),  WarmDimLight, 14f, 1.4f);
            CreatePointLight("Lamp_RoomA", new Vector3(0, 4.5f,  7),  WarmAccent,   16f, 1.7f);
            CreatePointLight("Lamp_RoomB", new Vector3(0, 4.5f, 25),  WarmAccent,   16f, 1.7f);
            CreatePointLight("Lamp_RoomC", new Vector3(0, 4.5f, 43),  WarmAccent,   16f, 1.7f);
            CreatePointLight("Lamp_Final", new Vector3(0, 4.5f, 58),  WarmAccent,   12f, 2.0f);

            // Атмосфера — тёплый, тёмный, глубокий ambient
            RenderSettings.ambientMode         = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.24f, 0.14f, 0.08f);
            RenderSettings.ambientEquatorColor = new Color(0.10f, 0.07f, 0.04f);
            RenderSettings.ambientGroundColor  = new Color(0.04f, 0.03f, 0.02f);
            RenderSettings.ambientIntensity    = 0.85f;

            RenderSettings.fog              = true;
            RenderSettings.fogMode          = FogMode.Linear;
            RenderSettings.fogColor         = new Color(0.05f, 0.04f, 0.03f);
            RenderSettings.fogStartDistance = 12f;
            RenderSettings.fogEndDistance   = 60f;
        }

        private static void CreatePointLight(string name, Vector3 pos, Color color, float range, float intensity)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type      = LightType.Point;
            light.color     = color;
            light.range     = range;
            light.intensity = intensity;
            light.shadows   = LightShadows.None;
        }

        // ── Геометрия уровня: Hub + 3 комнаты + Финальный пульт ──────────────

        private static void BuildLevelGeometry()
        {
            var levelRoot = new GameObject("Level");

            // Полы по секциям + смертельная бездна между ними в Зоне A
            BuildFloorTile("Floor_Hub",   levelRoot.transform, HubBack,     WallHub_A);
            BuildFloorTile("Floor_A1",    levelRoot.transform, WallHub_A,   3.5f);   // ближняя половина зоны A
            // пропасть Z[3.5..9.5] — нет пола (DeathZone заполняется в BuildRoomA_Weld)
            BuildFloorTile("Floor_A2",    levelRoot.transform, 9.5f,        WallA_B);  // дальняя половина
            BuildFloorTile("Floor_B",     levelRoot.transform, WallA_B,     WallB_C);
            BuildFloorTile("Floor_C",     levelRoot.transform, WallB_C,     WallC_Final);
            BuildFloorTile("Floor_Final", levelRoot.transform, WallC_Final, FinalEnd);

            // Внешние стены (длинные секции — север/юг и боковые)
            BuildOuterWalls(levelRoot.transform);

            // Перегородки между секциями — стена с дверным проёмом
            BuildPartitionWithDoorway("Wall_Hub_A",   WallHub_A,   levelRoot.transform);
            BuildPartitionWithDoorway("Wall_A_B",     WallA_B,     levelRoot.transform);
            BuildPartitionWithDoorway("Wall_B_C",     WallB_C,     levelRoot.transform);
            BuildPartitionWithDoorway("Wall_C_Final", WallC_Final, levelRoot.transform);

            CreatePlatform("Station_Roof_West",
                new Vector3(-8.5f, WallHeight + 0.08f, (HubBack + FinalEnd) * 0.5f),
                new Vector3(7f, 0.16f, FinalEnd - HubBack + WallThick * 2f),
                levelRoot.transform);
            CreatePlatform("Station_Roof_East",
                new Vector3(8.5f, WallHeight + 0.08f, (HubBack + FinalEnd) * 0.5f),
                new Vector3(7f, 0.16f, FinalEnd - HubBack + WallThick * 2f),
                levelRoot.transform);
            CreatePlatform("Station_Roof_HubPanel",
                new Vector3(0f, WallHeight + 0.08f, -9f),
                new Vector3(8f, 0.16f, 13f),
                levelRoot.transform);
            CreatePlatform("Station_Roof_FinalPanel",
                new Vector3(0f, WallHeight + 0.08f, 58f),
                new Vector3(8f, 0.16f, 12f),
                levelRoot.transform);

            // Потолочная балка по центру
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "Ceiling_Beam";
            beam.tag  = "Surface";
            beam.layer = GameConstants.LayerStatic;
            beam.transform.SetParent(levelRoot.transform, false);
            beam.transform.position   = new Vector3(0, WallHeight - 0.35f, (HubBack + FinalEnd) * 0.5f);
            beam.transform.localScale = new Vector3(2f, 0.3f, FinalEnd - HubBack);
            Paint(beam, BeamColor, metallic: 0.6f, smoothness: 0.25f);
        }

        private static GameObject BuildFloorTile(string name, Transform parent, float zMin, float zMax)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name;
            floor.tag  = "Surface";
            floor.layer = GameConstants.LayerStatic;
            floor.transform.SetParent(parent, false);
            float length = zMax - zMin;
            float zCenter = (zMin + zMax) * 0.5f;
            floor.transform.position   = new Vector3(0f, -0.05f, zCenter);
            floor.transform.localScale = new Vector3(HalfWidth * 2f, 0.1f, length);
            Paint(floor, FloorColor, metallic: 0.55f, smoothness: 0.25f);
            return floor;
        }

        private static void BuildOuterWalls(Transform parent)
        {
            // Восточная и западная стены — на всю длину уровня
            float length = FinalEnd - HubBack;
            float zMid   = (HubBack + FinalEnd) * 0.5f;

            CreateWall("Wall_East",
                new Vector3( HalfWidth + WallThick * 0.5f, WallHeight * 0.5f, zMid),
                new Vector3(WallThick, WallHeight, length), parent);
            CreateWall("Wall_West",
                new Vector3(-HalfWidth - WallThick * 0.5f, WallHeight * 0.5f, zMid),
                new Vector3(WallThick, WallHeight, length), parent);

            // Задняя стена за Hub
            CreateWall("Wall_Back",
                new Vector3(0, WallHeight * 0.5f, HubBack - WallThick * 0.5f),
                new Vector3(HalfWidth * 2f + WallThick * 2f, WallHeight, WallThick), parent);
            // Передняя стена за финальной комнатой
            CreateWall("Wall_Front",
                new Vector3(0, WallHeight * 0.5f, FinalEnd + WallThick * 0.5f),
                new Vector3(HalfWidth * 2f + WallThick * 2f, WallHeight, WallThick), parent);
        }

        /// <summary>Поперечная стена с дверным проёмом по центру.</summary>
        private static void BuildPartitionWithDoorway(string name, float z, Transform parent)
        {
            float halfDoor = DoorWidth * 0.5f;

            // Левый сегмент
            float leftWidth = HalfWidth - halfDoor;
            CreateWall($"{name}_L",
                new Vector3(-(halfDoor + leftWidth * 0.5f), WallHeight * 0.5f, z),
                new Vector3(leftWidth, WallHeight, WallThick), parent);
            // Правый сегмент
            CreateWall($"{name}_R",
                new Vector3((halfDoor + leftWidth * 0.5f), WallHeight * 0.5f, z),
                new Vector3(leftWidth, WallHeight, WallThick), parent);
            // Перемычка над дверью
            float lintelHeight = WallHeight - DoorHeight;
            CreateWall($"{name}_Top",
                new Vector3(0, DoorHeight + lintelHeight * 0.5f, z),
                new Vector3(DoorWidth, lintelHeight, WallThick), parent);
        }

        private static GameObject CreateWall(string name, Vector3 pos, Vector3 size, Transform parent = null)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.tag  = "Surface";
            wall.layer = GameConstants.LayerStatic;
            if (parent != null) wall.transform.SetParent(parent, false);
            wall.transform.position   = pos;
            wall.transform.localScale = size;
            Paint(wall, WallColor, metallic: 0.4f, smoothness: 0.35f);
            return wall;
        }

        private static GameObject CreatePlatform(string name, Vector3 pos, Vector3 size, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.tag = "Surface";
            go.layer = GameConstants.LayerStatic;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            Paint(go, new Color(0.11f, 0.10f, 0.09f), metallic: 0.75f, smoothness: 0.22f);
            return go;
        }

        private static GameObject CreateRamp(string name, Vector3 pos, Vector3 size, Vector3 euler, Transform parent = null)
        {
            var go = CreatePlatform(name, pos, size, parent);
            go.transform.rotation = Quaternion.Euler(euler);
            Paint(go, new Color(0.13f, 0.11f, 0.09f), metallic: 0.7f, smoothness: 0.2f);
            return go;
        }

        private static GameObject CreateCatwalk(string name, Vector3 pos, Vector3 size, Transform parent = null)
        {
            var deck = CreatePlatform(name, pos, size, parent);
            CreateRail($"{name}_Rail_L", pos + new Vector3(-size.x * 0.5f, 0.45f, 0f), new Vector3(0.12f, 0.9f, size.z), parent);
            CreateRail($"{name}_Rail_R", pos + new Vector3( size.x * 0.5f, 0.45f, 0f), new Vector3(0.12f, 0.9f, size.z), parent);
            return deck;
        }

        private static GameObject CreateRail(string name, Vector3 pos, Vector3 size, Transform parent = null)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = name;
            rail.tag = "Surface";
            rail.layer = GameConstants.LayerStatic;
            if (parent != null) rail.transform.SetParent(parent, false);
            rail.transform.position = pos;
            rail.transform.localScale = size;
            Paint(rail, new Color(0.22f, 0.18f, 0.14f), metallic: 0.8f, smoothness: 0.25f);
            return rail;
        }

        private static GameObject CreateStationPipe(string name, Vector3 pos, Vector3 scale, Vector3 euler, Transform parent = null)
        {
            var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = name;
            pipe.tag = "Surface";
            pipe.layer = GameConstants.LayerStatic;
            if (parent != null) pipe.transform.SetParent(parent, false);
            pipe.transform.position = pos;
            pipe.transform.rotation = Quaternion.Euler(euler);
            pipe.transform.localScale = scale;
            Object.DestroyImmediate(pipe.GetComponent<Collider>());
            Paint(pipe, new Color(0.18f, 0.14f, 0.11f), metallic: 0.85f, smoothness: 0.32f,
                  emission: new Color(0.45f, 0.16f, 0.06f), emissionIntensity: 0.25f);
            return pipe;
        }

        private static void CreateConsoleCluster(string name, Vector3 pos, Quaternion rot, Transform parent = null)
        {
            var baseGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGO.name = $"{name}_Base";
            baseGO.tag = "Surface";
            baseGO.layer = GameConstants.LayerStatic;
            if (parent != null) baseGO.transform.SetParent(parent, false);
            baseGO.transform.position = pos;
            baseGO.transform.rotation = rot;
            baseGO.transform.localScale = new Vector3(1.5f, 0.9f, 0.45f);
            Paint(baseGO, PanelBase, metallic: 0.7f, smoothness: 0.35f);

            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = $"{name}_Screen";
            screen.transform.SetParent(baseGO.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0.18f, -0.55f);
            screen.transform.localScale = new Vector3(0.9f, 0.38f, 0.04f);
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            Paint(screen, PanelScreen, metallic: 0f, smoothness: 0.9f,
                  emission: PanelScreen, emissionIntensity: 1.4f);
        }

        private static void BuildManagers(LevelObjective objective)
        {
            var gm = new GameObject("_GameManager");
            gm.AddComponent<GameManager>();

            var am = new GameObject("_AudioManager");
            var audio = am.AddComponent<AudioManager>();
            var music   = am.AddComponent<AudioSource>(); music.playOnAwake = false;
            var ambient = am.AddComponent<AudioSource>(); ambient.playOnAwake = false;
            SetField(audio, "_musicSource",   music);
            SetField(audio, "_ambientSource", ambient);

            var vs = new GameObject("_VibrationSystem");
            vs.AddComponent<VibrationSystem>();

            var lm = new GameObject("_LevelManager");
            var levelManager = lm.AddComponent<LevelManager>();
            lm.AddComponent<ObjectiveTracker>();
            SetField(levelManager, "_objective", objective);
        }

        private static (GameObject player, Camera cam) BuildPlayer(ToolConfig weld, ToolConfig gravity,
                                                                    ToolConfig foam, InputActionAsset actions)
        {
            var player = new GameObject("Player");
            player.tag   = "Player";
            player.layer = GameConstants.LayerPlayer;
            player.transform.position = new Vector3(0f, 1f, -10f);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            player.AddComponent<PlayerHealth>();
            var toolManager = player.AddComponent<ToolManager>();
            var pc          = player.AddComponent<PlayerController>();
            player.AddComponent<PlayerInteraction>();

            // Camera child
            var camGO = new GameObject("Camera");
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(player.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.04f, 0.03f);
            cam.fieldOfView     = 75f;
            cam.nearClipPlane   = 0.05f;
            camGO.AddComponent<AudioListener>();

            var toolMount = new GameObject("ToolMount");
            toolMount.transform.SetParent(camGO.transform, false);
            toolMount.transform.localPosition = new Vector3(0.3f, -0.25f, 0.5f);

            // GroundCheck
            var ground = new GameObject("GroundCheck");
            ground.transform.SetParent(player.transform, false);
            ground.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            // PlayerController fields
            SetField(pc, "_groundCheck", ground.transform);
            SetField(pc, "_cameraTarget", camGO.transform);
            SetField(pc, "_inputActions", actions);
            int groundMask = (1 << GameConstants.LayerDefault)
                           | (1 << GameConstants.LayerStatic)
                           | (1 << GameConstants.LayerBreakable);
            SetLayerMask(pc, "_groundMask", groundMask);

            // ToolManager fields
            SetArray(toolManager, "_toolConfigs", new Object[] { weld, gravity, foam });
            SetField(toolManager, "_toolMount", toolMount.transform);

            // PlayerController сам подписывается на действия в OnEnable —
            // компонент PlayerInput не нужен. Это устраняет хрупкую сериализацию
            // PlayerInput.m_Actions, которая теряется при сохранении сцены.

            // Lock cursor at start (small helper component)
            player.AddComponent<CursorLockHelper>();

            return (player, cam);
        }

        private static void BuildCanvas(GameObject player)
        {
            var canvasGO = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // EventSystem
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // HUD root + warning text
            var hud = new GameObject("HUD");
            hud.transform.SetParent(canvasGO.transform, false);
            var hudCtl = hud.AddComponent<HUDController>();
            var warningGO = MakeTMPText(canvasGO.transform, "WarningText",
                new Vector2(0, -150), 36, TextAlignmentOptions.Center, Color.yellow);
            SetField(hudCtl, "_warningText", warningGO.GetComponent<TextMeshProUGUI>());

            // Crosshair
            var crosshairGO = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
            crosshairGO.transform.SetParent(canvasGO.transform, false);
            var crossRT = (RectTransform)crosshairGO.transform;
            crossRT.sizeDelta = new Vector2(16, 16);
            crossRT.anchoredPosition = Vector2.zero;
            var crossImg = crosshairGO.GetComponent<Image>();
            crossImg.color = Color.white;
            var crossCtl = crosshairGO.AddComponent<CrosshairController>();
            SetField(crossCtl, "_crosshairImage", crossImg);

            // HealthBar (slider)
            var hbGO = MakeSlider(canvasGO.transform, "HealthBar",
                new Vector2(-700, 480), new Vector2(300, 28), Color.green);
            var hb = hbGO.AddComponent<HealthBar>();
            SetField(hb, "_slider", hbGO.GetComponent<Slider>());
            SetField(hb, "_fillImage", hbGO.transform.Find("Fill Area/Fill").GetComponent<Image>());

            // ToolIndicator (3 slots)
            var indicatorGO = new GameObject("ToolIndicator", typeof(RectTransform));
            indicatorGO.transform.SetParent(canvasGO.transform, false);
            var indRT = (RectTransform)indicatorGO.transform;
            indRT.anchoredPosition = new Vector2(0, -440);
            indRT.sizeDelta = new Vector2(720, 110);
            var indicator = indicatorGO.AddComponent<ToolIndicator>();
            var slotData = new List<ToolIndicator.SlotUI>();
            string[] slotLabels = { "Weld", "Gravity", "Foam" };
            for (int i = 0; i < 3; i++)
            {
                var slot = MakeIndicatorSlot(indicatorGO.transform, slotLabels[i], i);
                slotData.Add(slot);
            }
            SetSlotsArray(indicator, slotData);

            var helpPanel = new GameObject("ToolHelpPanel", typeof(RectTransform), typeof(Image));
            helpPanel.transform.SetParent(canvasGO.transform, false);
            var helpRT = (RectTransform)helpPanel.transform;
            helpRT.anchoredPosition = new Vector2(0, -350);
            helpRT.sizeDelta = new Vector2(980, 86);
            helpPanel.GetComponent<Image>().color = new Color(0.02f, 0.025f, 0.035f, 0.78f);
            var helpTitleGO = MakeTMPText(helpPanel.transform, "Title",
                new Vector2(-360, 18), 24, TextAlignmentOptions.Left, new Color(1f, 0.72f, 0.28f));
            var helpTitleRT = (RectTransform)helpTitleGO.transform;
            helpTitleRT.sizeDelta = new Vector2(250, 40);
            var helpBodyGO = MakeTMPText(helpPanel.transform, "Body",
                new Vector2(125, 2), 22, TextAlignmentOptions.Left, new Color(0.9f, 0.93f, 0.95f));
            var helpBodyRT = (RectTransform)helpBodyGO.transform;
            helpBodyRT.sizeDelta = new Vector2(720, 60);
            var helpBody = helpBodyGO.GetComponent<TextMeshProUGUI>();
            helpBody.enableWordWrapping = true;
            var help = helpPanel.AddComponent<ToolHelpPanel>();
            SetField(help, "_title", helpTitleGO.GetComponent<TextMeshProUGUI>());
            SetField(help, "_body", helpBody);

            // Objective text
            var objGO = MakeTMPText(canvasGO.transform, "ObjectiveText",
                new Vector2(0, 480), 32, TextAlignmentOptions.Center, Color.white);
            var objDisplay = objGO.AddComponent<ObjectiveDisplay>();
            SetField(objDisplay, "_objectiveText", objGO.GetComponent<TextMeshProUGUI>());

            // PressureGauge (hidden on tutorial via PressureGauge.Start auto-disable)
            var pressureGO = MakeSlider(canvasGO.transform, "PressureGauge",
                new Vector2(0, 440), new Vector2(400, 24), Color.red);
            var labelGO = MakeTMPText(pressureGO.transform, "Label",
                new Vector2(0, 30), 22, TextAlignmentOptions.Center, Color.white);
            var pressure = pressureGO.AddComponent<PressureGauge>();
            SetField(pressure, "_slider",    pressureGO.GetComponent<Slider>());
            SetField(pressure, "_label",     labelGO.GetComponent<TextMeshProUGUI>());
            SetField(pressure, "_fillImage", pressureGO.transform.Find("Fill Area/Fill").GetComponent<Image>());

            // PauseMenu
            var pauseRoot = new GameObject("PauseMenu", typeof(RectTransform));
            pauseRoot.transform.SetParent(canvasGO.transform, false);
            var pauseRT = (RectTransform)pauseRoot.transform;
            pauseRT.anchorMin = Vector2.zero; pauseRT.anchorMax = Vector2.one;
            pauseRT.offsetMin = Vector2.zero; pauseRT.offsetMax = Vector2.zero;
            var pausePanel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            pausePanel.transform.SetParent(pauseRoot.transform, false);
            var ppRT = (RectTransform)pausePanel.transform;
            ppRT.anchorMin = Vector2.zero; ppRT.anchorMax = Vector2.one;
            ppRT.offsetMin = Vector2.zero; ppRT.offsetMax = Vector2.zero;
            pausePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            MakeTMPText(pausePanel.transform, "PausedLabel", Vector2.zero, 64,
                TextAlignmentOptions.Center, Color.white).GetComponent<TextMeshProUGUI>().text = "PAUSED";

            var pauseScript = pauseRoot.AddComponent<PauseMenu>();
            SetField(pauseScript, "_pausePanel", pausePanel);
            pausePanel.SetActive(false);

            // ── Подсказки уровня (всплывающее окно, как Game Over) ─────────────
            var hintRoot = new GameObject("LevelHintRoot", typeof(RectTransform));
            hintRoot.transform.SetParent(canvasGO.transform, false);
            var hintRTR = (RectTransform)hintRoot.transform;
            hintRTR.anchorMin = Vector2.zero; hintRTR.anchorMax = Vector2.one;
            hintRTR.offsetMin = Vector2.zero; hintRTR.offsetMax = Vector2.zero;
            var hintPanel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            hintPanel.transform.SetParent(hintRoot.transform, false);
            var hpRT = (RectTransform)hintPanel.transform;
            hpRT.anchorMin = Vector2.zero; hpRT.anchorMax = Vector2.one;
            hpRT.offsetMin = Vector2.zero; hpRT.offsetMax = Vector2.zero;
            var hintPanelImg = hintPanel.GetComponent<Image>();
            hintPanelImg.color = new Color(0.02f, 0.02f, 0.05f, 0.88f);
            // false — клик по «пустоте» не ловит UI; закрытие — кнопка и клавиши
            hintPanelImg.raycastTarget = false;
            var hintTitleGO = MakeTMPText(hintPanel.transform, "Title",
                new Vector2(0, 220), 40, TextAlignmentOptions.Center, new Color(1f, 0.72f, 0.28f));
            var titleRT = (RectTransform)hintTitleGO.transform;
            titleRT.sizeDelta = new Vector2(1100, 120);
            var titleTMP = hintTitleGO.GetComponent<TextMeshProUGUI>();
            var hintBodyGO = MakeTMPText(hintPanel.transform, "Body",
                new Vector2(0, -20), 24, TextAlignmentOptions.Top, new Color(0.94f, 0.94f, 0.96f));
            var bodyRT = (RectTransform)hintBodyGO.transform;
            bodyRT.sizeDelta = new Vector2(1000, 400);
            var bodyTMP = hintBodyGO.GetComponent<TextMeshProUGUI>();
            bodyTMP.enableWordWrapping = true;
            bodyTMP.alignment = TextAlignmentOptions.TopLeft;
            // Кнопка «Продолжить»
            var btnGO = new GameObject("BtnContinue", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(hintPanel.transform, false);
            var bRT = (RectTransform)btnGO.transform;
            bRT.anchoredPosition = new Vector2(0, -300);
            bRT.sizeDelta = new Vector2(320, 56);
            var btnImg = btnGO.GetComponent<Image>();
            btnImg.color = new Color(0.15f, 0.4f, 0.2f, 0.95f);
            var btn = btnGO.GetComponent<Button>();
            var btnLabel = MakeTMPText(btnGO.transform, "Label", Vector2.zero, 26,
                TextAlignmentOptions.Center, new Color(0.95f, 1f, 0.95f));
            var bLabRT = (RectTransform)btnLabel.transform;
            bLabRT.anchorMin = Vector2.zero; bLabRT.anchorMax = Vector2.one;
            bLabRT.offsetMin = Vector2.zero; bLabRT.offsetMax = Vector2.zero;
            btnLabel.GetComponent<TextMeshProUGUI>().text = "Продолжить";
            var hintT = MakeTMPText(hintPanel.transform, "FooterHint", new Vector2(0, -360), 20,
                TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.75f));
            hintT.GetComponent<TextMeshProUGUI>().text = "Пробел · Enter · Esc — закрыть";
            var hintComp = hintRoot.AddComponent<LevelHintPanel>();
            SetField(hintComp, "_panel", hintPanel);
            SetField(hintComp, "_title", titleTMP);
            SetField(hintComp, "_body",  bodyTMP);
            SetField(hintComp, "_dismissButton", btn);
            hintPanel.SetActive(false);

            // ── Game Over оверлей ─────────────────────────────────────────────
            var goRoot = new GameObject("GameOverRoot", typeof(RectTransform));
            goRoot.transform.SetParent(canvasGO.transform, false);
            var goRT = (RectTransform)goRoot.transform;
            goRT.anchorMin = Vector2.zero; goRT.anchorMax = Vector2.one;
            goRT.offsetMin = Vector2.zero; goRT.offsetMax = Vector2.zero;
            var goPanel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            goPanel.transform.SetParent(goRoot.transform, false);
            var gpRT = (RectTransform)goPanel.transform;
            gpRT.anchorMin = Vector2.zero; gpRT.anchorMax = Vector2.one;
            gpRT.offsetMin = Vector2.zero; gpRT.offsetMax = Vector2.zero;
            goPanel.GetComponent<Image>().color = new Color(0.05f, 0f, 0f, 0.85f);
            MakeTMPText(goPanel.transform, "DeathLabel", new Vector2(0, 60), 80,
                TextAlignmentOptions.Center, new Color(1f, 0.2f, 0.15f))
                .GetComponent<TextMeshProUGUI>().text = "ВЫ ПОГИБЛИ";
            MakeTMPText(goPanel.transform, "RestartHint", new Vector2(0, -60), 32,
                TextAlignmentOptions.Center, new Color(0.95f, 0.95f, 0.95f))
                .GetComponent<TextMeshProUGUI>().text = "Нажмите R или клик ЛКМ — перезапустить";
            var goScreen = goRoot.AddComponent<GameOverScreen>();
            SetField(goScreen, "_panel", goPanel);
            goPanel.SetActive(false);
        }

        // ── Содержимое комнат ─────────────────────────────────────────────────

        /// <summary>
        /// Стартовая подсказка (UI) + невидимые триггеры при входе в зоны.
        /// </summary>
        private static void BuildLevelHints()
        {
            var go = new GameObject("_LevelStartHint");
            var lsh = go.AddComponent<LevelStartHint>();
            SetStringField(lsh, "_title", "Кольцо-7 — учебный сектор");
            SetStringField(lsh, "_body",
                "Сектор работает в аварийном режиме. Нужно пройти через учебные отсеки и выйти к главному пульту управления.\n\n" +
                "Инструменты: [1] сварка, [2] гравитация, [3] пена. Tab переключает следующий слот.\n" +
                "ЛКМ — основное действие, ПКМ — вторичное. Смотри подсказку внизу экрана: там показано, что делает текущий инструмент.\n\n" +
                "Первый отсек проверяет сварку. Иди к разрыву пола.");

            // Триггеры при пересечении границы зоны (показ один раз на зону)
            CreateHintZone("HintZone_A", new Vector3(0, 1.2f, 0.5f), new Vector3(22, 2.2f, 2.2f),
                "Отсек A — сварочный мост",
                "Пол впереди разорван. Падение вниз смертельно.\n\n" +
                "Выбери [1] Сварка. Сначала выстрели ЛКМ в длинную балку, затем в анкер на другой стороне. Получится жёсткая связка, по которой можно перейти.\n\n" +
                "После перехода подойди к зелёной панели у двери.");
            CreateHintZone("HintZone_B", new Vector3(0, 1.2f, 25f), new Vector3(22, 2.2f, 20f),
                "Отсек B — масса и Ползун",
                "Дверь в следующий сектор открывается жёлтой плитой. Ей нужна масса, а не сварка.\n\n" +
                "Поставь куб на плиту. Если куб не двигается — выбери [2] Гравитация и нажми ПКМ по кубу, чтобы облегчить его. Когда куб окажется на плите, наведи прицел на куб и нажми ЛКМ: Heavy сделает его достаточно тяжёлым.\n\n" +
                "Ползуна можно замедлять пеной [3]. Это не открывает дверь, но даёт время спокойно работать с кубом.",
                dismissContext: "ZoneB");
            CreateHintZone("HintZone_C", new Vector3(0, 1.2f, 35.5f), new Vector3(22, 2.2f, 2.2f),
                "Отсек C — три палубы",
                "Это вертикальный техотсек: нижняя палуба, грузовая палуба и верхний мостик. Здесь понадобятся все три инструмента.\n\n" +
                "Снизу задержи врага пеной [3]. На второй палубе поставь куб на плиту и утяжели его гравитацией [2], чтобы открыть проход выше. На верхней палубе сваркой [1] собери мост через разрыв.\n\n" +
                "После этого доберись до зелёной панели у двери в командный отсек.");
            CreateHintZone("HintZone_Final", new Vector3(0, 1.2f, 54f), new Vector3(20, 2.2f, 2.2f),
                "Командный отсек",
                "Почти всё. Доберись до главного пульта в конце станции и активируй его.\n\n" +
                "После касания зоны пульта задание будет завершено.");
        }

        private static void CreateHintZone(string name, Vector3 center, Vector3 size,
            string title, string body, string dismissContext = null)
        {
            var go = new GameObject(name);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var hz = go.AddComponent<HintZoneTrigger>();
            SetStringField(hz, "_title", title);
            SetStringField(hz, "_body",  body);
            if (!string.IsNullOrEmpty(dismissContext))
                SetStringField(hz, "_dismissContext", dismissContext);
        }

        // ─── ЗОНА A: СВАРКА ───────────────────────────────────────────────────
        // Сценарий: между ближней и дальней половиной зоны — пропасть.
        // На дальней стороне — анкер-столбы и пульт активации двери в зону B.
        // Игрок должен сварить движущийся ящик с анкер-столбом, чтобы соорудить
        // мост через пропасть и добраться до пульта.
        private static void BuildRoomA_Weld()
        {
            float zNear = WallHub_A;
            float zFar  = WallA_B;
            float zMid  = (zNear + zFar) * 0.5f;

            // Пропасть Z[3.5..9.5] — без зоны смерти в сцене; провал обрабатывает PlayerController
            CreateDeathPit("RoomA_Pit", zMin: 3.5f, zMax: 9.5f);

            // На ближней стороне — два длинных движущихся ящика (балки)
            CreateMovableBeam("WeldBeam_A1", new Vector3(-3, 0.5f, 2.5f));
            CreateMovableBeam("WeldBeam_A2", new Vector3( 3, 0.5f, 2.5f));

            // На дальней стороне — два анкерных столба (статичные, weldable)
            CreateStaticPillar("Anchor_A1", new Vector3(-3, 1.5f, 10.0f));
            CreateStaticPillar("Anchor_A2", new Vector3( 3, 1.5f, 10.0f));

            // Запасной длинный ящик (с лёгким спавном, чтобы можно было поднять/толкать)
            CreateMovableBeam("WeldBeam_A3", new Vector3(0, 0.5f, 1.0f));

            CreatePlatform("A_ServiceDeck_Near", new Vector3(-9.5f, 2.25f, 1.6f), new Vector3(3.2f, 0.2f, 5.5f));
            CreatePlatform("A_ServiceDeck_Far",  new Vector3( 9.5f, 2.25f, 12.0f), new Vector3(3.2f, 0.2f, 5.5f));
            CreateRamp("A_Ramp_Near", new Vector3(-9.5f, 1.1f, -0.9f), new Vector3(2.6f, 0.18f, 6.2f), new Vector3(-20f, 0f, 0f));
            CreateRamp("A_Ramp_Far",  new Vector3( 9.5f, 1.1f, 14.2f), new Vector3(2.6f, 0.18f, 6.2f), new Vector3(20f, 0f, 0f));
            CreateStationPipe("A_Pipe_Overhead_1", new Vector3(-10.8f, 3.6f, zMid), new Vector3(0.22f, 8f, 0.22f), new Vector3(90f, 0f, 0f));
            CreateStationPipe("A_Pipe_Overhead_2", new Vector3( 10.8f, 3.3f, zMid), new Vector3(0.16f, 8f, 0.16f), new Vector3(90f, 0f, 0f));
            CreateConsoleCluster("A_DiagConsole", new Vector3(-9.8f, 0.55f, zFar - 2.6f), Quaternion.Euler(0f, 90f, 0f));

            // Пульт активации двери в зону B
            var doorTrigger = CreateButtonPanel("BtnPanel_OpenA_B",
                new Vector3(0, 1f, zFar - 1.0f), Quaternion.Euler(0, 180f, 0));

            // Дверь между Зоной A и Зоной B
            var door = CreateAutoDoor("Door_A_B", WallA_B);

            // Подключение: панель → дверь Open
            var trig = doorTrigger.GetComponent<RoomTrigger>();
            UnityEventTools.AddPersistentListener(GetOnPlayerEntered(trig), (UnityAction)door.Open);
        }

        // ─── ЗОНА B: ГРАВИТАЦИЯ ───────────────────────────────────────────────
        // Сценарий: дверь в зону C открывает пресс-плита на массу >= 25.
        // В комнате — лёгкие ящики (mass=1). Чтобы дверь открылась, надо
        // положить ящик на плиту и сделать тяжёлым (ЛКМ Gravity → mass × 30).
        // По комнате патрулирует Ползун, реагирующий на вибрацию шагов и атак.
        private static void BuildRoomB_Gravity()
        {
            float zNear = WallA_B;
            float zFar  = WallB_C;
            float zMid  = (zNear + zFar) * 0.5f;

            // 5 лёгких ящиков, разбросанных по полу
            CreateGravityCube("GravBox_1", new Vector3(-7, 0.5f, zNear + 3f));
            CreateGravityCube("GravBox_2", new Vector3(-3, 0.5f, zNear + 6f));
            CreateGravityCube("GravBox_3", new Vector3( 4, 0.5f, zMid));
            CreateGravityCube("GravBox_4", new Vector3( 7, 0.5f, zNear + 4f));
            CreateGravityCube("GravBox_5", new Vector3( 0, 0.5f, zNear + 2f));

            // Декоративные стеллажи по краям
            CreateShelf("Shelf_BL", new Vector3(-HalfWidth + 0.6f, 0.5f, zMid - 2f));
            CreateShelf("Shelf_BR", new Vector3( HalfWidth - 0.6f, 0.5f, zMid + 2f));
            CreateShelf("Shelf_B_Cover_1", new Vector3(-5.5f, 0.5f, zMid - 1.8f));
            CreateShelf("Shelf_B_Cover_2", new Vector3( 5.8f, 0.5f, zMid + 1.4f));
            CreateCatwalk("B_UpperCatwalk_West", new Vector3(-9.2f, 2.2f, zMid), new Vector3(2.6f, 0.18f, 13f));
            CreateRamp("B_Ramp_Up_West",   new Vector3(-9.2f, 1.08f, zNear + 2.2f), new Vector3(2.4f, 0.18f, 6.5f), new Vector3(-20f, 0f, 0f));
            CreateRamp("B_Ramp_Down_West", new Vector3(-9.2f, 1.08f, zFar - 2.2f),  new Vector3(2.4f, 0.18f, 6.5f), new Vector3(20f, 0f, 0f));
            CreatePlatform("B_ObservationDeck", new Vector3(8.7f, 2.2f, zFar - 4.5f), new Vector3(3.6f, 0.18f, 4.2f));
            CreateRamp("B_Ramp_Observation", new Vector3(8.7f, 1.08f, zFar - 7.1f), new Vector3(2.4f, 0.18f, 6.2f), new Vector3(-20f, 0f, 0f));
            CreateStationPipe("B_Pipe_East", new Vector3(10.9f, 3.25f, zMid), new Vector3(0.2f, 8f, 0.2f), new Vector3(90f, 0f, 0f));
            CreateConsoleCluster("B_GravityConsole", new Vector3(-9.8f, 0.55f, zNear + 5f), Quaternion.Euler(0f, 90f, 0f));

            // Пресс-плита перед дверью
            var plate = CreatePressurePlate("Plate_B",
                new Vector3(0, 0.05f, zFar - 1.5f), activationMass: 25f);

            // Дверь B → C — открывается плитой и остаётся открытой
            var door = CreateAutoDoor("Door_B_C", WallB_C);
            UnityEventTools.AddPersistentListener(GetPlateOnActivated(plate), (UnityAction)door.Open);

            // Враг — Ползун. Стартует в дальнем углу, патрулирует, нападает
            // на игрока, когда тот вызывает вибрацию (шаги, удары пены/гравитации).
            CreateEnemy("Enemy_B_Crawler", _crawlerConfig,
                new Vector3(6.5f, 0.05f, zMid),
                visionGateUntilHintDismissed: true);
        }

        private static void BuildRoomC_Foam()
        {
            float zNear = WallB_C;
            float zFar  = WallC_Final;
            float zMid  = (zNear + zFar) * 0.5f;
            float deck2 = 2.65f;
            float deck3 = 5.15f;
            float deck2Top = deck2 + 0.1f;
            float deck3Top = deck3 + 0.1f;

            CreatePlatform("C_Floor2_MainDeck", new Vector3(1.5f, deck2, zMid), new Vector3(16.5f, 0.2f, 15.5f));
            CreatePlatform("C_Floor2_WestLanding", new Vector3(-8.8f, deck2, zFar - 2.8f), new Vector3(3.6f, 0.2f, 5.2f));
            CreatePlatform("C_Floor2_WestService", new Vector3(-11.0f, deck2, zNear + 1.6f), new Vector3(1.2f, 0.2f, 3.0f));
            CreatePlatform("C_Floor3_WestDeck", new Vector3(-6.4f, deck3, zMid), new Vector3(7.2f, 0.2f, 15.5f));
            CreatePlatform("C_Floor3_EastLowerDeck", new Vector3(6.4f, deck3, zNear + 3.4f), new Vector3(7.2f, 0.2f, 4.8f));
            CreatePlatform("C_Floor3_EastLanding", new Vector3(8.8f, deck3, zFar - 2.6f), new Vector3(3.8f, 0.2f, 4.8f));
            CreateRamp("C_Ramp_1_to_2", new Vector3(-8.8f, 1.35f, zNear + 6.3f), new Vector3(3.0f, 0.18f, 12.5f), new Vector3(-12f, 0f, 0f));
            CreateRamp("C_Ramp_2_to_3", new Vector3( 8.8f, 3.9f, zFar - 6.3f), new Vector3(3.0f, 0.18f, 12.5f), new Vector3(12f, 0f, 0f));

            CreateRail("C_Floor2_WestRail", new Vector3(-10.4f, deck2Top + 0.45f, zMid), new Vector3(0.12f, 0.9f, 15f));
            CreateRail("C_Floor2_EastRail", new Vector3( 10.4f, deck2Top + 0.45f, zMid), new Vector3(0.12f, 0.9f, 15f));
            CreateRail("C_Floor3_WestRail", new Vector3(-10.4f, deck3Top + 0.45f, zMid), new Vector3(0.12f, 0.9f, 15f));
            CreateRail("C_Floor3_EastRail", new Vector3( 10.4f, deck3Top + 0.45f, zMid), new Vector3(0.12f, 0.9f, 15f));
            CreateRail("C_Floor2_RampWellRail_N", new Vector3(-6.9f, deck2Top + 0.45f, zNear + 6.8f), new Vector3(0.12f, 0.9f, 6.2f));
            CreateRail("C_Floor2_RampWellRail_S", new Vector3(-10.6f, deck2Top + 0.45f, zNear + 6.8f), new Vector3(0.12f, 0.9f, 6.2f));
            CreateRail("C_Floor3_RampWellRail", new Vector3(6.7f, deck3Top + 0.45f, zFar - 5.9f), new Vector3(0.12f, 0.9f, 5.8f));

            CreateGasCanister("Gas_C1", new Vector3(-7.8f, 0.6f, zNear + 4.2f));
            CreateGasCanister("Gas_C2", new Vector3( 6.6f, 0.6f, zNear + 6.6f));
            CreateShelf("Shelf_C1", new Vector3(-2.5f, 0.7f, zMid - 2.8f));
            CreateShelf("Shelf_C2", new Vector3( 4.5f, 0.7f, zMid + 1.6f));
            CreateMovableCrate("C_LowerCrate_A", new Vector3(-6f, 0.5f, zFar - 5.4f));
            CreateConsoleCluster("C_FoamStation", new Vector3(-10.2f, 0.65f, zNear + 3.5f), Quaternion.Euler(0f, 90f, 0f));

            CreateShelf("Shelf_C2_A", new Vector3(-6.4f, deck2Top + 0.7f, zMid - 3.4f));
            CreateShelf("Shelf_C2_B", new Vector3( 3.8f, deck2Top + 0.7f, zMid + 2.6f));
            CreateGravityCube("C_Floor2_GravBox_A", new Vector3(-2.4f, deck2Top + 0.55f, zMid - 1.4f));
            CreateGravityCube("C_Floor2_GravBox_B", new Vector3( 1.8f, deck2Top + 0.55f, zMid + 1.4f));
            var midPlate = CreatePressurePlate("Plate_C_MidDeck",
                new Vector3(5.6f, deck2Top + 0.05f, zNear + 4.8f), activationMass: 25f);
            var stairGate = CreateBulkheadGate("Gate_C_UpperRamp",
                new Vector3(8.8f, deck2Top + 0.9f, zFar - 8.1f),
                new Vector3(3.2f, 1.8f, 0.3f), Quaternion.identity);
            UnityEventTools.AddPersistentListener(GetPlateOnActivated(midPlate), (UnityAction)stairGate.Open);
            CreateConsoleCluster("C_GravityStation", new Vector3(9.8f, deck2Top + 0.65f, zNear + 5.2f), Quaternion.Euler(0f, -90f, 0f));

            CreatePlatform("C_Floor3_BridgeLeft",  new Vector3(-5.4f, deck3Top + 0.05f, zMid + 3.0f), new Vector3(6.4f, 0.16f, 1.5f));
            CreatePlatform("C_Floor3_BridgeRight", new Vector3( 5.4f, deck3Top + 0.05f, zMid + 3.0f), new Vector3(6.4f, 0.16f, 1.5f));
            CreateMovableBeamX("C_WeldBridgeBeam", new Vector3(4.8f, deck3Top + 0.45f, zMid - 2.8f));
            CreateStaticPillar("C_WeldAnchor_L", new Vector3(-8.6f, deck3Top + 1.5f, zMid + 3.0f));
            CreateStaticPillar("C_WeldAnchor_R", new Vector3( 8.6f, deck3Top + 1.5f, zMid + 3.0f));
            CreateShelf("Shelf_C3_A", new Vector3(-1.8f, deck3Top + 0.7f, zMid - 4.0f));
            CreateGasCanister("Gas_C3", new Vector3(6.0f, deck3Top + 0.6f, zMid - 2.5f));
            CreateConsoleCluster("C_WeldStation", new Vector3(-9.7f, deck3Top + 0.65f, zMid - 4.8f), Quaternion.Euler(0f, 90f, 0f));
            CreateStationPipe("C_VerticalPipe_A", new Vector3(-11.0f, 3.8f, zMid), new Vector3(0.18f, 3.0f, 0.18f), Vector3.zero);
            CreateStationPipe("C_VerticalPipe_B", new Vector3( 11.0f, 3.8f, zMid + 2.8f), new Vector3(0.16f, 3.0f, 0.16f), Vector3.zero);

            CreateEnemy("Enemy_C_Floor1_Stalker", _bigEnemyConfig,
                new Vector3(0f, 0.05f, zMid - 1.5f));
            CreateEnemy("Enemy_C_Floor2_Crawler", _crawlerConfig,
                new Vector3(-5.0f, deck2Top + 0.05f, zMid + 2.5f));
            CreateEnemy("Enemy_C_Floor3_Crawler", _crawlerConfig,
                new Vector3(3.5f, deck3Top + 0.05f, zMid - 3.5f));

            var doorTrigger = CreateButtonPanel("BtnPanel_OpenC_Final",
                new Vector3(-8.4f, deck3Top + 0.9f, zFar - 2.6f), Quaternion.Euler(0, 90f, 0));

            var door = CreateAutoDoor("Door_C_Final", WallC_Final);
            var trig = doorTrigger.GetComponent<RoomTrigger>();
            UnityEventTools.AddPersistentListener(GetOnPlayerEntered(trig), (UnityAction)door.Open);
        }

        // ─── ФИНАЛЬНАЯ КОМНАТА ────────────────────────────────────────────────
        private static void BuildFinalRoom()
        {
            float zNear = WallC_Final;
            float zFar  = FinalEnd;
            float zMid  = (zNear + zFar) * 0.5f;

            CreatePlatform("Final_CommandDeck", new Vector3(0f, 0.15f, zMid + 1.2f), new Vector3(9.5f, 0.22f, 5.5f));
            CreateConsoleCluster("Final_LeftConsole", new Vector3(-5.5f, 0.65f, zMid), Quaternion.Euler(0f, 45f, 0f));
            CreateConsoleCluster("Final_RightConsole", new Vector3(5.5f, 0.65f, zMid), Quaternion.Euler(0f, -45f, 0f));
            CreateStationPipe("Final_CeilingPipe_L", new Vector3(-8.5f, 3.4f, zMid), new Vector3(0.18f, 5.8f, 0.18f), new Vector3(90f, 0f, 0f));
            CreateStationPipe("Final_CeilingPipe_R", new Vector3( 8.5f, 3.4f, zMid), new Vector3(0.18f, 5.8f, 0.18f), new Vector3(90f, 0f, 0f));
            CreatePointLight("Lamp_Final_ControlCore", new Vector3(0, 3.7f, zMid + 1.2f), new Color(0.9f, 0.65f, 0.35f), 9f, 1.6f);

            // Главный пульт — цель уровня (срабатывает Reach)
            CreateControlPanel("ControlPanel_Main", new Vector3(0, 1f, zMid + 1.5f));
        }

        private static void CreateMovableCrate(string name, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name  = name;
            go.tag   = "Interactable";
            go.layer = GameConstants.LayerInteractable;
            go.transform.position = pos;
            var rb = go.AddComponent<Rigidbody>(); rb.mass = 5f;
            go.AddComponent<WeldableObject>();
            Paint(go, CrateColor, metallic: 0.55f, smoothness: 0.35f);
        }

        private static void CreateStaticPillar(string name, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name  = name;
            go.tag   = "Interactable";
            go.layer = GameConstants.LayerStatic;
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
            var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true;
            go.AddComponent<WeldableObject>();
            Paint(go, PillarColor, metallic: 0.65f, smoothness: 0.30f);
        }

        private static void CreateGravityCube(string name, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name  = name;
            go.tag   = "Interactable";
            go.layer = GameConstants.LayerInteractable;
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * 0.9f;
            var rb = go.AddComponent<Rigidbody>(); rb.mass = 1f;
            go.AddComponent<WeldableObject>();
            go.AddComponent<GravityAffectableObject>();
            Paint(go, GravityColor, metallic: 0.35f, smoothness: 0.55f,
                  emission: GravityColor, emissionIntensity: 0.4f);
        }

        private static void CreateBreachPoint(string name, Vector3 pos)
        {
            var root = new GameObject(name);
            root.tag = "BreachPoint";
            root.transform.position = pos;
            var col = root.AddComponent<SphereCollider>();
            col.radius = 1.5f;
            col.isTrigger = true;
            root.AddComponent<BreachPoint>();

            // Визуальная «трещина» — диск-маркер
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"{name}_Marker";
            marker.transform.SetParent(root.transform, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale    = Vector3.one * 0.6f;
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            Paint(marker, BreachColor, metallic: 0.1f, smoothness: 0.2f,
                  emission: BreachColor, emissionIntensity: 1.8f);
        }

        private static void CreateControlPanel(string name, Vector3 pos)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name;
            visual.transform.position   = pos;
            visual.transform.localScale = new Vector3(2f, 1.5f, 0.4f);
            Paint(visual, PanelBase, metallic: 0.7f, smoothness: 0.40f);

            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = $"{name}_Screen";
            screen.transform.SetParent(visual.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0.1f, -0.55f);
            screen.transform.localScale    = new Vector3(0.85f, 0.55f, 0.05f);
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            Paint(screen, PanelScreen, metallic: 0f, smoothness: 0.9f,
                  emission: PanelScreen, emissionIntensity: 1.6f);

            var trigger = new GameObject($"{name}_Trigger");
            trigger.transform.SetParent(visual.transform, false);
            trigger.transform.localPosition = Vector3.zero;
            trigger.transform.localScale    = new Vector3(2f, 2.5f, 6f);
            var tcol = trigger.AddComponent<BoxCollider>();
            tcol.isTrigger = true;
            trigger.AddComponent<ControlPanel>();
        }

        // ── Доп. строительные блоки уровня ────────────────────────────────────

        /// <summary>Длинный ящик-балка для сварки моста.</summary>
        private static void CreateMovableBeam(string name, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name  = name;
            go.tag   = "Interactable";
            go.layer = GameConstants.LayerInteractable;
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(1.4f, 0.6f, 6f);
            var rb = go.AddComponent<Rigidbody>(); rb.mass = 4f;
            rb.linearDamping = 0.5f;
            go.AddComponent<WeldableObject>();
            Paint(go, CrateColor, metallic: 0.55f, smoothness: 0.35f);
        }

        private static void CreateMovableBeamX(string name, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name  = name;
            go.tag   = "Interactable";
            go.layer = GameConstants.LayerInteractable;
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(6f, 0.6f, 1.4f);
            var rb = go.AddComponent<Rigidbody>(); rb.mass = 4f;
            rb.linearDamping = 0.5f;
            go.AddComponent<WeldableObject>();
            Paint(go, CrateColor, metallic: 0.55f, smoothness: 0.35f);
        }

        /// <summary>Декоративный стеллаж/укрытие.</summary>
        private static void CreateShelf(string name, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name  = name;
            go.tag   = "Surface";
            go.layer = GameConstants.LayerStatic;
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(1.2f, 1.4f, 2.4f);
            Paint(go, PillarColor, metallic: 0.45f, smoothness: 0.2f);
        }

        /// <summary>Газовый баллон для атмосферы и взрывов.</summary>
        private static void CreateGasCanister(string name, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name  = name;
            go.tag   = "Interactable";
            go.layer = GameConstants.LayerInteractable;
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.5f, 0.7f, 0.5f);
            var rb = go.AddComponent<Rigidbody>(); rb.mass = 6f;
            go.AddComponent<WeldableObject>();
            go.AddComponent<GravityAffectableObject>();
            go.AddComponent<GasCanister>();
            Paint(go, BreachColor, metallic: 0.55f, smoothness: 0.45f,
                  emission: BreachColor, emissionIntensity: 0.5f);
        }

        /// <summary>
        /// Метка пустоты: коллайнера/DeathZone **нет** — смерть от провала только в
        /// <see cref="ReactorBreach.Player.PlayerController"/> (см. GameConstants Level1_Chasm*, StationFloorPlaneY).
        /// </summary>
        private static void CreateDeathPit(string name, float zMin, float zMax)
        {
            new GameObject(name);
        }

        /// <summary>Пресс-плита: активируется массой >= activationMass.</summary>
        private static PressurePlate CreatePressurePlate(string name, Vector3 pos, float activationMass)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name;
            visual.transform.position   = pos;
            visual.transform.localScale = new Vector3(2.6f, 0.1f, 2.6f);
            Paint(visual, new Color(0.55f, 0.40f, 0.15f), metallic: 0.6f, smoothness: 0.4f,
                  emission: new Color(1f, 0.5f, 0.15f), emissionIntensity: 0.6f);

            var trig = new GameObject($"{name}_Trigger");
            trig.transform.position = pos + new Vector3(0f, 0.45f, 0f);
            var box = trig.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.6f, 1.0f, 2.6f);
            var plate = trig.AddComponent<PressurePlate>();
            SetField(plate, "_activationMass", activationMass);
            return plate;
        }

        /// <summary>Дверь-перегородка: куб поднимается вверх когда «открыта».</summary>
        private static Door CreateAutoDoor(string name, float zPosition)
        {
            // Дверь — статичный блок в проёме. При открытии «уезжает» вверх через Door (rotate).
            // У нас Door вращает дверь на _openAngle. Сделаем поворот вокруг Y, чтобы
            // створка ушла в стену. Дверь — ребёнок anchorGO для корректного поворота от петли.
            var anchor = new GameObject(name);
            anchor.transform.position = new Vector3(-DoorWidth * 0.5f, 0f, zPosition);

            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = $"{name}_Leaf";
            leaf.tag  = "Surface";
            leaf.layer = GameConstants.LayerStatic;
            leaf.transform.SetParent(anchor.transform, false);
            // Створка ширины DoorWidth и высоты DoorHeight, центр сдвинут так,
            // чтобы петля была в начале координат anchor.
            leaf.transform.localPosition = new Vector3(DoorWidth * 0.5f, DoorHeight * 0.5f, 0f);
            leaf.transform.localScale    = new Vector3(DoorWidth, DoorHeight, WallThick * 0.8f);
            Paint(leaf, new Color(0.30f, 0.15f, 0.05f), metallic: 0.7f, smoothness: 0.45f,
                  emission: new Color(1f, 0.45f, 0.10f), emissionIntensity: 0.4f);

            var door = anchor.AddComponent<Door>();
            SetField(door, "_openAngle", -100f);
            SetField(door, "_openSpeed", 2.5f);
            return door;
        }

        private static Door CreateBulkheadGate(string name, Vector3 pos, Vector3 size, Quaternion rot)
        {
            var anchor = new GameObject(name);
            anchor.transform.position = pos;
            anchor.transform.rotation = rot;

            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = $"{name}_Leaf";
            leaf.tag = "Surface";
            leaf.layer = GameConstants.LayerStatic;
            leaf.transform.SetParent(anchor.transform, false);
            leaf.transform.localPosition = Vector3.zero;
            leaf.transform.localScale = size;
            Paint(leaf, new Color(0.26f, 0.12f, 0.05f), metallic: 0.75f, smoothness: 0.4f,
                  emission: new Color(1f, 0.32f, 0.08f), emissionIntensity: 0.25f);

            var door = anchor.AddComponent<Door>();
            SetField(door, "_openAngle", 105f);
            SetField(door, "_openSpeed", 3f);
            return door;
        }

        /// <summary>Кнопка-пульт: зелёный экран + триггер при входе игрока (подсказки — в UI).</summary>
        private static GameObject CreateButtonPanel(string name, Vector3 pos, Quaternion rot)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name;
            visual.transform.position   = pos;
            visual.transform.rotation   = rot;
            visual.transform.localScale = new Vector3(1.4f, 1.2f, 0.4f);
            Paint(visual, PanelBase, metallic: 0.7f, smoothness: 0.40f);

            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = $"{name}_Screen";
            screen.transform.SetParent(visual.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0.1f, -0.55f);
            screen.transform.localScale    = new Vector3(0.85f, 0.6f, 0.05f);
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            Paint(screen, new Color(0.1f, 0.9f, 0.4f), metallic: 0f, smoothness: 0.9f,
                  emission: new Color(0.2f, 1f, 0.5f), emissionIntensity: 1.6f);

            // Триггер вокруг панели
            var trig = new GameObject($"{name}_Trigger");
            trig.transform.SetParent(visual.transform, false);
            trig.transform.localPosition = Vector3.zero;
            trig.transform.localScale    = new Vector3(2.5f, 2.5f, 6f);
            var box = trig.AddComponent<BoxCollider>();
            box.isTrigger = true;
            trig.AddComponent<RoomTrigger>();
            return trig;
        }

        /// <summary>Враг-Ползун с FSM, NavMeshAgent и капсульной визуализацией.</summary>
        private static void CreateEnemy(string name, EnemyConfig config, Vector3 pos,
            bool visionGateUntilHintDismissed = false)
        {
            var go = new GameObject(name);
            go.tag   = GameConstants.TagEnemy;
            go.layer = GameConstants.LayerEnemy;
            go.transform.position = pos;

            // Визуалка — капсула + «глаз»
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = $"{name}_Body";
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            body.transform.localScale    = new Vector3(0.9f, 0.9f, 0.9f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            Paint(body, new Color(0.18f, 0.05f, 0.05f), metallic: 0.30f, smoothness: 0.55f,
                  emission: new Color(0.6f, 0.05f, 0.05f), emissionIntensity: 0.4f);

            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = $"{name}_Eye";
            eye.transform.SetParent(body.transform, false);
            eye.transform.localPosition = new Vector3(0f, 0.55f, -0.35f);
            eye.transform.localScale    = Vector3.one * 0.35f;
            Object.DestroyImmediate(eye.GetComponent<Collider>());
            Paint(eye, new Color(1f, 0.85f, 0.2f), metallic: 0f, smoothness: 0.95f,
                  emission: new Color(1f, 0.85f, 0.2f), emissionIntensity: 2.2f);

            // Коллайдер для столкновений (capsule)
            var cap = go.AddComponent<CapsuleCollider>();
            cap.height = 2f;
            cap.radius = 0.45f;
            cap.center = new Vector3(0f, 1f, 0f);

            // Rigidbody (kinematic — двигает NavMeshAgent)
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;

            // NavMeshAgent
            var agent = go.AddComponent<NavMeshAgent>();
            agent.height = 2f;
            agent.radius = 0.45f;
            agent.speed  = config != null ? config.MoveSpeed : 3f;
            agent.angularSpeed = 200f;
            agent.acceleration = 12f;
            agent.stoppingDistance = (config != null ? config.AttackRange : 1.5f) * 0.9f;
            agent.autoBraking = true;

            // Скрипты врага
            var enemy = go.AddComponent<EnemyCrawler>();
            SetField(enemy, "Config", config);
            go.AddComponent<EnemyStateMachine>();

            // EnemyStateMachine.Start() ставит Idle. Чтобы враги изначально
            // патрулировали, вешаем мини-стартер.
            go.AddComponent<EnemyAutoPatrol>();

            if (visionGateUntilHintDismissed)
            {
                var gate = go.AddComponent<EnemyVisionGatedByHint>();
                SetStringField(gate, "_requiredDismissContext", "ZoneB");
            }

            var aimEyes = go.AddComponent<EnemyAimEyes>();
            SetField(aimEyes, "_eye", eye.transform);
        }

        /// <summary>Сборка NavMesh из всей геометрии сцены.</summary>
        private static void BuildNavMesh()
        {
            var go = new GameObject("_NavMeshSurface");
            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry    = NavMeshCollectGeometry.PhysicsColliders;
            // Исключаем слои игрока и врагов из учёта препятствий, чтобы агенты
            // не «обходили» сами себя и игрока при первичной запечке.
            int mask = ~0;
            mask &= ~(1 << GameConstants.LayerPlayer);
            mask &= ~(1 << GameConstants.LayerEnemy);
            surface.layerMask = mask;
            surface.BuildNavMesh();
        }

        // ─── UnityEvent helpers (через рефлексию, чтобы достать private events) ───

        private static UnityEvent GetOnPlayerEntered(RoomTrigger trig)
        {
            return GetSerializedUnityEvent(trig, "_onPlayerEntered");
        }

        private static UnityEvent GetPlateOnActivated(PressurePlate plate)
        {
            return GetSerializedUnityEvent(plate, "_onActivated");
        }

        private static UnityEvent GetPlateOnDeactivated(PressurePlate plate)
        {
            return GetSerializedUnityEvent(plate, "_onDeactivated");
        }

        private static UnityEvent GetSerializedUnityEvent(Object owner, string fieldName)
        {
            var field = owner.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null) throw new System.Exception($"[Level1Builder] Field {fieldName} not found on {owner.GetType()}");
            var ev = field.GetValue(owner) as UnityEvent;
            if (ev == null)
            {
                ev = new UnityEvent();
                field.SetValue(owner, ev);
            }
            return ev;
        }

        // ── Build Settings ────────────────────────────────────────────────────

        private static void AddSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ── YAML-патч ссылки _inputActions ────────────────────────────────────
        // Объяснение хака: InputActionAsset импортируется ScriptedImporter'ом
        // (type=3 в YAML). Когда мы создаём такую ссылку через SerializedObject
        // в свежеизготовленной сцене и сразу же сохраняем сцену, Unity иногда
        // не успевает зафиксировать localFileID и пишет {fileID: 0}. Поэтому
        // после SaveScene вытаскиваем guid+localId через AssetDatabase и
        // подменяем строку в YAML напрямую.

        private static void PatchSceneInputActionsReference()
        {
            // ВАЖНО: AssetDatabase.Refresh(), который вызывается между BuildScene
            // и патчем, инвалидирует InputActionAsset из переменной — поэтому
            // загружаем его заново по пути.
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
            {
                Debug.LogWarning($"[Level1Builder] PatchScene: не удалось загрузить {InputActionsPath}.");
                return;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actions, out string guid, out long localId)
                || string.IsNullOrEmpty(guid))
            {
                // Фоллбэк: GUID берём из .meta-файла напрямую
                guid = AssetDatabase.AssetPathToGUID(InputActionsPath);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning("[Level1Builder] PatchScene: не удалось определить GUID InputActionAsset.");
                    return;
                }
                localId = 0;
            }

            if (localId == 0)
            {
                Debug.LogWarning("[Level1Builder] PatchScene: localFileID == 0. " +
                                 "Делаю Reimport ассета и пробую снова.");
                AssetDatabase.ImportAsset(InputActionsPath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actions, out guid, out localId);
                if (localId == 0)
                {
                    Debug.LogError("[Level1Builder] PatchScene: localFileID всё ещё 0 — не могу пропатчить сцену.");
                    return;
                }
            }

            string scenePath = Path.GetFullPath(ScenePath);
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"[Level1Builder] PatchScene: scene not found: {scenePath}");
                return;
            }

            string text = File.ReadAllText(scenePath);
            string oldRef = "_inputActions: {fileID: 0}";
            string newRef = $"_inputActions: {{fileID: {localId}, guid: {guid}, type: 3}}";

            if (!text.Contains(oldRef))
            {
                Debug.Log("[Level1Builder] PatchScene: ссылка уже не пустая или поле не найдено — патч не нужен.");
                return;
            }

            text = text.Replace(oldRef, newRef);
            File.WriteAllText(scenePath, text);

            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Перезагружаем сцену в Editor, чтобы инспектор показал обновлённую ссылку
            var current = EditorSceneManager.GetActiveScene();
            if (current.path == ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Debug.Log($"[Level1Builder] PatchScene: ссылка _inputActions проставлена " +
                      $"(fileID={localId}, guid={guid}).");
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private static GameObject MakeTMPText(Transform parent, string name, Vector2 anchored, float fontSize,
                                              TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(800, 80);
            rt.anchoredPosition = anchored;
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.fontSize = fontSize;
            txt.alignment = align;
            txt.color = color;
            txt.text = string.Empty;
            return go;
        }

        private static GameObject MakeSlider(Transform parent, string name, Vector2 anchored, Vector2 size, Color fill)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;

            var slider = go.AddComponent<Slider>();

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRT = (RectTransform)bg.transform;
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRT = (RectTransform)fillArea.transform;
            faRT.anchorMin = new Vector2(0, 0.25f); faRT.anchorMax = new Vector2(1, 0.75f);
            faRT.offsetMin = new Vector2(4, 0);    faRT.offsetMax = new Vector2(-4, 0);

            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(fillArea.transform, false);
            var fillRT = (RectTransform)fillGO.transform;
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            var fillImg = fillGO.GetComponent<Image>();
            fillImg.color = fill;

            slider.fillRect       = fillRT;
            slider.targetGraphic  = bg.GetComponent<Image>();
            slider.direction      = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;

            return go;
        }

        private static ToolIndicator.SlotUI MakeIndicatorSlot(Transform parent, string label, int index)
        {
            var slotGO = new GameObject($"Slot_{index}_{label}", typeof(RectTransform), typeof(Image));
            slotGO.transform.SetParent(parent, false);
            var slotRT = (RectTransform)slotGO.transform;
            slotRT.sizeDelta = new Vector2(220, 100);
            slotRT.anchoredPosition = new Vector2((index - 1) * 240, 0);
            var icon = slotGO.GetComponent<Image>();
            icon.color = new Color(0.15f, 0.15f, 0.18f, 0.85f);

            var activeFrame = new GameObject("ActiveFrame", typeof(RectTransform), typeof(Image));
            activeFrame.transform.SetParent(slotGO.transform, false);
            var afRT = (RectTransform)activeFrame.transform;
            afRT.anchorMin = Vector2.zero; afRT.anchorMax = Vector2.one;
            afRT.offsetMin = new Vector2(-3, -3); afRT.offsetMax = new Vector2(3, 3);
            var afImg = activeFrame.GetComponent<Image>();
            afImg.color = new Color(1f, 0.7f, 0.1f, 0.6f);
            activeFrame.SetActive(false);

            var slider = MakeSlider(slotGO.transform, "Slider",
                new Vector2(0, -38), new Vector2(200, 8), Color.cyan);

            var labelGO = MakeTMPText(slotGO.transform, "Label",
                new Vector2(0, 28), 22, TextAlignmentOptions.Center, Color.white);
            var labelText = labelGO.GetComponent<TextMeshProUGUI>();
            labelText.text = label;

            return new ToolIndicator.SlotUI
            {
                IconImage      = icon,
                ResourceSlider = slider.GetComponent<Slider>(),
                Label          = labelText,
                ActiveFrame    = activeFrame,
            };
        }

        private static void SetSlotsArray(ToolIndicator indicator, List<ToolIndicator.SlotUI> data)
        {
            var so = new SerializedObject(indicator);
            var arr = so.FindProperty("_slots");
            arr.arraySize = data.Count;
            for (int i = 0; i < data.Count; i++)
            {
                var elem = arr.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("IconImage").objectReferenceValue      = data[i].IconImage;
                elem.FindPropertyRelative("ResourceSlider").objectReferenceValue = data[i].ResourceSlider;
                elem.FindPropertyRelative("Label").objectReferenceValue          = data[i].Label;
                elem.FindPropertyRelative("ActiveFrame").objectReferenceValue    = data[i].ActiveFrame;
            }
            so.ApplyModifiedProperties();
        }

        private static Shader _litShader;
        private static Shader GetLitShader()
        {
            if (_litShader != null) return _litShader;

            // Magenta-фикс: если активный SRP не назначен, URP/Lit рисуется как
            // missing shader. Берём Standard в этом случае.
            bool urpActive = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null
                          || UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;

            if (urpActive)
                _litShader = Shader.Find("Universal Render Pipeline/Lit");

            if (_litShader == null)
                _litShader = Shader.Find("Standard");

            if (_litShader == null)
                _litShader = Shader.Find("Diffuse"); // последний фоллбэк

            return _litShader;
        }

        private static void Paint(GameObject go, Color color, float metallic = 0f, float smoothness = 0.5f,
                                  Color? emission = null, float emissionIntensity = 1f)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;

            var mat = new Material(GetLitShader());
            // URP/Lit использует _BaseColor; у Standard — _Color. Поставим оба.
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     color);
            mat.color = color;
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

            if (emission.HasValue)
            {
                Color e = emission.Value * emissionIntensity;
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", e.linear);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            rend.sharedMaterial = mat;
        }

        // ── SerializedObject helpers ──────────────────────────────────────────

        private static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Level1Builder] field {field} not found on {target.GetType().Name}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetField(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Level1Builder] field {field} not found on {target.GetType().Name}"); return; }
            p.floatValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetBoolField(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Level1Builder] field {field} not found on {target.GetType().Name}"); return; }
            p.boolValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetStringField(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Level1Builder] string field {field} not found on {target.GetType().Name}"); return; }
            p.stringValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetLayerMask(Object target, string field, int mask)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) return;
            p.intValue = mask;
            so.ApplyModifiedProperties();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var arr = so.FindProperty(field);
            if (arr == null) return;
            arr.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedProperties();
        }
    }

}
#endif
