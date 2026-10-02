using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance;

    enum State { Start, Playing, Won }
    State state = State.Start;

    [Header("Bottles")]
    [Tooltip("Để trống = tự tìm tất cả BottleController trong scene")]
    public BottleController[] bottles;
    [Tooltip("Số màu ngẫu nhiên (mỗi màu 4 tầng). Số ống = số màu + 1 ống trống")]
    public int colorCount = 3;
    public Color[] palette = new Color[]
    {
        new Color(0.85f, 0.10f, 0.10f), // đỏ
        new Color(1.00f, 0.92f, 0.00f), // vàng
        new Color(0.10f, 0.35f, 1.00f), // xanh dương
        new Color(0.50f, 0.05f, 0.65f), // tím
        new Color(0.10f, 0.75f, 0.25f), // xanh lá
        new Color(1.00f, 0.50f, 0.00f), // cam
        new Color(0.00f, 0.80f, 0.85f), // cyan
        new Color(1.00f, 0.40f, 0.70f), // hồng
    };

    [Header("Audio")]
    public AudioClip clickSfx;
    public AudioClip pourSfx;
    public AudioClip winSfx;
    public AudioClip completeBottleSfx;
    public AudioClip errorSfx;
    AudioSource audioSource;

    [Header("FX (tùy chọn)")]
    [Tooltip("Có thể kéo prefab Particle System của bạn vào. Để trống = tự tạo confetti bằng code")]
    public ParticleSystem confettiPrefab;

    BottleController FirstBottle;
    BottleController SecondBottle;

    bool isBusy = false;      // true khi đang chạy animation đổ nước -> khóa input
    GameUI ui;

    void Awake()
    {
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (bottles == null || bottles.Length == 0)
        {
            bottles = FindObjectsOfType<BottleController>();
            System.Array.Sort(bottles, (a, b) => string.CompareOrdinal(a.name, b.name));
        }

        SetupLevel();

        ui = gameObject.AddComponent<GameUI>();
        ui.Build(StartGame, RestartGame, ResetGame);
        ui.ShowStart(true);
        state = State.Start;
    }

    // ---------------------------------------------------------------- LEVEL
    void SetupLevel()
    {
        int maxColors = Mathf.Min(palette.Length, bottles.Length - 1);
        colorCount = Mathf.Clamp(colorCount, 1, maxColors);

        // 1. chọn ngẫu nhiên các màu
        List<Color> pool = new List<Color>(palette);
        Shuffle(pool);

        // 2. mỗi màu 4 tầng, xáo trộn
        List<Color> units = new List<Color>();
        for (int i = 0; i < colorCount; i++)
            for (int j = 0; j < 4; j++)
                units.Add(pool[i]);

        int tries = 0;
        do { Shuffle(units); tries++; }
        while (HasSolvedBottle(units) && tries < 100);   // tránh ống đã xong ngay từ đầu

        // 3. random ống nào là ống trống
        List<BottleController> order = new List<BottleController>(bottles);
        Shuffle(order);

        for (int i = 0; i < order.Count; i++)
        {
            if (i < colorCount)
            {
                Color[] layers = new Color[4];
                for (int k = 0; k < 4; k++) layers[k] = units[i * 4 + k];
                order[i].Init(layers, 4);
            }
            else
            {
                order[i].Init(new Color[4], 0);   // ống trống
            }
        }

        FirstBottle = null;
        SecondBottle = null;
        isBusy = false;
    }

    bool HasSolvedBottle(List<Color> units)
    {
        for (int b = 0; b < colorCount; b++)
        {
            int s = b * 4;
            if (units[s] == units[s + 1] && units[s + 1] == units[s + 2] && units[s + 2] == units[s + 3])
                return true;
        }
        return false;
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
    }

    // ---------------------------------------------------------------- UI CALLBACKS
    void StartGame()
    {
        ui.ShowStart(false);
        ui.ShowHud(true);
        state = State.Playing;
    }

    // Nút Reset trong lúc chơi: random lại ván, dừng mọi animation đang chạy
    void ResetGame()
    {
        if (state != State.Playing) return;
        PlaySfx(clickSfx);
        SetupLevel();   // BottleController.Init sẽ dừng animation và đưa ống về chỗ cũ
    }

    // Nút chơi lại: reset ván giống nút reset (random lại màu, mở khóa mọi ống)
    void RestartGame()
    {
        StopAllCoroutines();
        ui.ShowWin(false);
        ui.ShowHud(true);
        SetupLevel();
        state = State.Playing;
    }

    // ---------------------------------------------------------------- INPUT
    void Update()
    {
        if (state != State.Playing) return;
        if (isBusy) return;                                  // khóa input khi đang animation
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
        if (hit.collider == null) return;

        BottleController bottle = hit.collider.GetComponent<BottleController>();
        if (bottle == null || bottle.isLocked) return;        // ống đã xong -> bỏ qua

        HandleBottleClick(bottle);
    }

    void HandleBottleClick(BottleController bottle)
    {
        if (FirstBottle == null)
        {
            if (bottle.numberOfColorsInBottle == 0)
            {
            PlaySfx(errorSfx);
            return;   // không chọn ống trống làm ống nguồn
            }

            FirstBottle = bottle;
            FirstBottle.SetSelected(true);
            PlaySfx(clickSfx);
        }
        else if (FirstBottle == bottle)
        {
            FirstBottle.SetSelected(false);
            FirstBottle = null;
            PlaySfx(clickSfx);
        }
        else
        {
            SecondBottle = bottle;
            FirstBottle.BottleControllerRef = SecondBottle;

            FirstBottle.UpdateTopColorValues();
            SecondBottle.UpdateTopColorValues();

            FirstBottle.SetSelected(false);

            if (SecondBottle.FillBottleCheck(FirstBottle.topColor))
            {
                isBusy = true;                 // KHÓA input cho tới khi OnTransferFinished
                PlaySfx(clickSfx);
                FirstBottle.StartColorTransfer();
            }
            else
            {
                PlaySfx(errorSfx);
            }

            FirstBottle = null;
            SecondBottle = null;
        }
    }

    // BottleController gọi khi animation đổ nước kết thúc hoàn toàn
    public void OnTransferFinished(BottleController source, BottleController target)
    {
        source.UpdateTopColorValues();
        target.UpdateTopColorValues();

        if (!target.isLocked && target.IsComplete())
        {
            target.isLocked = true;            // khóa ống đã xong
            PlaySfx(completeBottleSfx);
            PlayConfetti(target);
        }

        isBusy = false;                        // mở khóa input

        if (AllSolved())
        {
            StartCoroutine(WinRoutine());
        }
    }

    bool AllSolved()
    {
        foreach (BottleController b in bottles)
        {
            if (b.numberOfColorsInBottle == 0) continue;
            if (!b.IsComplete()) return false;
        }
        return true;
    }

    IEnumerator WinRoutine()
    {
        state = State.Won;
        PlaySfx(winSfx);
        yield return new WaitForSeconds(1.3f);   // chờ confetti bắn xong rồi mới hiện Win
        ui.ShowHud(false);
        ui.ShowWin(true);
    }

    // ---------------------------------------------------------------- AUDIO
    public void PlaySfx(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip);
    }

    public void PlayPour()
    {
        PlaySfx(pourSfx);
    }

    // ---------------------------------------------------------------- CONFETTI
    void PlayConfetti(BottleController bottle)
    {
        Vector3 pos = bottle.GetMouthPosition();
        ParticleSystem ps;

        if (confettiPrefab != null)
        {
            ps = Instantiate(confettiPrefab, pos, Quaternion.identity);
        }
        else
        {
            Material mat = bottle.GetComponent<SpriteRenderer>().sharedMaterial;
            ps = CreateConfetti(pos, mat);
        }

        ps.Play();
        Destroy(ps.gameObject, 4f);
    }

    ParticleSystem CreateConfetti(Vector3 pos, Material mat)
    {
        GameObject go = new GameObject("Confetti");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // cone bắn lên trên

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Màu ngẫu nhiên trong 1 dải màu rời rạc
        Gradient rainbow = new Gradient();
        rainbow.mode = GradientMode.Fixed;
        rainbow.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.30f, 0.30f), 0.2f),
                new GradientColorKey(new Color(1f, 0.90f, 0.20f), 0.4f),
                new GradientColorKey(new Color(0.30f, 0.90f, 0.40f), 0.6f),
                new GradientColorKey(new Color(0.30f, 0.60f, 1f), 0.8f),
                new GradientColorKey(Color.white, 1f),   // trắng = bụi sáng
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        ParticleSystem.MinMaxGradient startColor = new ParticleSystem.MinMaxGradient(rainbow);
        startColor.mode = ParticleSystemGradientMode.RandomColor;
        main.startColor = startColor;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30f;
        shape.radius = 0.08f;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = fade;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.sortingOrder = 50;

        return ps;
    }
}