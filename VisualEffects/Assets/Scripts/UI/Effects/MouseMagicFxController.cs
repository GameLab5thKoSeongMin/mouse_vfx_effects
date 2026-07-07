// 코드 책임: UI 마우스 이동 반짝임과 클릭 링 버스트 효과를 비차단 풀링 방식으로 재생한다.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class MouseMagicFxController : MonoBehaviour
{
    private enum FxKind
    {
        Dust,
        Burst,
        Ring
    }

    private sealed class FxParticle
    {
        public RectTransform Rect;
        public Image Image;
        public FxKind Kind;
        public Vector2 AnchoredPosition;
        public Vector2 Velocity;
        public Color StartColor;
        public float Age;
        public float Lifetime;
        public float StartScale;
        public float EndScale;
        public float Gravity;

        public void Reset(
            FxKind kind,
            Vector2 anchoredPosition,
            Vector2 velocity,
            Color color,
            float lifetime,
            float startScale,
            float endScale,
            float gravity,
            Sprite sprite)
        {
            Kind = kind;
            AnchoredPosition = anchoredPosition;
            Velocity = velocity;
            StartColor = color;
            Age = 0f;
            Lifetime = Mathf.Max(0.01f, lifetime);
            StartScale = Mathf.Max(0.01f, startScale);
            EndScale = Mathf.Max(0.01f, endScale);
            Gravity = gravity;

            Image.sprite = sprite;
            Image.color = color;
            Image.raycastTarget = false;
            Image.enabled = true;
            Rect.gameObject.SetActive(true);
            Rect.anchoredPosition = anchoredPosition;
            Rect.localRotation = Quaternion.identity;
            Rect.localScale = Vector3.one;
            Rect.sizeDelta = Vector2.one * StartScale;
        }

        public bool Tick(float deltaTime)
        {
            Age += deltaTime;
            if (Age >= Lifetime)
            {
                Image.enabled = false;
                Rect.gameObject.SetActive(false);
                return false;
            }

            float normalizedAge = Age / Lifetime;
            float eased = 1f - (1f - normalizedAge) * (1f - normalizedAge);

            Velocity.y -= Gravity * deltaTime;
            AnchoredPosition += Velocity * deltaTime;
            Rect.anchoredPosition = AnchoredPosition;

            float scale = Mathf.Lerp(StartScale, EndScale, eased);
            Rect.sizeDelta = Vector2.one * scale;

            Color color = StartColor;
            color.a *= 1f - eased;
            Image.color = color;
            return true;
        }
    }

    [Header("General")]
    [SerializeField] private bool effectEnabled = true;
    [SerializeField, Min(1)] private int maxActiveParticles = 120;
    [SerializeField] private bool autoPlayLeftClickBurst = true;
    [SerializeField] private bool keepRootOnTop = true;
    [SerializeField] private Sprite dustSprite;
    [SerializeField] private Sprite ringSprite;

    [Header("Trail")]
    [SerializeField] private bool trailEnabled = true;
    [SerializeField, Min(1f)] private float trailSpawnDistance = 10f;
    [SerializeField, Range(1, 4)] private int maxTrailSpawnsPerFrame = 2;
    [SerializeField] private Vector2 trailLifetimeRange = new Vector2(0.25f, 0.42f);
    [SerializeField] private Vector2 trailScaleRange = new Vector2(2.5f, 6f);
    [SerializeField] private Color[] trailColors =
    {
        new Color(1f, 0.82f, 0.34f, 0.72f),
        new Color(1f, 0.96f, 0.78f, 0.58f),
        new Color(0.72f, 0.88f, 1f, 0.22f)
    };
    [SerializeField, Min(0f)] private float trailDownwardGravity = 24f;
    [SerializeField, Min(0f)] private float trailFallSpeed = 10f;
    [SerializeField, Min(0f)] private float trailJitterRange = 6f;
    [SerializeField, Min(0f)] private float trailBehindDistance = 8f;

    [Header("Click Burst")]
    [SerializeField, Range(1, 32)] private int burstCount = 10;
    [SerializeField] private Vector2 burstLifetimeRange = new Vector2(0.18f, 0.32f);
    [SerializeField] private Vector2 burstSpeedRange = new Vector2(55f, 120f);
    [SerializeField] private Vector2 burstScaleRange = new Vector2(3f, 7f);
    [SerializeField] private Color[] burstColors =
    {
        new Color(1f, 0.78f, 0.22f, 0.82f),
        new Color(1f, 0.94f, 0.70f, 0.70f),
        new Color(1f, 1f, 0.92f, 0.60f)
    };

    [Header("Click Ring")]
    [SerializeField, Min(0.01f)] private float ringLifetime = 0.24f;
    [SerializeField, Min(1f)] private float ringStartScale = 10f;
    [SerializeField, Min(1f)] private float ringEndScale = 42f;
    [SerializeField] private Color ringColor = new Color(1f, 0.73f, 0.22f, 0.62f);
    [SerializeField, Range(1, 8)] private int ringThickness = 2;

    private RectTransform rootRect;
    private Canvas rootCanvas;
    private Camera eventCamera;
    private FxParticle[] particles;
    private Sprite fallbackDustSprite;
    private Sprite fallbackRingSprite;
    private Texture2D fallbackDustTexture;
    private Texture2D fallbackRingTexture;
    private Vector2 lastMouseScreenPosition;
    private bool hasLastMousePosition;
    private int activeCount;

    private void Awake()
    {
        rootRect = (RectTransform)transform;
        rootCanvas = GetComponentInParent<Canvas>();
        eventCamera = ResolveEventCamera();

        StretchToParent();
        KeepRootAsLastSibling();
        CreateFallbackSprites();
        RebuildPool();
    }

    private void LateUpdate()
    {
        KeepRootAsLastSibling();
    }

    private void OnDestroy()
    {
        if (fallbackDustSprite != null)
        {
            Destroy(fallbackDustSprite);
        }

        if (fallbackRingSprite != null)
        {
            Destroy(fallbackRingSprite);
        }

        if (fallbackDustTexture != null)
        {
            Destroy(fallbackDustTexture);
        }

        if (fallbackRingTexture != null)
        {
            Destroy(fallbackRingTexture);
        }
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        TickParticles(deltaTime);

        if (!effectEnabled)
        {
            return;
        }

        Vector2 currentMousePosition;
        if (!TryGetPointerScreenPosition(out currentMousePosition))
        {
            hasLastMousePosition = false;
            return;
        }

        if (trailEnabled)
        {
            TickTrail(currentMousePosition);
        }
        else
        {
            lastMouseScreenPosition = currentMousePosition;
            hasLastMousePosition = true;
        }

        if (autoPlayLeftClickBurst && WasPrimaryPointerPressedThisFrame())
        {
            PlayClickBurst(currentMousePosition);
        }
    }

    public void SetEnabled(bool enabled)
    {
        effectEnabled = enabled;
        if (!enabled)
        {
            hasLastMousePosition = false;
            DeactivateAllParticles();
        }
    }

    public void PlayClickBurst(Vector2 screenPosition)
    {
        if (!effectEnabled || rootRect == null)
        {
            return;
        }

        Vector2 localPosition;
        if (!TryScreenToLocal(screenPosition, out localPosition))
        {
            return;
        }

        SpawnParticle(
            FxKind.Ring,
            localPosition,
            Vector2.zero,
            ringColor,
            ringLifetime,
            ringStartScale,
            ringEndScale,
            0f,
            ringSprite != null ? ringSprite : fallbackRingSprite);

        float angleStep = 360f / Mathf.Max(1, burstCount);
        for (int i = 0; i < burstCount; i++)
        {
            float angle = (angleStep * i + Random.Range(-8f, 8f)) * Mathf.Deg2Rad;
            float speed = RandomRange(burstSpeedRange);
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            Vector2 jitter = Random.insideUnitCircle * 2f;
            float startScale = RandomRange(burstScaleRange);

            SpawnParticle(
                FxKind.Burst,
                localPosition + jitter,
                velocity,
                PickColor(burstColors, ringColor),
                RandomRange(burstLifetimeRange),
                startScale,
                Mathf.Max(0.01f, startScale * 0.25f),
                trailDownwardGravity * 0.35f,
                dustSprite != null ? dustSprite : fallbackDustSprite);
        }
    }

    private void TickTrail(Vector2 currentMousePosition)
    {
        if (!hasLastMousePosition)
        {
            lastMouseScreenPosition = currentMousePosition;
            hasLastMousePosition = true;
            return;
        }

        Vector2 movement = currentMousePosition - lastMouseScreenPosition;
        float distance = movement.magnitude;
        if (distance < trailSpawnDistance)
        {
            return;
        }

        Vector2 direction = movement / distance;
        int spawnCount = Mathf.Min(maxTrailSpawnsPerFrame, Mathf.FloorToInt(distance / trailSpawnDistance));
        for (int i = 0; i < spawnCount; i++)
        {
            float step = (i + 1f) / (spawnCount + 1f);
            Vector2 screenPosition = Vector2.Lerp(lastMouseScreenPosition, currentMousePosition, step);
            SpawnTrailDust(screenPosition, direction);
        }

        lastMouseScreenPosition = currentMousePosition;
    }

    private void SpawnTrailDust(Vector2 screenPosition, Vector2 movementDirection)
    {
        Vector2 localPosition;
        Vector2 behindScreenPosition = screenPosition - movementDirection * trailBehindDistance;
        behindScreenPosition += Random.insideUnitCircle * trailJitterRange;

        if (!TryScreenToLocal(behindScreenPosition, out localPosition))
        {
            return;
        }

        float startScale = RandomRange(trailScaleRange);
        Vector2 drift = new Vector2(Random.Range(-7f, 7f), -trailFallSpeed + Random.Range(-4f, 4f));
        SpawnParticle(
            FxKind.Dust,
            localPosition,
            drift,
            PickColor(trailColors, Color.white),
            RandomRange(trailLifetimeRange),
            startScale,
            Mathf.Max(0.01f, startScale * 0.35f),
            trailDownwardGravity,
            dustSprite != null ? dustSprite : fallbackDustSprite);
    }

    private void TickParticles(float deltaTime)
    {
        if (particles == null)
        {
            return;
        }

        for (int i = 0; i < particles.Length; i++)
        {
            FxParticle particle = particles[i];
            if (particle == null || !particle.Rect.gameObject.activeSelf)
            {
                continue;
            }

            if (!particle.Tick(deltaTime))
            {
                activeCount = Mathf.Max(0, activeCount - 1);
            }
        }
    }

    private void SpawnParticle(
        FxKind kind,
        Vector2 anchoredPosition,
        Vector2 velocity,
        Color color,
        float lifetime,
        float startScale,
        float endScale,
        float gravity,
        Sprite sprite)
    {
        FxParticle particle = GetFreeParticle();
        if (particle == null)
        {
            return;
        }

        particle.Reset(kind, anchoredPosition, velocity, color, lifetime, startScale, endScale, gravity, sprite);
        particle.Rect.SetAsLastSibling();
        activeCount++;
    }

    private FxParticle GetFreeParticle()
    {
        if (particles == null || activeCount >= particles.Length)
        {
            return null;
        }

        for (int i = 0; i < particles.Length; i++)
        {
            FxParticle particle = particles[i];
            if (!particle.Rect.gameObject.activeSelf)
            {
                return particle;
            }
        }

        return null;
    }

    private void RebuildPool()
    {
        int poolSize = Mathf.Max(1, maxActiveParticles);
        particles = new FxParticle[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            GameObject particleObject = new GameObject("MouseMagicFxParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            particleObject.transform.SetParent(rootRect, false);

            RectTransform rectTransform = (RectTransform)particleObject.transform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Image image = particleObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.maskable = false;
            image.sprite = fallbackDustSprite;

            particleObject.SetActive(false);
            particles[i] = new FxParticle
            {
                Rect = rectTransform,
                Image = image
            };
        }
    }

    private void DeactivateAllParticles()
    {
        if (particles == null)
        {
            return;
        }

        for (int i = 0; i < particles.Length; i++)
        {
            FxParticle particle = particles[i];
            if (particle == null)
            {
                continue;
            }

            particle.Image.enabled = false;
            particle.Rect.gameObject.SetActive(false);
        }

        activeCount = 0;
    }

    private void StretchToParent()
    {
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void KeepRootAsLastSibling()
    {
        if (!keepRootOnTop || rootRect == null || rootRect.parent == null)
        {
            return;
        }

        if (rootRect.GetSiblingIndex() != rootRect.parent.childCount - 1)
        {
            rootRect.SetAsLastSibling();
        }
    }

    private Camera ResolveEventCamera()
    {
        if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
    }

    private bool TryScreenToLocal(Vector2 screenPosition, out Vector2 localPosition)
    {
        if (rootRect == null)
        {
            localPosition = Vector2.zero;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootRect,
            screenPosition,
            eventCamera,
            out localPosition);
    }

    private static float RandomRange(Vector2 range)
    {
        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);
        return Random.Range(min, max);
    }

    private static Color PickColor(Color[] colors, Color fallback)
    {
        if (colors == null || colors.Length == 0)
        {
            return fallback;
        }

        return colors[Random.Range(0, colors.Length)];
    }

    private static bool TryGetPointerScreenPosition(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        screenPosition = UnityEngine.Input.mousePosition;
        return true;
#else
        screenPosition = Vector2.zero;
        return false;
#endif
    }

    private static bool WasPrimaryPointerPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return UnityEngine.Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private void CreateFallbackSprites()
    {
        fallbackDustTexture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        fallbackDustTexture.name = "MouseMagicFxDot";
        fallbackDustTexture.wrapMode = TextureWrapMode.Clamp;
        fallbackDustTexture.filterMode = FilterMode.Bilinear;

        Vector2 dotCenter = new Vector2(3.5f, 3.5f);
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), dotCenter);
                float alpha = Mathf.Clamp01(1f - distance / 3.7f);
                fallbackDustTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        fallbackDustTexture.Apply(false, true);
        fallbackDustSprite = Sprite.Create(fallbackDustTexture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);

        int ringSize = 32;
        fallbackRingTexture = new Texture2D(ringSize, ringSize, TextureFormat.RGBA32, false);
        fallbackRingTexture.name = "MouseMagicFxRing";
        fallbackRingTexture.wrapMode = TextureWrapMode.Clamp;
        fallbackRingTexture.filterMode = FilterMode.Bilinear;

        float outerRadius = ringSize * 0.44f;
        float innerRadius = Mathf.Max(1f, outerRadius - ringThickness);
        Vector2 ringCenter = new Vector2((ringSize - 1) * 0.5f, (ringSize - 1) * 0.5f);
        for (int y = 0; y < ringSize; y++)
        {
            for (int x = 0; x < ringSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), ringCenter);
                float outerFade = Mathf.Clamp01(outerRadius + 0.5f - distance);
                float innerFade = Mathf.Clamp01(distance - innerRadius + 0.5f);
                float alpha = Mathf.Min(outerFade, innerFade);
                fallbackRingTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        fallbackRingTexture.Apply(false, true);
        fallbackRingSprite = Sprite.Create(fallbackRingTexture, new Rect(0f, 0f, ringSize, ringSize), new Vector2(0.5f, 0.5f), ringSize);
    }
}
