using UnityEngine;

public class ForgeBellows : MonoBehaviour, IInteract
{
    public Transform holder;
    public Transform forgeBellows;
    public ParticleSystem bellowsVFX; 

    [Header("settings")]
    public float holderMinY;
    public float holderMaxY;
    public float bellowsMinX;
    public float bellowsMaxX;
    [Space] public float minSpeed;
    public float maxSpeed = 3f;
    public float maxPressRate = 6f;
    public float pressDecayPerSecond = 2f;

    [Header("cycle / VFX")]
    [Tooltip("Сколько полных ходов (min → max) нужно, чтобы запустить VFX")]
    public int cyclesPerVFX = 1;

    [Tooltip("Насколько близко t к 0 считать, что мы в максимуме (bellowsMaxX)")]
    public float maxPosThresholdT = 0.05f;

    [Header("UI fill")]
    [Tooltip("Скорость, с которой ползунок готовки догоняет целевое значение")]
    public float fillLerpSpeed = 3f;

    private float _pressRate = 0;
    private float _lastPressTime = -1f;
    private float _cycle = 0;

    private Vector3 _holderBaseEuler;
    private Vector3 _bellowsBaseEuler;

    private int _cycleCount = 0;          // для VFX
    private float _prevT;
    private bool _cycleDetectionInitialized = false;

    // === ДЛЯ ГОТОВКИ ЗЕЛЬЯ ===
    private PotionData _lastPendingPotion; // чтобы понять, сменилось ли зелье
    private int _potionCyclesDone = 0;     // сколько циклов уже сделано для текущего зелья
    private float _potionFill01 = 0f;      // текущее плавное значение [0..1]

    private void Start()
    {
        if (holder != null)       _holderBaseEuler  = holder.localEulerAngles;
        if (forgeBellows != null) _bellowsBaseEuler = forgeBellows.localEulerAngles;

        _prevT = 0f;
        _cycleDetectionInitialized = false;
    }
    
    private void Update()
    {
        UpdatePressRateDecay();

        float speed = CalculateSpeedFromPressRate();
        float t     = UpdateCycleAndGetT(speed);

        UpdateTransforms(t);

        // сначала следим за сменой pendingPotion и обновляем UI
        UpdatePotionProgressUI();

        // потом считаем циклы (и там же добавляем прогресс по готовке)
        HandleCycleAndVFXAndPotionCooking(t, speed);
    }

    /// <summary>
    /// Затухание частоты нажатий.
    /// </summary>
    private void UpdatePressRateDecay()
    {
        if (_pressRate <= 0f)
            return;

        _pressRate = Mathf.Max(0f, _pressRate - pressDecayPerSecond * Time.deltaTime);
    }

    /// <summary>
    /// Рассчитываем скорость движения мехов на основе _pressRate.
    /// </summary>
    private float CalculateSpeedFromPressRate()
    {
        float speed01 = Mathf.Clamp01(_pressRate / maxPressRate);
        return Mathf.Lerp(minSpeed, maxSpeed, speed01);
    }

    /// <summary>
    /// Обновляет _cycle и возвращает t (0..1) через PingPong.
    /// </summary>
    private float UpdateCycleAndGetT(float speed)
    {
        if (speed > 0.0001f)
            _cycle += speed * Time.deltaTime;

        return Mathf.PingPong(_cycle, 1f);
    }

    /// <summary>
    /// Обновляет повороты holder и мехов по t.
    /// </summary>
    private void UpdateTransforms(float t)
    {
        // HOLDER
        if (holder != null)
        {
            float y = Mathf.LerpAngle(holderMinY, holderMaxY, t);
            holder.localRotation = Quaternion.Euler(_holderBaseEuler.x, y, _holderBaseEuler.z);
        }

        // BELLOWS
        if (forgeBellows != null)
        {
            // t == 0 → bellowsMaxX (максимум сжатия)
            // t == 1 → bellowsMinX
            float x = Mathf.LerpAngle(bellowsMaxX, bellowsMinX, t);

            forgeBellows.localRotation = Quaternion.Euler(
                x,
                _bellowsBaseEuler.y,
                _bellowsBaseEuler.z
            );
        }
    }

    /// <summary>
    /// Слежение за pendingPotion и плавное обновление прогресса в UI.
    /// </summary>
    private void UpdatePotionProgressUI()
    {
        var pending = G.run.pendingPotion;

        // Если зелье сменилось (или стало null) — сбрасываем прогресс
        if (pending != _lastPendingPotion)
        {
            _lastPendingPotion = pending;
            _potionCyclesDone = 0;
            _potionFill01 = 0f;

            if (G.main.cauldron != null && G.main.cauldron.cauldronUI != null)
            {
                G.main.cauldron.cauldronUI.SetPotionCookFillAmount(0f);
            }
        }

        if (pending == null)
        {
            // можно плавно опустить ползунок к 0, если хочешь
            if (_potionFill01 > 0f)
            {
                _potionFill01 = Mathf.MoveTowards(_potionFill01, 0f, fillLerpSpeed * Time.deltaTime);
                if (G.main.cauldron != null && G.main.cauldron.cauldronUI != null)
                    G.main.cauldron.cauldronUI.SetPotionCookFillAmount(_potionFill01);
            }

            return;
        }

        // Целевое значение заполнения = (сделанные циклы) / (нужные циклы)
        int neededCycles = Mathf.Max(1, pending.cyclePerCooking);
        float targetFill = Mathf.Clamp01((float)_potionCyclesDone / neededCycles);

        // Плавно приближаемся к целевому
        _potionFill01 = Mathf.MoveTowards(_potionFill01, targetFill, fillLerpSpeed * Time.deltaTime);

        if (G.main.cauldron != null && G.main.cauldron.cauldronUI != null)
            G.main.cauldron.cauldronUI.SetPotionCookFillAmount(_potionFill01);
    }

    /// <summary>
    /// Подсчёт циклов, запуск VFX и добавление прогресса готовки зелья.
    /// </summary>
    private void HandleCycleAndVFXAndPotionCooking(float t, float speed)
    {
        // первый кадр – просто инициализируем, чтобы не триггерить на старте
        if (!_cycleDetectionInitialized)
        {
            _prevT = t;
            _cycleDetectionInitialized = true;
            return;
        }

        // если мехи стоят (скорость маленькая) — не считаем цикл
        if (speed <= 0.0001f)
        {
            _prevT = t;
            return;
        }

        bool wasAwayFromMax = _prevT > maxPosThresholdT;   // раньше были не в максимуме
        bool nowAtMax      = t <= maxPosThresholdT;        // сейчас близко к максимальному сжатию
        bool movingToMax   = t < _prevT;                   // t уменьшается: идём 1 → 0 (min → max)

        if (wasAwayFromMax && nowAtMax && movingToMax)
        {
            // закончили один ход min → max
            _cycleCount++;

            // === Прогресс по готовке зелья ===
            var pending = G.run.pendingPotion;
            if (pending != null)
            {
                int neededCycles = Mathf.Max(1, pending.cyclePerCooking);

                if (_potionCyclesDone < neededCycles)
                {
                    _potionCyclesDone++;

                    // Если достигли нужного количества циклов — зелье готово
                    if (_potionCyclesDone >= neededCycles)
                    {
                        // тут можно, например, автоматически сварить зелье
                        // (если логика у тебя в Cauldron)
                        if (G.main.cauldron != null)
                        {
                            G.main.cauldron.CookPotion();
                        }
                    }
                }
            }

            // === VFX по циклам мехов ===
            if (_cycleCount >= cyclesPerVFX)
            {
                TriggerVFX();
                _cycleCount = 0; // начинаем новый набор для VFX
            }
        }

        _prevT = t;
    }
    
    public void Activate() => Push();

    private void Push()
    {
        float now = Time.time;

        if (_lastPressTime >= 0f)
        {
            float dt = Mathf.Max(0.0001f, now - _lastPressTime);
            float instantaneous = 1f / dt;
            _pressRate = Mathf.Lerp(_pressRate, instantaneous, 0.7f);
        }
        else
        {
            _pressRate = Mathf.Max(_pressRate, 1f);
        }

        _lastPressTime = now;
    }
    
    private void TriggerVFX()
    {
        if (bellowsVFX != null)
            bellowsVFX.Play();
        else
            Debug.Log("Bellows VFX triggered, но ParticleSystem не назначен.");
    }   
}
