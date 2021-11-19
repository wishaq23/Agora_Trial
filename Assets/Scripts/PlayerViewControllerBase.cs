using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using agora_gaming_rtc;
using agora_utilities;

public class PlayerViewControllerBase : IVideoChatClient
{
    public event Action OnViewControllerFinish;
    protected IRtcEngine mRtcEngine;
    protected Dictionary<uint, VideoSurface> UserVideoDict = new Dictionary<uint, VideoSurface>();
    protected const string SelfVideoName = "MyView";
    protected string mChannel;

    protected bool remoteUserJoined = false;
    protected bool _enforcing360p = false;

    public PlayerViewControllerBase()
    {

    }

    public void Join(string channel)
    {
        Debug.Log("calling join (channel = " + channel + ")");

        if (mRtcEngine == null)
            return;

        mChannel = channel;

        mRtcEngine.OnJoinChannelSuccess = OnJoinChannelSuccess;
        mRtcEngine.OnUserJoined = OnUserJoined;
        mRtcEngine.OnUserOffline = OnUserOffline;
        mRtcEngine.OnVideoSizeChanged = OnVideoSizeChanged;
        mRtcEngine.OnVolumeIndication = OnVolumeIndication;
        mRtcEngine.OnActiveSpeaker = OnActiveSpeaker;
        PrepareToJoin();

        mRtcEngine.JoinChannel(channel, null, 0);

        Debug.Log("initializeEngine done");
    }

    protected virtual void OnActiveSpeaker(uint uid)
    {
        Debug.Log("OnActiveSpeaker: uid = " + uid);
    }

    protected virtual void PrepareToJoin()
    {
        mRtcEngine.EnableVideo();

        mRtcEngine.EnableVideoObserver();
        mRtcEngine.EnableAudioVolumeIndication(100, 3, false);
    }

    public virtual void Leave()
    {
        Debug.Log("calling leave");

        if (mRtcEngine == null)
            return;

        mRtcEngine.LeaveChannel();
        mRtcEngine.DisableVideoObserver();
    }

    protected bool MicMuted { get; set; }

    protected virtual void SetupUI()
    {
        GameObject go = GameObject.Find(SelfVideoName);
        if (go != null)
        {
            UserVideoDict[0] = go.AddComponent<VideoSurface>();
            go.AddComponent<UIElementDragger>();
        }

        Button button = GameObject.Find("LeaveButton").GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnLeaveButtonClicked);
        }

        Button mutton = GameObject.Find("MuteButton").GetComponent<Button>();
        if (mutton != null)
        {
            mutton.onClick.AddListener(() =>
            {
                MicMuted = !MicMuted;
                mRtcEngine.EnableLocalAudio(!MicMuted);
                mRtcEngine.MuteLocalAudioStream(MicMuted);
                Text text = mutton.GetComponentInChildren<Text>();
                text.text = MicMuted ? "Unmute" : "Mute";
            });
        }

        go = GameObject.Find("ToggleScale");
        if (go != null)
        {
            Toggle toggle = go.GetComponent<Toggle>();
            _enforcing360p = toggle.isOn;
            toggle.onValueChanged.AddListener((val) =>
            {
                _enforcing360p = val;
            });
        }
    }

    protected void OnLeaveButtonClicked()
    {
        Leave();
        UnloadEngine();

        if (OnViewControllerFinish != null)
        {
            OnViewControllerFinish();
        }
    }

    protected virtual void OnVideoSizeChanged(uint uid, int width, int height, int rotation)
    {
        Debug.LogWarningFormat("uid:{3} OnVideoSizeChanged width = {0} height = {1} for rotation:{2}", width, height, rotation, uid);

        if (UserVideoDict.ContainsKey(uid))
        {
            GameObject go = UserVideoDict[uid].gameObject;
            Vector2 v2 = new Vector2(width, height);
            RawImage image = go.GetComponent<RawImage>();
            if (_enforcing360p)
            {
                v2 = AgoraUIUtils.GetScaledDimension(width, height, 240f);
            }

            if (IsPortraitOrientation(rotation))
            {
                v2 = new Vector2(v2.y, v2.x);
            }
            image.rectTransform.sizeDelta = v2;
        }
    }

    bool IsPortraitOrientation(int rotation)
    {
        return rotation == 90 || rotation == 270;
    }

    public void LoadEngine(string appId)
    {
        mRtcEngine = IRtcEngine.GetEngine(appId);

        mRtcEngine.OnError = (code, msg) =>
        {
            Debug.LogErrorFormat("RTC Error:{0}, msg:{1}", code, IRtcEngine.GetErrorDescription(code));
        };

        mRtcEngine.OnWarning = (code, msg) =>
        {
            Debug.LogWarningFormat("RTC Warning:{0}, msg:{1}", code, IRtcEngine.GetErrorDescription(code));
        };

        mRtcEngine.SetLogFilter(LOG_FILTER.DEBUG | LOG_FILTER.INFO | LOG_FILTER.WARNING | LOG_FILTER.ERROR | LOG_FILTER.CRITICAL);
    }

    public virtual void UnloadEngine()
    {
        Debug.Log("calling unloadEngine");

        if (mRtcEngine != null)
        {
            IRtcEngine.Destroy();
            mRtcEngine = null;
        }
    }

    public void EnableVideo(bool pauseVideo)
    {
        if (mRtcEngine != null)
        {
            if (!pauseVideo)
            {
                mRtcEngine.EnableVideo();
            }
            else
            {
                mRtcEngine.DisableVideo();
            }
        }
    }

    public virtual void OnSceneLoaded()
    {
        SetupUI();
    }

    protected virtual void OnJoinChannelSuccess(string channelName, uint uid, int elapsed)
    {
        Debug.Log("JoinChannelSuccessHandler: uid = " + uid);
    }


    protected virtual void OnVolumeIndication(AudioVolumeInfo[] speakers, int speakerNumber, int totalVolume)
    {

    }

    protected virtual void OnUserJoined(uint uid, int elapsed)
    {
        Debug.Log("onUserJoined: uid = " + uid + " elapsed = " + elapsed);

        GameObject go = GameObject.Find(uid.ToString());
        if (!ReferenceEquals(go, null))
        {
            return;
        }

        VideoSurface videoSurface = makeImageSurface(uid.ToString());
        if (!ReferenceEquals(videoSurface, null))
        {
            videoSurface.SetForUser(uid);
            videoSurface.SetEnable(true);
            videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);
            videoSurface.SetGameFps(30);
            videoSurface.EnableFilpTextureApply(enableFlipHorizontal: true, enableFlipVertical: false);
            UserVideoDict[uid] = videoSurface;
            Vector2 pos = AgoraUIUtils.GetRandomPosition(100);
            videoSurface.transform.localPosition = new Vector3(pos.x, pos.y, 0);
        }
    }

    protected virtual void OnUserOffline(uint uid, USER_OFFLINE_REASON reason)
    {
        Debug.Log("onUserOffline: uid = " + uid + " reason = " + reason);
        if (UserVideoDict.ContainsKey(uid))
        {
            var surface = UserVideoDict[uid];
            surface.SetEnable(false);
            UserVideoDict.Remove(uid);
            GameObject.Destroy(surface.gameObject);
        }
    }

    protected VideoSurface makeImageSurface(string goName)
    {
        GameObject go = new GameObject();

        if (go == null)
        {
            return null;
        }

        go.name = goName;

        RawImage image = go.AddComponent<RawImage>();
        image.rectTransform.sizeDelta = new Vector2(1, 1);

        go.AddComponent<UIElementDragger>();
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            go.transform.SetParent(canvas.transform);
        }

        go.transform.Rotate(0f, 0.0f, 180.0f);
        Vector2 v2 = AgoraUIUtils.GetRandomPosition(200);
        go.transform.position = new Vector3(v2.x, v2.y, 0);
        go.transform.localScale = Vector3.one;

        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }
}
