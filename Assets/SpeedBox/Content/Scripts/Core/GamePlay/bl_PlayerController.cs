using UnityEngine;
using System.Collections;

public class bl_PlayerController : Singleton<bl_PlayerController>
{
    [Header("Settings")]
    public float MoveSpeed = 2;
    public float FlipLerp = 6;
    public Vector2 HorizontalClamp;
    public Vector2 VerticalClamp;

    [Header("Scale Settings")]
    public bool UseScale = true;
    public bl_PlayerScale m_Scale = new bl_PlayerScale();

    private Transform m_Transform;
    private float MoveDirection = 0;
   [SerializeField] private bool CanControll = false;

    private bl_ChangerManager ChangerManager;
    private float buttonRate;
    private bool isRegister = false;
    private Animator m_Anim;

    /// <summary>
    /// 
    /// </summary>
    void Awake()
    {
        m_Anim = GetComponent<Animator>();
        ChangerManager = bl_ChangerManager.Instance;
        m_Transform = GetComponent<Transform>();
        SetupScene();
    }

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        m_Scale.Init(transform);
    }

    /// <summary>
    /// 
    /// </summary>
    void InitSequence()
    {
        CanControll = false;
        m_Anim.enabled = true;
        StopCoroutine("WaitInit");
        StartCoroutine("WaitInit");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator WaitInit()
    {
        yield return new WaitForSeconds(0.5f);
        m_Anim.Play("InitGame", 0, 0);
        yield return new WaitForSeconds(m_Anim.GetCurrentAnimatorClipInfo(0).Length);
        m_Anim.enabled = false;
        CanControll = true;
    }

    /// <summary>
    /// 
    /// </summary>
    void SetupScene()
    {
        if (Speedbox.bl_Utils.IsMobile)
        {
            QualitySettings.antiAliasing = 1;
        }
        else
        {
            QualitySettings.antiAliasing = 3;
        }
    }

    void OnEnable()
    {
        if (!isRegister)
        {
            Speedbox.bl_Event.Global.AddListener<Speedbox.bl_GlobalEvents.OnStartPlay>(OnStartPlay);
            Speedbox.bl_Event.Global.AddListener<Speedbox.bl_GlobalEvents.OnFailGame>(OnFailGame);
            Speedbox.bl_Event.Global.AddListener<Speedbox.bl_GlobalEvents.OnPoint>(OnPoint);
            Speedbox.bl_Event.Global.AddListener<Speedbox.bl_GlobalEvents.OnChangeSide>(OnChangeSide);
            isRegister = true;
        }
    }

    void OnDestroy()
    {
        Speedbox.bl_Event.Global.RemoveListener<Speedbox.bl_GlobalEvents.OnStartPlay>(OnStartPlay);
        Speedbox.bl_Event.Global.RemoveListener<Speedbox.bl_GlobalEvents.OnFailGame>(OnFailGame);
        Speedbox.bl_Event.Global.RemoveListener<Speedbox.bl_GlobalEvents.OnPoint>(OnPoint);
        Speedbox.bl_Event.Global.RemoveListener<Speedbox.bl_GlobalEvents.OnChangeSide>(OnChangeSide);
    }

    void OnStartPlay(Speedbox.bl_GlobalEvents.OnStartPlay e)
    {
        StopAllCoroutines();
        CanControll = true;
        InitSequence();
    }


    /// <summary>
    /// 
    /// </summary>
    void OnFailGame(Speedbox.bl_GlobalEvents.OnFailGame e)
    {
        MoveDirection = 0;
        CanControll = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    void OnPoint(Speedbox.bl_GlobalEvents.OnPoint e)
    {
      
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    void OnChangeSide(Speedbox.bl_GlobalEvents.OnChangeSide e)
    {
        if (!e.byFlip)
        {
            ChangeScaleState(bl_PlayerScale.State.Change);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void Update()
    {
        if (CanControll)
        {
            MoveControl();
            Move();
            ClampPosition();
        }
        else { MoveDirection = 0; }

        if (UseScale)
        {
            m_Scale.OnUpdate();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void MoveControl()
    {
        if (Speedbox.bl_Utils.IsMobile)
        {
            for (var i = 0; i < Input.touchCount; ++i)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved)
                {
                    float buttonsize = Screen.width / 3;
                    if (touch.position.x >= 0 && touch.position.x < buttonsize)
                    {
                        MoveDirection = -1;
                        ChangeScaleState(bl_PlayerScale.State.Move);
                    }
                    else if (touch.position.x >= buttonsize && touch.position.x < (buttonsize * 2) && buttonRate < Time.time)
                    {
                        buttonRate = Time.time + 0.2f;
                        Flip();
                    }
                    else if (touch.position.x >= (buttonsize * 2))
                    {
                        MoveDirection = 1;
                        ChangeScaleState(bl_PlayerScale.State.Move);
                    }
                }

                if (touch.phase == TouchPhase.Ended)
                {
                    MoveDirection = 0;
                }
            }
        }
        else
        {
             if (InputLeftKey)
             {
                 MoveDirection = -1;
                 ChangeScaleState(bl_PlayerScale.State.Move);
             }

             if (InputRightKey)
             {
                 MoveDirection = 1;
                 ChangeScaleState(bl_PlayerScale.State.Move);
             }

             if (InputUpKeys)
             {
                 MoveDirection = 0;
             }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Flip();
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void Flip()
    {
        bl_ChangerManager.Instance.Flip();
        ChangeScaleState(bl_PlayerScale.State.Flip);
    }

    /// <summary>
    /// 
    /// </summary>
    void Move()
    {
        if (MoveDirection == 0)
            return;

        Vector3 p = m_Transform.position;
        switch (ChangerManager.Side)
        {
            case LevelSides.Down:
                p.x += MoveDirection * (MoveSpeed * Time.timeScale);
                break;
            case LevelSides.Left:
                p.y -= MoveDirection * (MoveSpeed * Time.timeScale);
                break;
            case LevelSides.Right:
                p.y += MoveDirection * (MoveSpeed * Time.timeScale);
                break;
            case LevelSides.Up:
                p.x -= MoveDirection * (MoveSpeed * Time.timeScale);
                break;
        }

        m_Transform.position = p;
    
    }

    /// <summary>
    /// 
    /// </summary>
    void ClampPosition()
    {
        Vector3 p = m_Transform.position;
        p.x = Mathf.Clamp(p.x, HorizontalClamp.x, HorizontalClamp.y);
        p.y = Mathf.Clamp(p.y, VerticalClamp.x, VerticalClamp.y);
        m_Transform.position = p;
    }


    /// <summary>
    /// 
    /// </summary>
    public void DoChanger(bool smooth = false)
    {
        if (!smooth)
        {
            SetYOffSet(ChangerManager.GetSideInfo.YOffSet);
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(SetYOffSetSmooth(ChangerManager.GetSideInfo.YOffSet));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    IEnumerator SetYOffSetSmooth(float value)
    {
        Vector3 p = m_Transform.position;
        switch (ChangerManager.Side)
        {
            case LevelSides.Down:
                p.y = value;
                break;
            case LevelSides.Left:
                p.x = value;
                break;
            case LevelSides.Right:
                p.x = value;
                break;
            case LevelSides.Up:
                p.y = value;
                break;
        }
        while(Vector3.Distance(m_Transform.position,p) > 0.005f)
        {
            m_Transform.position = Vector3.MoveTowards(m_Transform.position, p, Time.deltaTime * FlipLerp);
            yield return null;
        }
        m_Transform.position = p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    private void SetYOffSet(float value)
    {
        Vector3 p = m_Transform.position;
        switch (ChangerManager.Side)
        {
            case LevelSides.Down:
                p.y = value;
                break;
            case LevelSides.Left:
                p.x = value;
                break;
            case LevelSides.Right:
                p.x = value;
                break;
            case LevelSides.Up:
                p.y = value;
                break;
        }
        m_Transform.position = p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="state"></param>
    public void ChangeScaleState(bl_PlayerScale.State state)
    {
        if (state == bl_PlayerScale.State.Move)
            return;

        StopCoroutine("ChangeScaleStateIE");
        StartCoroutine("ChangeScaleStateIE",state);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    IEnumerator ChangeScaleStateIE(bl_PlayerScale.State state)
    {
        m_Scale.ChangeState(state);
        yield return new WaitForSeconds(m_Scale.TimeMaintain);
        m_Scale.ChangeState(bl_PlayerScale.State.Idle);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator DeathSecuence()
    {
        ChangeScaleState(bl_PlayerScale.State.Death);
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }

    private bool InputRightKey
    {
        get
        {
            if(Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                return true;
            }
            return false;
        }
    }

    private bool InputLeftKey
    {
        get
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                return true;
            }
            return false;
        }
    }

    private bool InputUpKeys
    {
        get
        {
            if (Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow))
            {
                return true;
            }
            return false;
        }
    }

    public float MovementValue
    {
        get
        {
            return MoveDirection;
        }
    }

    public static bl_PlayerController Instance
    {
        get
        {
            return ((bl_PlayerController)mInstance);
        }
        set
        {
            mInstance = value;
        }
    }
}