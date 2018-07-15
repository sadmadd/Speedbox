﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class bl_UIManager : MonoBehaviour {

	[SerializeField]private Text ScoreText;
    [SerializeField]private Text LastScoreText;
    [SerializeField]private Text BestScoreText;
    [SerializeField]private Text GameOverScoreText;
    [SerializeField]private Text InstructionText;
    [SerializeField]private Text FlipText;
    [SerializeField]private Text LevelGameOverText;
    [SerializeField]private Text QualityText;
    [SerializeField]private Text AudioEnableText;
    [SerializeField]private Text TapInitText;
    [SerializeField]private Slider VolumeSlider;
    
    [SerializeField]private Animator MenuAnim;
    [SerializeField]private Animator BeginAnim;
    [SerializeField]private Animator NewHighScoreAnim;
    [SerializeField]private Animator PointScoreAnim;
    [SerializeField]private Animator PMWindows;

    [SerializeField]private GameObject NewHighScoreBox;
    [SerializeField]private GameObject ScoreUI;
    [SerializeField]private GameObject[] PauseWindows;
    [SerializeField]private GameObject[] PlayServiceGo;

    private bool quality;
    private bool audioEnable;

    void Start()
    {
        LoadPrefs();
        SetupScore();
        OnPlayAgain();
    } 

    void OnEnable()
    {
        Speedbox.bl_Event.Global.AddListener<Speedbox.bl_GlobalEvents.OnStartPlay>(OnStartPlay);
        Speedbox.bl_Event.Global.AddListener<Speedbox.bl_GlobalEvents.OnFailGame>(OnFailGame);
        Speedbox.bl_Event.Global.AddListener<Speedbox.bl_GlobalEvents.OnPoint>(OnPoint);
        Speedbox.bl_Event.Global.AddListener<Speedbox.bl_GlobalEvents.OnNewHighScore>(OnNewHigScore);
    }

    void OnDisable()
    {
        Speedbox.bl_Event.Global.RemoveListener<Speedbox.bl_GlobalEvents.OnStartPlay>(OnStartPlay);
        Speedbox.bl_Event.Global.RemoveListener<Speedbox.bl_GlobalEvents.OnFailGame>(OnFailGame);
        Speedbox.bl_Event.Global.RemoveListener<Speedbox.bl_GlobalEvents.OnPoint>(OnPoint);
        Speedbox.bl_Event.Global.RemoveListener<Speedbox.bl_GlobalEvents.OnNewHighScore>(OnNewHigScore);
    }

    void OnStartPlay(Speedbox.bl_GlobalEvents.OnStartPlay e)
    {
        ScoreText.gameObject.SetActive(true);
        if (bl_GameManager.Instance.m_GameMode == GameMode.PickUp)
        {
            ScoreUI.SetActive(true);
        }
        NewHighScoreBox.SetActive(false);
        InstructionText.CrossFadeColor(new Color(0, 0, 0, 1), 2, true, true);
        BeginAnim.SetBool("show", false);
        StartCoroutine(WaitForDesactive(BeginAnim.gameObject, BeginAnim.GetCurrentAnimatorClipInfo(0).Length));
        Invoke("HideFirstText", 3);
    }

    void OnFailGame(Speedbox.bl_GlobalEvents.OnFailGame e)
    {
        SetupScore();
        ScoreText.gameObject.SetActive(false);
        StartCoroutine(WaitForShowGOUI());
    }

    public void OnPlayAgain()
    {        
        BeginAnim.gameObject.SetActive(true);
        if (bl_GameManager.Instance.m_GameMode == GameMode.PickUp)
        {
            ScoreUI.SetActive(true);
        }
        BeginAnim.SetBool("show", true);

        MenuAnim.SetBool("show", false);
        StartCoroutine(WaitForDesactive(MenuAnim.gameObject, MenuAnim.GetCurrentAnimatorClipInfo(0).Length));
    }

    void OnNewHigScore(Speedbox.bl_GlobalEvents.OnNewHighScore e)
    {
        if (e.OnFinish)
        {
            StartCoroutine(PlayAnimationDelay(NewHighScoreAnim, "show", 3.5f, true));
            NewHighScoreBox.SetActive(true);
        }
    }

    public void ChangePauseWindow(int id)
    {
        PMWindows.Play("change", 0, 0);
        StartCoroutine(WaitActiveInArray(PauseWindows, id,0.2f));
    }

    IEnumerator PlayAnimationDelay(Animator anim,string animClip,float delay,bool desactive = false,float timeanim = 0)
    {
        yield return new WaitForSeconds(delay);
        anim.gameObject.SetActive(true);
        anim.Play(animClip, 0, timeanim);
        if (desactive)
        {
            StartCoroutine(WaitForDesactive(anim.gameObject, anim.GetCurrentAnimatorStateInfo(0).length));
        }
    }

    private IEnumerator WaitActiveInArray(GameObject[] obj, int index, float time)
    {
        yield return StartCoroutine(Speedbox.bl_Utils.CorrutinesUtils.WaitForRealSeconds(time));
        foreach (GameObject o in obj) { o.SetActive(false); }
        obj[index].SetActive(true);
    }

    IEnumerator WaitForShowGOUI()
    {
        yield return new WaitForSeconds(1.5f);
        MenuAnim.gameObject.SetActive(true);
        MenuAnim.SetBool("show", true);
    }

    IEnumerator WaitForDesactive(GameObject obj,float time)
    {
        yield return new WaitForSeconds(time);
        obj.SetActive(false);
    }
    

    void SetupScore()
    {
        if (bl_GameManager.Instance.m_GameMode == GameMode.PickUp)
        {
            ScoreUI.SetActive(false);
            ScoreText.gameObject.SetActive(false);
        }

        InstructionText.canvasRenderer.SetAlpha(0);
        if (!Speedbox.bl_Utils.IsMobile)
        {
            InstructionText.text = "Press arrow left to turn left and right to turn right";
            TapInitText.text = "CLICK TO PLAY";
            FlipText.text = "SPACE TO FLIP";
        }
        if (bl_GameManager.Instance.m_GameMode == GameMode.PickUp)
        {
            int bs = PlayerPrefs.GetInt(KeyMasters.BestScore, 0);
            int ls = PlayerPrefs.GetInt(KeyMasters.LastScore, 0);
            LevelGameOverText.text = bl_GameManager.Instance.CurrentLevel.ToString();

            BestScoreText.text = string.Format("BEST SCORE\n<size=125><b>{0}</b></size>", bs);
            LastScoreText.text = string.Format("LAST SCORE\n<size=125><b>{0}</b></size>", ls);
            GameOverScoreText.text = string.Format("SCORE\n<size=200><b>{0}</b></size>", bl_GameManager.Instance.GetCacheScore.ToString());
        }else if(bl_GameManager.Instance.m_GameMode == GameMode.Free)
        {
            int bs = PlayerPrefs.GetInt(KeyMasters.BestScoreProgresive, 0);
            int ls = PlayerPrefs.GetInt(KeyMasters.LastScoreProgresive, 0);
            LevelGameOverText.text = bl_GameManager.Instance.CurrentLevel.ToString();

            BestScoreText.text = string.Format("BEST SCORE\n<size=100><b>{0}</b></size>", bs);
            LastScoreText.text = string.Format("LAST SCORE\n<size=110><b>{0}</b></size>", ls);
            GameOverScoreText.text = string.Format("SCORE\n<size=150><b>{0}</b></size>", bl_GameManager.Instance.GetCacheScore.ToString());
        }
    }

    void OnPoint(Speedbox.bl_GlobalEvents.OnPoint e)
    {
        ScoreText.text = e.Point.ToString();
        PointScoreAnim.Play("Point", 0, 0);
    }

    void HideFirstText()
    {
        InstructionText.CrossFadeColor(new Color(0, 0, 0, 0), 1, true, true);
    }

    public void SetQuality()
    {
        quality = !quality;
        bl_GameManager.Instance.ChangeQualityGame(quality);
        QualityText.text = (quality) ? "LOW" : "GOOD";
        Speedbox.bl_Utils.PlayerPrefsX.SetBool(KeyMasters.Quality, quality);
    }

    public void SetAudioEnable()
    {
        audioEnable = !audioEnable;
        AudioListener.pause = !audioEnable;
        AudioEnableText.text = (audioEnable) ? "ENABLE" : "DISABLE";
        Speedbox.bl_Utils.PlayerPrefsX.SetBool(KeyMasters.Sound, audioEnable);
    }

    public void SetVolume(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(KeyMasters.Volume, v);
    }

    void LoadPrefs()
    {
        audioEnable = Speedbox.bl_Utils.PlayerPrefsX.GetBool(KeyMasters.Sound, true);
        AudioListener.pause = !audioEnable;
        AudioEnableText.text = (audioEnable) ? "ENABLE" : "DISABLE";

        quality = Speedbox.bl_Utils.PlayerPrefsX.GetBool(KeyMasters.Quality, false);
        QualityText.text = (quality) ? "LOW" : "GOOD";

        float v = PlayerPrefs.GetFloat(KeyMasters.Volume, 1);
        SetVolume(v);
        VolumeSlider.value = v;
    }
}