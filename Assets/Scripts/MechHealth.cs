using System;
using UnityEngine;

public class MechHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 600f;
    [SerializeField] private bool disableOnDestroyed = true;

    private float currentHealth;
    private bool isDestroyed;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float NormalizedHealth => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsDestroyed => isDestroyed;

    public event Action<float, float> OnHealthChanged;
    public event Action<float> OnDamaged;
    public event Action OnDestroyed;

    private void Awake()
    {
        currentHealth = Mathf.Max(1f, maxHealth);
    }

    public void SetMaximumHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    public bool TakeDamage(float damage)
    {
        if (isDestroyed || damage <= 0f)
        {
            return false;
        }

        float appliedDamage = Mathf.Min(damage, currentHealth);
        currentHealth -= appliedDamage;

        OnDamaged?.Invoke(appliedDamage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            DestroyMech();
        }

        return true;
    }

    public void RestoreHealth(float amount)
    {
        if (isDestroyed || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        ResetHealth(1f);
    }

    public void ResetHealth(float healthRatio)
    {
        isDestroyed = false;
        float clampedRatio = Mathf.Clamp01(healthRatio);

        if (clampedRatio >= 1f)
        {
            currentHealth = maxHealth;
        }
        else
        {
            // コストオーバー時は割合計算後の耐久値を10単位で切り捨てる。
            float reducedHealth = Mathf.Floor(maxHealth * clampedRatio / 10f) * 10f;
            currentHealth = Mathf.Max(1f, reducedHealth);
        }

        gameObject.SetActive(true);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void DestroyMech()
    {
        isDestroyed = true;
        currentHealth = 0f;
        OnDestroyed?.Invoke();

        if (disableOnDestroyed)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
    }
}
