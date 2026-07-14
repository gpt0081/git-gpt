using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fjordfall
{
    public sealed class FjordfallGame : MonoBehaviour
    {
        private enum Phase { Deploy, Battle, Pause, Win, Lose }
        private enum Kind { Sword, Spear, Bow }

        private sealed class Man
        {
            public Transform T;
            public Vector3 Slot;
            public float Hp = 50f;
            public bool Alive => T != null && T.gameObject.activeSelf && Hp > 0f;
        }

        private sealed class Squad
        {
            public string Name;
            public Kind Kind;
            public Color Color;
            public Transform Root;
            public Transform Flag;
            public readonly List<Man> Men = new List<Man>();
            public Vector3 Pos;
            public Vector3 Goal;
            public float Speed;
            public float Range;
            public float Damage;
            public float Cooldown;
            public float AttackClock;
            public float SkillClock;
            public bool Alive => Men.Any(m => m.Alive);
        }

        private sealed class Raider
        {
            public Man Man;
            public float AttackClock;
        }

        private sealed class Boat
        {
            public Transform T;
            public Vector3 Goal;
            public int Cargo;
            public bool Landed;
        }

        private readonly List<Squad> squads = new List<Squad>();
        private readonly List<Raider> raiders = new List<Raider>();
        private readonly List<Boat> boats = new List<Boat>();
        private readonly List<Button> squadButtons = new List<Button>();

        private Transform world;
        private Transform water;
        private Camera cam;
        private MeshCollider islandCollider;
        private LineRenderer selection;
        private Squad selected;
        private Phase phase;
        private Phase beforePause;
        private float battleTime;
        private float nextBoat;
        private int boatCount;
        private const int PlannedBoats = 4;

        private Material grass, cliff, sea, plaster, roof, tree, wood, white, raiderMat;
        private Font font;
        private GameObject deployPanel, resultPanel;
        private Text status, timer, selectedLabel, resultTitle, resultBody;
        private Button pauseButton, skillButton;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = false;
            BuildWorld();
            BuildUi();
            EnterDeploy();
        }

        private void Update()
        {
            AnimateEnvironment();
            DrawSelection();
            if (phase == Phase.Pause || phase == Phase.Win || phase == Phase.Lose) return;
            ReadPointer();
            TickSquads(Time.deltaTime);
            if (phase != Phase.Battle) return;
            battleTime += Time.deltaTime;
            SpawnSchedule();
            TickBoats(Time.deltaTime);
            TickRaiders(Time.deltaTime);
            CheckResult();
            timer.text = $"{Mathf.FloorToInt(battleTime) / 60:00}:{Mathf.FloorToInt(battleTime) % 60:00}";
        }

        private void BuildWorld()
        {
            world = new GameObject("Fjordfall World").transform;
            world.SetParent(transform, false);
            MakeMaterials();
            MakeCamera();
            MakeWater();
            MakeIsland();
            MakeVillage();
            MakeRain();
            MakeSquads();
            MakeSelection();
        }

        private void MakeMaterials()
        {
            grass = Mat("Grass", new Color(.38f, .47f, .47f));
            cliff = Mat("Cliff", new Color(.78f, .82f, .75f));
            sea = Mat("Sea", new Color(.62f, .72f, .70f, .94f));
            plaster = Mat("Plaster", new Color(.88f, .89f, .83f));
            roof = Mat("Roof", new Color(.27f, .33f, .34f));
            tree = Mat("Trees", new Color(.19f, .18f, .23f));
            wood = Mat("Wood", new Color(.26f, .18f, .17f));
            white = Mat("White", new Color(.92f, .94f, .89f));
            raiderMat = Mat("Raiders", new Color(.43f, .22f, .27f));
            if (sea.HasProperty("_Mode")) sea.SetFloat("_Mode", 3f);
            if (sea.HasProperty("_SrcBlend")) sea.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (sea.HasProperty("_DstBlend")) sea.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (sea.HasProperty("_ZWrite")) sea.SetInt("_ZWrite", 0);
            sea.EnableKeyword("_ALPHABLEND_ON");
            sea.renderQueue = 3000;
        }

        private Material Mat(string name, Color color)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse");
            Material m = new Material(shader) { name = name, color = color };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .18f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .18f);
            return m;
        }

        private void MakeCamera()
        {
            GameObject c = new GameObject("Main Camera");
            c.tag = "MainCamera";
            c.transform.SetParent(world, false);
            cam = c.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 12.2f;
            cam.backgroundColor = new Color(.72f, .77f, .73f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            c.transform.position = new Vector3(15.5f, 18f, -15.5f);
            c.transform.LookAt(Vector3.zero);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.62f, .66f, .65f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = cam.backgroundColor;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 55f;

            GameObject s = new GameObject("Sun");
            s.transform.SetParent(world, false);
            Light light = s.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(.88f, .90f, .86f);
            light.shadows = LightShadows.Soft;
            s.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private void MakeWater()
        {
            GameObject o = Primitive(PrimitiveType.Plane, "Water", world, new Vector3(7f, 1f, 7f), new Vector3(0f, -1.45f, 0f), sea);
            water = o.transform;
        }

        private void MakeIsland()
        {
            GameObject baseIsland = Primitive(PrimitiveType.Cylinder, "Island Cliff", world, new Vector3(10.9f, .7f, 7.6f), new Vector3(0f, -.68f, 0f), cliff);
            baseIsland.transform.localScale = new Vector3(10.9f, .7f, 7.6f);
            GameObject top = Primitive(PrimitiveType.Cylinder, "Island Top", world, new Vector3(10.35f, .16f, 7.05f), new Vector3(0f, -.02f, 0f), grass, false);
            top.transform.localScale = new Vector3(10.35f, .16f, 7.05f);
            islandCollider = top.AddComponent<MeshCollider>();
            islandCollider.sharedMesh = top.GetComponent<MeshFilter>().sharedMesh;
            Wall(new Vector3(-1f, .58f, .8f), new Vector3(14f, 1.1f, .38f), 6f);
            Wall(new Vector3(-5.8f, .62f, 4.25f), new Vector3(7.2f, 1.2f, .45f), -20f);
        }

        private void Wall(Vector3 p, Vector3 scale, float yaw)
        {
            GameObject o = Primitive(PrimitiveType.Cube, "Stone Wall", world, scale, p, tree);
            o.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void MakeVillage()
        {
            Vector3[] homes =
            {
                new Vector3(-5.7f,0f,-2.2f), new Vector3(-2.5f,0f,-1f), new Vector3(1f,0f,-2.3f),
                new Vector3(4.6f,0f,-1f), new Vector3(5.2f,0f,2.7f), new Vector3(1.7f,0f,3.4f),
                new Vector3(-3f,0f,3f), new Vector3(-6.5f,0f,2f)
            };
            for (int i = 0; i < homes.Length; i++) House(homes[i], .78f + (i % 3) * .08f, i * 17f);

            Vector3[] groves =
            {
                new Vector3(-7.4f,0f,4.8f), new Vector3(-3.8f,0f,5.4f), new Vector3(.5f,0f,5.6f),
                new Vector3(7f,0f,4.6f), new Vector3(-7.7f,0f,-4.4f), new Vector3(7.5f,0f,-3.8f)
            };
            for (int g = 0; g < groves.Length; g++)
                for (int i = 0; i < 6; i++) Tree(groves[g] + new Vector3(Mathf.Cos(i * 1.7f) * (.25f + i % 3 * .28f), 0f, Mathf.Sin(i * 1.7f) * (.25f + i % 3 * .28f)), .65f + i % 3 * .08f);
        }

        private void House(Vector3 p, float size, float yaw)
        {
            Transform r = new GameObject("House").transform;
            r.SetParent(world, false);
            r.position = p;
            r.rotation = Quaternion.Euler(0f, yaw, 0f);
            Primitive(PrimitiveType.Cube, "Walls", r, new Vector3(1.35f, 1.1f, 1.05f) * size, new Vector3(0f, .55f * size, 0f), plaster);
            GameObject top = Primitive(PrimitiveType.Cube, "Roof", r, new Vector3(1.12f, 1.12f, 1.25f) * size, new Vector3(0f, 1.2f * size, 0f), roof);
            top.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private void Tree(Vector3 p, float size)
        {
            Transform r = new GameObject("Tree").transform;
            r.SetParent(world, false);
            r.position = p;
            Primitive(PrimitiveType.Cylinder, "Trunk", r, new Vector3(.1f, .35f, .1f) * size, new Vector3(0f, .25f * size, 0f), wood);
            for (int i = 0; i < 3; i++)
            {
                GameObject crown = Primitive(PrimitiveType.Sphere, "Crown", r, Vector3.one * (.66f - i * .08f) * size,
                    new Vector3((i - 1) * .18f * size, (.72f + i * .12f) * size, Mathf.Sin(i * 2f) * .1f), tree);
                crown.transform.localScale = new Vector3(.9f, 1.15f, .9f) * (.66f - i * .08f) * size;
            }
        }

        private void MakeRain()
        {
            GameObject o = new GameObject("Rain");
            o.transform.SetParent(world, false);
            o.transform.position = new Vector3(0f, 13f, 0f);
            ParticleSystem p = o.AddComponent<ParticleSystem>();
            var main = p.main;
            main.loop = true;
            main.startLifetime = 1.5f;
            main.startSpeed = 0f;
            main.startSize = .055f;
            main.maxParticles = 1400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(.88f, .96f, .96f, .72f);
            var emission = p.emission;
            emission.rateOverTime = 500f;
            var shape = p.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 1f, 24f);
            var velocity = p.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = 1.5f;
            velocity.y = -18f;
            ParticleSystemRenderer pr = p.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.lengthScale = 5f;
            pr.velocityScale = .14f;
            Shader shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Unlit/Color");
            pr.material = new Material(shader) { color = new Color(.87f, .96f, .96f, .7f) };
            p.Play();
        }

        private void MakeSquads()
        {
            squads.Add(SquadMake("SKJOLD", Kind.Sword, new Color(.88f, .36f, .33f), new Vector3(-3.2f, .05f, -4.2f)));
            squads.Add(SquadMake("SPYD", Kind.Spear, new Color(.32f, .80f, .80f), new Vector3(0f, .05f, -4.6f)));
            squads.Add(SquadMake("BUE", Kind.Bow, new Color(.67f, .80f, .26f), new Vector3(3.2f, .05f, -4.2f)));
            Select(squads[0]);
        }

        private Squad SquadMake(string name, Kind kind, Color color, Vector3 p)
        {
            Squad s = new Squad
            {
                Name = name, Kind = kind, Color = color, Pos = p, Goal = p,
                Speed = kind == Kind.Spear ? 2.05f : 2.4f,
                Range = kind == Kind.Bow ? 8f : kind == Kind.Spear ? 1.75f : 1.3f,
                Damage = kind == Kind.Bow ? 10f : kind == Kind.Spear ? 14f : 11f,
                Cooldown = kind == Kind.Bow ? 1.12f : kind == Kind.Spear ? .82f : .68f
            };
            s.Root = new GameObject("Squad " + name).transform;
            s.Root.SetParent(world, false);
            s.Root.position = p;
            Material body = Mat(name + " Uniform", Color.Lerp(color, new Color(.14f, .2f, .24f), .62f));
            Vector3[] slots = Slots(8);
            for (int i = 0; i < slots.Length; i++) s.Men.Add(Soldier(s.Root, slots[i], body, color, kind, false));
            s.Flag = Flag(s.Root, color, name);
            return s;
        }

        private Vector3[] Slots(int count)
        {
            List<Vector3> list = new List<Vector3>();
            for (int row = 0; list.Count < count; row++)
            {
                int cols = row < 2 ? 3 : 2;
                for (int c = 0; c < cols && list.Count < count; c++) list.Add(new Vector3((c - (cols - 1) * .5f) * .55f, 0f, row * -.58f));
            }
            return list.ToArray();
        }

        private Man Soldier(Transform parent, Vector3 slot, Material body, Color accent, Kind kind, bool enemy)
        {
            Transform r = new GameObject(enemy ? "Raider" : "Soldier").transform;
            r.SetParent(parent, false);
            r.localPosition = slot;
            Primitive(PrimitiveType.Capsule, "Body", r, new Vector3(.24f, .38f, .24f), new Vector3(0f, .42f, 0f), body);
            Primitive(PrimitiveType.Sphere, "Head", r, new Vector3(.23f, .27f, .23f), new Vector3(0f, .92f, 0f), enemy ? raiderMat : white);
            Primitive(PrimitiveType.Sphere, "Helmet", r, new Vector3(.25f, .13f, .25f), new Vector3(0f, 1.02f, 0f), body);
            Material accentMat = enemy ? raiderMat : Mat("Accent", accent);
            if (kind == Kind.Sword || enemy)
            {
                GameObject shield = Primitive(PrimitiveType.Cylinder, "Shield", r, new Vector3(.28f, .06f, .28f), new Vector3(-.28f, .56f, -.08f), accentMat);
                shield.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            float length = kind == Kind.Spear ? 1.1f : .62f;
            Transform weapon = new GameObject("Weapon").transform;
            weapon.SetParent(r, false);
            weapon.localPosition = new Vector3(.28f, .58f, 0f);
            if (kind == Kind.Bow) Primitive(PrimitiveType.Cube, "Bow", weapon, new Vector3(.05f, .65f, .05f), Vector3.zero, wood).transform.localRotation = Quaternion.Euler(0f, 0f, 14f);
            else
            {
                GameObject shaft = Primitive(PrimitiveType.Cylinder, "Shaft", weapon, new Vector3(.045f, length * .5f, .045f), new Vector3(0f, 0f, length * .28f), wood);
                shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            return new Man { T = r, Slot = slot, Hp = enemy ? 28f : 50f };
        }

        private Transform Flag(Transform parent, Color color, string name)
        {
            Transform r = new GameObject("Flag " + name).transform;
            r.SetParent(parent, false);
            Primitive(PrimitiveType.Cylinder, "Pole", r, new Vector3(.035f, .95f, .035f), new Vector3(0f, .95f, .2f), wood);
            Primitive(PrimitiveType.Cube, "Cloth", r, new Vector3(.82f, .42f, .045f), new Vector3(.42f, 1.55f, .2f), Mat(name + " Flag", color));
            return r;
        }

        private void MakeSelection()
        {
            GameObject o = new GameObject("Selection Ring");
            o.transform.SetParent(world, false);
            selection = o.AddComponent<LineRenderer>();
            selection.loop = true;
            selection.positionCount = 48;
            selection.startWidth = selection.endWidth = .055f;
            selection.material = new Material(Shader.Find("Sprites/Default"));
        }

        private void BuildUi()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            GameObject c = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            c.transform.SetParent(transform, false);
            c.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = c.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;

            TextMake(c.transform, "SKJOLD ISLE", 42, TextAnchor.UpperCenter, new Vector2(.5f, 1f), new Vector2(0f, -28f), new Vector2(720f, 70f));
            timer = TextMake(c.transform, "00:00", 23, TextAnchor.UpperCenter, new Vector2(.5f, 1f), new Vector2(0f, -82f), new Vector2(240f, 48f));
            status = TextMake(c.transform, "", 24, TextAnchor.MiddleCenter, new Vector2(.5f, 0f), new Vector2(0f, 145f), new Vector2(920f, 58f));
            selectedLabel = TextMake(c.transform, "", 22, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(28f, 34f), new Vector2(430f, 52f));
            pauseButton = ButtonMake(c.transform, "Ⅱ", new Vector2(1f, 1f), new Vector2(-64f, -64f), new Vector2(88f, 88f), TogglePause);
            skillButton = ButtonMake(c.transform, "능력", new Vector2(1f, 0f), new Vector2(-100f, 72f), new Vector2(190f, 72f), Skill);

            for (int i = 0; i < squads.Count; i++)
            {
                int x = i;
                Button b = ButtonMake(c.transform, squads[i].Name, new Vector2(.5f, 0f), new Vector2((i - 1) * 225f, 62f), new Vector2(205f, 84f), () => Select(squads[x]));
                ColorBlock cb = b.colors;
                cb.normalColor = Color.Lerp(squads[i].Color, Color.white, .55f);
                cb.highlightedColor = Color.Lerp(squads[i].Color, Color.white, .75f);
                b.colors = cb;
                squadButtons.Add(b);
            }

            deployPanel = Panel(c.transform, new Color(.10f, .11f, .13f, .87f), new Vector2(0f, 1f), new Vector2(30f, -150f), new Vector2(610f, 290f));
            TextMake(deployPanel.transform, "전투 전 배치", 48, TextAnchor.UpperCenter, new Vector2(.5f, 1f), new Vector2(0f, -24f), new Vector2(540f, 64f));
            TextMake(deployPanel.transform, "분대 버튼을 누른 뒤 섬의 원하는 위치를 누르십시오.\n검병은 정면, 창병은 좁은 길, 궁병은 후방이 유리합니다.", 25, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(0f, 12f), new Vector2(540f, 118f));
            ButtonMake(deployPanel.transform, "전투 시작", new Vector2(.5f, 0f), new Vector2(0f, 38f), new Vector2(300f, 76f), StartBattle);

            resultPanel = Panel(c.transform, new Color(.08f, .09f, .11f, .92f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(700f, 390f));
            resultTitle = TextMake(resultPanel.transform, "", 58, TextAnchor.UpperCenter, new Vector2(.5f, 1f), new Vector2(0f, -42f), new Vector2(620f, 90f));
            resultBody = TextMake(resultPanel.transform, "", 25, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(0f, 18f), new Vector2(600f, 130f));
            ButtonMake(resultPanel.transform, "다시 시작", new Vector2(.5f, 0f), new Vector2(0f, 60f), new Vector2(320f, 82f), Restart);
            resultPanel.SetActive(false);
        }

        private Text TextMake(Transform parent, string value, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 rectSize)
        {
            GameObject o = new GameObject("Text", typeof(RectTransform), typeof(Text));
            o.transform.SetParent(parent, false);
            RectTransform r = o.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = pos;
            r.sizeDelta = rectSize;
            Text t = o.GetComponent<Text>();
            t.font = font;
            t.text = value;
            t.fontSize = size;
            t.alignment = align;
            t.color = new Color(.96f, .96f, .92f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private Button ButtonMake(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 rectSize, UnityEngine.Events.UnityAction action)
        {
            GameObject o = new GameObject("Button " + label, typeof(RectTransform), typeof(Image), typeof(Button));
            o.transform.SetParent(parent, false);
            RectTransform r = o.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = pos;
            r.sizeDelta = rectSize;
            Image image = o.GetComponent<Image>();
            image.color = new Color(.90f, .84f, .73f, .96f);
            Button b = o.GetComponent<Button>();
            b.onClick.AddListener(action);
            Text t = TextMake(o.transform, label, 28, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), Vector2.zero, rectSize);
            t.color = new Color(.12f, .12f, .13f);
            return b;
        }

        private GameObject Panel(Transform parent, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            GameObject o = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            o.transform.SetParent(parent, false);
            RectTransform r = o.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            o.GetComponent<Image>().color = color;
            return o;
        }

        private void EnterDeploy()
        {
            phase = Phase.Deploy;
            deployPanel.SetActive(true);
            pauseButton.interactable = false;
            skillButton.interactable = false;
            status.text = "분대를 선택하고 섬 위를 눌러 배치하십시오.";
            RefreshLabels();
        }

        private void StartBattle()
        {
            if (phase != Phase.Deploy) return;
            phase = Phase.Battle;
            deployPanel.SetActive(false);
            pauseButton.interactable = skillButton.interactable = true;
            status.text = "적 수송선이 접근합니다.";
            battleTime = 0f;
            nextBoat = 1.2f;
            boatCount = 0;
        }

        private void ReadPointer()
        {
            bool down = false;
            Vector2 screen = Vector2.zero;
            int pointerId = -1;
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                Touch t = Input.GetTouch(0);
                down = true;
                screen = t.position;
                pointerId = t.fingerId;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                down = true;
                screen = Input.mousePosition;
            }
            if (!down || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId)) return;
            if (!Physics.Raycast(cam.ScreenPointToRay(screen), out RaycastHit hit, 100f) || hit.collider != islandCollider) return;
            Squad near = squads.Where(s => s.Alive).OrderBy(s => Vector3.Distance(s.Pos, hit.point)).FirstOrDefault();
            if (near != null && Vector3.Distance(near.Pos, hit.point) < 1.55f) Select(near);
            else if (selected != null && selected.Alive)
            {
                selected.Goal = Clamp(hit.point);
                status.text = phase == Phase.Deploy ? selected.Name + " 배치 위치 지정" : selected.Name + " 이동";
            }
        }

        private void Select(Squad s)
        {
            if (s == null || !s.Alive) return;
            selected = s;
            skillButton.GetComponentInChildren<Text>().text = s.Kind == Kind.Sword ? "방패 결집" : s.Kind == Kind.Spear ? "창벽" : "집중 사격";
            RefreshLabels();
        }

        private void TickSquads(float dt)
        {
            foreach (Squad s in squads)
            {
                if (!s.Alive) continue;
                s.AttackClock -= dt;
                s.SkillClock = Mathf.Max(0f, s.SkillClock - dt);
                float d = Vector3.Distance(s.Pos, s.Goal);
                if (d > .05f)
                {
                    Vector3 dir = (s.Goal - s.Pos).normalized;
                    s.Pos += dir * Mathf.Min(d, s.Speed * dt);
                    s.Root.position = s.Pos;
                    s.Root.rotation = Quaternion.Slerp(s.Root.rotation, Quaternion.LookRotation(dir), dt * 7f);
                }
                foreach (Man m in s.Men.Where(m => m.Alive)) m.T.localPosition = Vector3.Lerp(m.T.localPosition, m.Slot, dt * 9f);
                if (phase == Phase.Battle)
                {
                    Raider target = raiders.Where(r => r.Man.Alive).OrderBy(r => Vector3.Distance(s.Pos, r.Man.T.position)).FirstOrDefault();
                    if (target != null && Vector3.Distance(s.Pos, target.Man.T.position) <= s.Range && s.AttackClock <= 0f)
                    {
                        s.AttackClock = s.Cooldown;
                        HitRaider(target, s.Damage, s.Pos);
                        Pulse(s.Men.FirstOrDefault(m => m.Alive));
                    }
                }
            }
            RefreshLabels();
        }

        private void SpawnSchedule()
        {
            if (boatCount >= PlannedBoats || battleTime < nextBoat) return;
            float[] degrees = { 205f, 330f, 120f, 25f };
            float a = degrees[boatCount] * Mathf.Deg2Rad;
            Vector3 start = new Vector3(Mathf.Cos(a) * 18f, -1.05f, Mathf.Sin(a) * 14f);
            Vector3 goal = new Vector3(Mathf.Cos(a) * 9.7f, -.88f, Mathf.Sin(a) * 6.7f);
            Transform r = new GameObject("Raider Longboat").transform;
            r.SetParent(world, false);
            r.position = start;
            r.rotation = Quaternion.LookRotation(goal - start);
            Primitive(PrimitiveType.Cube, "Hull", r, new Vector3(1f, .32f, 2.8f), Vector3.zero, wood);
            Primitive(PrimitiveType.Cylinder, "Mast", r, new Vector3(.06f, 1.1f, .06f), new Vector3(0f, 1.05f, 0f), wood);
            Primitive(PrimitiveType.Cube, "Sail", r, new Vector3(1.45f, .85f, .05f), new Vector3(0f, 1.25f, 0f), raiderMat);
            boats.Add(new Boat { T = r, Goal = goal, Cargo = 6 + boatCount });
            boatCount++;
            nextBoat = battleTime + 5.8f;
            status.text = "적 수송선 발견";
        }

        private void TickBoats(float dt)
        {
            foreach (Boat b in boats.Where(x => !x.Landed))
            {
                Vector3 delta = b.Goal - b.T.position;
                b.T.position += delta.normalized * Mathf.Min(delta.magnitude, 3.45f * dt);
                b.T.rotation = Quaternion.Slerp(b.T.rotation, Quaternion.LookRotation(delta), dt * 4f);
                if (delta.magnitude < .16f)
                {
                    b.Landed = true;
                    SpawnRaiders(b.Goal, b.Cargo);
                    status.text = "약탈자가 상륙했습니다";
                }
            }
        }

        private void SpawnRaiders(Vector3 shore, int count)
        {
            Transform group = new GameObject("Raider Group").transform;
            group.SetParent(world, false);
            group.position = new Vector3(shore.x, .04f, shore.z);
            for (int i = 0; i < count; i++)
            {
                Vector3 slot = new Vector3((i % 3 - 1) * .48f, 0f, i / 3 * .52f);
                Man m = Soldier(group, slot, raiderMat, new Color(.5f, .18f, .2f), Kind.Sword, true);
                raiders.Add(new Raider { Man = m, AttackClock = UnityEngine.Random.value * .5f });
            }
        }

        private void TickRaiders(float dt)
        {
            for (int i = raiders.Count - 1; i >= 0; i--)
            {
                Raider r = raiders[i];
                if (!r.Man.Alive) { raiders.RemoveAt(i); continue; }
                r.AttackClock -= dt;
                Squad target = squads.Where(s => s.Alive).OrderBy(s => Vector3.Distance(s.Pos, r.Man.T.position)).FirstOrDefault();
                if (target == null) continue;
                Vector3 delta = target.Pos - r.Man.T.position;
                if (delta.magnitude > 1f)
                {
                    r.Man.T.position += delta.normalized * 1.65f * dt;
                    r.Man.T.rotation = Quaternion.Slerp(r.Man.T.rotation, Quaternion.LookRotation(delta), dt * 8f);
                }
                else if (r.AttackClock <= 0f)
                {
                    r.AttackClock = .95f;
                    Man victim = target.Men.FirstOrDefault(m => m.Alive);
                    if (victim != null)
                    {
                        victim.Hp -= 8f;
                        Pulse(victim);
                        if (victim.Hp <= 0f) victim.T.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void HitRaider(Raider target, float damage, Vector3 source)
        {
            if (target == null || !target.Man.Alive) return;
            target.Man.Hp -= damage;
            target.Man.T.position += (target.Man.T.position - source).normalized * .16f;
            Pulse(target.Man);
            if (target.Man.Hp <= 0f) target.Man.T.gameObject.SetActive(false);
        }

        private void Pulse(Man m)
        {
            if (m != null && m.T != null) StartCoroutine(PulseRoutine(m.T));
        }

        private System.Collections.IEnumerator PulseRoutine(Transform t)
        {
            Vector3 original = t.localScale;
            t.localScale = original * 1.18f;
            yield return new WaitForSeconds(.08f);
            if (t != null) t.localScale = original;
        }

        private void Skill()
        {
            if (phase != Phase.Battle || selected == null || !selected.Alive) return;
            if (selected.SkillClock > 0f) { status.text = $"능력 재사용까지 {selected.SkillClock:0.0}초"; return; }
            selected.SkillClock = 12f;
            if (selected.Kind == Kind.Sword)
            {
                foreach (Man m in selected.Men.Where(m => m.Alive)) m.Hp = Mathf.Min(50f, m.Hp + 12f);
                status.text = "방패 결집: 생존 병력 회복";
            }
            else
            {
                int max = selected.Kind == Kind.Bow ? 5 : 99;
                float radius = selected.Kind == Kind.Bow ? 9f : 3.2f;
                float damage = selected.Kind == Kind.Bow ? 18f : 20f;
                foreach (Raider r in raiders.Where(r => r.Man.Alive && Vector3.Distance(r.Man.T.position, selected.Pos) < radius).Take(max).ToList()) HitRaider(r, damage, selected.Pos);
                status.text = selected.Kind == Kind.Bow ? "집중 사격" : "창벽";
            }
        }

        private void TogglePause()
        {
            if (phase == Phase.Deploy || phase == Phase.Win || phase == Phase.Lose) return;
            if (phase == Phase.Pause)
            {
                phase = beforePause;
                Time.timeScale = 1f;
                pauseButton.GetComponentInChildren<Text>().text = "Ⅱ";
                status.text = "전투 재개";
            }
            else
            {
                beforePause = phase;
                phase = Phase.Pause;
                Time.timeScale = 0f;
                pauseButton.GetComponentInChildren<Text>().text = "▶";
                status.text = "일시정지";
            }
        }

        private void CheckResult()
        {
            if (!squads.Any(s => s.Alive)) { Finish(false); return; }
            if (boatCount >= PlannedBoats && boats.All(b => b.Landed) && !raiders.Any(r => r.Man.Alive)) Finish(true);
        }

        private void Finish(bool win)
        {
            phase = win ? Phase.Win : Phase.Lose;
            resultPanel.SetActive(true);
            resultTitle.text = win ? "섬 방어 성공" : "방어선 붕괴";
            int alive = squads.Sum(s => s.Men.Count(m => m.Alive));
            resultBody.text = win ? $"모든 상륙 병력을 격퇴했습니다.\n생존 병력 {alive}/{squads.Sum(s => s.Men.Count)} · 전투 시간 {battleTime:0.0}초" : "모든 분대가 전투불능 상태입니다.\n배치와 병과 위치를 바꿔 다시 시험하십시오.";
            pauseButton.interactable = skillButton.interactable = false;
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void RefreshLabels()
        {
            for (int i = 0; i < squadButtons.Count; i++)
            {
                Squad s = squads[i];
                squadButtons[i].interactable = s.Alive;
                squadButtons[i].GetComponentInChildren<Text>().text = $"{s.Name}\n{KindName(s.Kind)} {s.Men.Count(m => m.Alive)}/{s.Men.Count}";
            }
            if (selected != null) selectedLabel.text = $"선택: {selected.Name} · {KindName(selected.Kind)} · 병력 {selected.Men.Count(m => m.Alive)}/{selected.Men.Count} · 능력 {selected.SkillClock:0.0}s";
        }

        private string KindName(Kind k) => k == Kind.Sword ? "검병" : k == Kind.Spear ? "창병" : "궁병";

        private void DrawSelection()
        {
            if (selected == null || !selected.Alive) { selection.enabled = false; return; }
            selection.enabled = true;
            float radius = 1.35f + Mathf.Sin(Time.unscaledTime * 4f) * .06f;
            Vector3 center = selected.Pos + Vector3.up * .05f;
            for (int i = 0; i < selection.positionCount; i++)
            {
                float a = i / (float)selection.positionCount * Mathf.PI * 2f;
                selection.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            selection.startColor = selection.endColor = Color.Lerp(selected.Color, Color.white, .25f);
        }

        private void AnimateEnvironment()
        {
            if (water == null) return;
            water.position = new Vector3(0f, -1.45f + Mathf.Sin(Time.unscaledTime * .75f) * .035f, 0f);
            sea.color = Color.Lerp(new Color(.62f, .72f, .70f, .94f), new Color(.68f, .77f, .75f, .94f), Mathf.Sin(Time.unscaledTime * .55f) * .5f + .5f);
        }

        private Vector3 Clamp(Vector3 p)
        {
            float rx = 9.2f, rz = 6.2f;
            float n = Mathf.Sqrt(p.x * p.x / (rx * rx) + p.z * p.z / (rz * rz));
            if (n > 1f) { p.x /= n; p.z /= n; }
            p.y = .05f;
            return p;
        }

        private GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 scale, Vector3 position, Material material, bool removeCollider = true)
        {
            GameObject o = GameObject.CreatePrimitive(type);
            o.name = name;
            o.transform.SetParent(parent, false);
            o.transform.localPosition = position;
            o.transform.localScale = scale;
            Renderer r = o.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = material;
            Collider c = o.GetComponent<Collider>();
            if (removeCollider && c != null) Destroy(c);
            return o;
        }
    }
}
