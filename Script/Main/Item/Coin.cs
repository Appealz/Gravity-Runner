using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Coin : PoolLabel
{
    private Rigidbody2D rb;
    [SerializeField] private ParticleSystem pickupParticle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.localScale = Vector3.one;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        SoundManager.Instance.PlaySFX("Coin");

        if (pickupParticle != null)
        {
            var particle = Instantiate(pickupParticle, transform.position, Quaternion.identity);
            particle.Play();
            Destroy(particle.gameObject, particle.main.duration + particle.main.startLifetime.constantMax);
        }

        // todo : 이벤트 버스 발행
        EventBus.Publish(new OnChangeCoin(1));

        ReturnPool();
    }
}
