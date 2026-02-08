using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SnakeGame : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int gridWidth = 17;
    [SerializeField] private int gridHeight = 17;

    [Header("Gameplay")]
    [SerializeField] private float startSpeed = 0.14f;
    [SerializeField] private float maxSpeed = 0.055f;
    [SerializeField] private float speedStep = 0.002f;
    [SerializeField] private int initialLength = 4;

    [Header("Fog of War")]
    [SerializeField] private float fogInnerRadius = 4f;
    [SerializeField] private float fogOuterRadius = 7f;
    [SerializeField] private float fogDarkness = 0.12f;

    // Snake state
    private readonly List<Vector2Int> snake = new List<Vector2Int>();
    private readonly List<Transform> segments = new List<Transform>();
    private Vector2Int direction;
    private Vector2Int nextDirection;
    private float moveInterval;
    private float moveTimer;
    private int snakeLength; // persists across levels

    // Food
    private Vector2Int foodPos;
    private Transform foodTr;

    // Game state
    private int score;
    private bool alive;
    private bool started;
    private bool paused;

    // Level/Progress
    private int level;
    private int foodThisLevel;
    private bool inTransition;
    private float transitionTimer;

    // Obstacles
    private readonly List<Vector2Int> obstacles = new List<Vector2Int>();
    private readonly List<Transform> obstacleVisuals = new List<Transform>();

    // Materials
    private Material matHead, matBody, matFood, matWall;
    private Material matEye, matPupil, matObstacle;

    // Head composite
    private Transform headRoot;

    // Tile tracking for fog of war
    private Renderer[,] tileRenderers;
    private Color[,] tileBaseColors;

    // UI
    private Text scoreText, messageText, levelText, progressText;
    private GameObject messagePanel, pausePanel;
    private Text pauseText;
    private Image progressFill;

    // Tile palette
    private static readonly Color[] Palette =
    {
        new Color(0.06f, 0.30f, 0.50f),
        new Color(0.08f, 0.38f, 0.55f),
        new Color(0.06f, 0.44f, 0.50f),
        new Color(0.08f, 0.50f, 0.46f),
        new Color(0.10f, 0.55f, 0.40f),
        new Color(0.13f, 0.50f, 0.36f),
        new Color(0.14f, 0.42f, 0.52f),
        new Color(0.10f, 0.36f, 0.58f),
    };

    // ===================== LIFECYCLE =====================

    private void Start()
    {
        CreateMaterials();
        BuildGrid();
        BuildWalls();
        CreateLight();
        PositionCamera();
        CreateUI();
        NewGame();
    }

    private void Update()
    {
        if (!alive)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                NewGame();
            return;
        }

        if (inTransition)
        {
            transitionTimer -= Time.deltaTime;
            if (transitionTimer <= 0f)
            {
                inTransition = false;
                StartLevel();
            }
            return;
        }

        if (!started)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)
                || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)
                || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A)
                || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
            {
                started = true;
                messagePanel.SetActive(false);
            }
            else return;
        }

        // Pause toggle
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            paused = !paused;
            pausePanel.SetActive(paused);
            return;
        }

        if (paused) return;

        HandleInput();
        AnimateFood();

        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0f)
        {
            moveTimer += moveInterval;
            Tick();
        }
    }

    // ===================== INPUT =====================

    private void HandleInput()
    {
        Vector2Int d = nextDirection;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            d = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            d = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            d = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            d = Vector2Int.right;

        if (d + direction != Vector2Int.zero)
            nextDirection = d;
    }

    // ===================== GAME LOGIC =====================

    private int FoodForLevel(int lvl) => 4 + lvl;

    private float SpeedForLevel(int lvl)
    {
        return Mathf.Max(maxSpeed, startSpeed - (lvl - 1) * 0.012f);
    }

    private int ObstacleCount(int lvl)
    {
        if (lvl <= 1) return 0;
        return (lvl - 1) * 3; // level 2 = 3, level 3 = 6, level 4 = 9...
    }

    private void NewGame()
    {
        score = 0;
        level = 1;
        foodThisLevel = 0;
        inTransition = false;
        alive = true;
        started = false;
        paused = false;
        snakeLength = initialLength;
        pausePanel.SetActive(false);

        ClearObstacles();
        ResetSnake();
        SpawnFood();
        SyncVisuals();
        UpdateHUD();
        ShowMessage("SNAKE 3D\n\nWASD / Arrow Keys\nPress any key to start");
    }

    private void StartLevel()
    {
        ClearObstacles();
        ResetSnake();
        PlaceObstacles(ObstacleCount(level));
        SpawnFood();
        SyncVisuals();
        UpdateHUD();
        messagePanel.SetActive(false);
        started = true;
    }

    private void NextLevel()
    {
        level++;
        foodThisLevel = 0;
        inTransition = true;
        transitionTimer = 2f;
        ShowMessage($"LEVEL {level}\n\nGet ready!");
    }

    private void ResetSnake()
    {
        foreach (var seg in segments)
            if (seg) Destroy(seg.gameObject);
        segments.Clear();
        snake.Clear();
        if (headRoot) { Destroy(headRoot.gameObject); headRoot = null; }
        if (foodTr) Destroy(foodTr.gameObject);
        foodTr = null;

        direction = Vector2Int.right;
        nextDirection = Vector2Int.right;
        Vector2Int center = new Vector2Int(gridWidth / 2, gridHeight / 2);
        int len = Mathf.Min(snakeLength, gridWidth / 2);
        for (int i = 0; i < len; i++)
            snake.Add(center + Vector2Int.left * i);

        CreateHead();
        for (int i = 1; i < snake.Count; i++)
            AddSegment();

        moveInterval = SpeedForLevel(level);
        moveTimer = moveInterval;
    }

    private void Tick()
    {
        direction = nextDirection;
        Vector2Int head = snake[0] + direction;

        // Wall collision
        if (head.x < 0 || head.x >= gridWidth || head.y < 0 || head.y >= gridHeight)
        { Die(); return; }

        // Obstacle collision
        if (obstacles.Contains(head))
        { Die(); return; }

        // Self collision
        bool eating = head == foodPos;
        int checkLimit = eating ? snake.Count : snake.Count - 1;
        for (int i = 0; i < checkLimit; i++)
            if (snake[i] == head) { Die(); return; }

        snake.Insert(0, head);

        if (eating)
        {
            score += 10 * level;
            moveInterval = Mathf.Max(maxSpeed, moveInterval - speedStep);
            AddSegment();
            snakeLength++;
            foodThisLevel++;
            UpdateHUD();

            if (foodThisLevel >= FoodForLevel(level))
            {
                NextLevel();
                return;
            }
            SpawnFood();
        }
        else
        {
            snake.RemoveAt(snake.Count - 1);
        }

        SyncVisuals();
    }

    private void Die()
    {
        alive = false;
        Color dark = new Color(0.25f, 0.04f, 0.04f);
        foreach (var seg in segments)
        {
            var renderers = seg.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.material.SetColor("_BaseColor", dark);
                r.material.SetColor("_Color", dark);
            }
        }
        // Reveal full grid on death
        RevealAllTiles();
        ShowMessage($"GAME OVER\n\nLevel: {level}  Score: {score}\n\nPress SPACE to restart");
    }

    private void SpawnFood()
    {
        var blocked = new HashSet<Vector2Int>(snake);
        foreach (var o in obstacles) blocked.Add(o);

        var free = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                var p = new Vector2Int(x, y);
                if (!blocked.Contains(p)) free.Add(p);
            }

        if (free.Count == 0)
        {
            alive = false;
            ShowMessage($"YOU WIN!\n\nScore: {score}\n\nPress SPACE to play again");
            return;
        }

        foodPos = free[Random.Range(0, free.Count)];

        if (!foodTr)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Food";
            go.transform.localScale = Vector3.one * 0.5f;
            go.GetComponent<Renderer>().sharedMaterial = matFood;
            Destroy(go.GetComponent<Collider>());
            foodTr = go.transform;
        }
        foodTr.position = GridToWorld(foodPos, 0.32f);
    }

    // ===================== OBSTACLES =====================

    private void PlaceObstacles(int count)
    {
        // Build set of positions to avoid (snake start area)
        var blocked = new HashSet<Vector2Int>(snake);
        // Also block adjacent cells to give the player room
        Vector2Int center = new Vector2Int(gridWidth / 2, gridHeight / 2);
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
                blocked.Add(center + new Vector2Int(dx, dy));

        var candidates = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                var p = new Vector2Int(x, y);
                if (!blocked.Contains(p)) candidates.Add(p);
            }

        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            int idx = Random.Range(0, candidates.Count);
            Vector2Int pos = candidates[idx];
            candidates.RemoveAt(idx);
            obstacles.Add(pos);

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Obstacle";
            go.transform.position = GridToWorld(pos, 0.22f);
            go.transform.localScale = new Vector3(0.9f, 0.45f, 0.9f);
            go.GetComponent<Renderer>().sharedMaterial = matObstacle;
            Destroy(go.GetComponent<Collider>());
            obstacleVisuals.Add(go.transform);
        }
    }

    private void ClearObstacles()
    {
        foreach (var o in obstacleVisuals)
            if (o) Destroy(o.gameObject);
        obstacleVisuals.Clear();
        obstacles.Clear();
    }

    // ===================== VISUALS =====================

    private void CreateHead()
    {
        headRoot = new GameObject("SnakeHead").transform;

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "HeadBody";
        body.transform.SetParent(headRoot, false);
        body.transform.localScale = new Vector3(0.82f, 0.48f, 0.92f);
        body.GetComponent<Renderer>().sharedMaterial = matHead;
        Destroy(body.GetComponent<Collider>());

        var snout = GameObject.CreatePrimitive(PrimitiveType.Cube);
        snout.name = "Snout";
        snout.transform.SetParent(headRoot, false);
        snout.transform.localPosition = new Vector3(0f, -0.04f, 0.42f);
        snout.transform.localScale = new Vector3(0.5f, 0.3f, 0.2f);
        snout.GetComponent<Renderer>().sharedMaterial = matHead;
        Destroy(snout.GetComponent<Collider>());

        MakeEye(headRoot, new Vector3(-0.22f, 0.22f, 0.28f));
        MakeEye(headRoot, new Vector3(0.22f, 0.22f, 0.28f));

        segments.Insert(0, headRoot);
    }

    private void MakeEye(Transform parent, Vector3 localPos)
    {
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Eye";
        eye.transform.SetParent(parent, false);
        eye.transform.localPosition = localPos;
        eye.transform.localScale = Vector3.one * 0.18f;
        eye.GetComponent<Renderer>().sharedMaterial = matEye;
        Destroy(eye.GetComponent<Collider>());

        var pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pupil.name = "Pupil";
        pupil.transform.SetParent(parent, false);
        pupil.transform.localPosition = localPos + new Vector3(0f, 0.02f, 0.06f);
        pupil.transform.localScale = Vector3.one * 0.1f;
        pupil.GetComponent<Renderer>().sharedMaterial = matPupil;
        Destroy(pupil.GetComponent<Collider>());
    }

    private void AddSegment()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Snake_" + segments.Count;
        go.transform.localScale = new Vector3(0.82f, 0.42f, 0.82f);
        go.GetComponent<Renderer>().sharedMaterial = matBody;
        Destroy(go.GetComponent<Collider>());
        segments.Add(go.transform);
    }

    private void SyncVisuals()
    {
        for (int i = 0; i < snake.Count && i < segments.Count; i++)
        {
            segments[i].position = GridToWorld(snake[i], 0.24f);

            if (i == 0)
            {
                float angle = 0f;
                if (direction == Vector2Int.up) angle = 0f;
                else if (direction == Vector2Int.down) angle = 180f;
                else if (direction == Vector2Int.left) angle = 270f;
                else if (direction == Vector2Int.right) angle = 90f;
                segments[i].rotation = Quaternion.Euler(0, angle, 0);
            }
            else
            {
                float t = (float)i / Mathf.Max(1, snake.Count - 1);
                float s = Mathf.Lerp(0.82f, 0.65f, t);
                segments[i].localScale = new Vector3(s, 0.42f, s);
                segments[i].rotation = Quaternion.identity;
            }
        }

        UpdateFog();
    }

    private void AnimateFood()
    {
        if (!foodTr) return;
        Vector3 pos = GridToWorld(foodPos, 0.32f);
        pos.y += Mathf.Sin(Time.time * 3.5f) * 0.08f;
        foodTr.position = pos;
        foodTr.Rotate(Vector3.up, 100f * Time.deltaTime);
    }

    // ===================== FOG OF WAR =====================

    private void UpdateFog()
    {
        if (tileRenderers == null || snake.Count == 0) return;

        Vector2 headPos = new Vector2(snake[0].x, snake[0].y);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (tileRenderers[x, y] == null) continue;

                float dist = Vector2.Distance(headPos, new Vector2(x, y));
                float visibility;

                if (dist <= fogInnerRadius)
                    visibility = 1f;
                else if (dist >= fogOuterRadius)
                    visibility = fogDarkness;
                else
                    visibility = Mathf.Lerp(1f, fogDarkness, (dist - fogInnerRadius) / (fogOuterRadius - fogInnerRadius));

                Color baseCol = tileBaseColors[x, y];
                Color foggedCol = baseCol * visibility;
                foggedCol.a = 1f;

                var mat = tileRenderers[x, y].material;
                mat.SetColor("_BaseColor", foggedCol);
                mat.SetColor("_Color", foggedCol);
            }
        }

        // Also dim/show obstacles based on fog
        for (int i = 0; i < obstacles.Count; i++)
        {
            if (i >= obstacleVisuals.Count || !obstacleVisuals[i]) continue;
            float dist = Vector2.Distance(headPos, new Vector2(obstacles[i].x, obstacles[i].y));
            float vis = dist <= fogInnerRadius ? 1f :
                        dist >= fogOuterRadius ? fogDarkness :
                        Mathf.Lerp(1f, fogDarkness, (dist - fogInnerRadius) / (fogOuterRadius - fogInnerRadius));

            Color obsBase = new Color(0.35f, 0.15f, 0.5f);
            Color c = obsBase * vis;
            c.a = 1f;
            var mat = obstacleVisuals[i].GetComponent<Renderer>().material;
            mat.SetColor("_BaseColor", c);
            mat.SetColor("_Color", c);
        }

        // Dim/show food based on fog
        if (foodTr)
        {
            float fdist = Vector2.Distance(headPos, new Vector2(foodPos.x, foodPos.y));
            float fvis = fdist <= fogInnerRadius ? 1f :
                         fdist >= fogOuterRadius ? 0f :
                         Mathf.Lerp(1f, 0f, (fdist - fogInnerRadius) / (fogOuterRadius - fogInnerRadius));
            // Scale food visibility (hide if in deep fog)
            foodTr.localScale = Vector3.one * (0.5f * fvis);
        }
    }

    private void RevealAllTiles()
    {
        if (tileRenderers == null) return;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (tileRenderers[x, y] == null) continue;
                Color c = tileBaseColors[x, y];
                var mat = tileRenderers[x, y].material;
                mat.SetColor("_BaseColor", c);
                mat.SetColor("_Color", c);
            }
        }
        // Show all obstacles
        for (int i = 0; i < obstacleVisuals.Count; i++)
        {
            if (!obstacleVisuals[i]) continue;
            Color obsCol = new Color(0.35f, 0.15f, 0.5f);
            var mat = obstacleVisuals[i].GetComponent<Renderer>().material;
            mat.SetColor("_BaseColor", obsCol);
            mat.SetColor("_Color", obsCol);
        }
        // Show food
        if (foodTr) foodTr.localScale = Vector3.one * 0.5f;
    }

    // ===================== SCENE SETUP =====================

    private void CreateMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");

        matHead = new Material(shader);
        SetCol(matHead, new Color(0.65f, 0.06f, 0.06f));
        matHead.SetFloat("_Smoothness", 0.8f);
        matHead.SetFloat("_Metallic", 0.15f);

        matBody = new Material(shader);
        SetCol(matBody, new Color(0.82f, 0.14f, 0.14f));
        matBody.SetFloat("_Smoothness", 0.7f);

        matFood = new Material(shader);
        SetCol(matFood, new Color(1f, 0.82f, 0.08f));
        matFood.SetFloat("_Smoothness", 0.85f);
        matFood.SetFloat("_Metallic", 0.3f);
        matFood.EnableKeyword("_EMISSION");
        matFood.SetColor("_EmissionColor", new Color(0.4f, 0.35f, 0.02f));

        matWall = new Material(shader);
        SetCol(matWall, new Color(0.15f, 0.15f, 0.22f));
        matWall.SetFloat("_Smoothness", 0.35f);

        matEye = new Material(shader);
        SetCol(matEye, Color.white);
        matEye.SetFloat("_Smoothness", 0.9f);

        matPupil = new Material(shader);
        SetCol(matPupil, new Color(0.02f, 0.02f, 0.02f));
        matPupil.SetFloat("_Smoothness", 0.95f);

        matObstacle = new Material(shader);
        SetCol(matObstacle, new Color(0.35f, 0.15f, 0.5f));
        matObstacle.SetFloat("_Smoothness", 0.5f);
        matObstacle.SetFloat("_Metallic", 0.2f);
    }

    private void SetCol(Material mat, Color c)
    {
        mat.SetColor("_BaseColor", c);
        mat.SetColor("_Color", c);
    }

    private void BuildGrid()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");

        tileRenderers = new Renderer[gridWidth, gridHeight];
        tileBaseColors = new Color[gridWidth, gridHeight];

        Transform parent = new GameObject("Grid").transform;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Tile_{x}_{y}";
                tile.transform.parent = parent;
                tile.transform.position = new Vector3(x, -0.05f, y);
                tile.transform.localScale = new Vector3(0.94f, 0.1f, 0.94f);
                Destroy(tile.GetComponent<Collider>());

                float noise = Mathf.PerlinNoise(x * 0.25f + 0.1f, y * 0.25f + 0.1f);
                int idx = (int)(noise * Palette.Length) % Palette.Length;
                Color c = Palette[idx];
                float shift = ((x + y) % 2 == 0) ? 0.03f : -0.02f;
                c = new Color(
                    Mathf.Clamp01(c.r + shift),
                    Mathf.Clamp01(c.g + shift),
                    Mathf.Clamp01(c.b + shift));

                var mat = new Material(shader);
                SetCol(mat, c);
                mat.SetFloat("_Smoothness", 0.55f);
                tile.GetComponent<Renderer>().material = mat;

                tileRenderers[x, y] = tile.GetComponent<Renderer>();
                tileBaseColors[x, y] = c;
            }
        }
    }

    private void BuildWalls()
    {
        Transform parent = new GameObject("Walls").transform;
        float w = gridWidth, h = gridHeight;
        float wh = 0.45f, wt = 0.35f;
        float cx = (w - 1) / 2f, cz = (h - 1) / 2f;

        MakeWall(parent, new Vector3(cx, wh / 2f, -0.5f - wt / 2f),
            new Vector3(w + wt * 2, wh, wt));
        MakeWall(parent, new Vector3(cx, wh / 2f, h - 0.5f + wt / 2f),
            new Vector3(w + wt * 2, wh, wt));
        MakeWall(parent, new Vector3(-0.5f - wt / 2f, wh / 2f, cz),
            new Vector3(wt, wh, h));
        MakeWall(parent, new Vector3(w - 0.5f + wt / 2f, wh / 2f, cz),
            new Vector3(wt, wh, h));
    }

    private void MakeWall(Transform parent, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.parent = parent;
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = matWall;
        Destroy(wall.GetComponent<Collider>());
    }

    private void CreateLight()
    {
        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.9f);
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private void PositionCamera()
    {
        Camera cam = Camera.main;
        if (!cam) return;
        float cx = (gridWidth - 1) / 2f;
        float cz = (gridHeight - 1) / 2f;
        // Near top-down view, slightly tilted for depth
        cam.transform.position = new Vector3(cx, 30f, cz - 3f);
        cam.transform.LookAt(new Vector3(cx, 0f, cz + 0.5f));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.08f);
        cam.fieldOfView = 38f;
    }

    // ===================== UI =====================

    private void CreateUI()
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Score (top center)
        scoreText = MakeText(canvasGo.transform, "ScoreText", 38, TextAnchor.UpperCenter);
        var srt = scoreText.rectTransform;
        srt.anchorMin = new Vector2(0, 1);
        srt.anchorMax = Vector2.one;
        srt.pivot = new Vector2(0.5f, 1);
        srt.sizeDelta = new Vector2(0, 45);
        srt.anchoredPosition = new Vector2(0, -6);
        var outline = scoreText.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, 2);

        // Level text (top left)
        levelText = MakeText(canvasGo.transform, "LevelText", 30, TextAnchor.UpperLeft);
        var lrt = levelText.rectTransform;
        lrt.anchorMin = new Vector2(0, 1);
        lrt.anchorMax = new Vector2(0.3f, 1);
        lrt.pivot = new Vector2(0, 1);
        lrt.sizeDelta = new Vector2(0, 40);
        lrt.anchoredPosition = new Vector2(16, -6);
        var lol = levelText.gameObject.AddComponent<Outline>();
        lol.effectColor = Color.black;
        lol.effectDistance = new Vector2(1.5f, 1.5f);

        // Progress bar (top right)
        var barBg = new GameObject("ProgressBarBg");
        barBg.transform.SetParent(canvasGo.transform, false);
        var bgImg = barBg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        var bgrt = bgImg.rectTransform;
        bgrt.anchorMin = new Vector2(0.7f, 1);
        bgrt.anchorMax = new Vector2(0.96f, 1);
        bgrt.pivot = new Vector2(1, 1);
        bgrt.sizeDelta = new Vector2(0, 18);
        bgrt.anchoredPosition = new Vector2(0, -14);

        var barFill = new GameObject("ProgressBarFill");
        barFill.transform.SetParent(barBg.transform, false);
        progressFill = barFill.AddComponent<Image>();
        progressFill.color = new Color(0.2f, 0.85f, 0.3f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillAmount = 0f;
        var frt = progressFill.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(2, 2);
        frt.offsetMax = new Vector2(-2, -2);

        progressText = MakeText(barBg.transform, "ProgressText", 14, TextAnchor.MiddleCenter);
        var prt2 = progressText.rectTransform;
        prt2.anchorMin = Vector2.zero;
        prt2.anchorMax = Vector2.one;
        prt2.offsetMin = Vector2.zero;
        prt2.offsetMax = Vector2.zero;

        // Message panel
        messagePanel = new GameObject("MsgPanel");
        messagePanel.transform.SetParent(canvasGo.transform, false);
        var img = messagePanel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.78f);
        var prt = img.rectTransform;
        prt.anchorMin = new Vector2(0.18f, 0.22f);
        prt.anchorMax = new Vector2(0.82f, 0.78f);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        var msgGo = new GameObject("MsgText");
        msgGo.transform.SetParent(messagePanel.transform, false);
        messageText = msgGo.AddComponent<Text>();
        messageText.font = GetFont();
        messageText.fontSize = 28;
        messageText.color = Color.white;
        messageText.alignment = TextAnchor.MiddleCenter;
        var mrt = messageText.rectTransform;
        mrt.anchorMin = Vector2.zero;
        mrt.anchorMax = Vector2.one;
        mrt.offsetMin = new Vector2(20, 20);
        mrt.offsetMax = new Vector2(-20, -20);
        var msgOutline = msgGo.AddComponent<Outline>();
        msgOutline.effectColor = Color.black;
        msgOutline.effectDistance = new Vector2(1, 1);

        // Pause overlay
        pausePanel = new GameObject("PausePanel");
        pausePanel.transform.SetParent(canvasGo.transform, false);
        var pauseImg = pausePanel.AddComponent<Image>();
        pauseImg.color = new Color(0, 0, 0, 0.6f);
        var pauseRt = pauseImg.rectTransform;
        pauseRt.anchorMin = Vector2.zero;
        pauseRt.anchorMax = Vector2.one;
        pauseRt.offsetMin = Vector2.zero;
        pauseRt.offsetMax = Vector2.zero;

        var pauseGo = new GameObject("PauseText");
        pauseGo.transform.SetParent(pausePanel.transform, false);
        pauseText = pauseGo.AddComponent<Text>();
        pauseText.font = GetFont();
        pauseText.fontSize = 48;
        pauseText.color = Color.white;
        pauseText.alignment = TextAnchor.MiddleCenter;
        pauseText.text = "PAUSED\n\n<size=24>Press P or ESC to resume</size>";
        var pauseTextRt = pauseText.rectTransform;
        pauseTextRt.anchorMin = Vector2.zero;
        pauseTextRt.anchorMax = Vector2.one;
        pauseTextRt.offsetMin = Vector2.zero;
        pauseTextRt.offsetMax = Vector2.zero;
        var pauseOl = pauseGo.AddComponent<Outline>();
        pauseOl.effectColor = Color.black;
        pauseOl.effectDistance = new Vector2(2, 2);
        pausePanel.SetActive(false);
    }

    private Text MakeText(Transform parent, string name, int size, TextAnchor anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = GetFont();
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = anchor;
        return t;
    }

    private Font GetFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (!f) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    private void UpdateHUD()
    {
        scoreText.text = $"SCORE  {score:D6}";
        levelText.text = $"LEVEL {level}";
        int needed = FoodForLevel(level);
        progressFill.fillAmount = (float)foodThisLevel / needed;
        progressText.text = $"{foodThisLevel}/{needed}";
    }

    private void ShowMessage(string text)
    {
        messageText.text = text;
        messagePanel.SetActive(true);
    }

    // ===================== UTILITY =====================

    private Vector3 GridToWorld(Vector2Int gridPos, float height)
    {
        return new Vector3(gridPos.x, height, gridPos.y);
    }
}
