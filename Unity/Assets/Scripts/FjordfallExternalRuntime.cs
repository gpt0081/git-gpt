using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fjordfall
{
    internal sealed class FjordfallExternalMarker : MonoBehaviour { }

    /// <summary>
    /// Replaces the prototype's visible runtime primitives with standalone OBJ models and
    /// textured materials. Gameplay remains owned by FjordfallGame, so an asset import failure
    /// only falls back to the original primitives instead of breaking the battle.
    /// </summary>
    public sealed class FjordfallExternalRuntime : MonoBehaviour
    {
        private GameObject housePrefab, treePrefab, boatPrefab, soldierPrefab, wallPrefab, rockPrefab, shieldPrefab;
        private Material grass, cliff, sea, plaster, roof, foliage, wood, skin, iron, raider, sail;
        private readonly Dictionary<string, Material> uniforms = new Dictionary<string, Material>();
        private float scanClock;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<FjordfallExternalRuntime>() != null) return;
            new GameObject("Fjordfall External Asset Runtime").AddComponent<FjordfallExternalRuntime>();
        }

        private IEnumerator Start()
        {
            yield return null;
            LoadAssets();
            BuildMaterials();
            SkinEnvironment();
            SkinDynamicObjects();
            AddShoreRocks();
        }

        private void Update()
        {
            scanClock -= Time.unscaledDeltaTime;
            if (scanClock > 0f) return;
            scanClock = .5f;
            SkinDynamicObjects();
        }

        private void LoadAssets()
        {
            housePrefab = Resources.Load<GameObject>("External/Models/fjord_house");
            treePrefab = Resources.Load<GameObject>("External/Models/fjord_tree");
            boatPrefab = Resources.Load<GameObject>("External/Models/fjord_longboat");
            soldierPrefab = Resources.Load<GameObject>("External/Models/fjord_soldier");
            wallPrefab = Resources.Load<GameObject>("External/Models/fjord_wall");
            rockPrefab = Resources.Load<GameObject>("External/Models/fjord_rock");
            shieldPrefab = Resources.Load<GameObject>("External/Models/fjord_shield");
        }

        private void BuildMaterials()
        {
            grass = Mat("Fjordfall Grass", new Color(.67f, .75f, .71f), "grass_moss", 4f, "grass_moss_normal");
            cliff = Mat("Fjordfall Cliff", new Color(.88f, .90f, .84f), "cliff_stone", 2.4f, "cliff_stone_normal");
            sea = Mat("Fjordfall Water", new Color(.72f, .83f, .80f, .94f), "fjord_water", 7f, "fjord_water_normal");
            plaster = Mat("Fjordfall Plaster", new Color(.96f, .95f, .89f), "village_plaster", 1.5f, "village_plaster_normal");
            roof = Mat("Fjordfall Roof", new Color(.48f, .55f, .55f), "roof_shingle", 2f, "roof_shingle_normal");
            foliage = Mat("Fjordfall Foliage", new Color(.22f, .20f, .25f), "dark_foliage", 1.4f);
            wood = Mat("Fjordfall Wood", new Color(.39f, .27f, .24f), "dark_bark", 2f, "dark_bark_normal");
            skin = Mat("Fjordfall Skin", new Color(.93f, .91f, .84f), "village_plaster", 1f);
            iron = Mat("Fjordfall Iron", new Color(.68f, .72f, .70f), "iron_worn", 1.4f);
            raider = Mat("Fjordfall Raider", new Color(.52f, .24f, .29f), "roof_shingle", 1f);
            sail = Mat("Fjordfall Sail", new Color(.80f, .72f, .67f), "linen_sail", 1f);
            ConfigureTransparent(sea);
        }

        private Material Mat(string name, Color color, string textureName, float tiling, string normalName = null)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse");
            Material material = new Material(shader) { name = name, color = color };
            Texture2D albedo = Resources.Load<Texture2D>("External/Textures/" + textureName);
            if (albedo != null)
            {
                material.mainTexture = albedo;
                material.mainTextureScale = Vector2.one * tiling;
            }
            if (!string.IsNullOrEmpty(normalName) && material.HasProperty("_BumpMap"))
            {
                Texture2D normal = Resources.Load<Texture2D>("External/Textures/" + normalName);
                if (normal != null)
                {
                    material.SetTexture("_BumpMap", normal);
                    material.SetFloat("_BumpScale", .45f);
                    material.EnableKeyword("_NORMALMAP");
                }
            }
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .12f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .12f);
            return material;
        }

        private static void ConfigureTransparent(Material material)
        {
            if (material == null) return;
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
        }

        private void SkinEnvironment()
        {
            SetMaterial("Island Top", grass);
            SetMaterial("Island Cliff", cliff);
            SetMaterial("Water", sea);

            foreach (Transform t in SceneTransforms("House")) SkinHouse(t);
            foreach (Transform t in SceneTransforms("Tree")) SkinTree(t);
            foreach (Transform t in SceneTransforms("Stone Wall")) SkinWall(t);
        }

        private void SkinDynamicObjects()
        {
            foreach (Transform t in SceneTransforms("Soldier")) SkinSoldier(t, false);
            foreach (Transform t in SceneTransforms("Raider")) SkinSoldier(t, true);
            foreach (Transform t in SceneTransforms("Raider Longboat")) SkinBoat(t);
        }

        private static IEnumerable<Transform> SceneTransforms(string exactName)
        {
            return Object.FindObjectsOfType<Transform>()
                .Where(t => t != null && t.gameObject.scene.IsValid() && t.name == exactName);
        }

        private static void SetMaterial(string objectName, Material material)
        {
            if (material == null) return;
            foreach (Transform t in SceneTransforms(objectName))
            {
                Renderer renderer = t.GetComponent<Renderer>();
                if (renderer != null) renderer.material = material;
            }
        }

        private void SkinHouse(Transform root)
        {
            if (root.GetComponent<FjordfallExternalMarker>() != null || housePrefab == null) return;
            root.gameObject.AddComponent<FjordfallExternalMarker>();
            Transform walls = root.Find("Walls");
            float size = walls != null ? Mathf.Max(.5f, walls.localScale.x / 1.35f) : 1f;
            DisableRenderers(root);
            GameObject model = Spawn(housePrefab, root, Vector3.zero, Quaternion.identity, Vector3.one * size, "Imported House");
            ApplyMaterials(model, ("plaster", plaster), ("roof", roof), ("wood", wood));
        }

        private void SkinTree(Transform root)
        {
            if (root.GetComponent<FjordfallExternalMarker>() != null || treePrefab == null) return;
            root.gameObject.AddComponent<FjordfallExternalMarker>();
            Transform trunk = root.Find("Trunk");
            float size = trunk != null ? Mathf.Max(.45f, trunk.localScale.y / .35f) : .75f;
            DisableRenderers(root);
            GameObject model = Spawn(treePrefab, root, Vector3.zero, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * size, "Imported Fir Tree");
            ApplyMaterials(model, ("bark", wood), ("foliage", foliage));
        }

        private void SkinWall(Transform original)
        {
            if (original.GetComponent<FjordfallExternalMarker>() != null || wallPrefab == null) return;
            original.gameObject.AddComponent<FjordfallExternalMarker>();
            Renderer renderer = original.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
            Vector3 scale = original.localScale;
            GameObject model = Spawn(wallPrefab, original.parent, original.localPosition, original.localRotation,
                new Vector3(scale.x / 2.4f, scale.y / 1.3f, scale.z / .5f), "Imported Stone Wall");
            ApplyMaterials(model, ("stone", cliff));
        }

        private void SkinSoldier(Transform root, bool enemy)
        {
            if (root.GetComponent<FjordfallExternalMarker>() != null || soldierPrefab == null) return;
            root.gameObject.AddComponent<FjordfallExternalMarker>();
            Renderer bodyRenderer = FindChildRenderer(root, "Body");
            Color uniformColor = enemy || bodyRenderer == null ? raider.color : bodyRenderer.material.color;
            Material uniform = Uniform(uniformColor, enemy);
            HideNamed(root, "Body", "Head", "Helmet");
            GameObject model = Spawn(soldierPrefab, root, Vector3.zero, Quaternion.identity, Vector3.one * .78f, enemy ? "Imported Raider Body" : "Imported Soldier Body");
            ApplyMaterials(model, ("cloth", uniform), ("skin", skin), ("metal", iron));

            Transform oldShield = root.Find("Shield");
            if (oldShield != null && shieldPrefab != null)
            {
                DisableRenderers(oldShield);
                GameObject shield = Spawn(shieldPrefab, root, new Vector3(-.31f, .57f, -.06f), Quaternion.Euler(90f, 0f, 0f), new Vector3(.88f, .18f, .88f), "Imported Shield");
                ApplyMaterials(shield, ("metal", uniform));
            }
        }

        private void SkinBoat(Transform root)
        {
            if (root.GetComponent<FjordfallExternalMarker>() != null || boatPrefab == null) return;
            root.gameObject.AddComponent<FjordfallExternalMarker>();
            DisableRenderers(root);
            GameObject model = Spawn(boatPrefab, root, Vector3.zero, Quaternion.identity, Vector3.one * .78f, "Imported Raider Longboat");
            ApplyMaterials(model, ("wood", wood), ("sail", raider), ("cloth", sail));
        }

        private void AddShoreRocks()
        {
            if (rockPrefab == null) return;
            Transform world = GameObject.Find("Fjordfall World")?.transform;
            if (world == null || world.Find("External Shore Props") != null) return;
            Transform props = new GameObject("External Shore Props").transform;
            props.SetParent(world, false);
            Vector3[] points =
            {
                new Vector3(-8.4f, 0f, -1.2f), new Vector3(8.1f, 0f, .4f),
                new Vector3(-1.2f, 0f, 5.8f), new Vector3(6.7f, 0f, -4.8f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                GameObject rock = Spawn(rockPrefab, props, points[i], Quaternion.Euler(0f, i * 53f, 0f), Vector3.one * (.52f + i * .08f), "Imported Shore Rock");
                ApplyMaterials(rock, ("stone", cliff));
            }
        }

        private Material Uniform(Color color, bool enemy)
        {
            string key = enemy ? "enemy" : ColorUtility.ToHtmlStringRGB(color);
            if (uniforms.TryGetValue(key, out Material cached)) return cached;
            Material material = Mat("Uniform " + key, color, enemy ? "roof_shingle" : "linen_sail", 1f);
            uniforms[key] = material;
            return material;
        }

        private static Renderer FindChildRenderer(Transform root, string childName)
        {
            Transform child = root.Find(childName);
            return child != null ? child.GetComponent<Renderer>() : null;
        }

        private static void HideNamed(Transform root, params string[] names)
        {
            foreach (string name in names)
            {
                Transform child = root.Find(name);
                if (child != null) DisableRenderers(child);
            }
        }

        private static void DisableRenderers(Transform root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        }

        private static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, string name)
        {
            GameObject instance = Instantiate(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = scale;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>()) Destroy(collider);
            return instance;
        }

        private static void ApplyMaterials(GameObject model, params (string key, Material material)[] replacements)
        {
            if (model == null) return;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string source = materials[i] != null ? materials[i].name.ToLowerInvariant() : string.Empty;
                    foreach (var replacement in replacements)
                    {
                        if (!source.Contains(replacement.key.ToLowerInvariant())) continue;
                        materials[i] = replacement.material;
                        break;
                    }
                }
                renderer.materials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }
    }
}
