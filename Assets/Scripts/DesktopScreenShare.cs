using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

using agora_gaming_rtc;
using AgoraNative;
using System;

[Serializable]
public class CustomAgoraUser
{
    public string channel;
    public uint uid;
    public int elapsed;
}

public class DesktopScreenShare : PlayerViewControllerBase
{

    Dropdown WindowOptionDropdown;
    List<VideoSurface> videoSurfaces = new List<VideoSurface>();
    public List<CustomAgoraUser> AllUsers = new List<CustomAgoraUser>();
    VideoSurface userVideoSurface;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    readonly List<AgoraNativeBridge.RECT> WinDisplays = new List<AgoraNativeBridge.RECT>();
#else
    List<uint> MacDisplays;
#endif
    int CurrentDisplay = 0;

    protected override void OnJoinChannelSuccess(string channelName, uint uid, int elapsed)
    {
        Debug.Log("JoinChannelSuccessHandler: uid = " + uid);
        Debug.Log("JoinChannelSuccessHandler: channelName = " + channelName);
        Debug.Log("JoinChannelSuccessHandler: elapsed = " + elapsed);
        DesktopSceneController.instance.AddAgoraUser(new CustomAgoraUser { uid = uid, channel = channelName, elapsed = elapsed });

        if (!ReferenceEquals(userVideoSurface, null))
        {
            userVideoSurface.gameObject.name = uid.ToString();
        }
    }

    protected override void OnUserJoined(uint uid, int elapsed)
    {
        Debug.Log("UserJoinedHandler: uid = " + uid);
        Debug.Log("UserJoinedHandler: elapsed = " + elapsed);

        GameObject quad = DesktopSceneController.instance.InitNewSpeaker();
        DesktopSceneController.instance.AddAgoraUser(new CustomAgoraUser { uid = uid, elapsed = elapsed });

        quad.gameObject.name = uid.ToString();
        if (ReferenceEquals(quad, null))
        {
            Debug.Log("Error: failed to find DisplayPlane");
            return;
        }
        else
        {
            VideoSurface vidSurface = quad.AddComponent<VideoSurface>();
            vidSurface.SetForUser(uid);
            vidSurface.SetEnable(true);
            vidSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);
        }
    }

    protected override void OnVolumeIndication(AudioVolumeInfo[] speakers, int speakerNumber, int totalVolume)
    {
        base.OnVolumeIndication(speakers, speakerNumber, totalVolume);
        DesktopSceneController.instance.OnVolumeIndication(speakers, speakerNumber);
    }

    protected override void OnActiveSpeaker(uint uid)
    {
        base.OnActiveSpeaker(uid);

        DesktopSceneController.instance.OnVolumeIndication(uid);
    }

    protected override void SetupUI()
    {
        base.SetupUI();

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                    MacDisplays = AgoraNativeBridge.GetMacDisplayIds();
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        var winDispInfoList = AgoraNativeBridge.GetWinDisplayInfo();
        if (winDispInfoList != null)
        {
            foreach (var dpInfo in winDispInfoList)
            {
                WinDisplays.Add(dpInfo.MonitorInfo.monitor);
            }
        }
#endif

        Button button = GameObject.Find("ShareDisplayButton").GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(ShareDisplayScreen);
        }

        button = GameObject.Find("StopShareButton").GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => { mRtcEngine.StopScreenCapture(); });
        }

        GameObject quad = DesktopSceneController.instance.InitNewSpeaker();
        if (ReferenceEquals(quad, null))
        {
            Debug.Log("Error: failed to find DisplayPlane");
            return;
        }
        else
        {
            userVideoSurface = quad.AddComponent<VideoSurface>();
        }
    }

    int displayID0or1 = 0;
    void ShareDisplayScreen()
    {
        ScreenCaptureParameters sparams = new ScreenCaptureParameters
        {
            captureMouseCursor = true,
            frameRate = 15
        };

        mRtcEngine.StopScreenCapture();

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        mRtcEngine.StartScreenCaptureByDisplayId(MacDisplays[CurrentDisplay], default(Rectangle), sparams); 
        CurrentDisplay = (CurrentDisplay + 1) % MacDisplays.Count;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        ShareWinDisplayScreen(CurrentDisplay);
        CurrentDisplay = (CurrentDisplay + 1) % WinDisplays.Count;
#endif
    }

    void ShareWinDisplayScreen(int index)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        var screenRect = new Rectangle
        {
            x = WinDisplays[index].left,
            y = WinDisplays[index].top,
            width = WinDisplays[index].right - WinDisplays[index].left,
            height = WinDisplays[index].bottom - WinDisplays[index].top
        };
        Debug.Log(string.Format(">>>>> Start sharing display {0}: {1} {2} {3} {4}", index, screenRect.x,
            screenRect.y, screenRect.width, screenRect.height));
        var ret = mRtcEngine.StartScreenCaptureByScreenRect(screenRect,
            new Rectangle { x = 0, y = 0, width = 0, height = 0 }, default(ScreenCaptureParameters));
#endif
    }
}
