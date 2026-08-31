using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;



public class Player : MonoBehaviour
{
    // --- 컴포넌트 캐싱 ---
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D col;
    private PlayerInputSystem playerInputSystem;

    // --- 주입된 능력 ---
    private IAbility _ability;

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 16f;
    [SerializeField] private float initPosX = -8f;
    [SerializeField] private float recoverDelay = 3f;
    [SerializeField] private float recoverSpeed = 2f;
    [SerializeField] private float defaultGravity = 3f;
    [SerializeField] private float invincibleTime = 3f;

    [Header("Visual Elements")]
    [SerializeField] private GameObject deathParticle;
    [SerializeField] private ParticleSystem trailParticle;

    private Vector3 defaultTrailLocalPos;
    private float outOfPosTimer = 0f;
    private bool isRecovering = false;
    private bool isDead;
    private bool isInvincible = false;
    private bool waitingForFirstInput = false;

    private void Awake()
    {
        BindComponents();
        InitInputSystem();


        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        waitingForFirstInput = true;

        if (trailParticle != null)
        {
            trailParticle.Stop();
            trailParticle.Clear(); // 잔상 제거
        }
    }
    private void BindComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();

        if (trailParticle != null)
            defaultTrailLocalPos = trailParticle.transform.localPosition;
    }

    private void InitInputSystem()
    {
        playerInputSystem = new PlayerInputSystem();
        playerInputSystem.Enable();
        playerInputSystem.Player.Disable(); // 시작 전엔 비활성화
        playerInputSystem.UI.Enable();
    }

    // --- 외부 주입 (의존성 주입) ---
    public void SetAbility(IAbility ability)
    {
        _ability = ability;
        _ability.Initialize(this);
    }


    private void OnEnable()
    {
        playerInputSystem.Player.Jump.performed += OnJump;
        playerInputSystem.UI.Cancel.performed += OnCancel;
    }

    private void OnDisable()
    {
        playerInputSystem.Disable();
        playerInputSystem.Player.Jump.performed -= OnJump;
        playerInputSystem.UI.Cancel.performed -= OnCancel;
    }

    private void Update()
    {
        if (GameManager.Instance.State == GameState.Ready)
            HandleReadyPosition();

        // 1. 위치 이탈 시 복귀 로직 (리팩토링: 메서드 분리)
        HandlePositionRecovery();

        // 2. 주입된 능력의 업데이트 실행
        _ability?.OnUpdate();

    }


    private void FixedUpdate()
    {
        if (isInvincible)
            ClampPositionWhileInvincible();
    }

    // --- 비즈니스 로직 분리 ---

    private void HandleReadyPosition()
    {
        float clampedY = Mathf.Clamp(transform.position.y, -2.985f, 2.985f);
        transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
    }

    private void HandlePositionRecovery()
    {
        if (transform.position.x >= initPosX)
        {
            outOfPosTimer = 0f;
            return;
        }

        if (!isRecovering)
        {
            outOfPosTimer += Time.deltaTime;
            if (outOfPosTimer >= recoverDelay)
                isRecovering = true;
        }

        if (isRecovering)
        {
            Vector3 targetPos = new Vector3(initPosX, transform.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, recoverSpeed * Time.deltaTime);

            if (Mathf.Abs(transform.position.x - initPosX) < 0.01f)
            {
                transform.position = targetPos;
                isRecovering = false;
                outOfPosTimer = 0f;
            }
        }
    }

    private void ClampPositionWhileInvincible()
    {
        float clampedY = Mathf.Clamp(rb.position.y, -2.985f, 2.985f);
        if (rb.position.y != clampedY)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.position = new Vector2(rb.position.x, clampedY);
        }
    }

    // --- 게임 상태 제어 ---

    public void Init()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (playerInputSystem == null) playerInputSystem = new PlayerInputSystem();

        // [수정] 게임 시작 시점에 중력과 파티클을 가동합니다.
        rb.gravityScale = defaultGravity; // 기본 중력값(3f) 복구
        rb.linearVelocity = Vector2.zero;

        if (trailParticle != null)
        {
            trailParticle.Play(); // 트레일 재생 시작
        }

        playerInputSystem.Player.Enable();
        isDead = false;
        waitingForFirstInput = false;

        // 나머지 데이터 초기화
        moveSpeed = 16f;
        initPosX = -8f;
        recoverDelay = 3f;
        recoverSpeed = 2f;
        defaultGravity = 3f;
        invincibleTime = 3f;
    }

    public void PlayerStartPosition() => transform.position = new Vector3(initPosX, 0f, 0f);

    private void OnJump(InputAction.CallbackContext context)
    {
        if (IsPointerOverUI()) return;

        if (waitingForFirstInput)
        {
            RestoreGravity();
            return;
        }

        HandleGravityInversion();

        // 사운드 로직
        //if (rb.gravityScale > 0)
        //    SoundManager.Instance.PlaySFX("PlayerDown");   // 천장에서 바닥으로
        //else
        //    SoundManager.Instance.PlaySFX("PlayerUp");     // 바닥에서 천장으로
    }

    private void HandleGravityInversion()
    {
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale *= -1;
        rb.linearVelocity = new Vector2(0f, rb.gravityScale > 0 ? -moveSpeed : moveSpeed);

        sr.flipY = rb.gravityScale < 0;

        // 트레일 파티클 위치 조정
        if (trailParticle != null)
        {
            Vector3 pos = defaultTrailLocalPos;
            if (sr.flipY) pos.y = -defaultTrailLocalPos.y;
            trailParticle.transform.localPosition = pos;
            trailParticle.Play();
        }
    }

    public void Pause()
    {
        playerInputSystem.Player.Disable();
    }

    public void ReStart()
    {
        playerInputSystem.Player.Enable();
    }



    // --- 충돌 판정 (핵심 리팩토링) ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead || isInvincible) return;

        if (collision.CompareTag("DeadZone")) GameOver();
        else if (collision.CompareTag("Obstacle"))
        {
            // Execute를 실행하고 그 결과가 true(방어성공)면 GameOver를 넘김
            if (_ability != null && _ability.Execute()) return;

            GameOver();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || isInvincible) return;

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (_ability != null && _ability.Execute()) return;

            GameOver();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
            Debug.Log($"OnCollisionStay2D - {collision.gameObject.name}");
    }

    private void GameOver()
    {
        if (isDead) return;
        isDead = true;

        SoundManager.Instance.PlaySFX("Dead");
        Instantiate(deathParticle, transform.position, Quaternion.identity);

        sr.enabled = false;
        rb.simulated = false;
        col.enabled = false;
        playerInputSystem.Player.Disable();

        EventBus.Publish(new GameOverEvent(false));
    }

    public void Revive()
    {
        // 1. 위치 및 상태 초기화
        PlayerStartPosition();
        sr.enabled = true;
        rb.simulated = true;
        col.enabled = true;
        sr.flipY = false;
        isDead = false; // Init 대신 여기서 직접 초기화

        // 2. 물리 정지 (첫 터치 대기 상태)
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        waitingForFirstInput = true;

        // 3. 입력 시스템 활성화 (터치 가능하게)
        if (playerInputSystem == null) playerInputSystem = new PlayerInputSystem();
        playerInputSystem.Player.Enable();

        // 4. 무적 효과 시작
        TriggerInvincible(3f).Forget();
    }


    public async UniTaskVoid TriggerInvincible(float duration)
    {
        if (isDead) return;

        isInvincible = true;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Obstacle"), true);

        float timer = 0f;
        while (timer < duration)
        {
            sr.enabled = !sr.enabled;
            await UniTask.Delay(100, ignoreTimeScale: true);
            timer += 0.1f;

            if (isDead) break;
        }

        sr.enabled = true;
        isInvincible = false;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Obstacle"), false);

        if (waitingForFirstInput)
        {
            RestoreGravity();
        }
    }

    private void RestoreGravity()
    {
        rb.gravityScale = defaultGravity;
        waitingForFirstInput = false;
    }

    private bool IsPointerOverUI()
    {
#if UNITY_EDITOR
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
#else
        if (EventSystem.current == null) return false;
        return Input.touchCount > 0 ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId) : EventSystem.current.IsPointerOverGameObject();
#endif
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        var gm = GameManager.Instance;
        if (gm.State == GameState.GameOver) return;
        EventBus.Publish(new OnPauseEvent(gm.State != GameState.Paused));
    }



}
