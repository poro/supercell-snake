using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SnakeGame : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int gridWidth = 17;
    [SerializeField] private int gridHeight = 17;

    [Header("Gameplay")]
    [SerializeField] private float startSpeed = 0.28f;
    [SerializeField] private float maxSpeed = 0.12f;
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

    // Food (multiple)
    private readonly List<Vector2Int> foodPositions = new List<Vector2Int>();
    private readonly List<Transform> foodTransforms = new List<Transform>();
    private int totalFoodThisLevel;
    private int foodEatenThisLevel;

    // Game state
    private int score;
    private bool alive;
    private bool started;
    private bool paused;

    // Score multiplier
    private float scoreMultiplier = 1f;
    private float multiplierTimer;

    // Level/Progress
    private int level;
    private bool inTransition;
    private float transitionTimer;

    // Obstacles
    private readonly List<Vector2Int> obstacles = new List<Vector2Int>();
    private readonly List<Transform> obstacleVisuals = new List<Transform>();

    // Mines
    private bool[,] mineGrid;
    private int[,] mineCountGrid;
    private bool[,] craterGrid;
    private TextMesh[,] numberHints;
    private readonly List<Vector2Int> minePositions = new List<Vector2Int>();
    private readonly HashSet<int> shrinkCooldown = new HashSet<int>();
    private readonly List<Transform> mineRevealMarkers = new List<Transform>();
    private readonly List<Transform> mineVisuals = new List<Transform>();

    // Effects
    private float freezeTimer;
    private float shakeTimer;
    private Vector3 cameraBasePos;

    // Audio
    private AudioSource audioSource;
    private AudioClip clipExplosion, clipEat, clipShrink, clipDeath;
    private AudioSource heartbeatSource;
    private AudioSource musicSource;
    private AudioClip clipHeartbeat;
    private float fogPenaltyTimer;
    private float panicTimer;
    private float baseFogInner;
    private float baseFogOuter;

    // Materials
    private Material matHead, matBody, matFood, matWall;
    private Material matEye, matPupil, matObstacle, matMine;

    // Head composite
    private Transform headRoot;

    // Tile tracking for fog of war
    private Renderer[,] tileRenderers;
    private Color[,] tileBaseColors;
    private Color[,] originalTileColors;

    // UI
    private Text scoreText, messageText, levelText, progressText, multiplierText;
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
        baseFogInner = fogInnerRadius;
        baseFogOuter = fogOuterRadius;
        CreateMaterials();
        CreateSounds();
        BuildGrid();
        BuildWalls();
        CreateLight();
        PositionCamera();
        CreateUI();
        NewGame();
    }

    private void Update()
    {
        // Screen shake (runs even during freeze)
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            Camera cam = Camera.main;
            if (cam)
            {
                if (shakeTimer > 0f)
                {
                    float intensity = shakeTimer * 2f;
                    cam.transform.position = cameraBasePos + Random.insideUnitSphere * intensity * 0.15f;
                }
                else
                {
                    cam.transform.position = cameraBasePos;
                }
            }
        }

        // Heartbeat proximity (runs even during freeze for drama)
        UpdateHeartbeat();

        // Freeze frame (explosion pause)
        if (freezeTimer > 0f)
        {
            freezeTimer -= Time.deltaTime;
            return;
        }

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

        // Fog penalty restoration
        if (fogPenaltyTimer > 0f)
        {
            fogPenaltyTimer -= Time.deltaTime;
            if (fogPenaltyTimer <= 0f)
            {
                fogInnerRadius = baseFogInner;
                fogOuterRadius = baseFogOuter;
            }
            else if (fogPenaltyTimer < 2f)
            {
                float restoreT = 1f - (fogPenaltyTimer / 2f);
                fogInnerRadius = Mathf.Lerp(baseFogInner * 0.45f, baseFogInner, restoreT);
                fogOuterRadius = Mathf.Lerp(baseFogOuter * 0.55f, baseFogOuter, restoreT);
            }
        }

        // Panic timer
        if (panicTimer > 0f)
            panicTimer -= Time.deltaTime;

        // Score multiplier countdown
        if (multiplierTimer > 0f)
        {
            multiplierTimer -= Time.deltaTime;
            if (multiplierTimer <= 0f)
            {
                scoreMultiplier = 1f;
                UpdateHUD();
            }
        }

        HandleInput();
        AnimateFood();

        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0f)
        {
            float effectiveInterval = panicTimer > 0f ? moveInterval * 0.5f : moveInterval;
            moveTimer += effectiveInterval;
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

    // ===================== GAME CONFIG =====================

    private int FoodForLevel(int lvl) => Mathf.Min(12, 4 + lvl);

    private int MineCountForLevel(int lvl)
    {
        if (lvl <= 1) return 4;
        if (lvl <= 3) return 6;
        if (lvl <= 6) return 10;
        return 14;
    }

    private float SpeedForLevel(int lvl)
    {
        return Mathf.Max(maxSpeed, startSpeed - (lvl - 1) * 0.02f);
    }

    private int ObstacleCount(int lvl)
    {
        if (lvl <= 1) return 0;
        return Mathf.Min(35, (lvl - 1) * 3);
    }

    // ===================== GAME LOGIC =====================

    private void NewGame()
    {
        score = 0;
        level = 1;
        foodEatenThisLevel = 0;
        inTransition = false;
        alive = true;
        started = false;
        paused = false;
        pausePanel.SetActive(false);
        fogPenaltyTimer = 0f;
        panicTimer = 0f;
        fogInnerRadius = baseFogInner;
        fogOuterRadius = baseFogOuter;
        if (heartbeatSource) heartbeatSource.volume = 0f;
        scoreMultiplier = 1f;
        multiplierTimer = 0f;

        ResetSnake();
        GenerateLayout();
        SyncVisuals();
        UpdateHUD();
        ShowMessage("SNAKE SWEEPER\n\nWASD / Arrow Keys\nRead the numbers, avoid the mines!\nPress any key to start");
    }

    private void StartLevel()
    {
        fogPenaltyTimer = 0f;
        panicTimer = 0f;
        fogInnerRadius = baseFogInner;
        fogOuterRadius = baseFogOuter;
        ResetSnake();
        GenerateLayout();
        SyncVisuals();
        UpdateHUD();
        messagePanel.SetActive(false);
        started = true;
    }

    private void NextLevel()
    {
        int bonus = 50 * level;
        score += bonus;

        if (level == 10)
        {
            // Victory! Player cleared all 10 levels
            level++;
            foodEatenThisLevel = 0;
            inTransition = true;
            transitionTimer = 5f;
            ShowMessage($"YOU WIN!\n\nAll 10 levels cleared!\nFinal Score: {score}\n\n...entering ENDLESS mode");
            return;
        }

        level++;
        foodEatenThisLevel = 0;
        inTransition = true;
        transitionTimer = 2f;
        ShowMessage(level > 10
            ? $"ENDLESS  Level {level}\n\nBonus: +{bonus}\nGet ready!"
            : $"LEVEL {level}\n\nBonus: +{bonus}\nGet ready!");
    }

    private void GenerateLayout()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            ClearObstacles();
            ClearMines();
            ClearFood();
            ClearRevealMarkers();
            PlaceMines(MineCountForLevel(level));
            PlaceObstacles(ObstacleCount(level));
            SpawnAllFood();
            if (AllFoodReachable()) return;
        }
        // Last attempt used as-is (caps prevent truly impossible layouts)
    }

    private bool AllFoodReachable()
    {
        if (foodPositions.Count == 0) return true;

        // BFS from snake start — mines don't block, only obstacles and walls
        Vector2Int start = new Vector2Int(gridWidth / 2, gridHeight / 2);
        bool[,] visited = new bool[gridWidth, gridHeight];

        // Mark obstacles as impassable
        foreach (var o in obstacles)
            visited[o.x, o.y] = true;

        var queue = new Queue<Vector2Int>();
        if (!visited[start.x, start.y])
        {
            queue.Enqueue(start);
            visited[start.x, start.y] = true;
        }

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in dirs)
            {
                Vector2Int next = pos + dir;
                if (next.x < 0 || next.x >= gridWidth || next.y < 0 || next.y >= gridHeight) continue;
                if (visited[next.x, next.y]) continue;
                visited[next.x, next.y] = true;
                queue.Enqueue(next);
            }
        }

        // Every food tile must be reachable
        foreach (var fp in foodPositions)
            if (!visited[fp.x, fp.y]) return false;

        return true;
    }

    private void ResetSnake()
    {
        foreach (var seg in segments)
            if (seg) Destroy(seg.gameObject);
        segments.Clear();
        snake.Clear();
        if (headRoot) { Destroy(headRoot.gameObject); headRoot = null; }

        direction = Vector2Int.right;
        nextDirection = Vector2Int.right;
        Vector2Int center = new Vector2Int(gridWidth / 2, gridHeight / 2);
        for (int i = 0; i < initialLength; i++)
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

        // Self collision (check all except last which will be removed in normal movement)
        for (int i = 0; i < snake.Count - 1; i++)
            if (snake[i] == head) { Die(); return; }

        // Move head
        snake.Insert(0, head);

        // === MINE HIT ===
        if (mineGrid[head.x, head.y])
        {
            HandleMineHit(head);
            // Grow by 3: head already inserted (+1), don't remove tail, add 2 extra tail copies
            snake.Add(snake[snake.Count - 1]);
            snake.Add(snake[snake.Count - 1]);
            for (int i = 0; i < 3; i++)
                AddSegment();
            score = Mathf.Max(0, score - 15 * level);
            UpdateHUD();
            SyncVisuals();
            return; // skip food/shrink this tick
        }

        // === NUMBER HINT MULTIPLIER ===
        if (!mineGrid[head.x, head.y] && mineCountGrid[head.x, head.y] > 0)
        {
            int count = mineCountGrid[head.x, head.y];
            if (count == 1)
            {
                // Flat bonus for passing a "1" tile
                score += (int)(15 * level * scoreMultiplier);
            }
            else
            {
                // Count 2/3/4 = score multiplier for 5 seconds
                scoreMultiplier = count;
                multiplierTimer = 5f;
            }
            UpdateHUD();
        }

        // === FOOD CHECK ===
        int foodIdx = -1;
        for (int i = 0; i < foodPositions.Count; i++)
        {
            if (foodPositions[i] == head)
            {
                foodIdx = i;
                break;
            }
        }

        bool eating = foodIdx >= 0;
        if (eating)
        {
            EatFood(foodIdx);
            AddSegment();
            score += (int)(10 * level * scoreMultiplier);
            foodEatenThisLevel++;
            UpdateHUD();
            if (audioSource && clipEat) audioSource.PlayOneShot(clipEat, 1f);
        }
        else
        {
            snake.RemoveAt(snake.Count - 1);
        }

        // === MINE AVOIDANCE (SHRINK) ===
        int shrinks = CheckMineAvoidance(head);
        for (int i = 0; i < shrinks; i++)
        {
            if (snake.Count <= 1) break;
            Vector3 tailPos = GridToWorld(snake[snake.Count - 1], 0.24f);
            snake.RemoveAt(snake.Count - 1);
            if (segments.Count > snake.Count)
            {
                var lastSeg = segments[segments.Count - 1];
                if (lastSeg) Destroy(lastSeg.gameObject);
                segments.RemoveAt(segments.Count - 1);
            }
            StartCoroutine(ShrinkSparkle(tailPos));
            if (audioSource && clipShrink) audioSource.PlayOneShot(clipShrink, 0.4f);
            score += (int)(25 * level * scoreMultiplier);
        }
        if (shrinks > 0) UpdateHUD();

        // === MINE ENCIRCLEMENT ===
        CheckMineEncirclement();

        SyncVisuals();

        // === WIN CHECK ===
        if (foodPositions.Count == 0 && foodEatenThisLevel >= totalFoodThisLevel)
        {
            NextLevel();
        }
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
        RevealAllTiles();
        RevealMines();
        if (audioSource && clipDeath) audioSource.PlayOneShot(clipDeath, 0.9f);
        if (heartbeatSource) heartbeatSource.volume = 0f;
        ShowMessage($"GAME OVER\n\nLevel: {level}  Score: {score}\n\nPress SPACE to restart");
    }

    // ===================== FOOD =====================

    private void SpawnAllFood()
    {
        totalFoodThisLevel = FoodForLevel(level);

        var blocked = new HashSet<Vector2Int>(snake);
        foreach (var o in obstacles) blocked.Add(o);
        foreach (var m in minePositions) blocked.Add(m);

        var free = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                var p = new Vector2Int(x, y);
                if (!blocked.Contains(p)) free.Add(p);
            }

        for (int i = 0; i < totalFoodThisLevel && free.Count > 0; i++)
        {
            int idx = Random.Range(0, free.Count);
            Vector2Int pos = free[idx];
            free.RemoveAt(idx);
            foodPositions.Add(pos);

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Food_" + i;
            go.transform.localScale = Vector3.one * 0.5f;
            go.GetComponent<Renderer>().sharedMaterial = matFood;
            Destroy(go.GetComponent<Collider>());
            go.transform.position = GridToWorld(pos, 0.32f);
            foodTransforms.Add(go.transform);
        }
    }

    private void EatFood(int idx)
    {
        if (idx < 0 || idx >= foodPositions.Count) return;
        if (foodTransforms[idx]) Destroy(foodTransforms[idx].gameObject);
        foodPositions.RemoveAt(idx);
        foodTransforms.RemoveAt(idx);
    }

    private void ClearFood()
    {
        foreach (var ft in foodTransforms)
            if (ft) Destroy(ft.gameObject);
        foodTransforms.Clear();
        foodPositions.Clear();
        foodEatenThisLevel = 0;
    }

    // ===================== MINES =====================

    private void PlaceMines(int count)
    {
        mineGrid = new bool[gridWidth, gridHeight];
        mineCountGrid = new int[gridWidth, gridHeight];
        craterGrid = new bool[gridWidth, gridHeight];
        minePositions.Clear();
        shrinkCooldown.Clear();

        var blocked = new HashSet<Vector2Int>(snake);
        Vector2Int center = new Vector2Int(gridWidth / 2, gridHeight / 2);
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
                blocked.Add(center + new Vector2Int(dx, dy));
        foreach (var o in obstacles) blocked.Add(o);

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
            mineGrid[pos.x, pos.y] = true;
            minePositions.Add(pos);
        }

        // Create mine visuals
        foreach (var mp in minePositions)
            mineVisuals.Add(CreateMineVisual(mp));

        CalculateMineCounts();
        UpdateAllNumberHints();
    }

    private void CalculateMineCounts()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                mineCountGrid[x, y] = CountAdjacentMines(x, y);
    }

    private int CountAdjacentMines(int x, int y)
    {
        int count = 0;
        if (x > 0 && mineGrid[x - 1, y]) count++;
        if (x < gridWidth - 1 && mineGrid[x + 1, y]) count++;
        if (y > 0 && mineGrid[x, y - 1]) count++;
        if (y < gridHeight - 1 && mineGrid[x, y + 1]) count++;
        return count;
    }

    private void HandleMineHit(Vector2Int pos)
    {
        mineGrid[pos.x, pos.y] = false;
        craterGrid[pos.x, pos.y] = true;

        // Destroy mine visual
        int mIdx = minePositions.IndexOf(pos);
        if (mIdx >= 0 && mIdx < mineVisuals.Count && mineVisuals[mIdx])
        {
            Destroy(mineVisuals[mIdx].gameObject);
            mineVisuals[mIdx] = null;
        }

        // Recalculate counts around detonated mine
        RecalculateCountsAround(pos.x, pos.y);
        UpdateAllNumberHints();

        // Crater visual
        if (tileRenderers[pos.x, pos.y])
        {
            Color craterColor = new Color(0.08f, 0.06f, 0.04f);
            tileBaseColors[pos.x, pos.y] = craterColor;
            var mat = tileRenderers[pos.x, pos.y].material;
            mat.SetColor("_BaseColor", craterColor);
            mat.SetColor("_Color", craterColor);
        }

        // Effects
        StartCoroutine(ExplosionEffect(GridToWorld(pos, 0.3f)));
        freezeTimer = 0.3f;
        shakeTimer = 0.2f;
        if (audioSource && clipExplosion) audioSource.PlayOneShot(clipExplosion, 0.8f);

        // Fog shrinks — reduced vision after mine hit
        fogPenaltyTimer = 5f;
        fogInnerRadius = baseFogInner * 0.45f;
        fogOuterRadius = baseFogOuter * 0.55f;

        // Panic mode — speed boost
        panicTimer = 3.5f;

        // Chain reaction — adjacent mines may detonate
        StartCoroutine(ChainReaction(pos));
    }

    private void RecalculateCountsAround(int cx, int cy)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                int x = cx + dx, y = cy + dy;
                if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                    mineCountGrid[x, y] = CountAdjacentMines(x, y);
            }
    }

    private int CheckMineAvoidance(Vector2Int headPos)
    {
        // Remove mines from cooldown that are no longer adjacent
        var toRemove = new List<int>();
        foreach (int idx in shrinkCooldown)
        {
            if (idx >= minePositions.Count) { toRemove.Add(idx); continue; }
            Vector2Int mp = minePositions[idx];
            if (!mineGrid[mp.x, mp.y]) { toRemove.Add(idx); continue; }
            int dx = Mathf.Abs(mp.x - headPos.x);
            int dy = Mathf.Abs(mp.y - headPos.y);
            if (dx + dy != 1) toRemove.Add(idx);
        }
        foreach (int idx in toRemove) shrinkCooldown.Remove(idx);

        // Check for new adjacent mines
        int shrinks = 0;
        for (int i = 0; i < minePositions.Count; i++)
        {
            if (shrinkCooldown.Contains(i)) continue;
            Vector2Int mp = minePositions[i];
            if (!mineGrid[mp.x, mp.y]) continue;
            int dx = Mathf.Abs(mp.x - headPos.x);
            int dy = Mathf.Abs(mp.y - headPos.y);
            if (dx + dy == 1)
            {
                shrinks++;
                shrinkCooldown.Add(i);
            }
        }
        return shrinks;
    }

    private void ClearMines()
    {
        foreach (var mv in mineVisuals)
            if (mv) Destroy(mv.gameObject);
        mineVisuals.Clear();
        minePositions.Clear();
        shrinkCooldown.Clear();

        if (mineGrid != null)
        {
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                {
                    mineGrid[x, y] = false;
                    mineCountGrid[x, y] = 0;
                    if (craterGrid[x, y])
                    {
                        craterGrid[x, y] = false;
                        if (originalTileColors != null)
                        {
                            tileBaseColors[x, y] = originalTileColors[x, y];
                            if (tileRenderers[x, y])
                            {
                                var mat = tileRenderers[x, y].material;
                                mat.SetColor("_BaseColor", originalTileColors[x, y]);
                                mat.SetColor("_Color", originalTileColors[x, y]);
                            }
                        }
                    }
                }
        }

        if (numberHints != null)
        {
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    if (numberHints[x, y]) numberHints[x, y].gameObject.SetActive(false);
        }
    }

    private void UpdateAllNumberHints()
    {
        if (numberHints == null) return;
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                if (numberHints[x, y] == null) continue;
                int count = mineCountGrid[x, y];
                if (mineGrid[x, y] || count <= 0)
                {
                    numberHints[x, y].text = "";
                    numberHints[x, y].gameObject.SetActive(false);
                    continue;
                }
                numberHints[x, y].text = count.ToString();
                switch (count)
                {
                    case 1: numberHints[x, y].color = new Color(0.3f, 0.6f, 1f); break;
                    case 2: numberHints[x, y].color = new Color(0.2f, 0.85f, 0.2f); break;
                    case 3: numberHints[x, y].color = new Color(1f, 0.75f, 0.1f); break;
                    default: numberHints[x, y].color = new Color(1f, 0.25f, 0.25f); break;
                }
                // Visibility controlled by UpdateFog
            }
    }

    private void RevealMines()
    {
        // Show red markers at all remaining mine positions
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");

        foreach (var mp in minePositions)
        {
            if (!mineGrid[mp.x, mp.y]) continue; // already detonated
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "MineReveal";
            go.transform.position = GridToWorld(mp, 0.25f);
            go.transform.localScale = Vector3.one * 0.4f;
            Destroy(go.GetComponent<Collider>());
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.9f, 0.1f, 0.1f));
            mat.SetColor("_Color", new Color(0.9f, 0.1f, 0.1f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.4f, 0.05f, 0.05f));
            go.GetComponent<Renderer>().material = mat;
            mineRevealMarkers.Add(go.transform);
        }

        // Show all number hints
        if (numberHints != null)
        {
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                {
                    if (numberHints[x, y] == null) continue;
                    if (mineCountGrid[x, y] > 0 && !mineGrid[x, y])
                        numberHints[x, y].gameObject.SetActive(true);
                }
        }
    }

    private void ClearRevealMarkers()
    {
        foreach (var m in mineRevealMarkers)
            if (m) Destroy(m.gameObject);
        mineRevealMarkers.Clear();
    }

    // ===================== OBSTACLES =====================

    private void PlaceObstacles(int count)
    {
        var blocked = new HashSet<Vector2Int>(snake);
        Vector2Int center = new Vector2Int(gridWidth / 2, gridHeight / 2);
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
                blocked.Add(center + new Vector2Int(dx, dy));
        foreach (var m in minePositions) blocked.Add(m);

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
        float bob = Mathf.Sin(Time.time * 3.5f) * 0.08f;
        for (int i = 0; i < foodPositions.Count; i++)
        {
            if (i >= foodTransforms.Count || !foodTransforms[i]) continue;
            Vector3 pos = GridToWorld(foodPositions[i], 0.32f);
            pos.y += bob;
            foodTransforms[i].position = pos;
            foodTransforms[i].Rotate(Vector3.up, 100f * Time.deltaTime);
        }
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

                // Number hints: only visible within inner fog radius
                if (numberHints != null && numberHints[x, y] != null)
                {
                    bool showHint = !mineGrid[x, y] && mineCountGrid[x, y] > 0 && dist <= fogInnerRadius;
                    numberHints[x, y].gameObject.SetActive(showHint);
                }
            }
        }

        // Dim/show obstacles based on fog
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
            var omat = obstacleVisuals[i].GetComponent<Renderer>().material;
            omat.SetColor("_BaseColor", c);
            omat.SetColor("_Color", c);
        }

        // Dim/show food based on fog
        for (int i = 0; i < foodPositions.Count; i++)
        {
            if (i >= foodTransforms.Count || !foodTransforms[i]) continue;
            float fdist = Vector2.Distance(headPos, new Vector2(foodPositions[i].x, foodPositions[i].y));
            float fvis = fdist <= fogInnerRadius ? 1f :
                         fdist >= fogOuterRadius ? 0f :
                         Mathf.Lerp(1f, 0f, (fdist - fogInnerRadius) / (fogOuterRadius - fogInnerRadius));
            foodTransforms[i].localScale = Vector3.one * (0.5f * fvis);
        }

        // Dim/show mines based on fog
        for (int i = 0; i < minePositions.Count; i++)
        {
            if (i >= mineVisuals.Count || !mineVisuals[i]) continue;
            if (!mineGrid[minePositions[i].x, minePositions[i].y]) { mineVisuals[i].gameObject.SetActive(false); continue; }
            float mdist = Vector2.Distance(headPos, new Vector2(minePositions[i].x, minePositions[i].y));
            float mvis = mdist <= fogInnerRadius ? 1f :
                         mdist >= fogOuterRadius ? 0f :
                         Mathf.Lerp(1f, 0f, (mdist - fogInnerRadius) / (fogOuterRadius - fogInnerRadius));
            mineVisuals[i].gameObject.SetActive(mvis > 0.05f);
            if (mvis > 0.05f)
            {
                Color mc = new Color(0.18f, 0.18f, 0.18f) * mvis;
                mc.a = 1f;
                foreach (var r in mineVisuals[i].GetComponentsInChildren<Renderer>())
                {
                    r.material.SetColor("_BaseColor", mc);
                    r.material.SetColor("_Color", mc);
                }
            }
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
        // Show all food
        for (int i = 0; i < foodTransforms.Count; i++)
        {
            if (foodTransforms[i]) foodTransforms[i].localScale = Vector3.one * 0.5f;
        }
        // Show all remaining mines
        for (int i = 0; i < mineVisuals.Count; i++)
        {
            if (!mineVisuals[i]) continue;
            mineVisuals[i].gameObject.SetActive(true);
            foreach (var r in mineVisuals[i].GetComponentsInChildren<Renderer>())
            {
                r.material.SetColor("_BaseColor", new Color(0.18f, 0.18f, 0.18f));
                r.material.SetColor("_Color", new Color(0.18f, 0.18f, 0.18f));
            }
        }
    }

    // ===================== EFFECTS =====================

    private IEnumerator ExplosionEffect(Vector3 worldPos)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) shader = Shader.Find("Unlit/Color");
        if (!shader) shader = Shader.Find("Universal Render Pipeline/Lit");

        var particles = new List<Transform>();
        var directions = new List<Vector3>();
        int count = 12;

        for (int i = 0; i < count; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "ExplosionParticle";
            Destroy(p.GetComponent<Collider>());
            p.transform.position = worldPos;
            p.transform.localScale = Vector3.one * 0.15f;
            var mat = new Material(shader);
            Color startColor = new Color(1f, 0.6f, 0f);
            mat.SetColor("_BaseColor", startColor);
            mat.SetColor("_Color", startColor);
            p.GetComponent<Renderer>().material = mat;
            particles.Add(p.transform);
            directions.Add(Random.onUnitSphere * Random.Range(1f, 3f));
        }

        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            for (int i = 0; i < particles.Count; i++)
            {
                if (!particles[i]) continue;
                particles[i].position = worldPos + directions[i] * t;
                float s = Mathf.Lerp(0.15f, 0.02f, t);
                particles[i].localScale = Vector3.one * s;
                Color c = Color.Lerp(new Color(1f, 0.6f, 0f), new Color(1f, 0.15f, 0f), t);
                var pmat = particles[i].GetComponent<Renderer>().material;
                pmat.SetColor("_BaseColor", c);
                pmat.SetColor("_Color", c);
            }
            yield return null;
        }

        foreach (var p in particles)
            if (p) Destroy(p.gameObject);
    }

    private IEnumerator ShrinkSparkle(Vector3 worldPos)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) shader = Shader.Find("Unlit/Color");
        if (!shader) shader = Shader.Find("Universal Render Pipeline/Lit");

        var particles = new List<Transform>();
        int count = 6;
        Color sparkleColor = new Color(0.3f, 1f, 0.5f);

        for (int i = 0; i < count; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.name = "ShrinkSparkle";
            Destroy(p.GetComponent<Collider>());
            p.transform.position = worldPos;
            p.transform.localScale = Vector3.one * 0.08f;
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", sparkleColor);
            mat.SetColor("_Color", sparkleColor);
            p.GetComponent<Renderer>().material = mat;
            particles.Add(p.transform);
        }

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            for (int i = 0; i < particles.Count; i++)
            {
                if (!particles[i]) continue;
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Sin(angle) * t * 0.5f,
                    t * 1.5f,
                    Mathf.Cos(angle) * t * 0.5f);
                particles[i].position = worldPos + offset;
                float s = Mathf.Lerp(0.08f, 0.01f, t);
                particles[i].localScale = Vector3.one * s;
            }
            yield return null;
        }

        foreach (var p in particles)
            if (p) Destroy(p.gameObject);
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

        matMine = new Material(shader);
        SetCol(matMine, new Color(0.18f, 0.18f, 0.18f));
        matMine.SetFloat("_Smoothness", 0.7f);
        matMine.SetFloat("_Metallic", 0.6f);
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
        originalTileColors = new Color[gridWidth, gridHeight];
        numberHints = new TextMesh[gridWidth, gridHeight];

        Transform gridParent = new GameObject("Grid").transform;
        Transform hintParent = new GameObject("NumberHints").transform;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Tile_{x}_{y}";
                tile.transform.parent = gridParent;
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
                originalTileColors[x, y] = c;

                // Number hint (TextMesh, initially hidden)
                var hintGo = new GameObject($"Hint_{x}_{y}");
                hintGo.transform.parent = hintParent;
                hintGo.transform.position = new Vector3(x, 0.15f, y);
                hintGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var tm = hintGo.AddComponent<TextMesh>();
                tm.text = "";
                tm.fontSize = 48;
                tm.characterSize = 0.15f;
                tm.alignment = TextAlignment.Center;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.fontStyle = FontStyle.Bold;
                tm.color = Color.white;
                hintGo.SetActive(false);
                numberHints[x, y] = tm;
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
        cam.transform.position = new Vector3(cx, 30f, cz - 3f);
        cam.transform.LookAt(new Vector3(cx, 0f, cz + 0.5f));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.08f);
        cam.fieldOfView = 38f;
        cameraBasePos = cam.transform.position;
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

        // Multiplier text (below score, center)
        multiplierText = MakeText(canvasGo.transform, "MultiplierText", 32, TextAnchor.UpperCenter);
        multiplierText.color = new Color(1f, 0.9f, 0.2f);
        var mxrt = multiplierText.rectTransform;
        mxrt.anchorMin = new Vector2(0, 1);
        mxrt.anchorMax = Vector2.one;
        mxrt.pivot = new Vector2(0.5f, 1);
        mxrt.sizeDelta = new Vector2(0, 40);
        mxrt.anchoredPosition = new Vector2(0, -48);
        var mxol = multiplierText.gameObject.AddComponent<Outline>();
        mxol.effectColor = Color.black;
        mxol.effectDistance = new Vector2(2, 2);

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
        levelText.text = level > 10 ? $"ENDLESS {level}" : $"LEVEL {level}";
        progressFill.fillAmount = totalFoodThisLevel > 0 ? (float)foodEatenThisLevel / totalFoodThisLevel : 0;
        progressText.text = $"{foodEatenThisLevel}/{totalFoodThisLevel}";

        if (multiplierText)
        {
            if (scoreMultiplier > 1f && multiplierTimer > 0f)
                multiplierText.text = $"x{scoreMultiplier:F0}  ({multiplierTimer:F1}s)";
            else
                multiplierText.text = "";
        }
    }

    private void ShowMessage(string text)
    {
        messageText.text = text;
        messagePanel.SetActive(true);
    }

    // ===================== AUDIO =====================

    private void CreateSounds()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Load audio files from Resources/Audio (MP3s imported via Unity)
        clipExplosion = Resources.Load<AudioClip>("Audio/explosion");
        clipEat = Resources.Load<AudioClip>("Audio/eat");
        clipDeath = Resources.Load<AudioClip>("Audio/death");

        // Procedural fallback if audio files are missing
        if (!clipExplosion)
        {
            int rate = 44100; int samples = (int)(rate * 0.8f);
            float[] data = new float[samples];
            System.Random rng = new System.Random(42);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float transient = Mathf.Exp(-t * 40f) * 0.5f;
                float bass = Mathf.Sin(2f * Mathf.PI * 45f * t) * Mathf.Exp(-t * 4f) * 0.5f;
                float mid = (Mathf.Sin(2f * Mathf.PI * 180f * t)
                           + Mathf.Sin(2f * Mathf.PI * 260f * t) * 0.5f) * Mathf.Exp(-t * 6f) * 0.3f;
                float hi = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 8f) * 0.35f;
                data[i] = Mathf.Clamp(transient + bass + mid + hi, -1f, 1f);
            }
            clipExplosion = AudioClip.Create("Explosion", samples, 1, rate, false);
            clipExplosion.SetData(data, 0);
        }

        if (!clipEat)
        {
            int rate = 44100; int samples = (int)(rate * 0.25f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float env1 = t < 0.08f ? Mathf.Exp(-t * 20f) : 0f;
                float env2 = t >= 0.06f ? Mathf.Exp(-(t - 0.06f) * 16f) : 0f;
                data[i] = (Mathf.Sin(2f * Mathf.PI * 880f * t) * env1
                         + Mathf.Sin(2f * Mathf.PI * 1320f * t) * env2) * 0.45f;
            }
            clipEat = AudioClip.Create("Eat", samples, 1, rate, false);
            clipEat.SetData(data, 0);
        }

        // --- Shrink: ascending sparkle arpeggio (C6-E6-G6) — always procedural ---
        {
            int rate = 44100; int samples = (int)(rate * 0.35f);
            float[] data = new float[samples];
            float[] notes = { 1047f, 1319f, 1568f };
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float val = 0f;
                for (int n = 0; n < 3; n++)
                {
                    float noteStart = n * 0.08f;
                    if (t >= noteStart)
                    {
                        float nt = t - noteStart;
                        val += Mathf.Sin(2f * Mathf.PI * notes[n] * t) * Mathf.Exp(-nt * 10f) * 0.25f;
                    }
                }
                data[i] = val;
            }
            clipShrink = AudioClip.Create("Shrink", samples, 1, rate, false);
            clipShrink.SetData(data, 0);
        }

        // --- Heartbeat: double-thump pulse — always procedural ---
        {
            int rate = 44100; int samples = (int)(rate * 0.9f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float b1 = Mathf.Exp(-t * 15f) * Mathf.Sin(2f * Mathf.PI * 55f * t);
                float t2 = t - 0.22f;
                float b2 = t2 > 0f ? Mathf.Exp(-t2 * 18f) * Mathf.Sin(2f * Mathf.PI * 50f * t2) * 0.7f : 0f;
                data[i] = (b1 + b2) * 0.6f;
            }
            clipHeartbeat = AudioClip.Create("Heartbeat", samples, 1, rate, false);
            clipHeartbeat.SetData(data, 0);
        }

        // Heartbeat source (loops continuously, volume/pitch controlled by proximity)
        heartbeatSource = gameObject.AddComponent<AudioSource>();
        heartbeatSource.clip = clipHeartbeat;
        heartbeatSource.loop = true;
        heartbeatSource.playOnAwake = false;
        heartbeatSource.spatialBlend = 0f;
        heartbeatSource.volume = 0f;
        heartbeatSource.Play();

        // Background music
        AudioClip musicClip = Resources.Load<AudioClip>("Audio/music");
        if (musicClip)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.3f;
            musicSource.Play();
        }
    }

    // ===================== MINE VISUAL =====================

    private Transform CreateMineVisual(Vector2Int pos)
    {
        var root = new GameObject("Mine_" + pos.x + "_" + pos.y).transform;
        root.position = GridToWorld(pos, 0.22f);

        // Body sphere
        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "MineBody";
        body.transform.SetParent(root, false);
        body.transform.localScale = Vector3.one * 0.38f;
        body.GetComponent<Renderer>().sharedMaterial = matMine;
        Destroy(body.GetComponent<Collider>());

        // Spikes (6 directions: +X, -X, +Z, -Z, +Y, -Y)
        Vector3[] spikeDirs = {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back, Vector3.up, Vector3.down
        };
        foreach (var dir in spikeDirs)
        {
            var spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spike.name = "Spike";
            spike.transform.SetParent(root, false);
            spike.transform.localPosition = dir * 0.22f;
            spike.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
            spike.transform.localRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, 45f);
            spike.GetComponent<Renderer>().sharedMaterial = matMine;
            Destroy(spike.GetComponent<Collider>());
        }

        root.gameObject.SetActive(false); // hidden until fog reveals
        return root;
    }

    // ===================== MINE ENCIRCLEMENT =====================

    private void CheckMineEncirclement()
    {
        var snakeSet = new HashSet<Vector2Int>(snake);
        var toDefuse = new List<Vector2Int>();

        for (int i = 0; i < minePositions.Count; i++)
        {
            Vector2Int mp = minePositions[i];
            if (!mineGrid[mp.x, mp.y]) continue;

            // All 4 neighbors must be blocked (wall, snake, or obstacle)
            // At least 2 must be snake body (player actively encircled it)
            bool allBlocked = true;
            int snakeNeighbors = 0;
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in dirs)
            {
                Vector2Int n = mp + dir;
                if (n.x < 0 || n.x >= gridWidth || n.y < 0 || n.y >= gridHeight)
                    continue; // wall = blocked
                if (snakeSet.Contains(n))
                    snakeNeighbors++;
                else if (obstacles.Contains(n))
                    { } // obstacle = blocked
                else
                    { allBlocked = false; break; }
            }

            if (allBlocked && snakeNeighbors >= 2)
                toDefuse.Add(mp);
        }

        foreach (var mp in toDefuse)
        {
            // Safe detonation — no growth, no fog shrink, no panic
            mineGrid[mp.x, mp.y] = false;

            // Destroy mine visual
            int mIdx = minePositions.IndexOf(mp);
            if (mIdx >= 0 && mIdx < mineVisuals.Count && mineVisuals[mIdx])
            {
                Destroy(mineVisuals[mIdx].gameObject);
                mineVisuals[mIdx] = null;
            }

            RecalculateCountsAround(mp.x, mp.y);
            UpdateAllNumberHints();

            // Green-tinted crater (defused, not detonated)
            if (tileRenderers[mp.x, mp.y])
            {
                Color defuseColor = new Color(0.08f, 0.25f, 0.12f);
                tileBaseColors[mp.x, mp.y] = defuseColor;
                var mat = tileRenderers[mp.x, mp.y].material;
                mat.SetColor("_BaseColor", defuseColor);
                mat.SetColor("_Color", defuseColor);
            }

            // Major score bonus (affected by multiplier)
            score += (int)(100 * level * scoreMultiplier);

            // Defuse effect (green implosion)
            StartCoroutine(DefuseEffect(GridToWorld(mp, 0.3f)));
            if (audioSource && clipShrink) audioSource.PlayOneShot(clipShrink, 0.8f);
        }

        if (toDefuse.Count > 0) UpdateHUD();
    }

    private IEnumerator DefuseEffect(Vector3 worldPos)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) shader = Shader.Find("Unlit/Color");
        if (!shader) shader = Shader.Find("Universal Render Pipeline/Lit");

        var particles = new List<Transform>();
        int count = 10;
        Color defuseColor = new Color(0.2f, 1f, 0.5f);

        // Particles start spread out and implode inward
        var startOffsets = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(angle), 0.5f, Mathf.Cos(angle)) * 0.8f;
            startOffsets.Add(offset);

            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.name = "DefuseParticle";
            Destroy(p.GetComponent<Collider>());
            p.transform.position = worldPos + offset;
            p.transform.localScale = Vector3.one * 0.1f;
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", defuseColor);
            mat.SetColor("_Color", defuseColor);
            p.GetComponent<Renderer>().material = mat;
            particles.Add(p.transform);
        }

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            for (int i = 0; i < particles.Count; i++)
            {
                if (!particles[i]) continue;
                // Implode: move from outer position toward center
                particles[i].position = worldPos + startOffsets[i] * (1f - t);
                float s = Mathf.Lerp(0.1f, 0.2f, t < 0.5f ? t * 2f : (1f - t) * 2f);
                particles[i].localScale = Vector3.one * s;
                Color c = Color.Lerp(defuseColor, new Color(0.5f, 1f, 0.8f), t);
                var pmat = particles[i].GetComponent<Renderer>().material;
                pmat.SetColor("_BaseColor", c);
                pmat.SetColor("_Color", c);
            }
            yield return null;
        }

        foreach (var p in particles)
            if (p) Destroy(p.gameObject);
    }

    // ===================== HEARTBEAT =====================

    private void UpdateHeartbeat()
    {
        if (!heartbeatSource || minePositions == null || snake.Count == 0 || !alive)
        {
            if (heartbeatSource) heartbeatSource.volume = 0f;
            return;
        }

        float nearestDist = float.MaxValue;
        Vector2 headPos = new Vector2(snake[0].x, snake[0].y);
        for (int i = 0; i < minePositions.Count; i++)
        {
            if (!mineGrid[minePositions[i].x, minePositions[i].y]) continue;
            float d = Vector2.Distance(headPos, new Vector2(minePositions[i].x, minePositions[i].y));
            if (d < nearestDist) nearestDist = d;
        }

        float threshold = baseFogOuter + 1f;
        if (nearestDist > threshold)
        {
            heartbeatSource.volume = Mathf.Lerp(heartbeatSource.volume, 0f, Time.deltaTime * 5f);
            return;
        }

        float proximity = 1f - Mathf.Clamp01(nearestDist / threshold);
        float targetVol = Mathf.Lerp(0f, 0.7f, proximity * proximity);
        float targetPitch = Mathf.Lerp(0.6f, 2.2f, proximity);
        heartbeatSource.volume = Mathf.Lerp(heartbeatSource.volume, targetVol, Time.deltaTime * 4f);
        heartbeatSource.pitch = Mathf.Lerp(heartbeatSource.pitch, targetPitch, Time.deltaTime * 4f);
    }

    // ===================== CHAIN REACTION =====================

    private IEnumerator ChainReaction(Vector2Int origin)
    {
        yield return new WaitForSeconds(0.18f);

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int neighbor = origin + dir;
            if (neighbor.x < 0 || neighbor.x >= gridWidth || neighbor.y < 0 || neighbor.y >= gridHeight)
                continue;
            if (!mineGrid[neighbor.x, neighbor.y]) continue;
            if (Random.value > 0.5f) continue; // 50% chance to chain

            // Detonate this mine
            mineGrid[neighbor.x, neighbor.y] = false;
            craterGrid[neighbor.x, neighbor.y] = true;

            // Destroy mine visual
            int mIdx = minePositions.IndexOf(neighbor);
            if (mIdx >= 0 && mIdx < mineVisuals.Count && mineVisuals[mIdx])
            {
                Destroy(mineVisuals[mIdx].gameObject);
                mineVisuals[mIdx] = null;
            }

            // Recalculate counts
            RecalculateCountsAround(neighbor.x, neighbor.y);
            UpdateAllNumberHints();

            // Crater tile
            if (tileRenderers[neighbor.x, neighbor.y])
            {
                Color craterColor = new Color(0.08f, 0.06f, 0.04f);
                tileBaseColors[neighbor.x, neighbor.y] = craterColor;
                var mat = tileRenderers[neighbor.x, neighbor.y].material;
                mat.SetColor("_BaseColor", craterColor);
                mat.SetColor("_Color", craterColor);
            }

            // Effects
            StartCoroutine(ExplosionEffect(GridToWorld(neighbor, 0.3f)));
            shakeTimer = Mathf.Max(shakeTimer, 0.15f);
            if (audioSource && clipExplosion) audioSource.PlayOneShot(clipExplosion, 0.6f);

            // Grow snake +2 per chain
            if (alive && snake.Count > 0)
            {
                snake.Add(snake[snake.Count - 1]);
                snake.Add(snake[snake.Count - 1]);
                AddSegment();
                AddSegment();
                SyncVisuals();
            }

            // Score penalty
            score = Mathf.Max(0, score - 10 * level);
            UpdateHUD();

            // Extend fog penalty
            fogPenaltyTimer = Mathf.Max(fogPenaltyTimer, 3f);

            yield return new WaitForSeconds(0.12f);
        }
    }

    // ===================== UTILITY =====================

    private Vector3 GridToWorld(Vector2Int gridPos, float height)
    {
        return new Vector3(gridPos.x, height, gridPos.y);
    }
}
