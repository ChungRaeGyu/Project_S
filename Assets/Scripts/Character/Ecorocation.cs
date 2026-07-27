using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ecorocation : MonoBehaviour
{
    private Coroutine wave;
    public float maxRadius = 30f;
    public float speed = 15f;
    public LayerMask revealLayer;

    private float currentRadius;

    private readonly Collider[] hits = new Collider[100];

    private HashSet<EchoObject> revealedObjects = new();
    [SerializeField] private EchoWave waveprefab;
    internal void OnEcoLocation()
    {
        //시간제한 하고
        if(wave != null)
        {
            StopCoroutine(wave);
        }

        
        wave = StartCoroutine(CWave());
    }
    IEnumerator CWave()
    {
        Instantiate(waveprefab,transform.position, Quaternion.identity);
        revealedObjects.Clear();
        //maxRadius까지 찍어놓고 순차적으로 켜줄까 그냥 음 근데 만약에 그렇게 하면 연타를 막아도 다 보이고 나서 그래야한단 말이지
        currentRadius = 0;
        //이렇게 하면 앞에것도 계속 찍는다.
        //이거보다는 음파같은게 날아가면서 찍힌 애들만 다 살짝 보였다 사라지는?
        while (currentRadius < maxRadius)
        {
            float lastRadius = currentRadius;
            currentRadius += speed * Time.deltaTime;

            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                currentRadius,
                hits,
                revealLayer);

            for (int i = 0; i < count; i++)
            {
                if (!hits[i].TryGetComponent(out EchoObject echo))
                    continue;

                float dist = Vector3.Distance(
                    transform.position,
                    hits[i].transform.position);

                // 이번 프레임에 음파가 지나간 경우만
                if (dist >= lastRadius && dist < currentRadius)
                {
                    // 중복 방지
                    if (revealedObjects.Add(echo))
                    {
                        echo.Reveal();
                    }
                }
            }

            yield return null;
        }
    }
}