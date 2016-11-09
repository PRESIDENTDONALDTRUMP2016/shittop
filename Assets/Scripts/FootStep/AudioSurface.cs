using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

public class AudioSurface : ScriptableObject
{    
    public AudioMixerGroup audioMixerGroup;                 // The AudioSource that will play the clips.   
    public List<string> TextureOrMaterialNames;             // The tag on the surfaces that play these sounds.
    public List<AudioClip> audioClips;                      // The different clips that can be played on this surface.    
    public GameObject particleObject;
    private FisherYatesRandom randomSource = new FisherYatesRandom();       // For randomly reordering clips.   
    public AudioSurface()
    {
        audioClips = new List<AudioClip>();
        TextureOrMaterialNames = new List<string>();
    }

    public void PlayRandomClip(FootStepObject footStepObject)
    {
        // If there are no clips to play return.
        if (audioClips == null || audioClips.Count == 0)
            return;

        // initialize variable if not already started
        if (randomSource == null)
            randomSource = new FisherYatesRandom();
        // Find a random clip and play it.
        var audioObject = new GameObject("audioObject");
        audioObject.transform.position = footStepObject.sender.position;
        var source = audioObject.AddComponent<AudioSurfaceControl>();
        if (audioMixerGroup != null)
        {
            source.outputAudioMixerGroup = audioMixerGroup;
        }
        int index = randomSource.Next(audioClips.Count);
        if (particleObject)
        {
            var particle = Instantiate(particleObject, footStepObject.sender.position, footStepObject.sender.rotation) as GameObject;
            particle.SendMessage("StepMark", footStepObject.sender, SendMessageOptions.DontRequireReceiver);
        }
        source.PlayOneShot(audioClips[index]);

    }
}