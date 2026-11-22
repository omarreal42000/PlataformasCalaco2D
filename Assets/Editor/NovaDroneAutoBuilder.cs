using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Generador automatico en 1 clic para NOVA DRONE / CALACO 2D.
/// Construye capas, materiales fisicos, sprites, animaciones (Idle, Walk, Shoot),
/// prefabs (EfectoImpacto, Bala, Enemigo, Nucleo), las 4 escenas completas
/// (MenuPrincipal, Nivel1, Nivel2, Resultados) y configura Build Settings.
/// </summary>
public static class NovaDroneAutoBuilder
{
    const int GROUND_LAYER = 8;

    [MenuItem("NovaDrone/1. Construir Proyecto Completo (1 Clic)", false, 1)]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureDirectories();
        ImportTmpEssentialsIfNeeded();
        ConfigureInputAndLayers();

        PhysicsMaterial2D sinFriccion = CreateNoFrictionMaterial();
        Sprite sqSprite = CreateProceduralSprite("Square", 64, false, false);
        Sprite circleSprite = CreateProceduralSprite("Circle", 64, true, false);
        Sprite capsuleSprite = CreateProceduralSprite("Capsule", 64, false, true);

        AnimatorController animController = CreatePlayerAnimator();

        GameObject fxPrefab = CreateImpactEffectPrefab();
        GameObject balaPrefab = CreateBulletPrefab(circleSprite, fxPrefab);
        GameObject enemigoPrefab = CreateEnemyPrefab(circleSprite, fxPrefab);
        GameObject nucleoPrefab = CreateCoinPrefab(sqSprite, fxPrefab);

        BuildMenuScene();
        BuildLevelScene(
            scenePath: "Assets/Scenes/Nivel1.unity",
            levelNumber: 1,
            nextScene: "Nivel2",
            sqSprite: sqSprite,
            capsuleSprite: capsuleSprite,
            sinFriccion: sinFriccion,
            animController: animController,
            balaPrefab: balaPrefab,
            enemigoPrefab: enemigoPrefab,
            nucleoPrefab: nucleoPrefab
        );
        BuildLevelScene(
            scenePath: "Assets/Scenes/Nivel2.unity",
            levelNumber: 2,
            nextScene: "Resultados",
            sqSprite: sqSprite,
            capsuleSprite: capsuleSprite,
            sinFriccion: sinFriccion,
            animController: animController,
            balaPrefab: balaPrefab,
            enemigoPrefab: enemigoPrefab,
            nucleoPrefab: nucleoPrefab
        );
        BuildResultsScene();

        ConfigureBuildSettings();

        EditorSceneManager.OpenScene("Assets/Scenes/MenuPrincipal.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Proyecto Construido",
            "Listo! Se crearon las 4 escenas (MenuPrincipal, Nivel1, Nivel2, Resultados), los prefabs, las animaciones y el Build Settings.\n\nPresiona PLAY para jugar o usa 'NovaDrone > 2. Compilar Ejecutable Windows (.exe)'.",
            "Excelente"
        );
    }

    [MenuItem("NovaDrone/2. Compilar Ejecutable Windows (.exe)", false, 2)]
    public static void BuildWindowsExe()
    {
        if (!File.Exists("Assets/Scenes/MenuPrincipal.unity"))
        {
            EditorUtility.DisplayDialog("Falta construir", "Primero ejecuta 'NovaDrone > 1. Construir Proyecto Completo (1 Clic)'.", "OK");
            return;
        }

        string folder = EditorUtility.SaveFolderPanel("Elige carpeta para guardar el .exe", "", "NovaDrone_Windows");
        if (string.IsNullOrEmpty(folder)) return;

        string exePath = Path.Combine(folder, "NovaDrone2D.exe");
        string[] scenes = new[]
        {
            "Assets/Scenes/MenuPrincipal.unity",
            "Assets/Scenes/Nivel1.unity",
            "Assets/Scenes/Nivel2.unity",
            "Assets/Scenes/Resultados.unity"
        };

        BuildPipeline.BuildPlayer(scenes, exePath, BuildTarget.StandaloneWindows64, BuildOptions.None);
        EditorUtility.RevealInFinder(exePath);
    }

    static void EnsureDirectories()
    {
        string[] dirs = { "Assets/Art", "Assets/Physics", "Assets/Animations", "Assets/Prefabs", "Assets/Scenes" };
        foreach (string d in dirs)
        {
            if (!AssetDatabase.IsValidFolder(d))
            {
                string parent = Path.GetDirectoryName(d).Replace("\\", "/");
                string folderName = Path.GetFileName(d);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }

    static void ImportTmpEssentialsIfNeeded()
    {
        if (Resources.Load("TMP Settings") != null) return;

        string pkgPath = Path.GetFullPath("Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage");
        if (File.Exists(pkgPath))
        {
            AssetDatabase.ImportPackage(pkgPath, false);
        }
    }

    static void ConfigureInputAndLayers()
    {
        // Layer 8 = Ground
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        if (layersProp != null && layersProp.arraySize > GROUND_LAYER)
        {
            SerializedProperty sp = layersProp.GetArrayElementAtIndex(GROUND_LAYER);
            if (sp != null) sp.stringValue = "Ground";
        }
        tagManager.ApplyModifiedProperties();

        // Active Input Handling = 0 (Old Input Manager)
        Object[] projSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (projSettings != null && projSettings.Length > 0)
        {
            SerializedObject ps = new SerializedObject(projSettings[0]);
            SerializedProperty inputProp = ps.FindProperty("activeInputHandler");
            if (inputProp != null && inputProp.intValue != 0)
            {
                inputProp.intValue = 0;
                ps.ApplyModifiedProperties();
            }
        }
    }

    static PhysicsMaterial2D CreateNoFrictionMaterial()
    {
        string path = "Assets/Physics/SinFriccion.physicsMaterial2D";
        PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("SinFriccion");
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.friction = 0f;
        mat.bounciness = 0f;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Sprite CreateProceduralSprite(string name, int size, bool isCircle, bool isCapsule)
    {
        string path = $"Assets/Art/{name}.png";
        int w = size;
        int h = isCapsule ? size * 2 : size;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        float cx = (w - 1) * 0.5f;
        float cy = (h - 1) * 0.5f;
        float r = (w - 2) * 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool inside = true;
                if (isCircle)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    inside = (dx * dx + dy * dy) <= r * r;
                }
                else if (isCapsule)
                {
                    float halfStraight = (h - w) * 0.5f;
                    float clampedY = Mathf.Clamp(y, cy - halfStraight, cy + halfStraight);
                    float dx = x - cx;
                    float dy = y - clampedY;
                    inside = (dx * dx + dy * dy) <= r * r;
                }
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = 64f;
            ti.filterMode = FilterMode.Bilinear;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static AnimatorController CreatePlayerAnimator()
    {
        string ctrlPath = "Assets/Animations/DroneVisual.controller";
        if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        controller.AddParameter("isWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("shootTrigger", AnimatorControllerParameterType.Trigger);

        // 1. Idle (1s, Loop)
        AnimationClip idleClip = new AnimationClip { frameRate = 60f };
        AnimationClipSettings idleSettings = AnimationUtility.GetAnimationClipSettings(idleClip);
        idleSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(idleClip, idleSettings);
        SetScaleCurves(idleClip, new[] { 0f, 0.5f, 1f }, new[] { 0.9f, 0.95f, 0.9f }, new[] { 0.9f, 0.95f, 0.9f });
        AssetDatabase.CreateAsset(idleClip, "Assets/Animations/Idle.anim");

        // 2. Walk (0.25s, Loop)
        AnimationClip walkClip = new AnimationClip { frameRate = 60f };
        AnimationClipSettings walkSettings = AnimationUtility.GetAnimationClipSettings(walkClip);
        walkSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(walkClip, walkSettings);
        SetScaleCurves(walkClip, new[] { 0f, 0.125f, 0.25f }, new[] { 0.9f, 1.0f, 0.9f }, new[] { 0.9f, 0.75f, 0.9f });
        AssetDatabase.CreateAsset(walkClip, "Assets/Animations/Walk.anim");

        // 3. Shoot (0.15s, No Loop)
        AnimationClip shootClip = new AnimationClip { frameRate = 60f };
        AnimationClipSettings shootSettings = AnimationUtility.GetAnimationClipSettings(shootClip);
        shootSettings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(shootClip, shootSettings);
        SetScaleCurves(shootClip, new[] { 0f, 0.07f, 0.15f }, new[] { 0.9f, 1.1f, 0.9f }, new[] { 0.9f, 0.8f, 0.9f });
        SetColorCurves(shootClip, new[] { 0f, 0.07f, 0.15f },
            new[] { new Color(0f, 0.9f, 1f, 1f), Color.white, new Color(0f, 0.9f, 1f, 1f) });
        AssetDatabase.CreateAsset(shootClip, "Assets/Animations/Shoot.anim");

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idleClip;
        sm.defaultState = idleState;

        AnimatorState walkState = sm.AddState("Walk");
        walkState.motion = walkClip;

        AnimatorState shootState = sm.AddState("Shoot");
        shootState.motion = shootClip;

        // Transiciones
        var tIdleWalk = idleState.AddTransition(walkState);
        tIdleWalk.hasExitTime = false;
        tIdleWalk.duration = 0f;
        tIdleWalk.AddCondition(AnimatorConditionMode.If, 0f, "isWalking");

        var tWalkIdle = walkState.AddTransition(idleState);
        tWalkIdle.hasExitTime = false;
        tWalkIdle.duration = 0f;
        tWalkIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isWalking");

        var tAnyShoot = sm.AddAnyStateTransition(shootState);
        tAnyShoot.hasExitTime = false;
        tAnyShoot.duration = 0f;
        tAnyShoot.canTransitionToSelf = false;
        tAnyShoot.AddCondition(AnimatorConditionMode.If, 0f, "shootTrigger");

        var tShootIdle = shootState.AddTransition(idleState);
        tShootIdle.hasExitTime = true;
        tShootIdle.exitTime = 1f;
        tShootIdle.duration = 0f;
        tShootIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isWalking");

        var tShootWalk = shootState.AddTransition(walkState);
        tShootWalk.hasExitTime = true;
        tShootWalk.exitTime = 1f;
        tShootWalk.duration = 0f;
        tShootWalk.AddCondition(AnimatorConditionMode.If, 0f, "isWalking");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void SetScaleCurves(AnimationClip clip, float[] times, float[] xs, float[] ys)
    {
        AnimationCurve cx = new AnimationCurve();
        AnimationCurve cy = new AnimationCurve();
        AnimationCurve cz = new AnimationCurve();
        for (int i = 0; i < times.Length; i++)
        {
            cx.AddKey(times[i], xs[i]);
            cy.AddKey(times[i], ys[i]);
            cz.AddKey(times[i], 1f);
        }
        clip.SetCurve("", typeof(Transform), "localScale.x", cx);
        clip.SetCurve("", typeof(Transform), "localScale.y", cy);
        clip.SetCurve("", typeof(Transform), "localScale.z", cz);
    }

    static void SetColorCurves(AnimationClip clip, float[] times, Color[] colors)
    {
        AnimationCurve cr = new AnimationCurve();
        AnimationCurve cg = new AnimationCurve();
        AnimationCurve cb = new AnimationCurve();
        AnimationCurve ca = new AnimationCurve();
        for (int i = 0; i < times.Length; i++)
        {
            cr.AddKey(times[i], colors[i].r);
            cg.AddKey(times[i], colors[i].g);
            cb.AddKey(times[i], colors[i].b);
            ca.AddKey(times[i], colors[i].a);
        }
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.r", cr);
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.g", cg);
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.b", cb);
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.a", ca);
    }

    static GameObject CreateImpactEffectPrefab()
    {
        string path = "Assets/Prefabs/EfectoImpacto.prefab";
        GameObject go = new GameObject("EfectoImpacto");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = 0.4f;
        main.startSpeed = 4f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.55f, 0.1f, 1f);
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject CreateBulletPrefab(Sprite circleSprite, GameObject fxPrefab)
    {
        string path = "Assets/Prefabs/Bala.prefab";
        GameObject go = new GameObject("Bala");
        go.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = new Color(1f, 0.92f, 0.15f, 1f);
        sr.sortingOrder = 5;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.drag = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = false;

        BulletController bc = go.AddComponent<BulletController>();
        bc.speed = 14f;
        bc.lifeTime = 2f;
        bc.damage = 1;
        bc.efectoImpactoPrefab = fxPrefab;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject CreateEnemyPrefab(Sprite circleSprite, GameObject fxPrefab)
    {
        string path = "Assets/Prefabs/Enemigo.prefab";
        GameObject go = new GameObject("Enemigo");
        go.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = new Color(0.95f, 0.2f, 0.25f, 1f);
        sr.sortingOrder = 3;

        go.AddComponent<CircleCollider2D>();

        EnemyHealth eh = go.AddComponent<EnemyHealth>();
        eh.maxHealth = 3;
        eh.mataAlJugadorAlTocar = true;
        eh.efectoMuertePrefab = fxPrefab;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject CreateCoinPrefab(Sprite sqSprite, GameObject fxPrefab)
    {
        string path = "Assets/Prefabs/Nucleo.prefab";
        GameObject go = new GameObject("Nucleo");
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sqSprite;
        sr.color = new Color(1f, 0.84f, 0f, 1f);
        sr.sortingOrder = 4;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        CoinPickup cp = go.AddComponent<CoinPickup>();
        cp.valor = 1;
        cp.velocidadGiro = 120f;
        cp.efectoPrefab = fxPrefab;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void BuildLevelScene(
        string scenePath,
        int levelNumber,
        string nextScene,
        Sprite sqSprite,
        Sprite capsuleSprite,
        PhysicsMaterial2D sinFriccion,
        AnimatorController animController,
        GameObject balaPrefab,
        GameObject enemigoPrefab,
        GameObject nucleoPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Camara
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0f, 1f, -10f);
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.024f, 0.06f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        camObj.AddComponent<AudioListener>();

        CameraFollow2D camFollow = camObj.AddComponent<CameraFollow2D>();
        camFollow.orthographicSize = 6f;
        camFollow.smoothTime = 0.15f;
        camFollow.offset = new Vector2(0f, 1f);
        camFollow.minX = -10f;
        camFollow.maxX = 60f;
        camFollow.minY = -2f;
        camFollow.maxY = 10f;

        // 2. Jugador
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D playerCol = player.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(0.9f, 0.9f);
        playerCol.sharedMaterial = sinFriccion;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(player.transform, false);
        visual.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        SpriteRenderer visSr = visual.AddComponent<SpriteRenderer>();
        visSr.sprite = sqSprite;
        visSr.color = new Color(0f, 0.9f, 1f, 1f);
        visSr.sortingOrder = 5;
        Animator anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = animController;

        GameObject visor = new GameObject("Visor");
        visor.transform.SetParent(visual.transform, false);
        visor.transform.localPosition = new Vector3(0.28f, 0.12f, 0f);
        visor.transform.localScale = new Vector3(0.32f, 0.28f, 1f);
        SpriteRenderer visorSr = visor.AddComponent<SpriteRenderer>();
        visorSr.sprite = sqSprite;
        visorSr.color = Color.white;
        visorSr.sortingOrder = 6;

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform, false);
        groundCheck.transform.localPosition = new Vector3(0f, -0.5f, 0f);

        GameObject puntoDisparo = new GameObject("PuntoDisparo");
        puntoDisparo.transform.SetParent(player.transform, false);
        puntoDisparo.transform.localPosition = new Vector3(0.75f, 0f, 0f);

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.moveSpeed = 6f;
        pc.jumpForce = 13f;
        pc.groundCheck = groundCheck.transform;
        pc.groundRadius = 0.22f;
        pc.groundLayer = 1 << GROUND_LAYER;
        pc.puntoDisparo = puntoDisparo.transform;
        pc.bulletPrefab = balaPrefab;
        pc.fireRate = 0.35f;
        pc.animator = anim;

        camFollow.target = player.transform;

        // 3. Suelo, plataformas y paredes
        Color groundColor = levelNumber == 1 ? new Color(0.14f, 0.2f, 0.32f, 1f) : new Color(0.24f, 0.14f, 0.3f, 1f);
        CreatePlatform("SueloPrincipal", new Vector2(25f, -1f), new Vector2(72f, 2f), sqSprite, groundColor, sinFriccion, GROUND_LAYER);
        CreatePlatform("ParedIzq", new Vector2(-11f, 8f), new Vector2(1f, 22f), sqSprite, groundColor, sinFriccion, 0);
        CreatePlatform("ParedDer", new Vector2(61f, 8f), new Vector2(1f, 22f), sqSprite, groundColor, sinFriccion, 0);

        Vector2[] platPos = levelNumber == 1
            ? new[] { new Vector2(6f, 1.2f), new Vector2(15f, 2.4f), new Vector2(26f, 1.5f), new Vector2(37f, 2.6f), new Vector2(47f, 1.4f) }
            : new[] { new Vector2(5f, 1.5f), new Vector2(13f, 3.0f), new Vector2(22f, 1.8f), new Vector2(31f, 3.2f), new Vector2(41f, 2.0f), new Vector2(49f, 3.0f) };

        for (int i = 0; i < platPos.Length; i++)
            CreatePlatform($"Plataforma_{i + 1}", platPos[i], new Vector2(4.5f, 0.5f), sqSprite, groundColor * 1.35f, sinFriccion, GROUND_LAYER);

        // 4. Enemigos
        float[] enemyXs = levelNumber == 1
            ? new[] { 10f, 20f, 30f, 40f, 49f }
            : new[] { 8f, 16f, 25f, 34f, 43f, 51f };

        foreach (float ex in enemyXs)
        {
            GameObject e = (GameObject)PrefabUtility.InstantiatePrefab(enemigoPrefab);
            e.transform.position = new Vector3(ex, 0.7f, 0f);
        }

        // 5. Nucleos (Monedas)
        Vector2[] coinPos = levelNumber == 1
            ? new[] {
                new Vector2(6f, 2.2f), new Vector2(15f, 3.4f), new Vector2(20f, 2.5f), new Vector2(26f, 2.5f),
                new Vector2(33f, 1.2f), new Vector2(37f, 3.6f), new Vector2(44f, 1.2f), new Vector2(47f, 2.4f)
            }
            : new[] {
                new Vector2(5f, 2.5f), new Vector2(13f, 4.0f), new Vector2(18f, 1.2f), new Vector2(22f, 2.8f),
                new Vector2(28f, 1.2f), new Vector2(31f, 4.2f), new Vector2(38f, 1.2f), new Vector2(41f, 3.0f),
                new Vector2(46f, 1.2f), new Vector2(49f, 4.0f)
            };

        foreach (Vector2 cp in coinPos)
        {
            GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(nucleoPrefab);
            c.transform.position = new Vector3(cp.x, cp.y, 0f);
        }

        // 6. Portal de meta
        GameObject portal = new GameObject("PortalMeta");
        portal.transform.position = new Vector3(56f, 1.5f, 0f);
        portal.transform.localScale = new Vector3(1.4f, 3f, 1f);
        SpriteRenderer portalSr = portal.AddComponent<SpriteRenderer>();
        portalSr.sprite = capsuleSprite;
        portalSr.color = new Color(0.95f, 0.15f, 0.85f, 1f);
        portalSr.sortingOrder = 2;
        CapsuleCollider2D portalCol = portal.AddComponent<CapsuleCollider2D>();
        portalCol.isTrigger = true;
        LevelGoal goal = portal.AddComponent<LevelGoal>();
        goal.nivel = levelNumber;
        goal.escenaSiguiente = nextScene;

        // 7. HUD + UIManager
        Canvas canvas = CreateCanvasAndEventSystem();
        TMP_Text txtNivel = CreateTMPText(canvas.transform, "TextoNivel", $"NIVEL {levelNumber}  |  Mover: A/D  |  Saltar: Espacio  |  Disparar: Clic / J",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -25f), new Vector2(900f, 45f), 24, TextAlignmentOptions.Left, Color.white);
        TMP_Text txtMonedas = CreateTMPText(canvas.transform, "TextoMonedas", "Monedas: 0",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -72f), new Vector2(450f, 50f), 32, TextAlignmentOptions.Left, new Color(1f, 0.85f, 0.1f, 1f));
        TMP_Text txtFb = CreateTMPText(canvas.transform, "TextoEstadoFirebase", "Firebase: conectando...",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -25f), new Vector2(520f, 45f), 22, TextAlignmentOptions.Right, Color.green);

        GameObject uiObj = new GameObject("UIManager");
        UIManager ui = uiObj.AddComponent<UIManager>();
        ui.textoMonedas = txtMonedas;
        ui.textoEstadoFirebase = txtFb;

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    static void BuildMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.09f, 1f);
        camObj.AddComponent<AudioListener>();

        Canvas canvas = CreateCanvasAndEventSystem();

        CreateTMPText(canvas.transform, "Titulo", "NOVA DRONE 2D",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 340f), new Vector2(900f, 90f), 64, TextAlignmentOptions.Center, new Color(0f, 0.9f, 1f, 1f));

        TMP_Text txtMonedas = CreateTMPText(canvas.transform, "TextoMonedas", "Monedas: 0",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 245f), new Vector2(600f, 50f), 34, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.1f, 1f));

        TMP_Text txtFb = CreateTMPText(canvas.transform, "TextoEstadoFirebase", "Firebase: conectando...",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(700f, 45f), 26, TextAlignmentOptions.Center, Color.green);

        TMP_Text txtBloq = CreateTMPText(canvas.transform, "TextoNivel2Bloqueado", "Completa Nivel 1 para desbloquear Nivel 2",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -155f), new Vector2(700f, 40f), 22, TextAlignmentOptions.Center, new Color(1f, 0.45f, 0.45f, 1f));

        GameObject uiObj = new GameObject("UIManager");
        UIManager ui = uiObj.AddComponent<UIManager>();
        ui.textoMonedas = txtMonedas;
        ui.textoEstadoFirebase = txtFb;
        ui.textoNivel2Bloqueado = txtBloq;

        Button btnJugar = CreateButton(canvas.transform, "BtnJugar", "JUGAR", new Vector2(0f, 90f), new Color(0f, 0.72f, 0.85f, 1f));
        Button btnN1 = CreateButton(canvas.transform, "BtnNivel1", "NIVEL 1", new Vector2(0f, 10f), new Color(0.18f, 0.28f, 0.45f, 1f));
        Button btnN2 = CreateButton(canvas.transform, "BtnNivel2", "NIVEL 2", new Vector2(0f, -70f), new Color(0.18f, 0.28f, 0.45f, 1f));
        Button btnReset = CreateButton(canvas.transform, "BtnReiniciar", "REINICIAR PROGRESO", new Vector2(0f, -230f), new Color(0.55f, 0.25f, 0.15f, 1f));
        Button btnSalir = CreateButton(canvas.transform, "BtnSalir", "SALIR", new Vector2(0f, -310f), new Color(0.35f, 0.15f, 0.2f, 1f));

        ui.botonNivel2 = btnN2;

        UnityEventTools.AddPersistentListener(btnJugar.onClick, ui.Jugar);
        UnityEventTools.AddPersistentListener(btnN1.onClick, ui.IrNivel1);
        UnityEventTools.AddPersistentListener(btnN2.onClick, ui.IrNivel2);
        UnityEventTools.AddPersistentListener(btnReset.onClick, ui.ReiniciarProgreso);
        UnityEventTools.AddPersistentListener(btnSalir.onClick, ui.Salir);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MenuPrincipal.unity");
    }

    static void BuildResultsScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.09f, 1f);
        camObj.AddComponent<AudioListener>();

        Canvas canvas = CreateCanvasAndEventSystem();

        CreateTMPText(canvas.transform, "Titulo", "MISION COMPLETA",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(900f, 90f), 64, TextAlignmentOptions.Center, new Color(0.25f, 1f, 0.55f, 1f));

        TMP_Text txtMonedas = CreateTMPText(canvas.transform, "TextoMonedas", "Monedas: 0",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(600f, 55f), 38, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.1f, 1f));

        TMP_Text txtFb = CreateTMPText(canvas.transform, "TextoEstadoFirebase", "Firebase: conectado",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 45f), new Vector2(700f, 45f), 26, TextAlignmentOptions.Center, Color.green);

        GameObject uiObj = new GameObject("UIManager");
        UIManager ui = uiObj.AddComponent<UIManager>();
        ui.textoMonedas = txtMonedas;
        ui.textoEstadoFirebase = txtFb;

        Button btnMenu = CreateButton(canvas.transform, "BtnMenu", "VOLVER AL MENU", new Vector2(0f, -70f), new Color(0f, 0.72f, 0.85f, 1f));
        Button btnSalir = CreateButton(canvas.transform, "BtnSalir", "SALIR", new Vector2(0f, -155f), new Color(0.35f, 0.15f, 0.2f, 1f));

        UnityEventTools.AddPersistentListener(btnMenu.onClick, ui.VolverMenu);
        UnityEventTools.AddPersistentListener(btnSalir.onClick, ui.Salir);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Resultados.unity");
    }

    static void CreatePlatform(string name, Vector2 pos, Vector2 scale, Sprite sprite, Color color, PhysicsMaterial2D mat, int layer)
    {
        GameObject go = new GameObject(name);
        go.layer = layer;
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.sharedMaterial = mat;
    }

    static Canvas CreateCanvasAndEventSystem()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        return canvas;
    }

    static TMP_Text CreateTMPText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        return tmp;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(360f, 62f);

        Image img = go.AddComponent<Image>();
        img.color = bgColor;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        CreateTMPText(go.transform, "Label", label,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 56f), 26, TextAlignmentOptions.Center, Color.white);

        return btn;
    }

    static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MenuPrincipal.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Nivel1.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Nivel2.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Resultados.unity", true)
        };
    }
}
