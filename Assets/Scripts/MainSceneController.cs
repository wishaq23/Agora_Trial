using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using System.Collections;
#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
using UnityEngine.Android;
#endif
using agora_gaming_rtc;

public enum TestSceneEnum
{
    DesktopScreenShare
};


public class MainSceneController : MonoBehaviour
{

#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
    private ArrayList permissionList = new ArrayList();
#endif
    static IVideoChatClient app = null;

    private string HomeSceneName = "MainScene";

    [Header("Agora Properties")]
    [SerializeField]
    private string AppID = "your_appid";

    [Header("UI Controls")]
    [SerializeField]
    private InputField channelInputField;
    [SerializeField]
    private RawImage previewImage;
    [SerializeField]
    private Toggle roleToggle;
    [SerializeField]
    private Text appIDText;
    [SerializeField]
    private Text echoHintText;

    private bool _initialized = false;

    void Awake()
    {
#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
		permissionList.Add(Permission.Microphone);         
		permissionList.Add(Permission.Camera);               
#endif

        DontDestroyOnLoad(this.gameObject);
        channelInputField = GameObject.Find("ChannelName").GetComponent<InputField>();
    }

    void Start()
    {
        CheckAppId();
        LoadLastChannel();
        ShowVersion();
    }

    void Update()
    {
        CheckPermissions();
        CheckExit();
    }

    private void CheckAppId()
    {
        Debug.Assert(AppID.Length > 10, "Please fill in your AppId first on Game Controller object.");
        if (AppID.Length > 10)
        {
            SetAppIdText();
            _initialized = true;
        }
    }

    void SetAppIdText()
    {
        //appIDText.text = "AppID:" + AppID.Substring(0, 4) + "********" + AppID.Substring(AppID.Length - 4, 4);
    }

    private void CheckPermissions()
    {
#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
        foreach(string permission in permissionList)
        {
            if (!Permission.HasUserAuthorizedPermission(permission))
            {                 
				Permission.RequestUserPermission(permission);
			}
        }
#endif
    }


    private void LoadLastChannel()
    {
        string channel = PlayerPrefs.GetString("ChannelName");
        if (!string.IsNullOrEmpty(channel))
        {
            GameObject go = GameObject.Find("ChannelName");
            InputField field = go.GetComponent<InputField>();

            field.text = channel;
        }
    }

    private void SaveChannelName()
    {
        if (!string.IsNullOrEmpty(channelInputField.text))
        {
            PlayerPrefs.SetString("ChannelName", channelInputField.text);
            PlayerPrefs.Save();
        }
    }

    public void HandleSceneButtonClick()
    {
        string channelName = channelInputField.text;

        if (string.IsNullOrEmpty(channelName))
        {
            Debug.LogError("Channel name can not be empty!");
            return;
        }

        if (!_initialized)
        {
            Debug.LogError("AppID null or app is not initialized properly!");
            return;
        }

        app = new DesktopScreenShare();

        if (app == null) return;

        app.OnViewControllerFinish += OnViewControllerFinish;

        app.LoadEngine(AppID);

        app.Join(channelName);
        SaveChannelName();
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }

    void ShowVersion()
    {
        GameObject go = GameObject.Find("VersionText");
        if (go != null)
        {
            Text text = go.GetComponent<Text>();
            var engine = IRtcEngine.GetEngine(AppID);
            Debug.Assert(engine != null, "Failed to get engine, appid = " + AppID);
            text.text = IRtcEngine.GetSdkVersion();
        }
    }


    public void OnViewControllerFinish()
    {
        if (!ReferenceEquals(app, null))
        {
            app = null;
            SceneManager.LoadScene(HomeSceneName, LoadSceneMode.Single);
        }
        Destroy(gameObject);
    }

    public void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if (!ReferenceEquals(app, null))
        {
            app.OnSceneLoaded();
        }
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    void OnApplicationPause(bool paused)
    {
        if (!ReferenceEquals(app, null))
        {
            app.EnableVideo(paused);
        }
    }

    void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit, clean up...");

        if (!ReferenceEquals(app, null))
        {
            app.UnloadEngine();
        }
        IRtcEngine.Destroy();
    }

    void CheckExit()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // Gracefully quit on OS like Android, so OnApplicationQuit() is called
            Application.Quit();
#endif
        }
    }

    void CheckDevices(IRtcEngine engine)
    {
        VideoDeviceManager deviceManager = VideoDeviceManager.GetInstance(engine);
        deviceManager.CreateAVideoDeviceManager();

        int cnt = deviceManager.GetVideoDeviceCount();
        Debug.Log("Device count =============== " + cnt);
    }
}
