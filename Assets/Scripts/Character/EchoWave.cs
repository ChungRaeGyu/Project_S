using UnityEngine;

public class EchoWave : MonoBehaviour
{

    [SerializeField] private float speed = 15f;
    [SerializeField] private float maxRadius = 20f;

    private SphereCollider sphereCollider;
    private float radius;

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
    }

    private void Update()
    {
        radius += speed * Time.deltaTime;

        sphereCollider.radius = radius;

        transform.localScale = Vector3.one * radius * 2f;

        if (radius >= maxRadius)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out EchoObject receiver))
        {
            receiver.Reveal();
        }
    }
}
