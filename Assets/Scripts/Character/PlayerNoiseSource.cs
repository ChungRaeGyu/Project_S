using UnityEngine;

// 플레이어에 부착. 이동 속도를 감지해서 "소리(청각 단서)"를 내고 있는지 여부를 노출한다.
// MonsterAI가 이 값을 읽어서 청각 탐지에 사용한다.
public class PlayerNoiseSource : MonoBehaviour
{
    [Tooltip("이 속도(m/s) 이상으로 움직이면 소리를 내는 것으로 간주")]
    public float noiseSpeedThreshold = 0.1f;

    public bool IsMakingNoise { get; private set; }

    Rigidbody rb;
    Vector3 lastPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;
    }

    void Update()
    {
        float speed = rb != null
            ? rb.linearVelocity.magnitude
            : (transform.position - lastPosition).magnitude / Time.deltaTime;

        IsMakingNoise = speed >= noiseSpeedThreshold;
        lastPosition = transform.position;
    }
}
