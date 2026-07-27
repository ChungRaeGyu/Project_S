using System;
using System.Collections;
using UnityEngine;

public class EchoObject : MonoBehaviour
{
    public float visibleTime = 1.5f;

    float timer;

    MeshRenderer render;

    MaterialPropertyBlock block; //최적화를 위한

    static readonly int Alpha = Shader.PropertyToID("_Reveal");

    [SerializeField] float revealDuration = 0.2f; // 나타나는 시간
    [SerializeField] float visibleDuration = 1.0f; // 유지 시간
    [SerializeField] float hideDuration = 0.5f;    // 사라지는 시간

    Coroutine revealCoroutine;


    void Awake()
    {
        render = GetComponent<MeshRenderer>();

        block = new MaterialPropertyBlock();
    }

    public void Reveal()
    {
        if (revealCoroutine != null)
            StopCoroutine(revealCoroutine);

        revealCoroutine = StartCoroutine(RevealRoutine());
    }

    IEnumerator RevealRoutine()
    {
        // 0 → 1
        float t = 0;

        while (t < revealDuration)
        {
            t += Time.deltaTime;
            SetReveal(Mathf.Clamp01(t / revealDuration));
            yield return null;
        }

        SetReveal(0.7f);

        // 유지
        yield return new WaitForSeconds(visibleDuration);

        // 1 → 0
        t = 0;

        while (t < hideDuration)
        {
            t += Time.deltaTime;
            SetReveal(1 - Mathf.Clamp01(t / hideDuration));
            yield return null;
        }

        SetReveal(0);

        revealCoroutine = null;
    }

    void SetReveal(float value)
    {
        value = Mathf.Min(value, 0.7f);
        render.GetPropertyBlock(block);
        block.SetFloat(Alpha, value);
        render.SetPropertyBlock(block);
    }
}