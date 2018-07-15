using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Audio;
using System.IO;
using UnityEngine.SceneManagement;

public class bl_GameManager : Singleton<bl_GameManager> 
{

    [Header("Global")]
    public GameMode m_GameMode = GameMode.PickUp;
    public List<bl_LevelInfo> Levels = new List<bl_LevelInfo>();
    [Header("Settings")]
    [Range(0,1)]public float RateTimeScore = 0.25f;
    public Vector2 PerSecondPoints = new Vector2(3,7);
    [Header("Audio")]
	public AudioClip pointSound;
    [SerializeField]private AudioClip progresivePointSound;
	public AudioClip[] musics;
	public AudioSource audioSource;
    [SerializeField]private AudioMixerSnapshot NormalSnap;
    [SerializeField]private AudioMixerSnapshot PauseSnap;
    [SerializeField]private AudioSource SfxSource;
    [SerializeField]private AudioSource VoiceSource;
    [Header("References")]
	public Transform player;
    public Material[] LevelMats;
    [SerializeField]private Text LevelText;
    [SerializeField]private Text SubLevelText;
    [SerializeField]private Text ProgresiveScoreText;
    [SerializeField]private Image ScreenShotImage;
    [SerializeField]private Image FilledScoreImg;
    [HideInInspector]public Vector3 CacheDefaultPlayerScale;
    private Vector3 cacheDeafultPlayerPosition;
    [SerializeField]private GameObject[] GameModeScore;
    [SerializeField]private Animator ScorePAnim;
    [SerializeField]private Animator PauseAnim;

	private int _point = 0;
    private int PointByLevel = 0;
    private int NextScoreByLevel = 0;
    private float prevRealTime;
    private float thisRealTime;
    private int CacheLastScore;
    private int CacheScore;
    private int PlayTimes;
    private int currentObstacleLevel = 0;
    private int progresiveScore = 0;
    private int CurrentCompleteLevel;
    private bool isPause = false;
    private Texture2D cacheScreenShot;
    private bool LowSettings = false;
    private bl_Ad Ad;
    private AudioSource enemy_sound;

    AndroidJavaClass JavaClass2;

    public int point
	{
		set 
		{
			_point = value;
            CacheScore = point;

            if (!isGameOver) 
			{
                new Speedbox.bl_GlobalEvents.OnPoint(value);
                SetPoint(value);
			}
		}
		get 
		{
			return _point;
		}
	}
    private bool isGameOver;
    private int lastLevel = 0;

	public float ScrollSpeed
	{
		get 
		{
			return GetLevelSpeed();
		}
	}

    /// <summary>
    /// 
    /// </summary>
	void Awake()
	{
		isGameOver = true;
        LoadSettings();
        audioSource = GetComponent<AudioSource> ();
        Ad = FindObjectOfType<bl_Ad>();
		Application.targetFrameRate = 60;
        PlayRandomMusic();
        PauseSnap.TransitionTo(1);
		RenderSettings.ambientLight = Color.white;
        FilledScoreImg.fillAmount = 0;
        LevelText.canvasRenderer.SetAlpha(0);
        SubLevelText.canvasRenderer.SetAlpha(0);
        CacheDefaultPlayerScale = player.transform.localScale;
        cacheDeafultPlayerPosition = player.localPosition;
    }

    void LoadSettings()
    {
        int gm = PlayerPrefs.GetInt(KeyMasters.GameMode, 1);
        m_GameMode = (GameMode)gm;
        LowSettings = Speedbox.bl_Utils.PlayerPrefsX.GetBool(KeyMasters.Quality, true);
        bool aa = (PlayerPrefs.GetInt(KeyMasters.Sound, 1) == 1) ? true : false;
        AudioListener.volume = PlayerPrefs.GetFloat(KeyMasters.Volume, 1);
        if (!aa) { AudioListener.volume = 0; }
        ChangeQualityGame(LowSettings);

        foreach(GameObject g in GameModeScore) { g.SetActive(false); }
        GameModeScore[gm - 1].SetActive(true);

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="p"></param>
    void SetPoint(int p)
    {
        if (PlayerPrefs.GetInt(KeyMasters.Sound, 1) == 1)
        {
            SfxSource.PlayOneShot(pointSound);
        }

        PointByLevel++;
        float porc = (PointByLevel * 100) / NextScoreByLevel;
        // Debug.Log(porc + "% :" + PointByLevel + " / " + NextScoreByLevel);
        FilledScoreImg.fillAmount = porc / 100;
        bl_Showwave.Instance.Play("middle",Color.black);
        CheckClassicAchiviements();
        if (HaveMoreLevels)
        {
            if (isNewLevel())
            {
                OnSpeedLevel();
            }
        }
        if (m_GameMode == GameMode.Free)
        {
            bl_Shaker.Instance.Do(0);
        }
        else if (m_GameMode == GameMode.PickUp) { bl_Shaker.Instance.Do(4); }
    }

    /// <summary>
    /// 
    /// </summary>
    void CheckClassicAchiviements()
    {
        #if !UNITY_EDITOR && UNITY_ANDROID || UNITY_IPHONE || UNITY_IOS
      /*  if (PointByLevel >= 50 && PointByLevel < 100)
        {
            bl_PlayService.Instance.UnlockAchievement(PlayServiceKey.achievement_reached_50);
        }
        else if (PointByLevel >= 100 && PointByLevel < 250)
        {
            bl_PlayService.Instance.UnlockAchievement(PlayServiceKey.achievement_reached_100);
        }
        else if (PointByLevel >= 250 && PointByLevel < 500)
        {
            bl_PlayService.Instance.UnlockAchievement(PlayServiceKey.achievement_reached_250);
        }
        else if (PointByLevel >= 500 && PointByLevel < 1000)
        {
            bl_PlayService.Instance.UnlockAchievement(PlayServiceKey.achievement_reached_500);
        }
        else if (PointByLevel >= 1000)
        {
            bl_PlayService.Instance.UnlockAchievement(PlayServiceKey.achievement_reached_1000);
        }*/
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="points"></param>
    public void SetProgresivePoint(int points = 5)
    {
        if (isGameOver)
            return;

        if (PlayerPrefs.GetInt(KeyMasters.Sound, 1) == 1)
        {
            SfxSource.PlayOneShot(progresivePointSound);
        }
        progresiveScore += points;
        ProgresiveScoreText.text = progresiveScore.ToString("000000");
        bl_Shaker.Instance.Do(0);
        ScorePAnim.GetComponent<Text>().text = progresiveScore.ToString("000000");
        ScorePAnim.Play("point", 0, 0);
        bl_Showwave.Instance.Play("little", Color.black);
    }

    /// <summary>
    /// 
    /// </summary>
    void OnNewHighScore()
    {
        if (m_GameMode == GameMode.PickUp)
        {
            PlayerPrefs.SetInt(KeyMasters.BestScore, point);
        }
        else
        {
            PlayerPrefs.SetInt(KeyMasters.BestScoreProgresive, progresiveScore);
        }
        new Speedbox.bl_GlobalEvents.OnNewHighScore(true);
    }

    /// <summary>
    /// 
    /// </summary>
    void OnSpeedLevel()
    {
        bl_LevelInfo l = Levels[lastLevel];
        if (Levels[lastLevel].ObstaclesLevel > currentObstacleLevel)
        {
            currentObstacleLevel = Levels[lastLevel].ObstaclesLevel;
        }
        ShowLevelText(); 
        bl_SlowMotion.Instance.DoSlow(2, 0.1f,true);

        if (m_GameMode == GameMode.PickUp)
        {
            PointByLevel = 0;
            int last = GetLastScoreNeeded();
            NextScoreByLevel = GetNextScoreNeeded() - last;
        }

        //When reached a new level
        if (l.isNewLevel)
        {
            CurrentCompleteLevel++;
            bl_Stylish.Instance.ChangeStyle(CurrentCompleteLevel);
        }
        if (Levels[lastLevel].m_AudioClip != null)
        {
            VoiceSource.clip = Levels[lastLevel].m_AudioClip;
            VoiceSource.PlayDelayed(1);
        }
    }

    public void ShowLoaderboard()
    {
        //show your leaderboard code here
        if(m_GameMode == GameMode.Free)
        {
            //bl_PlayService.Instance.ShotThisLeaderboard(PlayServiceKey.leaderboard_ranking_free);
        }
        else
        {
           //bl_PlayService.Instance.ShotThisLeaderboard(PlayServiceKey.leaderboard_ranking_classic);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void ShowLevelText(float hideIn = 3)
    {
        if (string.IsNullOrEmpty(Levels[lastLevel].SubName))
        {
            LevelText.CrossFadeAlpha(1, 2, true);
            LevelText.text = Levels[lastLevel].Name;
            SubLevelText.CrossFadeAlpha(0, 2, true);
        }
        else
        {
            LevelText.text = Levels[lastLevel].Name;
            LevelText.CrossFadeAlpha(1, 1.5f, true);
            SubLevelText.text = Levels[lastLevel].SubName;
            SubLevelText.CrossFadeAlpha(1, 2, true);
        }
        Invoke("HideLevelText", hideIn);
    }

    void HideLevelText() { LevelText.CrossFadeAlpha(0, 2, true); SubLevelText.CrossFadeAlpha(0, 1.5f, true); }

    /// <summary>
    /// 
    /// </summary>
	public void StartGame()
    {
        NormalSnap.TransitionTo(1.5f);
        player.gameObject.SetActive(true);
        Speedbox.bl_Event.Global.DispatchEvent(new Speedbox.bl_GlobalEvents.OnStartPlay());
        bl_ChangerManager.Instance.Change(LevelSides.Down, false);

        //reset values
        _point = 0;
        PointByLevel = 0;
        progresiveScore = 0;

        VoiceSource.clip = Levels[0].m_AudioClip;
        VoiceSource.PlayDelayed(1);

        if (m_GameMode == GameMode.PickUp)
        {
            NextScoreByLevel = GetNextScoreNeeded();
        }
        else if (m_GameMode == GameMode.Free)
        {
            InvokeRepeating("ProgresiveScore", 1, RateTimeScore);
        }
        new Speedbox.bl_GlobalEvents.OnPoint(0);
        isGameOver = false;
        bl_Stylish.Instance.ChangeStyle(0);
        PlayTimes++;
        ShowLevelText(1.5f);
        StopCoroutine("TakeScreenShot");
    }

    /// <summary>
    /// 
    /// </summary>
    public void Pause()
    {
        isPause = !isPause;
        new Speedbox.bl_GlobalEvents.OnPause(isPause);        
        if (isPause)
        {
            PauseSnap.TransitionTo(0.01f);
            PauseAnim.gameObject.SetActive(true);
            PauseAnim.SetBool("show", isPause);
        }
        else
        {
            PauseAnim.SetBool("show", isPause);
            StartCoroutine(Speedbox.bl_Utils.AnimatorUtils.WaitAnimationLenghtForDesactive(PauseAnim));
            NormalSnap.TransitionTo(1.5f);
        }
        Time.timeScale = (isPause) ? 0 : 1;
    }


    /// <summary>
    /// 
    /// </summary>
	public void GameOver()
    {
        if (isGameOver)
            return;

        StopAllCoroutines();
        if (isPause)
        {
            Pause();
        }
        PauseAnim.gameObject.SetActive(false);
        isGameOver = true;
        SetScores();
        bl_Showwave.Instance.Play("full", Color.white, bl_Showwave.Type.Full);
        //Per gamemode logic finish
        if (m_GameMode == GameMode.PickUp)
        {
        }
        else if (m_GameMode == GameMode.Free)
        {
            CancelInvoke("ProgresiveScore");
        }
        //Take Screen Shot
        ScreenShotImage.gameObject.SetActive(false);

        StartCoroutine("TakeScreenShot");
        
        PauseSnap.TransitionTo(1);
        new Speedbox.bl_GlobalEvents.OnFailGame();
        bl_Shaker.Instance.Do(1);
        Ad.SetTry();
        Reset();
      
        //play gameoversound

    }

    public Texture2D LoadTexture()
    {
#if !UNITY_WEBPLAYER
        string str = "MyRecordImage.png";
        path = Application.persistentDataPath + "/" + str;
        Texture2D textured = new Texture2D(1, 1);
        if (File.Exists(path))
        {
            byte[] data = File.ReadAllBytes(path);
            textured.LoadImage(data);
        }
        return textured;
#else
        return null;
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private bool takeInProgress = false;
#if !UNITY_WEBPLAYER
    string path = "";
#endif
    IEnumerator TakeScreenShot()
    {
        takeInProgress = true;
        yield return new WaitForSeconds(6);
        int width = Screen.width;
        int height = Screen.height;

        yield return new WaitForEndOfFrame();
        Texture2D textured = new Texture2D(width, height, TextureFormat.RGB24, true);
        textured.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        textured.Apply();
        cacheScreenShot = textured;
        Sprite s = Sprite.Create(cacheScreenShot, new Rect(0, 0, cacheScreenShot.width, cacheScreenShot.height), new Vector2(0.5f, 0.5f));

        ScreenShotImage.sprite = s;
        ScreenShotImage.gameObject.SetActive(true);
        ScreenShotImage.canvasRenderer.SetAlpha(0);
        ScreenShotImage.CrossFadeAlpha(1, 0.5f, true);
        takeInProgress = false;
    }

    public void ShareScreenshot()
    {
        if (takeInProgress)
            return;

#if !UNITY_WEBPLAYER
        byte[] dataToSave = cacheScreenShot.EncodeToPNG();
        string str = "MyRecordImage.png";
        path = Application.persistentDataPath + "/" + str;
        File.WriteAllBytes(path, dataToSave);

        if (!Application.isEditor)
        {
            JavaClass2 = new AndroidJavaClass("com.lovattostudio.plugin.MainActivity");
            JavaClass2.CallStatic("ShareImage", "Head Line", "my Subject", path);
            Debug.Log("File exits: " + File.Exists(path) + " --- " + path);
        }
        else
        {
            Application.OpenURL(path);
        }
#endif
    }


    /// <summary>
    /// 
    /// </summary>
    void OnDisable()
    {
        if (m_GameMode == GameMode.Free)
        {
            CancelInvoke("ProgresiveScore");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void Reset()
    {
        FilledScoreImg.fillAmount = 0;
        lastLevel = 0;
        _point = 0;
        PointByLevel = 0;
        CurrentCompleteLevel = 0;
        player.localPosition = cacheDeafultPlayerPosition;
    }

    /// <summary>
    /// 
    /// </summary>
	void SetScores()
    {
        if (m_GameMode == GameMode.PickUp)
        {
            CacheScore = point;
            if (PlayTimes > 1)
            {
                PlayerPrefs.SetInt(KeyMasters.LastScore, CacheLastScore);
                CacheLastScore = point;
            }
            else
            {
                CacheLastScore = PlayerPrefs.GetInt(KeyMasters.LastScore, 0);
            }

            int best = PlayerPrefs.GetInt(KeyMasters.BestScore, 0);

            if (point > best)
            {
                OnNewHighScore();
            }
        }
        else if (m_GameMode == GameMode.Free)
        {
            CacheScore = progresiveScore;
            if (PlayTimes > 1)
            {
                PlayerPrefs.SetInt(KeyMasters.LastScoreProgresive, CacheLastScore);
                CacheLastScore = progresiveScore;
            }
            else
            {
                CacheLastScore = PlayerPrefs.GetInt(KeyMasters.LastScoreProgresive, 0);
            }

            int best = PlayerPrefs.GetInt(KeyMasters.BestScoreProgresive, 0);

            if (progresiveScore > best)
            {
                OnNewHighScore();
            }
        }
        PlayerPrefs.Save();
    }

    public void Quit()
    {
        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
    }

    /// <summary>
    /// 
    /// </summary>
	void PlayRandomMusic()
	{
		if (PlayerPrefs.GetInt (KeyMasters.Sound, 1) == 0)
			return;
		
		audioSource.clip = musics [Random.Range (0, musics.Length)];
		audioSource.Play ();
	}

    /// <summary>
    /// 
    /// </summary>
    void Update()
    {
        TimeManager();
        ScrollLevel();
        PCInputs();
    }

    void PCInputs()
    {
     /*   if (Speedbox.bl_Utils.IsMobile)
            return;*/

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Pause();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void ProgresiveScore()
    {
        if (m_GameMode != GameMode.Free)
        {
            CancelInvoke("ProgresiveScore");
            return;
        }

        int ps = (int)Random.Range(PerSecondPoints.x, PerSecondPoints.y);
        progresiveScore += ps;
        ProgresiveScoreText.text = progresiveScore.ToString("000000");
        CalculateProgresiveLevel();
    }

    /// <summary>
    /// 
    /// </summary>
    void CalculateProgresiveLevel()
    {
        if(progresiveScore > Levels[lastLevel].ProgresiveNeeded)
        {
            lastLevel++;
            OnSpeedLevel();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void TimeManager()
    {
        prevRealTime = thisRealTime;
        thisRealTime = Time.realtimeSinceStartup;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="low"></param>
    public void ChangeQualityGame(bool low)
    {
        LowSettings = low;
        if (low)
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.masterTextureLimit = 2;
            RenderSettings.fog = false;
        }
        else
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.antiAliasing = (Speedbox.bl_Utils.IsMobile) ? 1 : 3;
            QualitySettings.masterTextureLimit = 0;
            RenderSettings.fog = true;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void ScrollLevel()
    {
        foreach(Material m in LevelMats)
        {
            m.SetTextureOffset("_MainTex", new Vector2(0, -Time.time * ScrollSpeed));
        }
    }

    public bool HaveMoreLevels
    {
        get
        {
            return !(lastLevel > Levels.Count - 1);
        }
    }

    public int GetCurrentObstacleLevel
    {
        get
        {
            return currentObstacleLevel;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public int GetLevelSpeed()
    {
        int speed = Levels[0].Speed;
        if (m_GameMode == GameMode.PickUp)
        {
            foreach (bl_LevelInfo info in Levels)
            {
                if (point > info.PointsNeeded)
                {
                    speed = info.Speed;
                }
            }
        }
        else if (m_GameMode == GameMode.Free)
        {
            speed = Levels[lastLevel].Speed;
        }
        return speed;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public int GetNextScoreNeeded()
    {
        int speed = Levels[lastLevel].PointsNeeded;
      
        return speed;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public int GetLastScoreNeeded()
    {
        int speed = (lastLevel > 0) ? Levels[lastLevel - 1].PointsNeeded : Levels[0].PointsNeeded;

        return speed;
    }

    /// <summary>
    /// 
    /// </summary>
    public float deltaTime
    {
        get
        {
            if (Time.timeScale > 0f) return Time.deltaTime / Time.timeScale;
            return Time.realtimeSinceStartup - prevRealTime; // Checks realtimeSinceStartup again because it may have changed since Update was called
        }
    }

    private bool isNewLevel()
    {
       if(point > Levels[lastLevel].PointsNeeded)
        {
            lastLevel++;
            return true;
        }
        return false;
    }

    public int GetCacheScore { get { return CacheScore; } }

    public int CurrentLevel
    {
        get
        {
            return lastLevel + 1;
        }
    }

    public static bl_GameManager Instance
    {
        get
        {
            return ((bl_GameManager)mInstance);
        }
        set
        {
            mInstance = value;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Reset Scores")]
    void ResetScores()
    {
        PlayerPrefs.DeleteKey(KeyMasters.BestScore);
        PlayerPrefs.DeleteKey(KeyMasters.LastScore);
        PlayerPrefs.DeleteKey(KeyMasters.BestScoreProgresive);
        PlayerPrefs.DeleteKey(KeyMasters.LastScoreProgresive);
    }
#endif
}