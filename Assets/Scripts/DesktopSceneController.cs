using agora_gaming_rtc;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DesktopSceneController : MonoBehaviour
{
    public static DesktopSceneController instance;
    public GameObject SpeakerPrefab;
    public Transform SpeakersContainer;

    public List<Speaker> AllSpeakers;
    public Speaker CurrentActiveSpeaker;
    public List<CustomAgoraUser> AllAgoraUsers;
    public int ActiveSpeakerSensitivity = 5;
    private void Awake()
    {
        AllSpeakers = new List<Speaker>();
        AllAgoraUsers = new List<CustomAgoraUser>();
        if (DesktopSceneController.instance == null)
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        var mockSpeakers = new AudioVolumeInfo[]
        {
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=1000, vad=11, volume=2},
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=1001, vad=11, volume=(uint)Random.Range(2, 55) },
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=1002, vad=11, volume=(uint)Random.Range(2, 55) },
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=1003, vad=11, volume=(uint)Random.Range(2, 55) },
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=1004, vad=11, volume=(uint)Random.Range(2, 55) },
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=1005, vad=11, volume=(uint)Random.Range(2, 55) },
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=1006, vad=11, volume=(uint)Random.Range(2, 55) },
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=1007, vad=11, volume=(uint)Random.Range(2, 55) },
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=1008, vad=11, volume=(uint)Random.Range(2, 55) },
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=1009, vad=11, volume=(uint)Random.Range(2, 55) },
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=10010, vad=11, volume=(uint)Random.Range(2, 55) },
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=10011, vad=11, volume=(uint)Random.Range(2, 55) },
            new AudioVolumeInfo{channelId="TEST_CHANNEL", uid=10012, vad=11, volume=(uint)Random.Range(2, 55) },
        };

        //OnVolumeIndication(mockSpeakers, mockSpeakers.Length);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddAgoraUser(CustomAgoraUser user)
    {
        if (user != null && AllAgoraUsers.Any(u => u.uid == user.uid) == false)
        {
            AllAgoraUsers.Add(user);
        }
    }

    public GameObject InitNewSpeaker()
    {
        var obj = Instantiate(SpeakerPrefab, SpeakersContainer);
        AllSpeakers.Add(obj.GetComponent<Speaker>());
        var speakerChild = SpeakersContainer.childCount;

        if (speakerChild > 0)
        {
            switch (speakerChild)
            {
                case 1:
                    SetSpeakerScale(1.0f);
                    break;
                case 2:
                    SetSpeakerScale(0.34f);
                    break;
                case 3:
                    SetSpeakerScale(0.17f);
                    break;
            }

        }

        return obj;
    }

    void SetSpeakerScale(float scale)
    {
        foreach (Transform speaker in SpeakersContainer.transform)
        {
            Vector3 speakerScale = speaker.transform.localScale;
            speakerScale.y = -1.0f * scale;

            speaker.transform.localScale = speakerScale;
        }
    }

    public void OnVolumeIndication(AudioVolumeInfo[] speakers, int speakerNumber)
    {
        //Debug.Log("OnVolumeIndication: speakers = " + speakerNumber);

        if (speakers != null && speakerNumber > 0)
        {
            foreach (var spk in speakers)
            {
                Debug.Log($"OnVolumeIndication: uid = {spk.uid}, volume = {spk.volume}");
            }
            AudioVolumeInfo activeSpeaker = speakers.OrderByDescending(s => s.volume).FirstOrDefault();

            //Debug.Log("OnVolumeIndication: activespeaker uid = " + activeSpeaker.uid);

            if (!Equals(activeSpeaker, null) && activeSpeaker.uid >= 0)
            {
                //Debug.Log("Active Speaker: uid = " + activeSpeaker.uid);
                CurrentActiveSpeaker = AllSpeakers.Where(u => u.UID == activeSpeaker.uid).FirstOrDefault();
                SetSpeakerIndicatorStatus(true, activeSpeaker.volume);
            }
            else
            {
                SetSpeakerIndicatorStatus();
            }
        }
    }

    public void OnVolumeIndication(uint uid)
    {
        //var activeSpeaker = AllSpeakers.Where(s => s.UID == uid).FirstOrDefault();

        //if (activeSpeaker != null)
        //{
        //    activeSpeaker.SetActiveSpeaker(true);
        //    var allInactiveUsers = AllSpeakers.Where(s => s.UID != uid);
        //    foreach (var speaker in allInactiveUsers)
        //    {
        //        speaker.SetActiveSpeaker(false);
        //    }
        //}
    }

    public void SetSpeakerIndicatorStatus(bool status = false, uint volume = 0)
    {
        if (CurrentActiveSpeaker != null)
        {
            CurrentActiveSpeaker.SetActiveSpeaker(status, volume);
        }
    }
}
