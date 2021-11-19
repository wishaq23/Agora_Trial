using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Speaker : MonoBehaviour
{
    [SerializeField] private bool isActiveSpeaker = false;
    [SerializeField] private GameObject ActiveSpeakerIndicator;
    public uint UID;
    [SerializeField] private uint VolumeThreshold;
    [SerializeField] private uint currentVolume;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ToggleSpeakerStatus());
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetActiveSpeaker(bool status = false, uint volume = 0)
    {
        currentVolume = volume;
        Debug.Log($"SetActiveSpeaker: uid = {UID}, volume = {volume}, status = {status}");
        if (status && volume >= VolumeThreshold)
        {
            isActiveSpeaker = status;
        }
        else
        {
            isActiveSpeaker = false;
        }
    }

    private IEnumerator ToggleSpeakerStatus()
    {
        while (true)
        {
            Debug.Log("Disabling indicator");
            ActiveSpeakerIndicator.SetActive(false);
            yield return new WaitWhile(() => isActiveSpeaker == false);

            Debug.Log("Enabling indicator");
            ActiveSpeakerIndicator.SetActive(true);
            yield return new WaitWhile(() => isActiveSpeaker == true);
        }
    }
}
